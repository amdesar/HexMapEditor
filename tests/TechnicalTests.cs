using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using HexMapEditor;

namespace HexMapEditor.Tests
{
    // These are method/control tests, not automation of native Windows dialogs.
    internal static class TechnicalTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static string root;
        static int passed;
        static object Get(object owner, string name) { return owner.GetType().GetField(name, Private).GetValue(owner); }
        static void Set(object owner, string name, object value) { owner.GetType().GetField(name, Private).SetValue(owner, value); }
        static object Call(MainWindow w, string name, params object[] args) { return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(w, args); }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static void Test(string name, Action<MainWindow> body) { using (var w = new MainWindow()) body(w); passed++; Console.WriteLine("PASS " + name); }
        static string PathFor(string name) { return Path.Combine(root, name); }
        static void Dirty(MainWindow w) { ((TextBox)Get(w, "mapName")).Text = "Niezapisana mapa"; }
        static bool TrySave(MainWindow w, string path, bool overwrite, out string error)
        {
            object[] args = { path, overwrite, null }; bool success = (bool)Call(w, "TrySaveTo", args); error = (string)args[2]; return success;
        }
        static void CheckUnchanged(MainWindow w, string currentPath, string snapshot)
        {
            Check((string)Get(w, "currentPath") == currentPath && (string)Get(w, "savedSnapshot") == snapshot && (bool)Get(w, "dirty"), "Failed/cancelled save changed project state");
        }

        [STAThread]
        static int Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            string tempRoot = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            root = Path.GetFullPath(Path.Combine(tempRoot, "HexMapEditor-technical-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(root);
            try
            {
                Test("overwrite confirmation names final path exactly once", w =>
                {
                    string[] inputs = { "mapa.hexmap", "mapa.txt", "mapa.txt.hexmap", "mapa.HEXMAP" };
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        string directory = PathFor("case-" + i); Directory.CreateDirectory(directory);
                        string input = Path.Combine(directory, inputs[i]); string final = input.EndsWith(".hexmap", StringComparison.OrdinalIgnoreCase) ? input : input + ".hexmap";
                        File.WriteAllText(final, "ORIGINAL"); Dirty(w); int prompts = 0;
                        Func<string, DialogResult> yes = path => { prompts++; Check(path == final, "Wrong final confirmation path"); return DialogResult.Yes; };
                        Check((bool)Call(w, "SaveAsTo", input, yes), "Confirmed save failed");
                        Check(prompts == 1 && (string)Get(w, "currentPath") == final && !(bool)Get(w, "dirty"), "Confirmation count/path/state incorrect");
                        Check(new JavaScriptSerializer().Deserialize<MapData>(File.ReadAllText(final)).FormatVersion == 1, "Format changed");
                    }
                });
                Test("No/Cancel preserve target, current path and dirty state", w =>
                {
                    string target = PathFor("refused.txt.hexmap"); File.WriteAllText(target, "KEEP");
                    foreach (string current in new[] { null, PathFor("open.hexmap") }) foreach (DialogResult answer in new[] { DialogResult.No, DialogResult.Cancel })
                    {
                        Set(w, "currentPath", current); Dirty(w); string snapshot = (string)Get(w, "savedSnapshot"); int prompts = 0;
                        Func<string, DialogResult> refuse = path => { prompts++; Check(path == target, "Confirmation did not use final path"); return answer; };
                        Check(!(bool)Call(w, "SaveAsTo", PathFor("refused.txt"), refuse), "Refusal accepted");
                        Check(prompts == 1 && File.ReadAllText(target) == "KEEP", "Refused file changed"); CheckUnchanged(w, current, snapshot);
                    }
                });
                Test("new target skips confirmation; ordinary Save uses current file", w =>
                {
                    Dirty(w); int prompts = 0; string path = PathFor("new.txt.hexmap");
                    Func<string, DialogResult> unexpected = p => { prompts++; return DialogResult.No; };
                    Check((bool)Call(w, "SaveAsTo", PathFor("new.txt"), unexpected) && prompts == 0, "New file asked to overwrite");
                    ((TextBox)Get(w, "mapName")).Text = "Zwykly zapis";
                    Check((bool)Call(w, "SaveProject"), "Ordinary Save failed");
                    Check((string)Get(w, "currentPath") == path && !(bool)Get(w, "dirty"), "Ordinary Save changed current path");
                    Check(new JavaScriptSerializer().Deserialize<MapData>(File.ReadAllText(path)).Name == "Zwykly zapis", "Ordinary Save missed changes");
                });
                Test("save failure preserves old file, path, snapshot and dirty state", w =>
                {
                    string path = PathFor("locked.hexmap"); File.WriteAllText(path, "KEEP LOCKED");
                    string current = PathFor("current.hexmap"); Set(w, "currentPath", current); Dirty(w); string snapshot = (string)Get(w, "savedSnapshot"), error;
                    using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        Check(!TrySave(w, path, true, out error) && !String.IsNullOrEmpty(error), "Locked target not rejected");
                    Check(File.ReadAllText(path) == "KEEP LOCKED", "Failed save damaged file"); CheckUnchanged(w, current, snapshot);
                    Check(!TrySave(w, Path.Combine(PathFor("missing"), "map.hexmap"), true, out error), "Missing directory not rejected");
                    CheckUnchanged(w, current, snapshot); Check(!Directory.GetFiles(root, ".hexmap-*.tmp").Any(), "Temporary file leaked");
                });
                Test("unconfirmed target appearing before commit is not replaced", w =>
                {
                    string path = PathFor("appeared.hexmap"); File.WriteAllText(path, "APPEARED"); Dirty(w); string error;
                    Check(!TrySave(w, path, false, out error) && File.ReadAllText(path) == "APPEARED", "Unapproved existing file replaced");
                    Check((string)Get(w, "currentPath") == null && (bool)Get(w, "dirty"), "Race failure changed state");
                });
                Test("explicit native dialog settings override remembered directories", w =>
                {
                    using (var d = (SaveFileDialog)Call(w, "CreateSaveDialog"))
                        Check(d.InitialDirectory == LibraryPaths.Hexmaps && !d.OverwritePrompt && !d.AddExtension && d.RestoreDirectory, "New project dialog configuration incorrect");
                    string current = Path.Combine(root, "open.hexmap"); Set(w, "currentPath", current);
                    using (var d = (SaveFileDialog)Call(w, "CreateSaveDialog")) Check(d.InitialDirectory == root, "Open project directory ignored");
                });
                Test("fresh process model still proposes executable Library/Hexmaps", w =>
                {
                    string path = PathFor("restart.hexmap"), error; Check(TrySave(w, path, true, out error), "Restart fixture save failed");
                    using (var restarted = new MainWindow())
                    {
                        using (var d = (SaveFileDialog)Call(restarted, "CreateSaveDialog")) Check(d.InitialDirectory == LibraryPaths.Hexmaps, "New instance inherited saved directory");
                        Set(restarted, "map", new JavaScriptSerializer().Deserialize<MapData>(File.ReadAllText(path))); Set(restarted, "currentPath", path);
                        using (var d = (SaveFileDialog)Call(restarted, "CreateSaveDialog")) Check(d.InitialDirectory == root, "Loaded project directory ignored");
                    }
                });
                Test("library folders are idempotent and preserve existing content", w =>
                {
                    string app = PathFor("app"); string terrain = Path.Combine(app, "Library", "Terrains"); Directory.CreateDirectory(terrain);
                    string keep = Path.Combine(terrain, "keep.txt"); File.WriteAllText(keep, "KEEP RESOURCE");
                    LibraryPaths.EnsureDirectories(app); LibraryPaths.EnsureDirectories(app);
                    foreach (string folder in new[] { "Backgrounds", "Terrains", "Icons", "Objects", "Hexmaps", Path.Combine("Exports", "Images"), Path.Combine("Exports", "Text") })
                        Check(Directory.Exists(Path.Combine(app, "Library", folder)), "Missing library directory: " + folder);
                    Check(File.ReadAllText(keep) == "KEEP RESOURCE", "Library content changed");
                });
                Test("library and initial save directory ignore working directory", w =>
                {
                    string before = Directory.GetCurrentDirectory();
                    try
                    {
                        Directory.SetCurrentDirectory(root);
                        Check(LibraryPaths.Root == Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library"), "Library uses working directory");
                        using (var d = (SaveFileDialog)Call(w, "CreateSaveDialog")) Check(d.InitialDirectory == LibraryPaths.Hexmaps, "Save directory uses working directory");
                    }
                    finally { Directory.SetCurrentDirectory(before); }
                });
                Test("library creation failure does not choose another location", w =>
                {
                    string app = PathFor("blocked-app"); Directory.CreateDirectory(app); string blocked = Path.Combine(app, "Library"); File.WriteAllText(blocked, "BLOCKED");
                    bool failed = false; try { LibraryPaths.EnsureDirectories(app); } catch (IOException) { failed = true; }
                    Check(failed && File.ReadAllText(blocked) == "BLOCKED", "Blocked library not reported/preserved");
                });
                Test("new map dialog always resets dimensions and displays limits", w =>
                {
                    using (var first = new NewMapDialog("FlatTop")) { ((NumericUpDown)Get(first, "cols")).Value = 200; ((NumericUpDown)Get(first, "rows")).Value = 1; }
                    using (var second = new NewMapDialog("FlatTop"))
                    {
                        var cols = (NumericUpDown)Get(second, "cols"); var rows = (NumericUpDown)Get(second, "rows");
                        Check(cols.Value == 15 && rows.Value == 10 && cols.Minimum == 1 && cols.Maximum == 200 && rows.Minimum == 1 && rows.Maximum == 200, "New dimensions/limits incorrect");
                        var labels = second.Controls[0].Controls.OfType<Label>().Select(l => l.Text).ToArray();
                        Check(labels.Contains("Kolumny") && labels.Contains("Wiersze"), "Axis labels changed");
                        var layout = (TableLayoutPanel)second.Controls[0]; second.PerformLayout(); layout.PerformLayout();
                        foreach (string rangeName in new[] { "columnsRange", "rowsRange" })
                        {
                            var range = (Label)layout.Controls[rangeName]; var numeric = rangeName == "columnsRange" ? cols : rows;
                            Check(range.Text == "(1–200)" && range.Font.Italic && layout.GetColumn(range) == 2, "Range is not a separate italic third column");
                            Check(range.Left > numeric.Right && range.Right <= layout.ClientSize.Width - layout.Padding.Right, "Range overlaps or is clipped");
                            Check(TextRenderer.MeasureText(range.Text, range.Font).Width <= range.Width, "Range text is clipped");
                        }
                        Check(((ComboBox)Get(second, "orient")).Items.Cast<string>().SequenceEqual(new[] { "Pointy Top", "Flat Top" }), "Orientation choices incorrect");
                    }
                    var created = (MapData)typeof(MainWindow).GetMethod("NewMap", Private).Invoke(null, new object[] { 15, 10, "PointyTop" });
                    Check(created.LabelMode == "Axes", "New map labels changed");
                });
                Test("orientation names match polygon, status and persisted identifiers", w =>
                {
                    var map = (MapData)Get(w, "map"); map.Cells[0].Name = "KEEP NAME";
                    foreach (string orientation in new[] { "FlatTop", "PointyTop" })
                    {
                        Call(w, "ChangeOrientation", orientation);
                        using (var path = (GraphicsPath)Call(w, "HexPath", new PointF(0, 0), 28f))
                        {
                            float top = path.PathPoints.Min(p => p.Y); int count = path.PathPoints.Count(p => Math.Abs(p.Y - top) < .001);
                            Check(count == (orientation == "FlatTop" ? 2 : 1), "Polygon top does not match orientation");
                        }
                        string display = OrientationNames.Display(orientation);
                        Check(((Button)Get(w, "orientationToggle")).Text == display && ((Label)Get(w, "statusLabel")).Text.Contains(display), "UI orientation mismatch");
                        Check(map.Orientation == orientation && map.Cells[0].Name == "KEEP NAME" && (bool)Get(w, "dirty"), "Orientation data/dirty state incorrect");
                    }
                });
                Console.WriteLine("Passed: " + passed + "; methods/control configuration only, not native dialog interaction."); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
            finally
            {
                // Delete only this run's unique, resolved directory next to the test EXE in tests/_work.
                if (!root.StartsWith(tempRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(root).StartsWith("HexMapEditor-technical-")) throw new InvalidOperationException("Unsafe test cleanup path");
                Directory.Delete(root, true);
            }
        }
    }
}
