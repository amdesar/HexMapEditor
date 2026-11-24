using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HexMapEditor.Tests
{
    internal static class Patch0121Tests
    {
        static void Check(bool condition,string error) { if(!condition)throw new InvalidOperationException(error); }
        static void Legacy(string oldData,string mapPath)
        {
            var preset=PresetCatalog.Default();
            preset.Id="legacy-0121-fixture";preset.Name="Próbny preset z 0.12.0";
            PresetCatalog.Write(preset,Path.Combine(oldData,"Library","Terrains","Presets"),false);
            var project=MainWindow.CreateProjectData(7,5,"PointyTop","Mapa poza instalacją",preset);
            MainWindow.WriteProjectData(project,mapPath,false);
            var preserved=Directory.CreateDirectory(Path.Combine(oldData,"Library","Hexmaps")).FullName;
            MainWindow.WriteProjectData(project,Path.Combine(preserved,"Mapa w starej Library.hexmap"),false);
            File.WriteAllText(Path.Combine(oldData,"recent-projects.json"),MapLogic.Serializer().Serialize(new[]{mapPath}),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(oldData,"settings.json"),"{\"fixture\":\"0.12.0\"}");
        }
        static void Paths()
        {
            string baseDir=AppDomain.CurrentDomain.BaseDirectory;
            var portable=new AppStorage(baseDir);
            Check(!portable.Installed&&portable.LibraryRoot==Path.Combine(baseDir,"Library"),"Portable root");
            var local=Directory.CreateDirectory(Path.Combine(baseDir,"TestInstalled")).FullName;
            File.WriteAllText(Path.Combine(local,AppStorage.ModeFile),AppStorage.InstalledMode);
            try
            {
                new AppStorage(local);
                throw new InvalidOperationException("Installed EXE switched to default after missing configuration.");
            }
            catch(InvalidDataException) { }
            var alternative=Directory.CreateDirectory(Path.Combine(baseDir,"Zasoby Ę")).FullName;
            File.WriteAllText(Path.Combine(local,AppStorage.LibraryLocationFile),alternative);
            var installed=new AppStorage(local);
            Check(installed.Installed&&installed.LibraryRoot==alternative&&installed.DataRoot==Path.Combine(local,"Data")&&
                installed.MapRoot==Path.Combine(local,"Data","Hexmaps")&&installed.RecentProjectsPath==Path.Combine(local,"Data","recent-projects.json"),"Installed roots");
        }
        static void Installed(string mapPath)
        {
            Check(AppStorage.Current.Installed,"Wrong deployment mode");
            string warning;
            var presets=PresetCatalog.Load(LibraryPaths.Presets,out warning);
            Check(warning.Length==0&&presets.Any(p=>p.Id=="legacy-0121-fixture"),"Failed to reload migrated preset");
            Check(RecentProjects.Shared.Entries.Contains(mapPath,StringComparer.OrdinalIgnoreCase),"MRU did not retain the map path");
            var open=MainWindow.ReadProjectData(mapPath);
            Check(open.FormatVersion==12&&open.Presets.Any(p=>p.Id=="legacy-0121-fixture")&&open.LastSavedAppVersion=="0.12.0","Map12 loaded or original save untouched");
            Check(!Directory.Exists(Path.Combine(LibraryPaths.Root,"Hexmaps")),"Maps migrated into chosen Library");
            var own=new TerrainPreset{Name="Preset zapisany po upgrade"};
            PresetCatalog.Write(own,LibraryPaths.Presets,false);
            Check(PresetCatalog.Load(LibraryPaths.Presets,out warning).Any(p=>p.Id==own.Id),"New preset could not round-trip");
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                switch(args[0])
                {
                    case "paths": Paths(); break;
                    case "legacy": Legacy(args[1],args[2]); break;
                    case "installed": Installed(args[1]); break;
                    default: throw new ArgumentException("Wymagany jawny przypadek próby.");
                }
                Console.WriteLine(args[0]+" PASS");
                return 0;
            }
            catch(Exception error) { Console.Error.WriteLine(error); return 1; }
        }
    }
}
