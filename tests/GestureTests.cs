using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using HexMapEditor;

namespace HexMapEditor.Tests
{
    // Replay event sequences directly; real native dialog checks are reported separately.
    internal static class GestureTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static int passed;
        static object Get(MainWindow w, string name) { return typeof(MainWindow).GetField(name, Private).GetValue(w); }
        static void Set(MainWindow w, string name, object value) { typeof(MainWindow).GetField(name, Private).SetValue(w, value); }
        static object Call(MainWindow w, string name, params object[] args) { return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(w, args); }
        static DialogResult Modal(MainWindow w, Func<DialogResult> show) { return (DialogResult)typeof(MainWindow).GetMethod("RunModal", Private).MakeGenericMethod(typeof(DialogResult)).Invoke(w, new object[] { show }); }
        static int Count(MainWindow w, string name) { return ((ICollection)Get(w, name)).Count; }
        static MapData Map(MainWindow w) { return (MapData)Get(w, "map"); }
        static Point CellPoint(MainWindow w, int col) { return Point.Round((PointF)Call(w, "ScreenCenter", col, 0)); }
        static void Mouse(MainWindow w, string kind, Point point, MouseButtons button, int clicks = 1) { Call(w, "CanvasMouse" + kind, Get(w, "canvas"), new MouseEventArgs(button, clicks, point.X, point.Y, 0)); }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void CheckClean(MainWindow w)
        {
            Check(!(bool)Get(w, "dirty") && Count(w, "undo") == 0 && Count(w, "redo") == 0, "Unexpected dirty state/history");
            Check((int)Get(w, "selected") == -1 && !(bool)Get(w, "rightPanelOpen"), "Unexpected selection");
            Check(new JavaScriptSerializer().Serialize(Map(w)) == (string)Get(w, "savedSnapshot"), "Map data changed");
        }
        static void Test(string name, Action<MainWindow> body)
        {
            using (var w = new MainWindow()) body(w);
            passed++; Console.WriteLine("PASS " + name);
        }
        [STAThread]
        static int Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Test("orphan Move/Up cannot paint, select or pan with any tool", w =>
                {
                    var pan = (PointF)Get(w, "pan");
                    foreach (EditorTool tool in new[] { EditorTool.Brush, EditorTool.Select, EditorTool.Hand })
                    {
                        Set(w, "currentTool", tool);
                        Mouse(w, "Move", CellPoint(w, 0), MouseButtons.Left, 0); Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                        Mouse(w, "Move", CellPoint(w, 1), MouseButtons.Middle, 0); Mouse(w, "Up", CellPoint(w, 1), MouseButtons.Middle);
                        CheckClean(w); Check(pan.Equals(Get(w, "pan")), "Orphan gesture moved camera");
                    }
                });
                Test("recorded 0.1.3 dialog-return -> Move(Left) -> Up sequence is inert", w =>
                {
                    Set(w, "currentTool", EditorTool.Brush); Check(Modal(w, () => DialogResult.OK) == DialogResult.OK, "Modal result changed");
                    Mouse(w, "Move", CellPoint(w, 0), MouseButtons.Left, 0); Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                    CheckClean(w); Check((EditorTool)Get(w, "currentTool") == EditorTool.Brush, "Tool changed");
                });
                Test("first fresh canvas stroke after a modal paints and groups cells", w =>
                {
                    Set(w, "currentTool", EditorTool.Brush); Modal(w, () => DialogResult.OK);
                    Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left); Mouse(w, "Move", CellPoint(w, 1), MouseButtons.Left, 0); Mouse(w, "Up", CellPoint(w, 1), MouseButtons.Left);
                    Check(Map(w).Cells.Take(2).All(c => c.Terrain == "Równina"), "Fresh stroke did not paint");
                    Check(Count(w, "undo") == 1 && (bool)Get(w, "dirty"), "Fresh stroke history/dirty state incorrect");
                    Check((MouseButtons)Get(w, "gestureButton") == MouseButtons.None && !((Control)Get(w, "canvas")).Capture, "Stroke kept capture/state");
                });
                Test("held modal button must be released before starting a canvas gesture", w =>
                {
                    Set(w, "currentTool", EditorTool.Brush); Modal(w, () => DialogResult.OK); Set(w, "waitingForMouseRelease", true);
                    Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left); Mouse(w, "Move", CellPoint(w, 0), MouseButtons.Left, 0); CheckClean(w);
                    Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                    Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left); Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                    Check(Count(w, "undo") == 1, "Fresh stroke after release was blocked");
                });
                Test("double-click tail after a modal is not a fresh canvas Down", w =>
                {
                    Set(w, "currentTool", EditorTool.Brush); Modal(w, () => DialogResult.OK);
                    Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left, 2); Mouse(w, "Move", CellPoint(w, 0), MouseButtons.Left, 0); Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                    CheckClean(w);
                    Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left); Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                    Check(Count(w, "undo") == 1, "Single click after rejected tail was blocked");
                });
                Test("fresh Select click works after cancellation; orphan Up does not toggle it", w =>
                {
                    Modal(w, () => DialogResult.Cancel);
                    Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left); CheckClean(w);
                    Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left); Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                    Check((int)Get(w, "selected") == 0 && (bool)Get(w, "rightPanelOpen"), "Fresh selection failed");
                    Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left);
                    Check((int)Get(w, "selected") == 0 && !(bool)Get(w, "dirty"), "Orphan Up toggled selection");
                });
                Test("fresh Hand, Select drag and middle-button pan remain available", w =>
                {
                    foreach (EditorTool tool in new[] { EditorTool.Hand, EditorTool.Select, EditorTool.Brush })
                    {
                        Set(w, "currentTool", tool); Modal(w, () => DialogResult.Cancel); Point start = CellPoint(w, 0), end = new Point(start.X + 40, start.Y + 20);
                        MouseButtons button = tool == EditorTool.Brush ? MouseButtons.Middle : MouseButtons.Left; var before = (PointF)Get(w, "pan");
                        Mouse(w, "Down", start, button); Mouse(w, "Move", end, button, 0); Mouse(w, "Up", end, button);
                        Check(!before.Equals(Get(w, "pan")), "Fresh pan was blocked"); CheckClean(w);
                    }
                });
                Test("modal entry completes an existing owned stroke once and releases capture", w =>
                {
                    Set(w, "currentTool", EditorTool.Brush); Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left);
                    Set(w, "dragging", true); Set(w, "panGesture", true);
                    Modal(w, () =>
                    {
                        Check((MouseButtons)Get(w, "gestureButton") == MouseButtons.None && !(bool)Get(w, "dragging") && !(bool)Get(w, "panGesture"), "Stale gesture survived modal entry");
                        Check(!((Control)Get(w, "canvas")).Capture && Count(w, "strokeBefore") == 0, "Capture/pending stroke survived");
                        Check(Count(w, "undo") == 1, "Existing stroke not completed once");
                        Mouse(w, "Down", CellPoint(w, 1), MouseButtons.Left); Mouse(w, "Move", CellPoint(w, 1), MouseButtons.Left, 0); Mouse(w, "Up", CellPoint(w, 1), MouseButtons.Left);
                        Check(Map(w).Cells[1].Terrain == "Teren neutralny", "Modal callback edited canvas"); return DialogResult.Cancel;
                    });
                    Mouse(w, "Up", CellPoint(w, 1), MouseButtons.Left); Check(Count(w, "undo") == 1, "Dialog tail created another operation");
                });
                Test("nested warnings and exceptional modal exits always reset gesture state", w =>
                {
                    Set(w, "currentTool", EditorTool.Brush);
                    Modal(w, () => { Modal(w, () => DialogResult.No); Check((int)Get(w, "modalDepth") == 1, "Nested modal depth incorrect"); Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left); return DialogResult.Cancel; });
                    bool failed = false; try { Modal(w, () => { throw new InvalidOperationException("controlled test exception"); }); } catch (TargetInvocationException) { failed = true; }
                    Check(failed && (int)Get(w, "modalDepth") == 0 && (MouseButtons)Get(w, "gestureButton") == MouseButtons.None, "Exceptional modal cleanup failed");
                    Mouse(w, "Move", CellPoint(w, 0), MouseButtons.Left, 0); Mouse(w, "Up", CellPoint(w, 0), MouseButtons.Left); CheckClean(w);
                });
                Test("capture loss ends owned stroke and rejects later Move/Up", w =>
                {
                    Set(w, "currentTool", EditorTool.Brush); Mouse(w, "Down", CellPoint(w, 0), MouseButtons.Left);
                    var canvas = (Control)Get(w, "canvas"); Check(canvas.Capture, "Test could not obtain control capture"); canvas.Capture = false;
                    Check((MouseButtons)Get(w, "gestureButton") == MouseButtons.None && Count(w, "undo") == 1, "Capture loss left active stroke");
                    Mouse(w, "Move", CellPoint(w, 1), MouseButtons.Left, 0); Mouse(w, "Up", CellPoint(w, 1), MouseButtons.Left);
                    Check(Map(w).Cells[1].Terrain == "Teren neutralny" && Count(w, "undo") == 1, "Orphan events continued a lost-capture stroke");
                });
                Console.WriteLine("Passed: " + passed + "; method/event replay tests, not native dialog interaction."); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
        }
    }
}
