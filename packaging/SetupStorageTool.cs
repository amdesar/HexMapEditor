using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

// Only Setup runs this tool, using Inno's pre-elevation credentials.
internal static class SetupStorageTool
{
    static readonly string[] ProfileFiles = { "recent-projects.json", "settings.json" };
    const uint Delete = 0x10000, DeleteChild = 0x40, BackupSemantics = 0x02000000, ShareAll = 7;

    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint mode, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern uint GetFinalPathNameByHandle(SafeFileHandle handle, StringBuilder path, uint size, uint flags);

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length < 2) return 2;
        try
        {
            string[] result = new[] { "OK" };
            switch (args[1])
            {
                case "inspect": result = Inspect(args[2], args[3]); break;
                case "program": RequireProtectedProgram(Absolute(args[2])); break;
                case "probe": Probe(args[2], args[3], args[4], args[5] == "protected"); break;
                case "copy": Copy(args[2], args[3], args[4]); break;
                default: throw new InvalidOperationException("Nieznana operacja konfiguracji danych.");
            }
            Respond(args[0], result);
            return 0;
        }
        catch (Exception error)
        {
            try { Respond(args[0], new[] { "ERROR", error.Message.Replace('\r',' ').Replace('\n',' ') }); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return 1;
        }
    }

    static void Respond(string response, string[] lines)
    {
        // Open only the communication file pre-created by Setup.
        using (var file = new FileStream(response, FileMode.Truncate, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(file, new UTF8Encoding(true)))
            foreach (var line in lines) writer.WriteLine(line);
    }

    static string[] Inspect(string fixtureProfile, string legacyKey)
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                throw new IOException("Uruchom instalator normalnie, bez „Uruchom jako administrator”, i dopiero potwierdź monit UAC. Potrzebne jest konto użytkownika sprzed UAC.");
            string userLocal = fixtureProfile.Length == 0 ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) : fixtureProfile;
            string documents = fixtureProfile.Length == 0
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify)
                : Path.Combine(fixtureProfile, "Przekierowane Dokumenty");
            string legacyRoot = Path.Combine(userLocal, "amdesar", "HexMapEditor");
            string detected = "";
            using (var registry = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
            using (var key = registry.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + legacyKey + "_is1"))
            {
                if (key != null)
                {
                    string folder = key.GetValue("InstallLocation", "") as string;
                    if (!String.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, "HexMapEditor.exe"))) detected = folder;
                }
            }
            if (detected.Length == 0)
            {
                string standard = Path.Combine(userLocal, "Programs", "HexMapEditor");
                if (File.Exists(Path.Combine(standard, "HexMapEditor.exe")) &&
                    File.Exists(Path.Combine(standard, "deployment.mode")) &&
                    File.ReadAllText(Path.Combine(standard, "deployment.mode")).Trim() == "HexMapEditor-installed-v1")
                    detected = standard;
            }
            bool hasData = Directory.Exists(Path.Combine(legacyRoot, "Library")) || ProfileFiles.Any(name => File.Exists(Path.Combine(legacyRoot, name)));
            string library = String.IsNullOrWhiteSpace(documents) ? "" : Path.Combine(documents, "HexMapEditor", "Library");
            return new[] { "OK", library, detected, legacyRoot, hasData ? "1" : "0", identity.User.Value };
        }
    }

    static string Absolute(string path)
    {
        if (!Regex.IsMatch(path, @"^(?:[A-Za-z]:\\|\\\\[^\\?\.][^\\]*\\[^\\]+)") || path.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0)
            throw new IOException("Podaj pełną ścieżkę katalogu.");
        return Path.GetFullPath(path).TrimEnd('\\');
    }
    static bool ContainsPath(string parent, string child)
    {
        return child.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
            child.StartsWith(parent + "\\", StringComparison.OrdinalIgnoreCase);
    }
    static void EnsureSeparate(string program, string data, string library)
    {
        string adjacent = Path.Combine(program, "Library");
        if (ContainsPath(library, program) || ContainsPath(library, data) || ContainsPath(data, library) ||
            (ContainsPath(program, library) && !library.Equals(adjacent, StringComparison.OrdinalIgnoreCase)))
            throw new IOException("Wybierz odrębny katalog Library. Dozwolony podkatalog programu to tylko Library, obok katalogu Data.");
    }
    static string FinalDirectory(string path)
    {
        using (var handle = CreateFile(path, 0, ShareAll, IntPtr.Zero, 3, BackupSemantics, IntPtr.Zero))
        {
            var name = new StringBuilder(32768);
            uint n = handle.IsInvalid ? 0 : GetFinalPathNameByHandle(handle, name, (uint)name.Capacity, 0);
            if (n == 0 || n >= name.Capacity) throw new Win32Exception(Marshal.GetLastWin32Error(), "Nie można ustalić rzeczywistej lokalizacji katalogu " + path + ".");
            string result = name.ToString();
            if (result.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) return @"\\" + result.Substring(8).TrimEnd('\\');
            if (result.StartsWith(@"\\?\", StringComparison.Ordinal)) return result.Substring(4).TrimEnd('\\');
            return result.TrimEnd('\\');
        }
    }
    static bool CanChangeDirectory(string path, uint access)
    {
        using (var handle = CreateFile(path, access, ShareAll, IntPtr.Zero, 3, BackupSemantics, IntPtr.Zero))
            return !handle.IsInvalid;
    }
    static void RequireProtectedProgram(string program)
    {
        // An otherwise protected child is still replaceable if its parent can delete it.
        for (var at = new DirectoryInfo(program); at.Parent != null; at = at.Parent)
            if (CanChangeDirectory(at.FullName, Delete) || CanChangeDirectory(at.FullName, 0x40000) ||
                CanChangeDirectory(at.Parent.FullName, DeleteChild))
                throw new IOException("Katalog programu musi być chroniony przed modyfikacją przez zwykłego użytkownika. Wybierz Program Files lub inny chroniony katalog.");
        if (CanChangeDirectory(program, 0x2))
            throw new IOException("Bieżący użytkownik ma prawo zapisu plików w katalogu programu. Wybierz chroniony katalog programu.");
        foreach (string name in new[] { "HexMapEditor.exe", "deployment.mode", "library-location.txt" })
        {
            string file = Path.Combine(program, name);
            if (File.Exists(file) && (CanChangeDirectory(file, 0x2) || CanChangeDirectory(file, Delete)))
                throw new IOException("Istniejący plik programu pozwala na modyfikację przez zwykłego użytkownika.");
        }
    }
    static void Probe(string program, string data, string library, bool lockedProgram)
    {
        program = Absolute(program); data = Absolute(data); library = Absolute(library);
        EnsureSeparate(program, data, library);
        if (lockedProgram) RequireProtectedProgram(program);
        Directory.CreateDirectory(library);
        string actualProgram = FinalDirectory(program);
        string actualData = FinalDirectory(data);
        string actualLibrary = FinalDirectory(library);
        EnsureSeparate(actualProgram, actualData, actualLibrary);
        if (!actualData.Equals(Path.Combine(actualProgram,"Data"),StringComparison.OrdinalIgnoreCase))
            throw new IOException("Katalog Data programu przekierowuje do innego miejsca.");
        foreach (string directory in new[] { library, data })
        {
            try
            {
                string path = Path.Combine(directory, ".hexmap-setup-probe-" + Guid.NewGuid().ToString("N"));
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.DeleteOnClose))
                    file.WriteByte(1);
            }
            catch (IOException error) { throw WriteError(error, directory); }
            catch (UnauthorizedAccessException error) { throw WriteError(error, directory); }
        }
    }
    static IOException WriteError(Exception error, string directory)
    {
        return new IOException("Nie można zapisywać w „" + directory + "” na koncie użytkownika. " + error.Message, error);
    }

    static void CheckCopy(string source, string data, string library)
    {
        var oldLibrary = Path.Combine(source,"Library");
        if (Directory.Exists(oldLibrary) && Directory.EnumerateFileSystemEntries(library).Any())
            throw new IOException("Docelowa Library zawiera już dane. Wybierz pustą lokalizację albo wyłącz kopiowanie.");
        foreach (string name in ProfileFiles)
            if (File.Exists(Path.Combine(source, name)) &&
                (File.Exists(Path.Combine(data, name)) || Directory.Exists(Path.Combine(data, name))))
                throw new IOException("Katalog Data zawiera już „" + name + "”. Wyłącz kopiowanie albo wcześniej zabezpiecz plik samodzielnie.");
    }
    static void CheckOrdinaryEntry(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Kopiowanie przerwano przy przekierowaniu w danych źródłowych: " + path);
    }
    static void CopyLibrary(string source, string stage, bool root)
    {
        CheckOrdinaryEntry(source);
        foreach (string directory in Directory.EnumerateDirectories(source))
        {
            string leaf = Path.GetFileName(directory);
            if (root && leaf.Equals("Hexmaps",StringComparison.OrdinalIgnoreCase)) continue;
            CheckOrdinaryEntry(directory);
            string dest = Directory.CreateDirectory(Path.Combine(stage,leaf)).FullName;
            CopyLibrary(directory,dest,false);
        }
        foreach (string file in Directory.EnumerateFiles(source))
        {
            if (Path.GetExtension(file).Equals(".hexmap",StringComparison.OrdinalIgnoreCase)) continue;
            CheckOrdinaryEntry(file);
            File.Copy(file, Path.Combine(stage,Path.GetFileName(file)),false);
        }
    }
    static void Copy(string source, string data, string library)
    {
        CheckCopy(source,data,library);
        if (Directory.Exists(source)) CheckOrdinaryEntry(source);
        var work = ".hexmap-copy-" + Guid.NewGuid().ToString("N");
        string stageLibrary = Path.Combine(library,work), stageData = Path.Combine(data,work);
        var moved = new List<Tuple<string,string,bool>>();
        bool retainStages = false;
        try
        {
            string oldLibrary = Path.Combine(source,"Library");
            if (Directory.Exists(oldLibrary))
            {
                Directory.CreateDirectory(stageLibrary);
                CopyLibrary(oldLibrary,stageLibrary,true);
                if (Directory.EnumerateFileSystemEntries(library).Any(p=>!String.Equals(p,stageLibrary,StringComparison.OrdinalIgnoreCase)))
                    throw new IOException("W docelowej Library pojawiły się dane podczas kopiowania.");
            }
            Directory.CreateDirectory(stageData);
            foreach (string name in ProfileFiles)
            {
                string path = Path.Combine(source,name);
                if (!File.Exists(path)) continue;
                CheckOrdinaryEntry(path);
                File.Copy(path,Path.Combine(stageData,name),false);
            }
            Publish(stageLibrary,library,moved);
            Publish(stageData,data,moved);
        }
        catch
        {
            foreach (var entry in moved.AsEnumerable().Reverse())
                try { Move(entry.Item2,entry.Item1,entry.Item3); }
                catch (IOException) { retainStages = true; }
                catch (UnauthorizedAccessException) { retainStages = true; }
            if (retainStages) throw new IOException("Kopiowanie przerwano. Oryginał pozostał na miejscu. Część nowych plików może znajdować się w katalogu docelowym lub roboczym " + work + ".");
            throw;
        }
        finally
        {
            if (!retainStages)
            {
                RemoveStage(stageLibrary);
                RemoveStage(stageData);
            }
        }
    }
    static void Publish(string stage,string target,List<Tuple<string,string,bool>> moved)
    {
        if (!Directory.Exists(stage)) return;
        foreach (string entry in Directory.GetFileSystemEntries(stage))
        {
            string destination = Path.Combine(target,Path.GetFileName(entry));
            bool directory = Directory.Exists(entry);
            Move(entry,destination,directory);
            moved.Add(Tuple.Create(entry,destination,directory));
        }
    }
    static void Move(string source,string target,bool directory)
    {
        if (directory) Directory.Move(source,target);
        else File.Move(source,target);
    }
    static void RemoveStage(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path,true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
