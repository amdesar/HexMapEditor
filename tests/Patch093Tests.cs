using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch093Tests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
        static object Call(MainWindow w, string name, params object[] args)
        {
            return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(w, args);
        }
        static T Field<T>(MainWindow w, string name) { return (T)typeof(MainWindow).GetField(name, Private).GetValue(w); }
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static TerrainPreset CreatePreset(string name)
        {
            var p = new TerrainPreset { Name = name };
            PresetCatalog.Write(p, LibraryPaths.Presets, false);
            return p;
        }
        static string Source(string name, Color color)
        {
            var path = Path.Combine(Root, name + ".png");
            using (var bmp = new Bitmap(80, 60))
            {
                using (var g = Graphics.FromImage(bmp))
                using (var brush = new SolidBrush(color)) g.FillEllipse(brush, 10, 5, 60, 50);
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            return path;
        }
        static IconAsset Crop(string path)
        {
            using (var dialog = new SquareImportDialog(path))
            {
                dialog.Show();
                ((Button)dialog.AcceptButton).PerformClick();
                Check(dialog.Result != null, "Import crop missing");
                return dialog.Result;
            }
        }
        static void Add(MainWindow w, object entry, bool objects)
        {
            object[] args = { null, entry, false, Path.Combine(LibraryPaths.Root, objects ? "Objects" : "Icons"), null };
            Check((bool)Call(w, objects ? "TryChangeObjectTemplate" : "TryChangeLibraryIcon", args), "Import failed: " + args[4]);
        }
        static void Save(MainWindow w, TerrainPreset p, PresetSections sections)
        {
            object[] args = { LibraryPaths.Presets, false, p.Name, sections, null };
            Check((bool)Call(w, "TrySaveWorkingPreset", args), "Preset write failed: " + args[4]);
        }
        static void State(MainWindow w, bool expected, string label)
        {
            var p = (TerrainPreset)Call(w, "ActivePreset");
            Check((bool)Call(w, "WorkingPresetDirty", p) == expected, label + ": dirty");
            Check((Field<Button>(w, "savePresetButton").BackColor == ChoiceStyle.ActiveBack) == expected, label + ": button");
            Check(Field<Label>(w, "presetStateLabel").Text.Contains("*") == expected, label + ": status");
            int prompts = 0;
            w.ConfirmPresetForTest = draft => { prompts++; return DialogResult.Cancel; };
            Check((bool)Call(w, "ConfirmPresetsBeforeClose") != expected && prompts == (expected ? 1 : 0), label + ": close");
        }
        static void Run()
        {
            var p = CreatePreset("Preset 093");
            var other = CreatePreset("Czysty 093");
            var map = MainWindow.CreateProjectData(8, 7, "PointyTop", "Próba 093", p);
            using (var w = new MainWindow(map, null))
            {
                State(w, false, "Initial");
                var icon = new LibraryIcon { Name = "Ikona 093", Asset = Crop(Source("Ikona 093", Color.Cyan)) };
                var obj = new ObjectTemplate { Name = "Obiekt 093", Asset = Crop(Source("Obiekt 093", Color.Orange)) };
                Add(w, obj, true);
                State(w, true, "Object alone until undo");
                Call(w, "Undo");
                State(w, false, "Undo object");
                Add(w, icon, false);
                Check(((QuickSnapshot)Call(w, "CaptureQuick")).Icons.Contains(icon.Id), "File icon absent from quick");
                State(w, true, "After icon import");
                Add(w, obj, true);
                Check(((QuickSnapshot)Call(w, "CaptureQuick")).Objects.Contains(obj.Id), "File object absent from quick");
                State(w, true, "After both imports");
                Check(!Field<bool>(w, "dirty"), "Import dirtied map");
                Save(w, p, PresetSections.Icons);
                var saved = PresetCatalog.ReadFile(PresetCatalog.FindFile(LibraryPaths.Presets, p.Id));
                Check(saved.Icons.Single().Id == icon.Id && saved.Objects.Count == 0, "Partial disk write");
                State(w, true, "After partial save");
                Save(w, p, PresetSections.Objects);
                saved = PresetCatalog.ReadFile(PresetCatalog.FindFile(LibraryPaths.Presets, p.Id));
                Check(saved.Icons.Single().Id == icon.Id && saved.Objects.Single().Id == obj.Id, "Section round trip");
                State(w, false, "After full save");
                VerifyExceptions(w, p, other, icon, obj);
            }
            Console.WriteLine("Preset PASS");
        }
        static void VerifyExceptions(MainWindow w, TerrainPreset p, TerrainPreset other, LibraryIcon icon, ObjectTemplate obj)
        {
            var standalone = new LibraryIcon { Name = "Tylko biblioteka", Asset = icon.Asset };
            var directory = Path.Combine(LibraryPaths.Root, "Icons");
            object[] libraryOnly = { null, standalone, false, directory, false, null };
            Check((bool)Call(w, "TryChangeLibraryIcon", libraryOnly), "Library-only write");
            Check(!((QuickSnapshot)Call(w, "CaptureQuick")).Icons.Contains(standalone.Id), "Library-only promoted");
            State(w, false, "Library-only write");
            var target = Field<List<LibraryIcon>>(w, "libraryIcons").Single(i => i.Id == icon.Id);
            var presetFile = PresetCatalog.FindFile(LibraryPaths.Presets, p.Id);
            var persisted = File.ReadAllBytes(presetFile);
            bool mapDirty = Field<bool>(w, "dirty");
            object[] remove = { target, null, false, directory, null };
            Check((bool)Call(w, "TryChangeLibraryIcon", remove), "Automatic cleanup");
            Check(!((QuickSnapshot)Call(w, "CaptureQuick")).Icons.Contains(icon.Id)
                && persisted.SequenceEqual(File.ReadAllBytes(presetFile))
                && Field<bool>(w, "dirty") == mapDirty, "Cleanup affected preset file/map");
            State(w, false, "Automatic cleanup");
            Call(w, "Undo");
            State(w, false, "Undo cleanup");
            object[] convert = { obj.Graphic(), false, null };
            Check((bool)Call(w, "TryImportCounterpart", convert), "Cross-library import");
            State(w, true, "Cross-library import");
            Call(w, "SelectPreset", other.Id);
            Check(Field<Button>(w, "savePresetButton").BackColor == ChoiceStyle.NeutralBack, "Other draft highlighted current");
            Call(w, "SelectPreset", p.Id);
            State(w, true, "Return to draft");
            object[] restore = { LibraryPaths.Presets, PresetSections.Icons, null };
            Check((bool)Call(w, "TryRestorePreset", restore), "Restore section");
            State(w, false, "Restored icons");
        }
        static void Fixture()
        {
            var p = CreatePreset("Preset 093");
            var path = Path.Combine(Root, "Próba 093.hexmap");
            MainWindow.WriteProjectData(MainWindow.CreateProjectData(8, 7, "PointyTop", "Próba 093", p), path, false);
            RecentProjects.Shared.Touch(path);
            Source("Ikona 093", Color.Cyan);
        }
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                LibraryPaths.EnsureDirectories(Root);
                if (args.Single() == "fixture") Fixture(); else if (args.Single() == "preset") Run(); else throw new ArgumentException("Unknown case");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
