using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using HexMapEditor;

namespace HexMapEditor.Tests
{
    // Method/control tests: no shown window and no injected keyboard input.
    internal static class HistoryTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static int passed;

        static object Get(MainWindow window, string name) { return typeof(MainWindow).GetField(name, Private).GetValue(window); }
        static void Set(MainWindow window, string name, object value) { typeof(MainWindow).GetField(name, Private).SetValue(window, value); }
        static object Call(MainWindow window, string name, params object[] args)
        {
            return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(window, args);
        }
        static MapData Map(MainWindow window) { return (MapData)Get(window, "map"); }
        static int Count(MainWindow window, string name) { return ((ICollection)Get(window, name)).Count; }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Paint(MainWindow window, int brush, params int[] cells)
        {
            Set(window, "brushIndex", brush);
            foreach (int index in cells) Call(window, "PaintCell", index);
            Call(window, "FinishStroke");
        }
        static void SelectFirst(MainWindow window)
        {
            Set(window, "selected", 0); Set(window, "rightPanelOpen", true); Call(window, "UpdateUI");
        }
        static void EditNotes(MainWindow window, string name, string description)
        {
            ((TextBox)Get(window, "cellName")).Text = name;
            ((TextBox)Get(window, "cellDescription")).Text = description;
        }
        static void Test(string name, Action<MainWindow> test)
        {
            using (var window = new MainWindow()) test(window);
            passed++; Console.WriteLine("PASS " + name);
        }

        [STAThread]
        static int Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Test("paint -> edit name/description -> Undo", w =>
                {
                    var original = Map(w).Cells[0]; string terrain = original.Terrain, color = original.Color;
                    Paint(w, 1, 0); SelectFirst(w); EditNotes(w, "Przełęcz Cieni", "Nowy opis\r\nDrugi akapit");
                    Call(w, "Undo"); var cell = Map(w).Cells[0];
                    Check(cell.Terrain == terrain && cell.Color == color, "Terrain/color not restored");
                    Check(cell.Name == "Przełęcz Cieni" && cell.Description == "Nowy opis\r\nDrugi akapit", "Later text lost");
                    Check(Object.ReferenceEquals(original, cell), "Undo replaced the cell object");
                });
                Test("Undo -> edit name/description -> Redo", w =>
                {
                    Paint(w, 1, 0); string terrain = Map(w).Cells[0].Terrain, color = Map(w).Cells[0].Color;
                    Call(w, "Undo"); SelectFirst(w); EditNotes(w, "Nazwa po Undo", "Opis po Undo"); Call(w, "Redo");
                    var cell = Map(w).Cells[0];
                    Check(cell.Terrain == terrain && cell.Color == color, "Redo did not restore paint");
                    Check(cell.Name == "Nazwa po Undo" && cell.Description == "Opis po Undo", "Redo lost later text");
                });
                Test("paint/Undo/Redo preserve current NotesFormat", w =>
                {
                    Map(w).Cells[0].NotesFormat = "test-before"; Paint(w, 1, 0);
                    Check(Map(w).Cells[0].NotesFormat == "test-before", "Paint changed NotesFormat");
                    Map(w).Cells[0].NotesFormat = "test-after-paint"; Call(w, "Undo");
                    Check(Map(w).Cells[0].NotesFormat == "test-after-paint", "Undo restored stale NotesFormat");
                    Map(w).Cells[0].NotesFormat = "test-after-undo"; Call(w, "Redo");
                    Check(Map(w).Cells[0].NotesFormat == "test-after-undo", "Redo restored stale NotesFormat");
                });
                Test("empty stroke preserves Redo and clean state", w =>
                {
                    Paint(w, 1, 0); Call(w, "Undo"); Paint(w, 0, 0, 0, 1);
                    Check(Count(w, "undo") == 0 && Count(w, "redo") == 1, "Empty stroke changed history");
                    Check(!(bool)Get(w, "dirty"), "Empty stroke dirtied project");
                    Check(((Button)Get(w, "saveButton")).Text == "Zapisz", "Empty stroke changed save indicator");
                    ((TextBox)Get(w, "mapName")).Text = "Niezapisana nazwa"; Paint(w, 0, 0);
                    Check((bool)Get(w, "dirty") && Count(w, "redo") == 1, "Empty stroke changed existing dirty state/Redo");
                });
                Test("real paint after Undo clears Redo", w =>
                {
                    Paint(w, 1, 0); Call(w, "Undo"); Paint(w, 3, 0);
                    Check(Count(w, "redo") == 0 && Count(w, "undo") == 1, "Real paint did not branch history");
                    Check((bool)Get(w, "dirty"), "Real paint not marked dirty");
                });
                Test("same HTML color case remains a no-op", w =>
                {
                    Map(w).Cells[0].Color = "#777f91";
                    Set(w, "savedSnapshot", new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(Map(w)));
                    Paint(w, 1, 0); Call(w, "Undo"); Paint(w, 0, 0);
                    Check(Map(w).Cells[0].Color == "#777f91" && !(bool)Get(w, "dirty"), "Same color spelling changed project");
                    Set(w, "brushIndex", 2); Call(w, "PaintCell", 0);
                    Set(w, "brushIndex", 0); Call(w, "PaintCell", 0); Call(w, "FinishStroke");
                    Check(Map(w).Cells[0].Color == "#777f91" && !(bool)Get(w, "dirty"), "Net-empty stroke changed color spelling");
                    Check(Count(w, "redo") == 1 && Count(w, "undo") == 0, "Same color changed history");
                });
                Test("one stroke groups repeated and multiple cells", w =>
                {
                    Paint(w, 1, 0, 1, 0, 2); Check(Count(w, "undo") == 1, "Stroke not grouped"); Call(w, "Undo");
                    Check(Map(w).Cells.Take(3).All(c => c.Terrain == "Teren neutralny"), "Undo missed a cell");
                    Call(w, "Redo"); Check(Map(w).Cells.Take(3).All(c => c.Terrain == "Woda"), "Redo missed a cell");
                });
                Test("stroke ending at original terrain preserves Redo", w =>
                {
                    Paint(w, 1, 0); Call(w, "Undo");
                    Set(w, "brushIndex", 2); Call(w, "PaintCell", 0);
                    Set(w, "brushIndex", 0); Call(w, "PaintCell", 0); Call(w, "FinishStroke");
                    Check(Count(w, "undo") == 0 && Count(w, "redo") == 1 && !(bool)Get(w, "dirty"), "Net-empty stroke changed history/state");
                });
                Test("save -> Undo -> Redo restores saved dirty indicator", w =>
                {
                    string directory = Path.Combine(Path.GetTempPath(), "HexMapEditor-history-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(directory); string path = Path.Combine(directory, "map.hexmap");
                    try
                    {
                        Paint(w, 1, 0); object[] saveArgs = { path, true, null };
                        Check((bool)Call(w, "TrySaveTo", saveArgs), "Save failed: " + saveArgs[2]);
                        Check(!(bool)Get(w, "dirty") && Count(w, "undo") == 1, "Save cleared history or stayed dirty");
                        var saved = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<MapData>(File.ReadAllText(path));
                        Check(saved.FormatVersion == 1, "Map format changed");
                        Call(w, "Undo"); Check((bool)Get(w, "dirty") && w.Text.EndsWith(" *"), "Undo not marked dirty");
                        Call(w, "Redo"); Check(!(bool)Get(w, "dirty") && !w.Text.EndsWith(" *"), "Redo to saved state remains dirty");
                        Check(((Button)Get(w, "saveButton")).Text == "Zapisz", "Save indicator not restored");
                    }
                    finally { if (File.Exists(path)) File.Delete(path); Directory.Delete(directory); }
                });
                Test("text fields leave Ctrl+Z/Y to native control", w =>
                {
                    Paint(w, 1, 0); SelectFirst(w); string terrain = Map(w).Cells[0].Terrain;
                    foreach (string field in new[] { "mapName", "cellName", "cellDescription" })
                    {
                        var box = (TextBox)Get(w, field); w.ActiveControl = box;
                        Check(w.ActiveControl == box, "Active text control not set");
                        foreach (Keys key in new[] { Keys.Z, Keys.Y })
                        {
                            var args = new KeyEventArgs(Keys.Control | key); Call(w, "OnKeyDown", w, args);
                            Check(!args.Handled && !args.SuppressKeyPress, "Text shortcut intercepted");
                            Check(Map(w).Cells[0].Terrain == terrain && Count(w, "undo") == 1, "Text shortcut changed map");
                        }
                        Call(w, "Undo");
                        var redoKey = new KeyEventArgs(Keys.Control | Keys.Y); Call(w, "OnKeyDown", w, redoKey);
                        Check(Count(w, "redo") == 1 && Map(w).Cells[0].Terrain == "Teren neutralny", "Text Ctrl+Y invoked map Redo");
                        Call(w, "Redo");
                    }
                });
                Test("map shortcuts still work outside text fields", w =>
                {
                    Paint(w, 1, 0); w.ActiveControl = (Control)Get(w, "canvas");
                    var undoKey = new KeyEventArgs(Keys.Control | Keys.Z); Call(w, "OnKeyDown", w, undoKey);
                    Check(undoKey.SuppressKeyPress && Count(w, "redo") == 1, "Map Undo shortcut broken");
                    var redoKey = new KeyEventArgs(Keys.Control | Keys.Y); Call(w, "OnKeyDown", w, redoKey);
                    Check(redoKey.SuppressKeyPress && Count(w, "undo") == 1, "Map Redo shortcut broken");
                });
                Console.WriteLine("Passed: " + passed + "; method/control tests only, no interactive window test.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
        }
    }
}
