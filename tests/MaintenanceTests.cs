using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class MaintenanceTests
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
        static string root; static int passed;
        static object Get(MainWindow w, string name) { return typeof(MainWindow).GetField(name, F).GetValue(w); }
        static void Set(MainWindow w, string name, object value) { typeof(MainWindow).GetField(name, F).SetValue(w, value); }
        static object Call(MainWindow w, string name, params object[] args) { return typeof(MainWindow).GetMethods(F).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(w, args); }
        static MapData Map(MainWindow w) { return (MapData)Get(w, "map"); }
        static int Count(MainWindow w, string name) { return ((ICollection)Get(w, name)).Count; }
        static void Check(bool condition, string why) { if (!condition) throw new Exception(why); }
        static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
        static string Dir(string name) { string dir = Path.Combine(root, name); Directory.CreateDirectory(dir); return dir; }
        static void Write(string file, TerrainPreset p) { File.WriteAllText(file, MapLogic.Serializer().Serialize(p)); }
        [STAThread] static int Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"); Directory.CreateDirectory(root);
            try
            {
                Test("migration recovers protected ID, exact backup, variants and idempotent second start", () =>
                {
                    string dir = Dir("migration"); var p = PresetCatalog.Default(); p.Terrains[1].Color = "#112233"; p.Terrains[1].VariantId = "synthetic-water-v2"; p.Terrains[1].Revision = 2;
                    string source = Path.Combine(dir, "old.terrainset.json"); Write(source, p); byte[] original = File.ReadAllBytes(source); string content = PresetLibrary.ContentKey(p);
                    string warning; List<string> actions; var loaded = PresetLibrary.LoadAndMigrate(dir, out warning, out actions);
                    Check(warning == "" && actions.Count == 1 && loaded.Count == 2 && !File.Exists(source), "Recovery failed: " + warning);
                    var recovered = loaded.Single(x => x.Id != PresetCatalog.DefaultId); Check(PresetLibrary.ContentKey(recovered) == content && recovered.Name != p.Name, "Recovery changed terrain data");
                    string backup = Directory.GetFiles(Path.Combine(dir, "Presets", "_backups")).Single(); Check(File.ReadAllBytes(backup).SequenceEqual(original), "Backup not byte identical");
                    string target = PresetCatalog.FindFile(Path.Combine(dir, "Presets"), recovered.Id); byte[] saved = File.ReadAllBytes(target);
                    loaded = PresetLibrary.LoadAndMigrate(dir, out warning, out actions); Check(warning == "" && actions.Count == 0 && loaded.Count == 2 && saved.SequenceEqual(File.ReadAllBytes(target)), "Migration repeated");
                });
                Test("protected recovery reuses one equivalent own entry and ignores backup subfolders", () =>
                {
                    string dir = Dir("equivalent"), active = PresetLibrary.DirectoryFor(dir); Directory.CreateDirectory(active);
                    var old = PresetCatalog.Default(); var own = old.Copy(); own.Id = "own-equivalent"; own.Name = "Own equivalent"; PresetCatalog.Write(own, active, false);
                    Write(Path.Combine(active, "protected.terrainset.json"), old); Directory.CreateDirectory(Path.Combine(active, "scratch")); File.WriteAllText(Path.Combine(active, "scratch", "bad.terrainset.json"), "bad");
                    string warning; List<string> actions; var loaded = PresetLibrary.LoadAndMigrate(dir, out warning, out actions);
                    Check(warning == "" && loaded.Count == 2 && Directory.GetFiles(active, "*.terrainset.json").Length == 1 && actions.Count == 1, "Equivalent entry duplicated or recursive scan");
                });
                Test("old own preset moves byte-for-byte and identical destination is not duplicated", () =>
                {
                    string dir = Dir("own-move"); var p = PresetCatalog.Default(); p.Id = "synthetic-own"; p.Name = "Own"; string source = Path.Combine(dir, "portable.terrainset.json"); Write(source, p); byte[] bytes = File.ReadAllBytes(source);
                    string warning; List<string> actions; PresetLibrary.LoadAndMigrate(dir, out warning, out actions); string active = PresetLibrary.DirectoryFor(dir), target = PresetCatalog.FindFile(active, p.Id);
                    Check(warning == "" && bytes.SequenceEqual(File.ReadAllBytes(target)), "Move altered bytes"); File.WriteAllBytes(source, bytes);
                    var loaded = PresetLibrary.LoadAndMigrate(dir, out warning, out actions); Check(warning == "" && !File.Exists(source) && loaded.Count == 2 && Directory.GetFiles(active, "*.terrainset.json").Length == 1, "Duplicate migration");
                });
                Test("ambiguous ID, invalid file and failed backup leave source unchanged and warn", () =>
                {
                    string dir = Dir("conflict"), active = PresetLibrary.DirectoryFor(dir); Directory.CreateDirectory(active); var p = PresetCatalog.Default(); p.Id = "conflict"; p.Name = "Conflict"; PresetCatalog.Write(p, active, false);
                    var changed = p.Copy(); changed.Name = "Different"; string source = Path.Combine(dir, "legacy.terrainset.json"); Write(source, changed); byte[] bytes = File.ReadAllBytes(source);
                    string warning; List<string> actions; PresetLibrary.LoadAndMigrate(dir, out warning, out actions); Check(warning.Contains("Konflikt ID") && actions.Count == 0 && File.ReadAllBytes(source).SequenceEqual(bytes), "Ambiguous source changed");
                    string bad = Path.Combine(active, "invalid.terrainset.json"); File.WriteAllText(bad, "{\"Name\":\"missing identity\"}"); PresetLibrary.LoadAndMigrate(dir, out warning, out actions); Check(warning.Contains("invalid.terrainset.json") && File.Exists(bad), "Invalid file hidden/removed");
                    string blocked = Dir("backup-error"); Directory.CreateDirectory(PresetLibrary.DirectoryFor(blocked)); File.WriteAllText(Path.Combine(PresetLibrary.DirectoryFor(blocked), "_backups"), "KEEP"); source = Path.Combine(blocked, "protected.terrainset.json"); Write(source, PresetCatalog.Default()); bytes = File.ReadAllBytes(source);
                    PresetLibrary.LoadAndMigrate(blocked, out warning, out actions); Check(warning.Length > 0 && bytes.SequenceEqual(File.ReadAllBytes(source)) && Directory.GetFiles(PresetLibrary.DirectoryFor(blocked), "*.terrainset.json").Length == 0, "Backup failure changed data");
                });
                Test("portable manual file loads next start; name and ID collisions are reported", () =>
                {
                    string dir = Dir("portable"), active = PresetLibrary.DirectoryFor(dir); Directory.CreateDirectory(active); var p = new TerrainPreset { Name = "Portable" }; p.Terrains.Add(PresetCatalog.Neutral()); Write(Path.Combine(active, "hand-copied.terrainset.json"), p);
                    string warning; List<string> actions; var loaded = PresetLibrary.LoadAndMigrate(dir, out warning, out actions); Check(loaded.Count == 2 && warning == "" && actions.Count == 0, "Manual file not loaded");
                    var duplicate = p.Copy(); Write(Path.Combine(active, "z-id.terrainset.json"), duplicate); duplicate.Id = "different"; Write(Path.Combine(active, "z-name.terrainset.json"), duplicate);
                    loaded = PresetLibrary.LoadAndMigrate(dir, out warning, out actions); Check(loaded.Count == 2 && warning.Contains("identyfikator") && warning.Contains("nazwa"), "Collision silently accepted");
                });
                Test("new preset immediately has one selected saved entry; first legacy save does not duplicate", () =>
                {
                    using (var w = new MainWindow())
                    {
                        string dir = Dir("create"); object[] args = { dir, "Created", null }; Check((bool)Call(w, "TryCreatePreset", args), "Create failed: " + args[2]); string id = Map(w).ActivePresetId;
                        Check(Map(w).Presets.Count(p => p.Id == id) == 1 && Map(w).Presets.Last().Terrains.Count == 1 && !((Label)Get(w, "presetStateLabel")).Text.Contains("niezapisany"), "False/duplicate entry");
                        string before = MapLogic.Serializer().Serialize(Map(w)); args = new object[] { dir, "CREATED", null }; Check(!(bool)Call(w, "TryCreatePreset", args) && before == MapLogic.Serializer().Serialize(Map(w)), "Create collision altered map");
                        args = new object[] { dir, true, "Intentional copy", null }; Check((bool)Call(w, "TrySaveWorkingPreset", args) && Map(w).ActivePresetId != id, "Saved preset copy missing own ID");
                        var legacy = new TerrainPreset { Name = "Legacy unsaved" }; legacy.Terrains.Add(PresetCatalog.Neutral()); Map(w).Presets.Add(legacy); Map(w).ActivePresetId = legacy.Id; int count = Map(w).Presets.Count;
                        args = new object[] { dir, true, "First saved", null }; Check((bool)Call(w, "TrySaveWorkingPreset", args) && Map(w).Presets.Count == count && Map(w).ActivePresetId == legacy.Id && Map(w).Presets.Count(p => p.Id == legacy.Id) == 1, "First save duplicated entry");
                    }
                });
                Test("preset delete failure/cancel preserves state; Undo/Redo never touch library files", () =>
                {
                    using(var w=new MainWindow())
                    {
                        string dir=Dir("delete"); object[] create={dir,"Synthetic deletion",null}; Check((bool)Call(w,"TryCreatePreset",create),"Create fixture failed");
                        string id=Map(w).ActivePresetId, path=PresetCatalog.FindFile(dir,id); var saved=PresetCatalog.ReadFile(path); string file=File.ReadAllText(path);
                        Call(w,"AddTerrain",new TerrainDefinition {Name="Working unsaved"}); string before=MapLogic.Serializer().Serialize(Map(w)); int undo=Count(w,"undo");
                        object[] failure={dir,new Action<string>(p=>{throw new IOException("synthetic failure");}),null};
                        Check(!(bool)Call(w,"TryDeletePreset",failure) && before==MapLogic.Serializer().Serialize(Map(w)) && Count(w,"undo")==undo && File.ReadAllText(path)==file,"Delete failure changed map/file/history");
                        object[] cancel={dir,new Action<string>(p=>{}),null}; Check(!(bool)Call(w,"TryDeletePreset",cancel) && before==MapLogic.Serializer().Serialize(Map(w)),"Cancelled delete altered map");
                        string archived=Path.Combine(root,"own-delete-archive.json"); int diskCalls=0;
                        object[] remove={dir,new Action<string>(p=>{diskCalls++;File.Move(p,archived);}),null}; Check((bool)Call(w,"TryDeletePreset",remove) && !File.Exists(path),"Delete transaction failed");
                        Check(Map(w).ActivePresetId==PresetCatalog.DefaultId,"No default selection after removal");
                        for(int n=0;n<3;n++){Call(w,"Undo");Check(Map(w).ActivePresetId==id && ((Label)Get(w,"presetStateLabel")).Text.Contains("niezapisany") && Map(w).Presets.Single(p=>p.Id==id).Terrains.Count==2,"Undo did not restore unsaved working palette");Call(w,"Redo");}
                        Check(diskCalls==1 && !File.Exists(path) && File.ReadAllText(archived)==file,"History wrote or removed files");
                        object[] protectedArgs={dir,new Action<string>(p=>{throw new Exception("Must not reach disk");}),null}; Check(!(bool)Call(w,"TryDeletePreset",protectedArgs),"Default deletion allowed");
                    }
                });
                Test("single-column palette has permanent external actions and no unrelated rebuild", () =>
                {
                    using(var w=new MainWindow())
                    {
                        w.Size=w.MinimumSize; w.Show(); Application.DoEvents(); var panel=(FlowLayoutPanel)Get(w,"paletteTiles"); var tiles=panel.Controls.Cast<TerrainTile>().ToArray();
                        foreach(var tile in tiles) Check(tile.Choose.Right<tile.Edit.Left && tile.Edit.Right<tile.Delete.Left && tile.Delete.Right<=tile.Width && tile.Edit.Visible && tile.Delete.Visible && tile.Choose.AutoEllipsis,"Actions overlap name or require hover");
                        Call(w,"SelectTerrain",Map(w).Presets[0].Terrains[1].TerrainId); Call(w,"RefreshPresetPicker"); Check(tiles.SequenceEqual(panel.Controls.Cast<TerrainTile>()),"Unrelated refresh recreated rows");
                        var left=(Panel)Get(w,"left"); Check(!left.HorizontalScroll.Visible && panel.Controls.Cast<Control>().All(c=>c.Right<=panel.Width),"Palette requires horizontal scroll"); w.Close();
                    }
                });
                Test("full long cell labels fit either hex without wrapping or partial clipping", () =>
                {
                    using(var bitmap=new Bitmap(1800,1600)) using(var graphics=Graphics.FromImage(bitmap)) using(var w=new MainWindow())
                    {
                        foreach(string orientation in new[]{"PointyTop","FlatTop"}) { Map(w).Orientation=orientation;
                            foreach(float scale in new[]{.8f,1f,1.5f,4f}) foreach(string label in new[]{"A1","Z49","AA50","GR200"})
                            {
                                var center=new PointF(800,700); RectangleF bounds; float pixels; Check(CellLabels.Layout(graphics,label,center,28*scale,scale,new Rectangle(0,0,1800,1600),out bounds,out pixels),"Normal label hidden: "+label+" "+scale);
                                using(var path=(System.Drawing.Drawing2D.GraphicsPath)Call(w,"HexPath",center,28*scale)) foreach(var p in new[]{new PointF(bounds.Left,bounds.Top),new PointF(bounds.Right,bounds.Top),new PointF(bounds.Left,bounds.Bottom),new PointF(bounds.Right,bounds.Bottom)}) Check(path.IsVisible(p),"Label rectangle escaped hex");
                            }
                        }
                        RectangleF rect;float size; Check(!CellLabels.Layout(graphics,"GR200",new PointF(100,100),28*.79f,.79f,new Rectangle(0,0,1800,1600),out rect,out size),"Tiny scale showed incomplete label");
                        Check(!CellLabels.Layout(graphics,"GR200",new PointF(1,100),28,1,new Rectangle(0,0,1800,1600),out rect,out size),"Viewport edge clipped identifier");
                        Check(MapLogic.Coordinate(199*200+199,200)=="GR200" && MapLogic.Coordinate(49*200+26,200)=="AA50","Logical coordinate changed");
                        var button=(Button)Get(w,"labelToggle"); Map(w).LabelMode="InCells";Call(w,"UpdateUI");Check(button.Text=="W heksach" && button.Width>=110,"Label button truncated");
                    }
                });
                Test("removal groups are disjoint, protect true terrain identity and keep old coordinates", () =>
                {
                    var map=new MapData {Columns=4,Rows=4}; for(int i=0;i<16;i++)map.Cells.Add(new CellData());
                    MapLogic.ApplyTerrain(map.Cells[0],PresetCatalog.Default().Terrains[1]); map.Cells[1].Name="Name only"; MapLogic.ApplyTerrain(map.Cells[2],PresetCatalog.Default().Terrains[1]);map.Cells[2].Description="Text";
                    map.Cells[3].TerrainId="same-color-but-other-terrain";map.Cells[4].RichDescription=NoteCodec.Write(NoteCodec.FromPlain("formatowany opis"));
                    var plan=MapLogic.ResizeAll(map,-1,0,-1,0);var groups=RemovalGroup.For(map,plan.RemovedIndices.Concat(plan.RemovedIndices));
                    Check(plan.RemovedIndices.Count==7 && groups.Sum(g=>g.Coordinates.Length)==5 && groups.SelectMany(g=>g.Coordinates).Distinct().Count()==5,"Duplicate count or missing user data");
                    Check(groups.Single(g=>g.Kind==CellContentKind.Terrain).Coordinates.SequenceEqual(new[]{"A1","D1"}) && groups.Single(g=>g.Kind==CellContentKind.Text).Coordinates.SequenceEqual(new[]{"B1","A2"}) && groups.Single(g=>g.Kind==(CellContentKind.Terrain|CellContentKind.Text)).Coordinates.Single()=="C1","Wrong groups/old indices");
                    using(var warning=new ResizeWarningDialog(7,groups)) Check(warning.GroupCoordinates.Count==3 && warning.GroupCoordinates.Sum(t=>t.Text.Split(',').Length)==5 && ((Button)warning.AcceptButton).DialogResult==DialogResult.No,"Group UI lost coordinates/safe default");
                });
                Test("spatial resize controls surround local diagram and retain atomic validation", () =>
                {
                    using(var d=new ResizeDialog(15,10))
                    {
                        d.Show(); Application.DoEvents(); var fields=new[]{"topDelta","bottomDelta","leftDelta","rightDelta"}.Select(n=>(NumericUpDown)typeof(ResizeDialog).GetField(n,F).GetValue(d)).ToArray();
                        var diagram=d.Controls[0].Controls.OfType<ResizeDiagram>().Single(); Func<Control,Rectangle> bounds=c=>new Rectangle(d.PointToClient(c.PointToScreen(Point.Empty)),c.Size);
                        Check(bounds(fields[0]).Bottom<bounds(diagram).Top && bounds(fields[1]).Top>bounds(diagram).Bottom && bounds(fields[2]).Right<bounds(diagram).Left && bounds(fields[3]).Left>bounds(diagram).Right,"Fields not spatial: "+String.Join(";",fields.Select(c=>bounds(c).ToString()))+" diagram="+bounds(diagram));
                        fields[0].Value=-10;Check(!((Button)d.AcceptButton).Enabled,"Removed full old axis");
                    }
                });
                Test("New and Open reset map, palette, redo, name and description histories", () =>
                {
                    using(var w=new MainWindow())
                    {
                        Action populate=()=>{Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");((TextBox)Get(w,"cellName")).Text="history name";var editor=(RichNoteEditor)Get(w,"cellDescription");editor.Mutate("paste",()=>editor.Box.Selection.Text="history description");Set(w,"brushIndex",1);Call(w,"PaintCell",0);Call(w,"FinishStroke");Call(w,"AddTerrain",new TerrainDefinition());Call(w,"Undo");};
                        Func<int> texts=()=>((ICollection)typeof(TextHistoryStore).GetField("entries",F).GetValue(Get(w,"textHistory"))).Count;
                        populate();Check(Count(w,"undo")>0 && Count(w,"redo")>0 && texts()==2,"History fixture missing");Call(w,"StartNewProject",15,10,"PointyTop","Fresh",PresetCatalog.Default());Check(Count(w,"undo")==0 && Count(w,"redo")==0 && texts()==0,"New kept histories");
                        string file=Path.Combine(root,"fresh.hexmap");object[] save={file,true,null};Check((bool)Call(w,"TrySaveTo",save),"Save fixture");populate();object[] open={file,null,false};Check((bool)Call(w,"TryLoadProject",open) && Count(w,"undo")==0 && Count(w,"redo")==0 && texts()==0,"Open kept histories");
                    }
                });
                Test("context plain paste keeps Unicode/newlines, one text Undo and no map history", () =>
                {
                    var previous=System.Windows.Clipboard.GetDataObject();
                    try { using(var w=new MainWindow())
                    {
                        Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");var editor=(RichNoteEditor)Get(w,"cellDescription");string original=editor.ContentJson;
                        var clipboard=new System.Windows.DataObject();clipboard.SetData(System.Windows.DataFormats.UnicodeText,"Zażółć gęślą\r\njaźń\t50");clipboard.SetData(System.Windows.DataFormats.Rtf,@"{\rtf1\ansi\b FORMATTED}");System.Windows.Clipboard.SetDataObject(clipboard,true);
                        editor.Box.ContextMenu.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.ContextMenu.OpenedEvent));Check(editor.PlainPasteItem.IsEnabled,"Plain text disabled");editor.PlainPasteItem.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
                        string pasted=editor.ContentJson;Check(NoteCodec.Plain(pasted).Replace("\r\n","\n").Contains("Zażółć gęślą\njaźń\t50") && !pasted.Contains("\"Bold\":true") && !pasted.Contains("\"Kind\":\"table\"") && Count(w,"undo")==0,"Plain paste transferred format or changed map stack");
                        Call(w,"TextUndo","description",false);Check(editor.ContentJson==original,"Paste not one Undo");Call(w,"TextUndo","description",true);Check(editor.ContentJson==pasted,"Paste Redo failed");
                        System.Windows.Clipboard.Clear();editor.Box.SelectAll();editor.Box.ContextMenu.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.ContextMenu.OpenedEvent));Check(!editor.PlainPasteItem.IsEnabled,"Empty clipboard enabled");editor.Paste(true);Check(editor.ContentJson==pasted,"Empty paste removed selection");
                        Check(editor.Box.ContextMenu.Items.OfType<System.Windows.Controls.MenuItem>().Count()==4,"Standard context actions lost");
                    }} finally { if(previous!=null)System.Windows.Clipboard.SetDataObject(previous,true); else System.Windows.Clipboard.Clear(); }
                });
                Test("each shrinking grip move repaints exposed canvas before release with empty/table notes", () =>
                {
                    using(var w=new MainWindow())
                    {
                        w.Show();Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");var canvas=(Control)Get(w,"canvas");var editor=(RichNoteEditor)Get(w,"cellDescription");
                        foreach(bool table in new[]{false,true})
                        {
                            if(table)editor.InsertTable(2,3);Call(w,"CaptureSavedState");Call(w,"SetRightWidth",700);Application.DoEvents();
                            string before=MapLogic.Serializer().Serialize(Map(w));object camera=Get(w,"pan");object document=editor.Box.Document;int paints=0;
                            PaintEventHandler observe=(s,e)=>{if((bool)Get(w,"rightResizing")){paints++;Check(e.ClipRectangle.Width<canvas.Width,"Grip repainted entire canvas");}};canvas.Paint+=observe;
                            Call(w,"BeginRightResize",0);
                            for(int x=10;x<=300;x+=10){int previous=paints;Call(w,"MoveRightResize",x);Check(paints>previous,"Canvas waited until mouse release");}
                            Call(w,"EndRightResize");canvas.Paint-=observe;
                            Check(before==MapLogic.Serializer().Serialize(Map(w)) && camera.Equals(Get(w,"pan")) && Object.ReferenceEquals(document,editor.Box.Document) && Count(w,"undo")==0 && !(bool)Get(w,"dirty"),"Drag changed data, camera, history or rebuilt document");
                        }
                        w.Close();
                    }
                });
                Console.WriteLine("Passed: " + passed + "; 0.2.2 methods/control tests, not interactive UI."); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
            finally { Directory.Delete(root, true); }
        }
    }
}
