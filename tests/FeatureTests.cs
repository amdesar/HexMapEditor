using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class FeatureTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
        static int passed; static string root;
        static object Get(MainWindow w, string n) { return typeof(MainWindow).GetField(n, Flags).GetValue(w); }
        static void Set(MainWindow w, string n, object value) { typeof(MainWindow).GetField(n, Flags).SetValue(w, value); }
        static object Call(MainWindow w, string n, params object[] args) { return typeof(MainWindow).GetMethods(Flags).Single(m => m.Name == n && m.GetParameters().Length == args.Length).Invoke(w, args); }
        static MapData Map(MainWindow w) { return (MapData)Get(w, "map"); }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static int Count(MainWindow w, string n) { return ((ICollection)Get(w, n)).Count; }
        static void Select(MainWindow w, int i) { Set(w, "selected", i); Set(w, "rightPanelOpen", true); Call(w, "UpdateUI"); }
        static void Paint(MainWindow w, int brush, params int[] indices) { Set(w, "brushIndex", brush); foreach (int i in indices) Call(w, "PaintCell", i); Call(w, "FinishStroke"); }
        static void Test(string name, Action<MainWindow> action) { using (var w = new MainWindow()) action(w); passed++; Console.WriteLine("PASS " + name); }
        static void Save(MainWindow w, string name) { object[] args = { Path.Combine(root, name), true, null }; Check((bool)Call(w, "TrySaveTo", args), "Save failed: " + args[2]); }
        static void Load(MainWindow w, string name, bool migration) { object[] args = { Path.Combine(root, name), null, false }; Check((bool)Call(w, "TryLoadProject", args), "Load failed: " + args[1]); Check((bool)args[2] == migration, "Migration flag"); }
        static RichNoteEditor Editor(MainWindow w) { var e = (RichNoteEditor)Get(w, "cellDescription"); e.Error = message => { throw new Exception("Editor: " + message); }; return e; }
        static string Notes(string text) { return NoteCodec.Write(NoteCodec.FromPlain(text)); }
        static void Description(MainWindow w, string text) { string doc = Notes(text); Call(w, "DescriptionEdited", doc, NoteCodec.Plain(doc), "paste"); }
        [STAThread] static int Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"); Directory.CreateDirectory(root);
            try
            {
                Test("filename owns display name, one final extension only", w =>
                {
                    Map(w).Name = "Old internal name"; Save(w, "mapa.txt.HEXMAP"); Check(((Label)Get(w, "mapName")).Text == "mapa.txt", "Wrong displayed name");
                    using (var dialog = (SaveFileDialog)Call(w, "CreateSaveDialog")) Check(dialog.FileName == "mapa.txt.hexmap", "Wrong suggested name");
                    File.Copy(Path.Combine(root, "mapa.txt.HEXMAP"), Path.Combine(root, "renamed.hexmap")); Load(w, "renamed.hexmap", false); Check(((Label)Get(w, "mapName")).Text == "renamed", "Internal Name won");
                });
                Test("initial name suggested and cancelled overwrite preserves label", w =>
                {
                    Map(w).Name = "Początek"; Call(w, "MarkDirty"); using (var d = (SaveFileDialog)Call(w, "CreateSaveDialog")) Check(d.FileName == "Początek.hexmap", "Initial name not suggested");
                    string before = ((Label)Get(w, "mapName")).Text; string path = Path.Combine(root, "refused.hexmap"); File.WriteAllText(path, "KEEP");
                    Check(!(bool)Call(w, "SaveAsTo", path, new Func<string, DialogResult>(p => DialogResult.No)), "Refusal ignored"); Check(((Label)Get(w, "mapName")).Text == before && File.ReadAllText(path) == "KEEP", "Refusal changed title/data");
                });
                Test("rich tables, numbering and all styles survive file roundtrip", w =>
                {
                    Select(w, 0); var e = Editor(w); e.Mutate("paste", () => { var d = NoteCodec.FromPlain("Akapit"); var run = (Run)((Paragraph)d.Blocks.FirstBlock).Inlines.FirstInline; run.FontWeight = System.Windows.FontWeights.Bold; run.FontStyle = System.Windows.FontStyles.Italic; run.FontSize = 24; run.TextDecorations = new System.Windows.TextDecorationCollection { System.Windows.TextDecorations.Underline[0], System.Windows.TextDecorations.Strikethrough[0] }; ((Paragraph)d.Blocks.FirstBlock).TextAlignment = System.Windows.TextAlignment.Center; var list = new System.Windows.Documents.List { MarkerStyle = System.Windows.TextMarkerStyle.Decimal }; list.ListItems.Add(new ListItem(new Paragraph(new Run("pierwszy")))); d.Blocks.Add(list); e.Box.Document.Blocks.Clear(); while (d.Blocks.FirstBlock != null) { var block = d.Blocks.FirstBlock; d.Blocks.Remove(block); e.Box.Document.Blocks.Add(block); } });
                    e.InsertTable(2, 3); var table = e.Box.Document.Blocks.OfType<Table>().Single(); e.Mutate("typing", () => ((Paragraph)table.RowGroups[0].Rows[0].Cells[0].Blocks.FirstBlock).Inlines.Add(new Run("komórka")));
                    string expected = Map(w).Cells[0].RichDescription; Save(w, "rich.hexmap"); Load(w, "rich.hexmap", false);
                    Check(Map(w).Cells[0].RichDescription == expected && Map(w).Cells[0].Description.Contains("komórka"), "Formatted content lost");
                    var model = NoteCodec.Read(expected); var styled = model.Blocks.First().Children.First(); Check(styled.Bold && styled.Italic && styled.Underline && styled.Strike && styled.Size == 24, "Styles lost"); Check(model.Blocks.Any(n => n.Kind == "list" && n.Marker == "decimal"), "Numbering lost"); Check(NoteCodec.FromJson(expected).Blocks.OfType<Table>().Single().RowGroups[0].Rows[0].Cells.Count == 3, "Not a real table");
                });
                Test("real table cell and row/column editing, full deletion, Undo", w =>
                {
                    Select(w, 0); var e = Editor(w); e.InsertTable(2, 2); e.ChangeTable("addRow"); e.ChangeTable("addColumn"); var t = e.Box.Document.Blocks.OfType<Table>().Single(); Check(t.RowGroups[0].Rows.Count == 3 && t.RowGroups[0].Rows[0].Cells.Count == 3, "Table growth failed");
                    e.ChangeTable("removeColumn"); e.ChangeTable("removeRow"); e.ChangeTable("remove"); Check(!e.Box.Document.Blocks.OfType<Table>().Any(), "Table not removed"); Call(w, "TextUndo", "description", false); Check(e.Box.Document.Blocks.OfType<Table>().Any(), "Table Undo failed");
                });
                Test("RTF paste preserves supported data and strips objects, themes and field instructions", w =>
                {
                    Select(w, 0); var e = Editor(w); e.PasteRtf(@"{\rtf1\ansi plain \b bold\b0\par\trowd\cellx1500\cellx3000 one\cell two\cell\row}", "plain bold\none two");
                    Check(NoteCodec.Read(e.ContentJson).Blocks.Any(n => n.Kind == "table") && e.ContentJson.Contains("\"Bold\":true"), "RTF format/table lost");
                    e.SetContent(null, ""); e.PasteRtf(@"{\rtf1 before {\object\objdata 1234} after}", "before after"); Check(NoteCodec.Plain(e.ContentJson).Contains("before") && NoteCodec.Plain(e.ContentJson).Contains("after") && !e.ContentJson.Contains("1234"), "Object was not safely simplified");
                    e.SetContent(null, ""); e.PasteRtf(@"{\rtf1\ansi {\*\themedata 1234}\b bold\b0 {\field{\*\fldinst HYPERLINK \""https://example.invalid\""}{\fldrslt visible}}}", "bold visible"); Check(e.ContentJson.Contains("\"Bold\":true") && NoteCodec.Plain(e.ContentJson).Contains("visible") && !e.ContentJson.Contains("https"), "Theme/field simplification lost supported content");
                    e.SetContent(null, ""); e.PasteRtf(@"{\rtf1 before {\object{\*\objdata 1234}{\result readable}} after}", "before readable after"); Check(NoteCodec.Plain(e.ContentJson).Contains("readable") && !e.ContentJson.Contains("1234"), "Object result text lost");
                });
                Test("description multilevel history, formatting steps, save dirty", w =>
                {
                    Select(w, 0); Description(w, "one"); Description(w, "two"); Description(w, "three"); Save(w, "text-history.hexmap");
                    Call(w, "TextUndo", "description", false); Check(Map(w).Cells[0].Description == "two" && (bool)Get(w, "dirty"), "First Undo"); Call(w, "TextUndo", "description", false); Check(Map(w).Cells[0].Description == "one", "Second Undo");
                    Call(w, "TextUndo", "description", true); Call(w, "TextUndo", "description", true); Check(Map(w).Cells[0].Description == "three" && !(bool)Get(w, "dirty"), "Redo to save");
                    Call(w, "TextUndo", "description", false); Description(w, "branch"); Call(w, "TextUndo", "description", true); Check(Map(w).Cells[0].Description == "branch", "Redo not invalidated");
                });
                Test("description Undo restores exact original plain format and clean state", w => { Select(w, 0); Description(w, "new"); Call(w, "TextUndo", "description", false); Check(Map(w).Cells[0].NotesFormat == "plainText" && Map(w).Cells[0].RichDescription == null && !(bool)Get(w, "dirty"), "Original representation not restored"); });
                Test("name history is multilevel and isolated per hex and field", w =>
                {
                    Select(w, 0); var name = (TextBox)Get(w, "cellName"); name.Text = "A"; ((TextHistoryStore)Get(w, "textHistory")).BreakGroups(); name.Text = "AB"; Description(w, "opis A"); Select(w, 1); name.Text = "B"; Description(w, "opis B");
                    Select(w, 0); Call(w, "TextUndo", "name", false); Check(Map(w).Cells[0].Name == "A" && Map(w).Cells[1].Name == "B" && Map(w).Cells[0].Description == "opis A", "Histories mixed"); Call(w, "TextUndo", "name", false); Check(Map(w).Cells[0].Name == "", "No multilevel name Undo"); Call(w, "TextUndo", "name", true); Check(Map(w).Cells[0].Name == "A", "Name Redo");
                });
                Test("opening resets text history; it is absent from map file", w => { Select(w, 0); Description(w, "saved"); Save(w, "no-history.hexmap"); Check(!File.ReadAllText(Path.Combine(root, "no-history.hexmap")).Contains("States"), "History serialized"); Load(w, "no-history.hexmap", false); Select(w, 0); Call(w, "TextUndo", "description", false); Check(Map(w).Cells[0].Description == "saved" && !(bool)Get(w, "dirty"), "Open retained old Undo"); });
                Test("text grouping, branching and bounded snapshot memory", w =>
                {
                    var h = new TextHistory(""); h.Record("a", "typing"); h.Record("ab", "typing"); Check(h.Undo() == "", "Typing not grouped"); h.Redo(); h.BreakGroup(); h.Record("abc", "typing"); Check(h.Undo() == "ab", "Groups not separated"); h.Record("x", "paste"); Check(h.Redo() == "x", "Branch retained Redo");
                    for (int i = 0; i < 150; i++) h.Record(i.ToString(), "format"); Check(h.States.Count == 101, "Step cap failed"); var store = new TextHistoryStore(); var big = store.For("a", "description", new string('x', 9000000)); big.Record(new string('y', 9000000), "paste"); big.Record(new string('z', 9000000), "paste"); store.Trim(); Check(big.Bytes <= TextHistoryStore.Budget, "Budget failed");
                });
                Test("preset persists offline, default recreated, deletion leaves map readable", w =>
                {
                    var dir = Path.Combine(root, "presets"); var p = PresetCatalog.Default(); p.Id = Guid.NewGuid().ToString("N"); p.Name = "Mój zestaw"; PresetCatalog.Save(p, dir); string warning; var loaded = PresetCatalog.Load(dir, out warning); Check(loaded.Any(x => x.Id == p.Id) && loaded.Any(x => x.Id == PresetCatalog.DefaultId), "Preset/default missing");
                    Paint(w, 1, 0); Save(w, "self-contained.hexmap"); foreach (var f in Directory.GetFiles(dir)) File.Delete(f); Load(w, "self-contained.hexmap", false); Check(Map(w).Cells[0].Terrain == "Woda" && Map(w).Presets[0].Terrains.Count == 6, "Map depended on local preset");
                });
                Test("terrain revision update is opt-in and matches exact variant", w =>
                {
                    Paint(w, 1, 0, 1); Select(w, 0); Description(w, "keep"); ((TextBox)Get(w, "cellName")).Text = "keep name";
                    var p = Map(w).Presets[0]; var old = p.Terrains[1]; var next = old.Copy(); next.Revision++; next.VariantId = Guid.NewGuid().ToString("N"); next.Name = "Ocean"; next.Color = "#123456";
                    Call(w, "UpdateTerrain", old, next, false); Check(Map(w).Cells[0].Terrain == "Woda", "Default update changed old map"); Paint(w, 1, 2);
                    var third = next.Copy(); third.VariantId = Guid.NewGuid().ToString("N"); third.Revision++; third.Name = "Ocean nowy"; int before = Count(w, "undo"); Call(w, "UpdateTerrain", next, third, true); Check(Count(w, "undo") == before + 1 && Map(w).Cells[2].Terrain == "Ocean nowy" && Map(w).Cells[0].Terrain == "Woda", "Wrong revision updated");
                    Call(w, "Undo"); Check(Map(w).Cells[2].VariantId == next.VariantId && Map(w).Cells[0].Name == "keep name" && Map(w).Cells[0].Description == "keep", "Terrain Undo lost independent data"); p.Terrains.RemoveAt(1); Check(Map(w).Cells[0].Terrain == "Woda", "Delete palette changed map");
                });
                Test("apply terrain version updates all exact users as one Undo, never text", w =>
                {
                    Paint(w, 2, 0, 1, 2); var old = Map(w).Presets[0].Terrains[2]; var next = old.Copy(); next.Revision++; next.VariantId = Guid.NewGuid().ToString("N"); next.Color = "#111111"; Map(w).Cells[1].Name = "keep";
                    Call(w, "UpdateTerrain", old, next, true); Check(Map(w).Cells.Take(3).All(c => c.VariantId == next.VariantId), "Update incomplete"); Call(w, "Undo"); Check(Map(w).Cells.Take(3).All(c => c.VariantId == old.VariantId) && Map(w).Cells[1].Name == "keep", "Undo incomplete"); Call(w, "Redo"); Check(Map(w).Cells.Take(3).All(c => c.VariantId == next.VariantId), "Redo incomplete");
                });
                foreach (string orientation in new[] { "PointyTop", "FlatTop" })
                {
                    string o = orientation;
                    Test("six real symmetric neighbors, geometry and parity: " + o, w =>
                    {
                        var m = Map(w); m.Orientation = o;
                        foreach (int parity in new[] { 0, 1 }) { m.RowParity = m.ColumnParity = parity; for (int i = 0; i < m.Cells.Count; i++) foreach (int n in MapLogic.Neighbors(m, i)) { Check(MapLogic.Neighbors(m, n).Contains(i), "Asymmetric neighbors"); var a = (PointF)Call(w, "Center", i % m.Columns, i / m.Columns); var b = (PointF)Call(w, "Center", n % m.Columns, n / m.Columns); double distance = Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y)); Check(Math.Abs(distance - Math.Sqrt(3)*28) < .01, "Not geometrical neighbors"); } } Check(MapLogic.Neighbors(m, m.Columns*4+4).Count() == 6, "Not six neighbors");
                    });
                    Test("bucket separated region, exact variants, one Undo/no-op: " + o, w =>
                    {
                        var m = Map(w); m.Orientation = o; Paint(w, 1, 0, m.Cells.Count - 1); m.Cells[0].Name = "keep"; m.Cells[0].Description = "notes"; Set(w, "brushIndex", 2); int before = Count(w, "undo"); Call(w, "FillRegion", 0); Check(m.Cells[0].Terrain == "Równina" && m.Cells.Last().Terrain == "Woda" && Count(w, "undo") == before + 1, "Flood crossed disconnected area"); Call(w, "Undo"); Check(m.Cells[0].Name == "keep" && m.Cells[0].Description == "notes", "Flood lost notes"); Set(w, "brushIndex", 1); Call(w, "FillRegion", 0); Check(Count(w, "redo") == 1, "No-op flood removed Redo");
                        var same = m.Cells[1]; MapLogic.ApplyTerrain(same, m.Presets[0].Terrains[1]); same.VariantId = "different-same-color"; Check(!MapLogic.Flood(m, 0).Contains(1), "Flood matched only name/color");
                    });
                    foreach (string side in new[] { "top", "bottom", "left", "right" })
                    {
                        string s = side;
                        Test("resize preserve layout/data/history add/remove: " + o + " " + s, w =>
                        {
                            var m = Map(w); m.Orientation = o; Select(w, m.Columns + 1); var original = m.Cells.ToArray(); var target = m.Cells[m.Columns + 1]; ((TextBox)Get(w, "cellName")).Text = "named"; Description(w, "described"); var positions = original.ToDictionary(c => c.Id, c => { int i = m.Cells.IndexOf(c); return (PointF)Call(w, "Center", i % m.Columns, i / m.Columns); });
                            int removed, data; Call(w, "ApplyResize", MapLogic.Resize(m, s, 1, out removed, out data)); Check(original.All(c => m.Cells.Contains(c)), "Preserved identity lost"); var anchor = m.Cells.IndexOf(original[0]); var p = (PointF)Call(w, "Center", anchor % m.Columns, anchor / m.Columns); var shift = new PointF(p.X - positions[original[0].Id].X, p.Y - positions[original[0].Id].Y);
                            foreach (var c in original) { int i = m.Cells.IndexOf(c); var now = (PointF)Call(w, "Center", i%m.Columns, i/m.Columns); Check(Math.Abs(now.X-positions[c.Id].X-shift.X)<.01 && Math.Abs(now.Y-positions[c.Id].Y-shift.Y)<.01, "Parity changed relative layout"); }
                            Check(m.Cells.Where(c => !original.Contains(c)).All(c => !MapLogic.HasUserData(c)), "New cells copied content"); Call(w, "Undo"); Check(m.Cells.SequenceEqual(original), "Resize Undo data/order"); Call(w, "Redo"); Call(w, "TextUndo", "description", false); Check(target.Description == "" && target.Name == "named", "Text history moved to another cell"); Call(w, "TextUndo", "description", true); Check(target.Description == "described", "Text Redo lost on resize");
                            var state = MapLogic.Resize(m, s, -1, out removed, out data); Call(w, "ApplyResize", state); Check(m.Cells.SequenceEqual(original) && target.Description == "described", "Remove did not restore original rectangle");
                        });
                    }
                }
                Test("resize warns accurate data counts and leaves preview side-effect-free", w => { var m = Map(w); m.Cells[0].Name = "data"; m.Cells[1].Description = "notes"; m.Cells[2].Color = "#000000"; string before = MapLogic.Serializer().Serialize(m); int cut,data; MapLogic.Resize(m, "top", -1, out cut, out data); Check(cut == 15 && data == 3 && MapLogic.Serializer().Serialize(m) == before && Count(w, "undo") == 0, "Preview changed map/counts"); foreach (int d in new[] { -10, 191 }) { bool rejected = false; try { MapLogic.Resize(m,"top",d,out cut,out data); } catch (InvalidDataException) { rejected = true; } Check(rejected,"Invalid dimensions accepted"); } });
                Test("paint -> resize -> paint and removal Undo are coherent by stable cell ID", w => { var m = Map(w); Paint(w,1,0); int a,b; Call(w,"ApplyResize",MapLogic.Resize(m,"top",-1,out a,out b)); Paint(w,2,0); Call(w,"Undo"); Call(w,"Undo"); Check(m.Cells[0].Terrain=="Woda", "Removed terrain not restored"); Call(w,"Undo"); Check(m.Cells[0].Terrain=="Teren neutralny", "Pre-resize paint Undo hit wrong cell"); Call(w,"Redo"); Call(w,"Redo"); Call(w,"Redo"); Check(m.Cells[0].Terrain=="Równina" && m.Rows==9, "Combined Redo wrong"); });
                Test("legacy format 1 migration retains exact plain text, deterministic variants", w => { var m = Map(w); m.FormatVersion=1; m.Cells[0].Description="Old\r\nplain\ttext"; m.Cells[1].Terrain="Custom"; m.Cells[1].Color="#123456"; File.WriteAllText(Path.Combine(root,"legacy.hexmap"),MapLogic.Serializer().Serialize(m)); Load(w,"legacy.hexmap",true); Check(Map(w).Cells[0].Description=="Old\r\nplain\ttext" && Map(w).Cells[0].NotesFormat=="plainText" && Map(w).Cells[1].VariantId==PresetCatalog.Legacy("Custom","#123456").VariantId,"Migration loss"); Save(w,"migrated.hexmap"); Load(w,"migrated.hexmap",false); });
                Test("invalid rich data/future format reject before replacing current map", w => { string before=MapLogic.Serializer().Serialize(Map(w)); var bad=MapLogic.Serializer().Deserialize<MapData>(before); bad.FormatVersion=99; File.WriteAllText(Path.Combine(root,"bad.hexmap"),MapLogic.Serializer().Serialize(bad)); object[] args={Path.Combine(root,"bad.hexmap"),null,false}; Check(!(bool)Call(w,"TryLoadProject",args) && before==MapLogic.Serializer().Serialize(Map(w)),"Invalid load replaced map"); bool rejected=false; try { NoteCodec.Read("{\"SchemaVersion\":1,\"Blocks\":[{\"Kind\":\"script\"}]}"); } catch {rejected=true;} Check(rejected,"Active document node accepted"); });
                Test("actual minimal format 1 without new IDs or palettes migrates safely", w =>
                {
                    string old = "{\"FormatVersion\":1,\"Name\":\"Old\",\"Columns\":1,\"Rows\":1,\"Orientation\":\"PointyTop\",\"Cells\":[{\"Terrain\":\"Woda\",\"Color\":\"#287DB3\",\"Name\":\"Harbor\",\"Description\":\"old plain\\r\\ntext\",\"NotesFormat\":\"plainText\"}]}";
                    File.WriteAllText(Path.Combine(root,"minimal-old.hexmap"),old); Load(w,"minimal-old.hexmap",true); Check(Map(w).Cells[0].Description=="old plain\r\ntext" && Map(w).Cells[0].Name=="Harbor" && Map(w).Cells[0].VariantId=="default-water-v1" && Map(w).LabelMode=="Axes","Minimal old map lost data");
                });
                Test("format 2 requires explicit persisted IDs and immutable variants", w =>
                {
                    string original=MapLogic.Serializer().Serialize(Map(w)); var raw=(Dictionary<string,object>)MapLogic.Serializer().DeserializeObject(original); raw.Remove("FormatVersion");
                    foreach (int step in new[] { 0, 1, 2 })
                    {
                        if(step==1) { raw=(Dictionary<string,object>)MapLogic.Serializer().DeserializeObject(original); ((Dictionary<string,object>)((object[])raw["Cells"])[0]).Remove("VariantId"); }
                        if(step==2) { raw=(Dictionary<string,object>)MapLogic.Serializer().DeserializeObject(original); ((Dictionary<string,object>)((object[])raw["Cells"])[0])["Color"]="#000000"; }
                        string path=Path.Combine(root,"invalid-identity.hexmap"); File.WriteAllText(path,MapLogic.Serializer().Serialize(raw)); object[] args={path,null,false}; Check(!(bool)Call(w,"TryLoadProject",args) && original==MapLogic.Serializer().Serialize(Map(w)),"Invalid identity accepted or replaced map");
                    }
                });
                Test("resize saved dirty state and text edits survive resize Undo/Redo", w =>
                {
                    var m=Map(w); int a,b; var cell=m.Cells[16]; Call(w,"ApplyResize",MapLogic.Resize(m,"left",1,out a,out b)); Save(w,"resize-saved.hexmap"); Call(w,"Undo"); Check((bool)Get(w,"dirty"),"Resize Undo not dirty"); Call(w,"Redo"); Check(!(bool)Get(w,"dirty"),"Resize Redo did not return saved state"); Select(w,m.Cells.IndexOf(cell)); Description(w,"later text"); Call(w,"Undo"); Call(w,"Redo"); Check(cell.Description=="later text" && (bool)Get(w,"dirty"),"Resize history replaced later text");
                });
                Test("empty palette painting preserves map Redo", w => { Paint(w,1,0); Call(w,"Undo"); Map(w).Presets[0].Terrains.Clear(); Set(w,"brushIndex",0); Call(w,"FillRegion",0); Check(Count(w,"redo")==1 && Map(w).Cells.All(c=>c.Terrain=="Teren neutralny"),"Empty palette edited map/history"); });
                Test("200x200 flood iterative, complete and bounded", w => { var m=(MapData)Call(w,"NewMap",200,200,"PointyTop"); Check(MapLogic.Flood(m,0).Count==40000,"Large flood incomplete"); m.Orientation="FlatTop"; Check(MapLogic.Flood(m,39999).Count==40000,"Large flat flood incomplete"); });
                Test("minimum layout avoids overlapping toolbar and right controls", w => { w.Size=w.MinimumSize; w.PerformLayout(); Call(w,"PositionOverlays"); var toolbar=(Panel)Get(w,"shellToolbar"); var flows=toolbar.Controls.OfType<FlowLayoutPanel>().ToArray(); Check(flows[0].Right<=flows[1].Left,"Toolbar groups overlap"); foreach(var f in flows) foreach(Control c in f.Controls) Check(c.Right<=f.ClientSize.Width,"Toolbar control clipped"); Check(!((Label)Get(w,"mapName")).CanSelect,"Project title editable"); });
                Console.WriteLine("Passed: " + passed + "; method/control tests only, no Word or native-dialog test."); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
            finally { Directory.Delete(root, true); }
        }
    }
}
