using System;
using System.IO;

namespace HexMapEditor
{
    internal sealed class AppStorage
    {
        internal const string ModeFile = "deployment.mode";
        internal const string InstalledMode = "HexMapEditor-installed-v2";
        internal const string PortableMode = "HexMapEditor-portable-v1";
        internal const string LibraryLocationFile = "library-location.txt";
#if PACKAGE_FIXTURE
        internal const string InstallMutex = @"Local\amdesar.HexMapEditor.InstallFixture";
#else
        internal const string InstallMutex = @"Local\amdesar.HexMapEditor.Installed";
#endif
        static readonly Lazy<AppStorage> current = new Lazy<AppStorage>(() => new AppStorage(AppDomain.CurrentDomain.BaseDirectory));

        internal static AppStorage Current { get { return current.Value; } }
        internal readonly bool Installed;
        internal readonly string DataRoot, LibraryRoot;

        internal AppStorage(string directory)
        {
            var marker = Path.Combine(directory, ModeFile);
            string mode;
            try
            {
                if (new FileInfo(marker).Length > 100)
                    throw new InvalidDataException("Nieprawidłowy plik trybu uruchomienia.");
                mode = File.ReadAllText(marker).TrimEnd('\r', '\n');
            }
            catch (FileNotFoundException) { mode = PortableMode; }
            Installed = mode == InstalledMode;
            if (!Installed && mode != PortableMode)
                throw new InvalidDataException("Nieznany tryb uruchomienia. Użyj kompletnej paczki HexMapEditor.");
            DataRoot = Path.GetFullPath(Installed ? Path.Combine(directory, "Data") : directory);
            LibraryRoot = Installed ? ReadLibraryLocation(directory) : Path.Combine(DataRoot, "Library");
        }

        static string ReadLibraryLocation(string directory)
        {
            var location = Path.Combine(directory, LibraryLocationFile);
            if (!File.Exists(location) || new FileInfo(location).Length > 4096)
                throw new InvalidDataException("Brakuje poprawnej lokalizacji biblioteki. Uruchom ponownie Setup.");
            string path = File.ReadAllText(location).TrimEnd('\r', '\n');
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                !System.Text.RegularExpressions.Regex.IsMatch(path, @"^(?:[A-Za-z]:\\|\\\\[^\\]+\\[^\\]+)"))
                throw new InvalidDataException("Lokalizacja biblioteki w konfiguracji jest nieprawidłowa. Uruchom ponownie Setup.");
            return Path.GetFullPath(path);
        }
        internal string RecentProjectsPath { get { return Path.Combine(DataRoot, "recent-projects.json"); } }
        internal string MapRoot { get { return Path.Combine(Installed ? DataRoot : LibraryRoot, "Hexmaps"); } }
    }
}
