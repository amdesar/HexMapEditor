using System;
using System.IO;

namespace HexMapEditor
{
    internal sealed class AppStorage
    {
        internal const string ModeFile = "deployment.mode";
        internal const string InstalledMode = "HexMapEditor-installed-v1";
        internal const string PortableMode = "HexMapEditor-portable-v1";
#if PACKAGE_FIXTURE
        internal const string InstallMutex = @"Local\amdesar.HexMapEditor.InstallFixture";
#else
        internal const string InstallMutex = @"Local\amdesar.HexMapEditor.Installed";
#endif
        static readonly Lazy<AppStorage> current = new Lazy<AppStorage>(() => {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
#if PACKAGE_FIXTURE
            string profile = Path.Combine(directory, "..", "FixtureProfile");
#else
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
            return new AppStorage(directory, profile);
        });

        internal static AppStorage Current { get { return current.Value; } }
        internal readonly bool Installed;
        internal readonly string DataRoot;

        internal AppStorage(string directory, string localProfile)
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
            if (Installed && String.IsNullOrWhiteSpace(localProfile))
                throw new IOException("Windows nie udostępnił lokalnego katalogu danych bieżącego użytkownika.");
            DataRoot = Path.GetFullPath(Installed ? Path.Combine(localProfile, "amdesar", "HexMapEditor") : directory);
        }

        internal string RecentProjectsPath { get { return Path.Combine(DataRoot, "recent-projects.json"); } }
    }
}
