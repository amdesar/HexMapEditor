using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

// Setup reads profile data and tests write access with the original token.
// The separate machine check reads ACLs and uses an administrator-only response.
internal static class SetupStorageTool
{
    static readonly string[] ProfileFiles = { "recent-projects.json", "settings.json" };
    const uint BackupSemantics = 0x02000000, ShareAll = 7;
    const string TrustedInstaller = "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464";
    const FileSystemRights Redirect = FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles |
        FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership | FileSystemRights.WriteAttributes |
        (FileSystemRights)0x50000000; // GENERIC_WRITE and GENERIC_ALL

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
                case "inspect": result = Inspect(args[2], args[3], args[4]); break;
                case "machine": RequireProtectedProgram(Absolute(args[2])); break;
                case "user-program": RequireWritableProgram(Absolute(args[2])); break;
                case "probe": Probe(args[2], args[3], args[4]); break;
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

    static string UserInstallation(string product)
    {
        using (var registry = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
        using (var key = registry.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + product + "_is1"))
            return key == null ? "" : (key.GetValue("InstallLocation", "") as string ?? "").TrimEnd('\\');
    }

    static string[] Inspect(string fixtureProfile, string legacyKey, string currentKey)
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
            string detected = UserInstallation(legacyKey);
            if (detected.Length != 0 && !File.Exists(Path.Combine(detected, "HexMapEditor.exe"))) detected = "";
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
            string desktop = fixtureProfile.Length == 0
                ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolderOption.DoNotVerify)
                : Path.Combine(fixtureProfile, "Desktop");
            return new[] { "OK", library, detected, legacyRoot, hasData ? "1" : "0", identity.User.Value, desktop, UserInstallation(currentKey) };
        }
    }

    static string Absolute(string path)
    {
        if (!Regex.IsMatch(path, @"^(?:[A-Za-z]:\\|\\\\[^\\?\.][^\\]*\\[^\\]+)") || path.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0)
            throw new IOException("Podaj pełną ścieżkę katalogu.");
        string full = Path.GetFullPath(path);
        return full.Length == 3 ? full : full.TrimEnd('\\');
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
    static bool Trusted(SecurityIdentifier sid)
    {
        return sid != null && (sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ||
            sid.IsWellKnown(WellKnownSidType.LocalSystemSid) || sid.Value == TrustedInstaller);
    }

    static void CheckMachineAccess(FileSystemInfo entry, FileSystemSecurity acl, FileSystemRights rights)
    {
        var raw = new RawSecurityDescriptor(acl.GetSecurityDescriptorBinaryForm(), 0);
        if (!Trusted(raw.Owner) || raw.DiscretionaryAcl == null ||
            raw.DiscretionaryAcl.Cast<GenericAce>().OfType<QualifiedAce>().Any(rule =>
                rule.AceQualifier == AceQualifier.AccessAllowed && (rule.AceFlags & AceFlags.InheritOnly) == 0 &&
                ((FileSystemRights)rule.AccessMask & rights) != 0 && !Trusted(rule.SecurityIdentifier)))
            throw new IOException("W tej lokalizacji użyj trybu „Tylko dla mnie”. Instalacja dla wszystkich wymaga chronionego katalogu: " + entry.FullName);
    }

    static DirectoryInfo ExistingDirectory(string program)
    {
        var drive = new DriveInfo(Path.GetPathRoot(program));
        if (!drive.IsReady) throw new IOException("Wybrany dysk nie jest dostępny: " + drive.Name);
        var at = new DirectoryInfo(program);
        while (!at.Exists)
        {
            if (File.Exists(at.FullName)) throw new IOException("Wybrana ścieżka koliduje z istniejącym plikiem: " + at.FullName);
            at = at.Parent;
            if (at == null) throw new IOException("Nie znaleziono dostępnego katalogu nadrzędnego.");
        }
        return at;
    }

    static void RequireProtectedProgram(string program)
    {
        var directory = ExistingDirectory(program);
        for (var at = directory; at != null; at = at.Parent)
        {
            CheckOrdinaryEntry(at.FullName);
            CheckMachineAccess(at, at.GetAccessControl(), Redirect | (at == directory ? FileSystemRights.Write : 0));
        }
        if (!directory.FullName.Equals(program, StringComparison.OrdinalIgnoreCase)) return;
        foreach (var file in directory.EnumerateFiles().Where(file =>
            new[] { "HexMapEditor.exe", "deployment.mode", "library-location.txt", "LICENSE" }.Contains(file.Name, StringComparer.OrdinalIgnoreCase) ||
            Regex.IsMatch(file.Name, @"^unins\d+\.(exe|dat|msg)$", RegexOptions.IgnoreCase)))
        {
            CheckOrdinaryEntry(file.FullName);
            CheckMachineAccess(file, file.GetAccessControl(), Redirect | FileSystemRights.Write);
        }
    }

    static void RequireWritableProgram(string program)
    {
        var directory = ExistingDirectory(program);
        // Non-mutating check, also used to select the safe scope before Setup creates any folders.
        uint access = directory.FullName.Equals(program, StringComparison.OrdinalIgnoreCase) ? 0x2u : 0x4u;
        if (!CanChangeDirectory(directory.FullName, access))
            throw new IOException("Nie można zapisywać w wybranym katalogu programu z bieżącego konta: " + program);
    }
    static void Probe(string program, string data, string library)
    {
        program = Absolute(program); data = Absolute(data); library = Absolute(library);
        EnsureSeparate(program, data, library);
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
