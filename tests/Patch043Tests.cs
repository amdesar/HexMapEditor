using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch043Tests
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        static string root;
        static object Get(MainWindow w, string name) { return typeof(MainWindow).GetField(name, Fields).GetValue(w); }
        static void Set(MainWindow w, string name, object value) { typeof(MainWindow).GetField(name, Fields).SetValue(w, value); }
        static object Call(MainWindow w, string name, params object[] args)
        {
            return typeof(MainWindow).GetMethods(Fields).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(w, args);
        }
        static void Check(bool result, string message) { if (!result) throw new Exception(message); }
        static MapData Map(MainWindow w) { return (MapData)Get(w, "map"); }
        static void History(MainWindow w, bool redo = false)
        {
            object[] args = { redo, null };
            Check((bool)Call(w, "TryHistory", args), "Historia: " + args[1]);
        }
        static ObjectStateDefinition State(string id, string name, bool reveals = false)
        {
            return new ObjectStateDefinition { Id = id, Name = name, Color = "#447755", CanReveal = reveals };
        }
        static MapObject Add(MainWindow w, string name, PointF point)
        {
            var map = Map(w);
            if (map.IconAssets.Count == 0) map.IconAssets.Add(IconAssets.Starters().First());
            var obj = new MapObject { Name = name, X = point.X, Y = point.Y, AssetId = map.IconAssets[0].Id, TemplateId = "test" };
            map.Objects.Add(obj);
            return obj;
        }
        static void States()
        {
            using (var w = new MainWindow())
            {
                string dir = Path.Combine(root, "states");
                var z = State("z", "Zeta");
                var a = State("a", "Alfa", true);
                Call(w, "ChangeStateFile", z, z.Id, true, dir);
                Call(w, "ChangeStateFile", a, a.Id, true, dir);
                z.Name = "Zeta zmieniona";
                Call(w, "ChangeStateFile", z, z.Id, true, dir);
                string warning;
                var disk = StateLibrary.Load(dir, out warning);
                Check(warning == "" && disk.Skip(3).Select(f => f.Definition.Id).SequenceEqual(new[] { "z", "a" }), "Porządek po restarcie i edycji");
                using (var dialog = new StateRemovalDialog(z, Map(w).ObjectStates.Where(s => s.Id != z.Id).ToList(),
                    new string[0], true))
                    Check(dialog.ReplacementId == "Neutral" && !dialog.DeleteFile && dialog.DialogResult != DialogResult.OK, "Domyślny zamiennik i anulowanie");

                Call(w, "RemoveDocumentState", "z", "Neutral", false);
                Check(!((List<ObjectStateDefinition>)Call(w, "AvailableStates")).Any(s => s.Id == "z") &&
                    StateLibrary.Load(dir, out warning).Any(f => f.Definition.Id == "z"), "Usunięcie nieużywanego tylko z mapy");
                Check(((MapData)Call(w, "MapForSave")).ExcludedStateIds.Contains("z"), "Trwałe wyłączenie");
                History(w);
                Check(Map(w).ObjectStates.Skip(3).Select(s => s.Id).SequenceEqual(new[] { "z", "a" }), "Undo kolejności stanu");

                var map = Map(w);
                var obj = Add(w, "Baza pod nakładką", PointF.Empty);
                obj.Side = "z";
                obj.RevealsFog = true;
                var other = Add(w, "Inny", BrushGeometry.Center(map, 2, 0));
                var g = new ObjectGroup { StateId = "z", Members = new List<string> { obj.Id, other.Id }, RevealsFog = true };
                map.Groups.Add(g);
                map.Cells[0].Fog = true;
                string stored = disk.Single(f => f.Definition.Id == "z").Path;
                byte[] bytes = File.ReadAllBytes(stored);
                Call(w, "RemoveDocumentState", "z", "a", true);
                Check(map.Objects[0].Side == "a" && map.Groups[0].StateId == "a" && map.Objects[1].Side == "Neutral"
                    && !map.Cells[0].Fog && !File.Exists(stored), "Baza, grupa, mgła i plik jednej transakcji");
                History(w);
                Check(map.Objects[0].Side == "z" && map.Groups[0].StateId == "z" && map.Cells[0].Fog
                    && File.ReadAllBytes(stored).SequenceEqual(bytes), "Undo pliku, referencji i mgły");
                History(w, true);
                Check(map.Groups[0].StateId == "a" && !File.Exists(stored), "Redo usunięcia stanu");
            }
        }

        static void ViewAndList()
        {
            using (var w = new MainWindow())
            {
                var map = Map(w);
                var a = Add(w, "Zeta", PointF.Empty);
                var b = Add(w, "Alfa", BrushGeometry.Center(map, 1, 0));
                var c = Add(w, "Beta", BrushGeometry.Center(map, 2, 0));
                var group = new ObjectGroup { Name = "Drużyna", Members = new List<string>{ a.Id }, StateId = "Enemy",
                    FrameEnabled = true, FrameColor = "#FF00FF" };
                map.Groups.Add(group);
                map.Cells[0].Fog = true;
                Call(w, "CaptureSavedState");
                var rows = map.Objects.Select((item, index) =>
                {
                    var row = (ObjectListRow)Call(w, "ObjectListItem", item);
                    row.DocumentIndex = index;
                    return row;
                }).ToList();
                Check(rows[0].SubItems.Cast<ListViewItem.ListViewSubItem>().Take(4).All(s => s.Text == "???")
                    && !rows[0].StateColor.HasValue && rows[0].HiddenFraction == 1, "Maska widoku");
                var order = new ObjectListOrder();
                for (int i = 0; i < 3; i++)
                {
                    order.Advance(0);
                    rows.Sort((x, y) => order.Compare(x, y));
                    var names = rows.Where(row => row.Text != "???").Select(row => row.Text).ToArray();
                    Check(names.SequenceEqual(i == 1 ? new[] { "Beta", "Alfa" } : new[] { "Alfa", "Beta" }), "Kierunek");
                }
                Check(rows.Select(row => (string)row.Tag).SequenceEqual(map.Objects.Select(item => item.Id)), "Powrót do kolejności i ID");
                order.Advance(4);
                Check(order.Compare(rows[0], rows[1]) > 0 && order.Compare(rows[1], rows[2]) < 0, "Liczbowa mgła i stabilny remis");

                Call(w, "SetLayerVisible", MapLayer.Objects, false);
                Check(Call(w, "HitObject", new PointF(b.X, b.Y)) == null, "Hit-test ukrytych obiektów");
                Call(w, "RestoreLayerAfterPrompt", MapLayer.Objects, false);
                Check(!(bool)Call(w, "LayerVisible", MapLayer.Objects), "Odmowa");
                Call(w, "RestoreLayerAfterPrompt", MapLayer.Objects, true);
                Call(w, "SetLayerVisible", MapLayer.Fog, false);
                Call(w, "SetLayerVisible", MapLayer.Icons, false);
                Call(w, "SetLayerVisible", MapLayer.Terrain, false);
                var unmasked = (ObjectListRow)Call(w, "ObjectListItem", a);
                Check(unmasked.Text == a.Name && unmasked.SubItems[2].Text == group.Name &&
                    unmasked.StateColor == ColorTranslator.FromHtml(map.ObjectStates.Single(s => s.Id == "Enemy").Color), "Efektywny stan");
                var saved = (MapData)Call(w, "MapForSave");
                Check(saved.Cells[0].Fog && saved.Objects.Count == 3 && saved.IconAssets.Count == 1 &&
                    !(bool)Get(w, "dirty") && ((Stack<EditAction>)Get(w, "undo")).Count == 0, "Widok nie edytuje zapisu");

                Set(w, "pan", new PointF(90, 90));
                Set(w, "scale", 1f);
                ((HashSet<string>)Get(w, "selectedObjects")).Add(a.Id);
                using (var image = new Bitmap(180, 180))
                {
                    using (var graphics = Graphics.FromImage(image)) Call(w, "DrawObjects", graphics);
                    var box = (RectangleF)Call(w, "ObjectBounds", a);
                    int y = (int)(box.Top + 90);
                    Func<Color, int, bool> line = (color, row) => Enumerable.Range(70, 40)
                        .Any(x => image.GetPixel(x, row).ToArgb() == color.ToArgb());
                    Check(line(Color.Magenta, y - 1) && line(Color.Gold, y - 6), "Ramka blisko, złoto na zewnątrz");
                    Call(w, "SetLayerVisible", MapLayer.Fog, true);
                    using (var graphics = Graphics.FromImage(image))
                    {
                        graphics.Clear(Color.Black);
                        Call(w, "DrawObjects", graphics);
                    }
                    // Corners can extend beyond the hex. The central segment of
                    // each upper edge is inside the fogged hex.
                    Check(!Enumerable.Range(84, 12).Any(x => image.GetPixel(x, y - 1).ToArgb() == Color.Magenta.ToArgb()
                        || image.GetPixel(x, y - 6).ToArgb() == Color.Gold.ToArgb()), "Mgła przycina oba obrysy");
                }
            }
        }

        static void LibraryOrder()
        {
            using (var w = new MainWindow())
            {
                var asset = IconAssets.Starters().First();
                string icons = Path.Combine(root, "icons");
                string objects = Path.Combine(root, "objects");
                string presets = Path.Combine(root, "presets");
                Directory.CreateDirectory(icons);
                Directory.CreateDirectory(objects);
                File.WriteAllText(Path.Combine(icons, ".starter-icons-v1"), "1");
                File.WriteAllText(Path.Combine(objects, ".starter-objects-v1"), "1");

                var z = new LibraryIcon { Name = "Zeta", Asset = asset };
                var a = new LibraryIcon { Name = "Alfa", Asset = asset };
                foreach (var icon in new[] { z, a })
                {
                    object[] args = { null, icon, false, icons, null };
                    Check((bool)Call(w, "TryChangeLibraryIcon", args), "Zapis ikony: " + args[4]);
                }
                var first = ((List<LibraryIcon>)Get(w, "libraryIcons"))[0];
                var renamed = first.Copy();
                renamed.Name = "Zeta później";
                object[] edit = { first, renamed, false, icons, null };
                Check((bool)Call(w, "TryChangeLibraryIcon", edit), "Zmiana nazwy ikony");
                History(w);
                History(w, true);
                string warning;
                Check(IconLibrary.Load(icons, out warning).Select(i => i.Id).SequenceEqual(new[] { z.Id, a.Id }), "Porządek ikon");

                var tz = new ObjectTemplate { Name = "Zeta", Asset = asset };
                var ta = new ObjectTemplate { Name = "Alfa", Asset = asset };
                foreach (var obj in new[] { tz, ta })
                {
                    object[] args = { null, obj, false, objects, null };
                    Check((bool)Call(w, "TryChangeObjectTemplate", args), "Zapis wzorca: " + args[4]);
                }
                Check(ObjectLibrary.Load(objects, out warning).Select(o => o.Id).SequenceEqual(new[] { tz.Id, ta.Id }), "Porządek wzorców");
                foreach (var name in new[] { "Zeta", "Alfa" })
                {
                    var preset = new TerrainPreset { Name = name };
                    preset.Terrains.Add(PresetCatalog.Neutral());
                    PresetCatalog.Save(preset, presets);
                }
                Check(PresetCatalog.Load(presets, out warning).Skip(1).Select(p => p.Name).SequenceEqual(new[] { "Zeta", "Alfa" }), "Porządek presetów");
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
                    case "order": LibraryOrder(); break;
                    case "view-list": ViewAndList(); break;
                    default: throw new ArgumentException("Nieznany scenariusz.");
                }
                Console.WriteLine("PASS " + args[0]);
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
