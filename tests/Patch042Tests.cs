using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch042Tests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static string root;
        static object Get(MainWindow w, string name) { return typeof(MainWindow).GetField(name, Private).GetValue(w); }
        static object Call(MainWindow w, string name, params object[] args)
        {
            return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(w, args);
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static MapData Map(MainWindow w) { return (MapData)Get(w, "map"); }
        static void Undo(MainWindow w, bool redo = false)
        {
            object[] args = { redo, null };
            Require((bool)Call(w, "TryHistory", args), "Historia: " + args[1]);
        }
        static MapObject AddObject(MapData map, string name, PointF center)
        {
            if (map.IconAssets.Count == 0) map.IconAssets.Add(IconAssets.Starters().First());
            var obj = new MapObject { Name = name, X = center.X, Y = center.Y, TemplateId = "fixture", AssetId = map.IconAssets[0].Id };
            map.Objects.Add(obj);
            return obj;
        }
        static void Save(MainWindow w, string path)
        {
            object[] args = { path, false, null };
            Require((bool)Call(w, "TrySaveTo", args), "Zapis: " + args[2]);
        }
        static void Load(MainWindow w, string path)
        {
            object[] args = { path, null, false };
            Require((bool)Call(w, "TryLoadProject", args), "Odczyt: " + args[1]);
        }
        static void States()
        {
            string directory = Path.Combine(root, "states");
            using (var w = new MainWindow())
            {
                var map = Map(w);
                var builtin = map.ObjectStates.Single(s => s.Id == "Ally").Copy();
                builtin.Name = "Zwiadowca";
                builtin.CanReveal = false;
                Require((bool)Call(w, "ChangeStateFile", builtin, builtin.Id, false, directory), "Zapis wzorca");
                string file = Directory.GetFiles(directory).Single();
                Require(Path.GetFileName(file) == "state-Zwiadowca.json" && !((bool)Get(w, "dirty")), "Nazwa lub fałszywa gwiazdka");
                Require(map.ObjectStates.Single(s => s.Id == "Ally").CanReveal, "Sam zapis zmienił mapę");
                string warning;
                var reload = StateLibrary.Load(directory, out warning);
                Require(warning == "" && reload.Single(s => s.Definition.Id == "Ally").Definition.Name == builtin.Name, "Restart biblioteki");
                Call(w, "StartNewProject", 8, 5, "PointyTop", "Próba", PresetCatalog.Default());
                map = Map(w);
                Require(map.ObjectStates.Single(s => s.Id == "Ally").Name == builtin.Name, "Nowa mapa pomija bibliotekę");
                try { Call(w, "ChangeStateFile", null, "Ally", false, directory); throw new Exception("Usunięto stan podstawowy"); }
                catch (TargetInvocationException ex) { Require(ex.InnerException is InvalidOperationException, "Inny błąd usuwania"); }
                var baseState = new ObjectStateDefinition { Name = "Baza", Color = "#778899" };
                var covering = new ObjectStateDefinition { Name = "Nakładka", Color = "#006600", CanReveal = true };
                var unused = new ObjectStateDefinition { Name = "Poza plikiem", Color = "#112233" };
                foreach (var state in new[] { baseState, covering, unused }) Call(w, "ChangeStateFile", state, state.Id, true, directory);
                var a = AddObject(map, "A", BrushGeometry.Center(map, 2, 2));
                a.Side = baseState.Id;
                a.RevealsFog = true;
                a.RevealRadius = 0;
                int index = 2 * map.Columns + 2;
                map.Cells[index].Fog = true;
                string baseFile = Directory.GetFiles(directory, "state-Baza.json").Single();
                byte[] previous = File.ReadAllBytes(baseFile);
                var active = baseState.Copy();
                active.CanReveal = true;
                Call(w, "ChangeStateFile", active, active.Id, true, directory);
                Require(!map.Cells[index].Fog && a.RevealsFog, "Aktualizacja uprawnienia i mgły");
                Undo(w);
                Require(map.Cells[index].Fog && File.ReadAllBytes(baseFile).SequenceEqual(previous)
                    && !map.ObjectStates.Single(s => s.Id == active.Id).CanReveal, "Wspólne Undo pliku, stanu i mgły");
                Undo(w, true);
                Call(w, "ChangeStateFile", null, active.Id, false, directory);
                Require(!File.Exists(baseFile) && map.ObjectStates.Any(s => s.Id == active.Id), "Usunięcie biblioteki naruszyło mapę");
                Undo(w);
                Require(File.Exists(baseFile), "Undo usunięcia nie odtworzyło pliku");
                map.Groups.Add(new ObjectGroup { Members = new List<string> { a.Id }, StateId = covering.Id, FrameEnabled = true });
                string portable = Path.Combine(root, "portable.hexmap");
                Save(w, portable);
                var saved = MapLogic.Serializer().Deserialize<MapData>(File.ReadAllText(portable));
                Require(saved.FormatVersion == 6 && saved.ObjectStates.Count == 5 && saved.ObjectStates.Any(s => s.Id == baseState.Id)
                    && !saved.ObjectStates.Any(s => s.Id == unused.Id) && saved.Groups[0].FrameEnabled && saved.IconAssets.Count == 1
                    && map.ObjectStates.Any(s => s.Id == unused.Id), "Filtrowanie przenośnego zapisu");
                using (var reader = new MainWindow())
                {
                    Load(reader, portable);
                    Require(Map(reader).ObjectStates.Single(s => s.Id == "Ally").Name == builtin.Name, "Lokalny stan zastąpił mapę");
                    Require(Map(reader).Groups[0].FrameEnabled, "Utracona ramka");
                    string v5 = Path.Combine(root, "compatible-5.hexmap");
                    var raw = (Dictionary<string, object>)MapLogic.Serializer().DeserializeObject(File.ReadAllText(portable));
                    raw["FormatVersion"] = 5;
                    foreach (Dictionary<string, object> group in (object[])raw["Groups"])
                    {
                        group.Remove("FrameEnabled");
                        group.Remove("FrameColor");
                    }
                    File.WriteAllText(v5, MapLogic.Serializer().Serialize(raw));
                    Load(reader, v5);
                    Require(!Map(reader).Groups[0].FrameEnabled, "Domyślna ramka formatu 5");
                }
            }
        }

        static void GroupsAndFog()
        {
            using (var w = new MainWindow())
            {
                var map = Map(w);
                var a = AddObject(map, "Alfa", new PointF(0, 0));
                var b = AddObject(map, "Beta", new PointF(60, 0));
                var c = AddObject(map, "Gamma", new PointF(120, 0));
                var g1 = new ObjectGroup { Name = "Pierwsza", Members = new List<string> { a.Id }, FrameEnabled = true };
                var g2 = new ObjectGroup { Name = "Druga", Members = new List<string> { b.Id }, SizeMode = "Offset", SizeValue = 12 };
                map.Groups.AddRange(new[] { g1, g2 });
                Call(w, "ChooseObjectMember", b, false, false);
                Call(w, "ChooseObjectMember", a, true, false);
                Require(((List<ObjectGroup>)Call(w, "SelectedWholeGroups")).First().Id == g2.Id, "Kolejność Ctrl");
                Require((bool)Call(w, "JoinSelectedGroups", g2.Id, null), "Połączenie");
                Require(map.Groups.Single().Id == g2.Id && map.Groups[0].Members.Count == 2 && map.Groups[0].SizeValue == 12 && b.Size == 30, "Merge nie zachował nakładki lub bazy");
                Undo(w);
                Require(Get(w, "selectedGroupId") == null, "Panel wskazuje podgrupę mimo szerszego zaznaczenia");
                Call(w, "ChooseObjectMember", a, false, false);
                Call(w, "ChooseObjectMember", c, true, false);
                Require((bool)Call(w, "JoinSelectedGroups", g1.Id, null)
                    && map.Groups.Single(g => g.Id == g1.Id).FrameEnabled, "Dołączenie utraciło ramkę");
                Undo(w);
                var selected = (HashSet<string>)Get(w, "selectedObjects");
                selected.UnionWith(map.Objects.Select(o => o.Id));
                Require((bool)Call(w, "JoinSelectedGroups", null, "Od nowa"), "Świeże ustawienia");
                Require(map.Groups.Single().Members.Count == 3 && !map.Groups[0].FrameEnabled && map.Groups[0].SizeMode == "Individual", "Świeża grupa");
                Undo(w);
                map.Orientation = "FlatTop";
                map.Cells[0].Fog = true;
                foreach (var pair in new[] { new[] { 0d, 0d }, new[] { 13d, .25 }, new[] { 14d, .5 }, new[] { 15d, .75 }, new[] { 30d, 1d } })
                {
                    // Cross the horizontal edge of a flat-top hex.
                    double visible = FogVisibility.VisibleFraction(map, new RectangleF(-2, (float)(14 * Math.Sqrt(3) + (pair[0] - 14) - 2), 4, 4));
                    Require(Math.Abs(visible - pair[1]) < 1e-6, "Ułamek widoczności: " + visible);
                }
                Require(FogVisibility.MasksDetails(.5, false) && FogVisibility.MasksDetails(.499999, false) &&
                    !FogVisibility.MasksDetails(.500001, false) && !FogVisibility.MasksDetails(0, true), "Niezaokrąglony próg");
                var square = IconAssets.Starters().First(x => x.Width == x.Height);
                if (!map.IconAssets.Any(x => x.Id == square.Id)) map.IconAssets.Add(square);
                a.AssetId = square.Id; a.Size = 8; a.X = 0; a.Y = 23;
                var below = (ListViewItem)Call(w, "ObjectListItem", a);
                a.Y = 26;
                var above = (ListViewItem)Call(w, "ObjectListItem", a);
                Require(below.Text == "???" && above.Text == a.Name && above.SubItems[4].Text.Contains("% ukrycia"), "Maskowanie listy");
            }
        }

        static void Controls()
        {
            using (var w = new MainWindow())
            {
                var map = Map(w);
                var item = AddObject(map, "Do usunięcia", new PointF(100, 100));
                Call(w, "ChooseObjectMember", item, false, true);
                Call(w, "CaptureSavedState");
                int prompts = 0;
                Func<string, bool> cancel = text => { prompts++; Require(text.Contains(item.Name), "Brak nazwy w komunikacie"); return false; };
                Require(!(bool)Call(w, "DeleteObjectsAfterConfirmation", cancel) && map.Objects.Count == 1, "Anulowanie usunęło instancję");
                Func<string, bool> accept = text => { prompts++; return true; };
                Require((bool)Call(w, "DeleteObjectsAfterConfirmation", accept) && map.Objects.Count == 0 && prompts == 2, "Potwierdzenie");
                Call(w, "HandleHistoryShortcut", Keys.Control | Keys.Shift | Keys.Z);
                Require(map.Objects.Count == 0, "Shift+Z zmieniło mapę");
                Call(w, "HandleHistoryShortcut", Keys.Control | Keys.Z);
                Require(map.Objects.Count == 1 && !(bool)Get(w, "dirty"), "Natychmiastowe Undo");
                Call(w, "HandleHistoryShortcut", Keys.Control | Keys.Y);
                Require(map.Objects.Count == 0, "Ctrl+Y");
                var editor = (RichNoteEditor)Get(w, "cellDescription");
                var events = new List<bool>();
                editor.HistoryRequested = redo => events.Add(redo);
                var ctrl = System.Windows.Input.ModifierKeys.Control;
                editor.HandleHistoryKey(System.Windows.Input.Key.Z, ctrl | System.Windows.Input.ModifierKeys.Shift);
                editor.HandleHistoryKey(System.Windows.Input.Key.Z, ctrl);
                editor.HandleHistoryKey(System.Windows.Input.Key.Y, ctrl);
                Require(events.SequenceEqual(new[] { false, true }), "Skróty WPF");
            }
        }

        static void RenderAndLayout()
        {
            using (var w = new MainWindow())
            {
                w.Size = w.MinimumSize;
                w.PerformLayout();
                Call(w, "PositionOverlays");
                var viewport = (Panel)Get(w, "terrainScroll");
                Require(viewport.ClientSize.Height >= 72, "Dwa pełne wiersze terenów: " + viewport.ClientSize.Height);
                var map = Map(w);
                var obj = AddObject(map, "Ramka", new PointF(0, 20));
                map.Cells[0].Fog = true;
                map.Groups.Add(new ObjectGroup { Members = new List<string> { obj.Id }, FrameEnabled = true, FrameColor = "#FF00FF" });
                typeof(MainWindow).GetField("pan", Private).SetValue(w, new PointF(90, 90));
                typeof(MainWindow).GetField("scale", Private).SetValue(w, 1f);
                Func<int> framePixels = () =>
                {
                    using (var bitmap = new Bitmap(180, 180))
                    {
                        using (var g = Graphics.FromImage(bitmap)) Call(w, "DrawObjects", g);
                        int count = 0;
                        for (int y = 60; y < 135; y++)
                        for (int x = 60; x < 125; x++)
                            if (bitmap.GetPixel(x, y).ToArgb() == Color.Magenta.ToArgb()) count++;
                        return count;
                    }
                };
                int clipped = framePixels();
                typeof(MainWindow).GetField("previewWithoutFog", Private).SetValue(w, true);
                int full = framePixels();
                Require(clipped > 0 && full > clipped, "Clipping ramki przez mgłę");
                map.Groups.Clear();
                Require(framePixels() == 0, "Ramka pozostała po odłączeniu");
                Console.WriteLine("Viewport at minimum: " + viewport.ClientSize.Height + " px");
            }
        }

        [STAThread]
        static int Main(string[] args)
        {
            root = AppDomain.CurrentDomain.BaseDirectory;
            Application.EnableVisualStyles();
            try
            {
                switch (args.Single())
                {
                    case "states": States(); break;
                    case "groups": GroupsAndFog(); break;
                    case "controls": Controls(); break;
                    case "render": RenderAndLayout(); break;
                    case "fixture": Fixture(); break;
                    default: throw new ArgumentException("Nieznany scenariusz.");
                }
                Console.WriteLine("PASS " + args[0]);
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }

        static void Fixture()
        {
            using (var w = new MainWindow())
            {
                Call(w, "StartNewProject", 9, 6, "FlatTop", "Próba 0.4.2", PresetCatalog.Default());
                var map = Map(w);
                var a = AddObject(map, "Aster", BrushGeometry.Center(map, 1, 1));
                var b = AddObject(map, "Brzoza", BrushGeometry.Center(map, 2, 2));
                var c = AddObject(map, "Cedr", BrushGeometry.Center(map, 4, 1));
                var d = AddObject(map, "Dąb", BrushGeometry.Center(map, 5, 2));
                AddObject(map, "Próba usunięcia 0.4.2", BrushGeometry.Center(map, 1, 4));
                var covered = AddObject(map, "Przy granicy mgły", new PointF(0, 20));
                covered.Size = 40;
                map.Cells[0].Fog = true;
                map.Groups.Add(new ObjectGroup { Name = "Zieloni", Members = new List<string> { a.Id, b.Id }, FrameEnabled = true });
                map.Groups.Add(new ObjectGroup { Name = "Niebiescy", Members = new List<string> { c.Id, d.Id } });
                Save(w, Path.Combine(root, "Próba 0.4.2.hexmap"));
            }
        }
    }
}
