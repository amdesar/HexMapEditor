using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch050Tests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Get(MainWindow window, string name) { return typeof(MainWindow).GetField(name, Private).GetValue(window); }
        static void Set(MainWindow window, string name, object value) { typeof(MainWindow).GetField(name, Private).SetValue(window, value); }
        static object Call(MainWindow window, string name, params object[] args)
        {
            return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(window, args);
        }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static MapData Map(MainWindow window) { return (MapData)Get(window, "map"); }
        static void History(MainWindow window, bool redo = false)
        {
            var args = new object[] { redo, null };
            Check((bool)Call(window, "TryHistory", args), "Historia: " + args[1]);
        }
        static void Save(MainWindow window, string path)
        {
            var args = new object[] { path, false, null };
            Check((bool)Call(window, "TrySaveTo", args), "Zapis: " + args[2]);
        }
        static bool Load(MainWindow window, string path)
        {
            var args = new object[] { path, null, false };
            bool result = (bool)Call(window, "TryLoadProject", args);
            if (!result) Console.WriteLine("Odrzucono plik: " + args[1]);
            return result;
        }
        static Bitmap Stripes()
        {
            var image = new Bitmap(640, 384);
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.DodgerBlue);
                for (int y = 0; y < 384; y += 64) g.FillRectangle(Brushes.Gold, 0, y, 640, 29);
                g.FillRectangle(Brushes.Tomato, 150, 100, 70, 84);
            }
            return image;
        }
        static IconAsset Crop()
        {
            using (var source = Stripes())
            using (var surface = new CropSurface(source, false) { Size = new Size(448, 361) })
            {
                var first = surface.Crop("Pasy");
                surface.ShowFlat(true);
                Check(first.Png == surface.Crop("Pasy").Png, "Zmiana maski zmieniła kadr.");
                using (var result = TerrainTextures.Decode(first))
                    Check(result.GetPixel(2, 2).A == 255 && result.GetPixel(253, 253).A == 255, "Puste kliny w kwadracie.");
                return first;
            }
        }
        static TerrainPreset Palette(IconAsset asset)
        {
            var preset = new TerrainPreset { Name = "Próba tekstur" };
            preset.Terrains.Add(PresetCatalog.Neutral());
            preset.Terrains.Add(new TerrainDefinition { Name = "Poziome pasy", Color = "#297ABB",
                FillKind = asset == null ? "color" : "graphic", Texture = asset, TextureId = asset == null ? null : asset.Id });
            return preset;
        }
        static void Assets(string root)
        {
            var asset = Crop();
            var preset = Palette(asset);
            var path = Path.Combine(root, "Portable.hexmap");
            var library = Path.Combine(root, "graphics");
            Directory.CreateDirectory(library);
            using (var window = new MainWindow())
            {
                Call(window, "StartNewProject", 5, 4, "PointyTop", "Próba", preset);
                var map = Map(window);
                map.Cells[0].Name = "Treść niezależna";
                map.Cells[0].Description = "Notatka";
                Call(window, "CaptureSavedState");
                Set(window, "brushIndex", 1);
                Call(window, "PaintCell", 0);
                Call(window, "FinishStroke");
                Check(map.Cells[0].Texture == asset && map.Cells[0].Name == "Treść niezależna", "Malowanie grafiki.");
                History(window);
                Check(map.Cells[0].Texture == null && map.Cells[0].Description == "Notatka", "Undo malowania.");
                History(window, true);
                Set(window, "currentTool", EditorTool.Bucket);
                Call(window, "FillRegion", 1);
                Check(map.Cells.All(c => c.TextureId == asset.Id), "Wypełnienie grafiką.");
                History(window);
                Check(map.Cells.Count(c => c.TextureId == asset.Id) == 1, "Jedno Undo wiadra.");
                History(window, true);
                Call(window, "ChangeOrientation", "FlatTop");
                Check(map.Cells.All(c => c.TextureId == asset.Id), "Orientacja zmieniła źródło grafiki.");
                map.Cells[0].Fog = true;
                Save(window, path);

                var entries = new List<TerrainGraphic>();
                Set(window, "terrainGraphics", entries);
                var entry = TerrainGraphicLibrary.Prepare(library, null, "Pasy", asset, entries);
                Call(window, "ChangeTerrainGraphic", null, entry);
                var renamed = TerrainGraphicLibrary.Prepare(library, entry, "Zmienione pasy", asset, entries);
                Call(window, "ChangeTerrainGraphic", entry, renamed);
                Check(!File.Exists(entry.Path) && File.Exists(renamed.Path), "Zmiana nazwy pliku.");
                Call(window, "ChangeTerrainGraphic", renamed, null);
                Check(!File.Exists(renamed.Path) && !(bool)Get(window, "dirty"), "Usunięcie biblioteczne nie zmienia mapy.");
                File.WriteAllText(renamed.Path, "zajęta ścieżka");
                var conflict = new object[] { false, null };
                Check(!(bool)Call(window, "TryHistory", conflict) && File.ReadAllText(renamed.Path) == "zajęta ścieżka", "Konflikt nie nadpisuje pliku.");
                File.Delete(renamed.Path);
                History(window);
                Check(File.ReadAllBytes(renamed.Path).SequenceEqual(renamed.Bytes), "Undo biblioteczne odtwarza bajty.");
                History(window, true);
                Check(!File.Exists(renamed.Path) && map.Cells[0].TextureId == asset.Id, "Redo nie niszczy mapy.");
            }
            using (var window = new MainWindow())
            {
                Check(Load(window, path), "Odczyt samodzielnej mapy.");
                var map = Map(window);
                Check(map.FormatVersion == 7 && map.Cells[0].Fog && map.Cells[0].TextureId == asset.Id &&
                    Object.ReferenceEquals(map.Cells[0].Texture, map.Presets[0].Terrains[1].Texture), "Dedup i odczyt bez biblioteki.");
                var broken = Path.Combine(root, "Broken.hexmap");
                File.WriteAllText(broken, File.ReadAllText(path).Replace(asset.Id, "brak-" + asset.Id));
                Check(!Load(window, broken) && Object.ReferenceEquals(map, Map(window)), "Błąd odczytu zmienił otwartą mapę.");
                Render(window);
            }
            string presets = Path.Combine(root, "presets");
            PresetCatalog.Save(preset, presets);
            var loaded = PresetCatalog.ReadFile(Directory.GetFiles(presets).Single());
            Check(loaded.PresetFormatVersion == 2 && loaded.Terrains[1].Texture.Png == asset.Png, "Samodzielny preset.");
        }
        static void Render(MainWindow window)
        {
            var map = Map(window);
            map.LabelMode = "None";
            Set(window, "pan", new PointF(90, 90));
            Set(window, "scale", 1f);
            using (var bitmap = new Bitmap(180, 180))
            using (var g = Graphics.FromImage(bitmap))
            using (var args = new PaintEventArgs(g, new Rectangle(0, 0, 180, 180)))
            {
                Call(window, "PaintMap", null, args);
                Check(bitmap.GetPixel(90, 90).ToArgb() == Color.FromArgb(101, 107, 117).ToArgb(), "Mgła kryje teksturę.");
                map.Cells[0].Fog = false;
                foreach (var orientation in new[] { "PointyTop", "FlatTop" })
                {
                    map.Orientation = orientation;
                    Call(window, "PaintMap", null, args);
                    Check(bitmap.GetPixel(84, 88).ToArgb() == bitmap.GetPixel(96, 88).ToArgb() &&
                        bitmap.GetPixel(84, 88).ToArgb() != bitmap.GetPixel(84, 94).ToArgb(), "Poziome pasy w " + orientation);
                }
                Set(window, "hideTerrain", true);
                Call(window, "PaintMap", null, args);
                Check(bitmap.GetPixel(90, 90).ToArgb() == ColorTranslator.FromHtml("#777F91").ToArgb(), "Widok neutralnej siatki.");
            }
        }
        static void ObjectsAndColor(string root)
        {
            using (var window = new MainWindow())
            {
                var map = Map(window);
                var dir = Path.Combine(root, "states");
                var state = new ObjectStateDefinition { Name = "Testowy", Color = "#C04455" };
                Call(window, "ChangeStateFile", state, state.Id, true, dir);
                Call(window, "RemoveDocumentState", state.Id, "Neutral", false);
                var another = new ObjectStateDefinition { Name = state.Name, Color = "#223344" };
                Call(window, "ChangeStateFile", another, another.Id, true, dir);
                Check((bool)Call(window, "RestoreDocumentState", state.Id), "Przywrócenie po ID.");
                History(window);
                Check(map.ExcludedStateIds.SequenceEqual(new[] { state.Id }) && map.ObjectStates.Any(s => s.Id == another.Id), "Undo oraz inny stan o tej samej nazwie.");
                History(window, true);
                var obj = AddObject(map);
                map.Groups.Add(new ObjectGroup { Members = new List<string> { obj.Id }, StateId = "Ally" });
                var row = (ObjectListRow)Call(window, "ObjectListItem", obj);
                var ally = map.ObjectStates.Single(s => s.Id == "Ally");
                Check(row.SubItems[1].Text == ally.Name && row.StateColor == ColorTranslator.FromHtml(ally.Color) && obj.Side == "Neutral", "Lista używa stanu efektywnego.");
            }
            using (var color = new EditorColorDialog(Color.White))
            {
                var hex = color.Controls.OfType<TextBox>().Single();
                hex.Text = "#ab09f7";
                Check(color.Hex == "#AB09F7" && ((Button)color.AcceptButton).Enabled, "HEX mieszany i zachowane RGB.");
                hex.Text = "#1234567";
                Check(color.Hex == "#AB09F7" && !((Button)color.AcceptButton).Enabled, "Niedozwolony HEX zmienił wynik.");
                Check(!MapLogic.ValidColor("#12A3F ") && !MapLogic.ValidColor("#ABCDE"), "Ścisłe sześć cyfr.");
            }
        }
        static MapObject AddObject(MapData map)
        {
            var asset = IconAssets.Starters()[0];
            map.IconAssets.Add(asset);
            var center = BrushGeometry.Center(map, 2, 1);
            var obj = new MapObject { Name = "Obiekt próby", TemplateId = "fixture", AssetId = asset.Id, X = center.X, Y = center.Y };
            map.Objects.Add(obj);
            return obj;
        }
        static void GroupClick()
        {
            using (var window = new MainWindow())
            {
                var map = Map(window);
                var first = AddObject(map);
                var second = first.Copy();
                second.Id = Guid.NewGuid().ToString("N");
                second.X += 70;
                map.Objects.Add(second);
                map.Groups.Add(new ObjectGroup { Members = new List<string> { first.Id, second.Id } });
                Set(window, "pan", PointF.Empty);
                Set(window, "scale", 1f);
                Set(window, "gestureTool", EditorTool.Select);
                var point = Point.Round(new PointF(first.X, first.Y));
                Set(window, "dragOrigin", point);
                var click = new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0);
                var selected = (HashSet<string>)Get(window, "selectedObjects");
                Call(window, "ObjectMouseDown", click);
                Call(window, "ObjectMouseUp", click);
                Call(window, "FinishObjectGesture", true);
                Check(selected.SetEquals(map.Groups[0].Members) && (bool)Get(window, "rightPanelOpen"), "Klik wybiera grupę.");
                Call(window, "ObjectMouseDown", click);
                Check(selected.Count == 2, "MouseDown nie odznacza grupy przed ewentualnym ruchem.");
                Call(window, "ObjectMouseUp", click);
                Call(window, "FinishObjectGesture", true);
                Check(selected.Count == 0 && !(bool)Get(window, "rightPanelOpen"), "Drugi zwykły klik odznacza grupę.");
            }
            Console.WriteLine("PASS: ponowny klik grupy");
        }
        static void Fixture(string directory)
        {
            using (var source = Stripes()) source.Save(Path.Combine(directory, "Poziome pasy.png"), ImageFormat.Png);
            using (var window = new MainWindow())
            {
                Call(window, "StartNewProject", 8, 6, "PointyTop", "Próba 0.5.0", Palette(null));
                AddObject(Map(window));
                Save(window, Path.Combine(directory, "Proba050.hexmap"));
            }
        }
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "group-click") { GroupClick(); return 0; }
                var root = AppDomain.CurrentDomain.BaseDirectory;
                Assets(root);
                ObjectsAndColor(root);
                GroupClick();
                if (args.Length == 1) Fixture(args[0]);
                Console.WriteLine("PASS: zasoby, wiadro, historia, zapis, render, stany, HEX");
                return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
        }
    }
}
