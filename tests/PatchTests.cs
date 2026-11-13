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
    internal static class PatchTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
        static int passed; static string root;
        static object Get(MainWindow w, string n) { return typeof(MainWindow).GetField(n, Flags).GetValue(w); }
        static void Set(MainWindow w, string n, object value) { typeof(MainWindow).GetField(n, Flags).SetValue(w, value); }
        static object Call(MainWindow w, string n, params object[] args) { return typeof(MainWindow).GetMethods(Flags).Single(m => m.Name == n && m.GetParameters().Length == args.Length).Invoke(w, args); }
        static MapData Map(MainWindow w) { return (MapData)Get(w,"map"); }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static int Count(MainWindow w,string n) { return ((ICollection)Get(w,n)).Count; }
        static void Test(string name, Action<MainWindow> body) { using(var w=new MainWindow()) body(w); passed++; Console.WriteLine("PASS " + name); }
        static void UseMap(MainWindow w,int cols,int rows,string orientation) { Set(w,"map",Call(w,"NewMap",cols,rows,orientation)); Set(w,"selected",-1); Set(w,"editorCellId",null); Call(w,"CaptureSavedState"); Call(w,"UpdateUI"); }
        static void Paint(MainWindow w,int brush,params int[] cells) { Set(w,"brushIndex",brush); foreach(int cell in cells) Call(w,"PaintCell",cell); Call(w,"FinishStroke"); }
        [STAThread] static int Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data"); Directory.CreateDirectory(root);
            try
            {
                Test("one toolbar row and centered ellipsis title at minimum/large size",w=>
                {
                    foreach(var size in new[]{w.MinimumSize,new Size(2200,1400)}) { w.Size=size; Call(w,"PositionOverlays"); var bar=(Panel)Get(w,"shellToolbar"); var label=(Label)Get(w,"mapName"); var left=(FlowLayoutPanel)Get(w,"fileGroup"); var right=(FlowLayoutPanel)Get(w,"viewGroup");
                        Check(bar.Height==42 && Math.Abs(label.Top+label.Height/2-left.Top-left.Height/2)<=1 && label.Height<2*label.Font.Height && label.Right<right.Left && label.Left>left.Right && label.Width>80,"Toolbar overlap/extra row"); Check(label.AutoEllipsis && label.TextAlign==ContentAlignment.MiddleCenter,"Name not centered/ellipsized"); foreach(var group in new[]{left,right}) foreach(Button b in group.Controls) Check(b.Height==32 && b.Right<=group.Width,"Button alignment/clipping"); }
                });
                foreach(string orientation in new[]{"PointyTop","FlatTop"})
                {
                    string o=orientation;
                    Test("fit exceeds 170/400 percent and wheel changes smoothly: "+o,w=>
                    { w.Size=new Size(2200,1500); UseMap(w,2,2,o); Call(w,"FitMap"); float scale=(float)Get(w,"scale"); Check(scale>4,"Fit still capped"); var canvas=(Control)Get(w,"canvas"); Call(w,"CanvasMouseWheel",canvas,new MouseEventArgs(MouseButtons.None,0,canvas.Width/2,canvas.Height/2,120)); Check(Math.Abs((float)Get(w,"scale")/scale-1.12)<.001,"First wheel jumped"); });
                    Test("fit contains tiny, large and thin maps with either left panel state: "+o,w=>
                    { foreach(var dims in new[]{new Size(1,1),new Size(15,10),new Size(200,200),new Size(1,200),new Size(200,1)}) foreach(bool collapsed in new[]{false,true}) { UseMap(w,dims.Width,dims.Height,o); Set(w,"leftCollapsed",collapsed); Call(w,"PositionOverlays"); Call(w,"FitMap"); var bounds=(RectangleF)Call(w,"MapBounds"); var pan=(PointF)Get(w,"pan"); float scale=(float)Get(w,"scale"); var canvas=(Control)Get(w,"canvas"); Check(pan.X+bounds.Left*scale>=(collapsed?28:220)+31 && pan.Y+bounds.Top*scale>=31 && pan.X+bounds.Right*scale<=canvas.Width-31 && pan.Y+bounds.Bottom*scale<=canvas.Height-31,"Map escaped fit rectangle"); Set(w,"rightPanelOpen",true); Call(w,"FitMap"); Check(Math.Abs((float)Get(w,"scale")-scale)<.00001 && pan.Equals(Get(w,"pan")),"Right overlay changed fit"); } });
                }
                Test("right panel width persists, clamps and does not move camera",w=>
                { Set(w,"selected",0); Set(w,"rightPanelOpen",true); Call(w,"UpdateUI"); var pan=Get(w,"pan"); var scale=Get(w,"scale"); Call(w,"SetRightWidth",650); Check(((Panel)Get(w,"right")).Width==650,"Width not applied"); Call(w,"CloseDetailsPanel"); Set(w,"selected",0); Set(w,"rightPanelOpen",true); Call(w,"UpdateUI"); Check(((Panel)Get(w,"right")).Width==650,"Width lost on reopen"); Call(w,"SetRightWidth",1); Check(((Panel)Get(w,"right")).Width==340,"No minimum"); Call(w,"SetRightWidth",99999); Check(((Panel)Get(w,"right")).Width<w.ClientSize.Width-200,"No maximum"); Check(pan.Equals(Get(w,"pan")) && scale.Equals(Get(w,"scale")) && !(bool)Get(w,"dirty"),"Panel changed map/camera"); Call(w,"SetRightWidth",440); Check(((Panel)Get(w,"right")).Right==w.ClientSize.Width,"Right edge moved"); });
                Test("panel shrink/reset recalculates scroll extent and table width without editing",w=>
                {
                    w.Show(); Set(w,"selected",0); Set(w,"rightPanelOpen",true); Call(w,"UpdateUI");
                    var editor=(RichNoteEditor)Get(w,"cellDescription"); editor.InsertTable(2,3); Call(w,"CaptureSavedState");
                    string before=MapLogic.Serializer().Serialize(Map(w)); int history=Count(w,"undo");
                    var table=editor.Box.Document.Blocks.OfType<System.Windows.Documents.Table>().Single();
                    foreach(var size in new[]{new Size(1440,940),w.MinimumSize})
                    {
                        w.Size=size;
                        foreach(int width in new[]{440,700,440,340,700,440})
                        {
                            foreach(var column in table.Columns) column.Width=new System.Windows.GridLength(150);
                            Call(w,"SetRightWidth",width); Application.DoEvents(); editor.FitTables();
                            var scroll=((Panel)Get(w,"right")).Controls.OfType<DetailsScrollPanel>().Single();
                            Check(!scroll.HorizontalScroll.Visible && scroll.Content.Right<=scroll.ClientSize.Width-14,"Stale horizontal extent or clipped editor");
                            Check(scroll.Content.Controls.Cast<Control>().All(c=>c.Right<=scroll.Content.ClientSize.Width),"A detail control extends beyond its content panel");
                            Check(table.Columns.All(c=>c.Width.IsAuto),"Table retained fixed width");
                            Check(before==MapLogic.Serializer().Serialize(Map(w)) && Count(w,"undo")==history && !(bool)Get(w,"dirty"),"Layout edited map/history");
                        }
                    }
                    w.Close();
                });
                Test("resize grip owns gesture; orphan canvas events cannot paint/select/pan",w=>
                { Set(w,"selected",0); Set(w,"rightPanelOpen",true); Call(w,"UpdateUI"); var canvas=(Control)Get(w,"canvas"); var point=Point.Round((PointF)Call(w,"ScreenCenter",2,2)); string before=MapLogic.Serializer().Serialize(Map(w)); var pan=Get(w,"pan");
                    foreach(EditorTool tool in Enum.GetValues(typeof(EditorTool))) { Set(w,"currentTool",tool); Call(w,"BeginRightResize",1000); Call(w,"MoveRightResize",900); Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)); Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.Left,0,point.X+20,point.Y+20,0)); Call(w,"EndRightResize"); Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)); Check(before==MapLogic.Serializer().Serialize(Map(w)) && Count(w,"undo")==0 && pan.Equals(Get(w,"pan")),"Grip gesture edited canvas"); }
                    Set(w,"currentTool",EditorTool.Brush); Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.None,0,point.X,point.Y,0)); Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)); Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0)); Check(Count(w,"undo")==1,"Fresh gesture blocked after grip");
                });
                Test("palette selection and unrelated updates preserve controls and layout",w=>
                {
                    var panel=(FlowLayoutPanel)Get(w,"paletteTiles"); var original=panel.Controls.Cast<Control>().ToArray(); int adds=0,removes=0; var heights=new List<int>(); panel.ControlAdded+=(s,e)=>adds++; panel.ControlRemoved+=(s,e)=>removes++; panel.SizeChanged+=(s,e)=>heights.Add(panel.Height);
                    foreach(var terrain in Map(w).Presets[0].Terrains) Call(w,"SelectTerrain",terrain.TerrainId); Call(w,"RefreshPresetPicker"); Call(w,"UpdateUI");
                    Check(adds==0 && removes==0 && original.SequenceEqual(panel.Controls.Cast<Control>()) && heights.Count==0,"Unchanged palette rebuilt/flickered"); Check(original.All(c=>c.Height==32 && c.Width==panel.Width) && panel.FlowDirection==FlowDirection.TopDown && !panel.WrapContents,"Palette not one compact column");
                    var add=panel.Parent.Controls.OfType<Button>().Single(b=>b.Text=="Dodaj teren"); int y=add.Top; Call(w,"AddTerrain",new TerrainDefinition { Name="Extra" }); Check(add.Top==y && original.All(c=>!c.IsDisposed),"Adding moved header/recreated old tiles");
                });
                Test("tile actions are accessible, do not select, and never resize tile",w=>
                {
                    int choose=0,edit=0,delete=0; using(var tile=new TerrainTile(id=>choose++,id=>edit++,id=>delete++)) { tile.UpdateTerrain(PresetCatalog.Default().Terrains[1],false); var bounds=tile.Bounds; typeof(Control).GetMethod("OnMouseEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tile,new object[]{EventArgs.Empty}); Check(choose+edit+delete==0 && tile.Bounds==bounds,"Hover triggered action/layout"); typeof(Button).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tile.Edit,new object[]{EventArgs.Empty}); Check(edit==1 && choose==0 && delete==0,"Edit also selected/deleted"); typeof(Button).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tile.Delete,new object[]{EventArgs.Empty}); Check(delete==1 && choose==0 && tile.Bounds==bounds,"Delete action also selected/layout"); Check(tile.Edit.AccessibleName.Contains("Woda") && tile.Delete.AccessibleName.Contains("Woda") && tile.Choose.TabStop && tile.Edit.TabStop && tile.Delete.TabStop,"Actions lack accessible keyboard controls"); }
                });
                Test("required light paint -> dark palette -> dark paint chronological Undo/Redo",w=>
                {
                    Paint(w,1,0); var old=Map(w).Presets[0].Terrains[1].Copy(); var next=old.Copy(); next.Color="#102030"; next.VariantId=Guid.NewGuid().ToString("N"); next.Revision++;
                    Call(w,"UpdateTerrain",old,next,false); Paint(w,1,1); Map(w).Cells[0].Name="preserve"; Map(w).Cells[1].Description="later notes";
                    Call(w,"Undo"); Check(Map(w).Cells[1].VariantId==PresetCatalog.NeutralVariant && Map(w).Presets[0].Terrains[1].VariantId==next.VariantId,"First Undo wrong operation");
                    Call(w,"Undo"); Check(Map(w).Presets[0].Terrains[1].VariantId==old.VariantId && Map(w).Cells[0].VariantId==old.VariantId,"Second Undo did not restore light palette");
                    Call(w,"Redo"); Call(w,"Redo"); Check(Map(w).Presets[0].Terrains[1].VariantId==next.VariantId && Map(w).Cells[1].VariantId==next.VariantId && Map(w).Cells[0].Name=="preserve" && Map(w).Cells[1].Description=="later notes","Redo regenerated version/lost notes");
                });
                Test("palette and map terrain update is one atomic operation",w=>
                {
                    Paint(w,1,0,1); var old=Map(w).Presets[0].Terrains[1].Copy(); var next=old.Copy(); next.Name="Dark water"; next.VariantId=Guid.NewGuid().ToString("N"); next.Revision++; Map(w).Cells[0].Description="keep"; int count=Count(w,"undo");
                    Call(w,"UpdateTerrain",old,next,true); Check(Count(w,"undo")==count+1,"Atomic operation split"); Call(w,"Undo"); Check(Map(w).Presets[0].Terrains[1].VariantId==old.VariantId && Map(w).Cells.Take(2).All(c=>c.VariantId==old.VariantId),"Undo missed map or palette"); Call(w,"Redo"); Check(Map(w).Presets[0].Terrains[1].VariantId==next.VariantId && Map(w).Cells.Take(2).All(c=>c.VariantId==next.VariantId) && Map(w).Cells[0].Description=="keep","Atomic Redo lost version/text");
                });
                Test("palette deletion restores order/selection; no-op preserves Redo",w=>
                {
                    var p=Map(w).Presets[0]; string[] ids=p.Terrains.Select(t=>t.TerrainId).ToArray(); Call(w,"SelectTerrain",ids[3]); Call(w,"DeleteTerrain",ids[1]); Check((int)Get(w,"brushIndex")==2,"Deleting other tile changed selection"); Call(w,"Undo"); Check(Map(w).Presets[0].Terrains.Select(t=>t.TerrainId).SequenceEqual(ids) && (int)Get(w,"brushIndex")==3,"Delete Undo order/selection wrong");
                    var old=Map(w).Presets[0].Terrains[1].Copy(); var same=old.Copy(); same.VariantId=Guid.NewGuid().ToString("N"); Call(w,"UpdateTerrain",old,same,true); Check(Count(w,"redo")==1,"No-op removed Redo"); Call(w,"AddTerrain",new TerrainDefinition()); Check(Count(w,"redo")==0,"Real palette action kept Redo"); Call(w,"Undo"); Check(Map(w).Presets[0].Terrains.Count==6,"Add Undo failed");
                });
                Test("protected default cannot be overwritten even after rename; old disk override ignored",w=>
                {
                    string dir=Path.Combine(root,"protected"); Directory.CreateDirectory(dir); var p=Map(w).Presets[0]; p.Name="Renamed working copy"; Call(w,"MarkDirty"); string before=MapLogic.Serializer().Serialize(Map(w)); object[] args={dir,false,null,null}; Check(!(bool)Call(w,"TrySaveWorkingPreset",args) && before==MapLogic.Serializer().Serialize(Map(w)) && (bool)Get(w,"dirty"),"Default bypassed by rename or failure changed state"); Check(Directory.GetFiles(dir).Length==0,"Default wrote file");
                    var modified=PresetCatalog.Default(); modified.Terrains[0].Name="OLD MODIFIED"; string file=Path.Combine(dir,"old-default.terrainset.json"); File.WriteAllText(file,MapLogic.Serializer().Serialize(modified)); string warning; var loaded=PresetCatalog.Load(dir,out warning); Check(loaded.Single(x=>x.Id==PresetCatalog.DefaultId).Terrains[0].Name=="Teren neutralny" && !String.IsNullOrEmpty(warning) && File.Exists(file),"Stored default replaced template or was removed");
                    using(var dialog=new SavePresetDialog(p,true)) Check(!dialog.Controls[0].Controls.OfType<RadioButton>().First().Enabled && dialog.AsNew,"Default overwrite UI enabled");
                });
                Test("save new copies working palette to own ID; collision and cancel do not change state",w=>
                {
                    string dir=Path.Combine(root,"save-new"); var original=Map(w).Presets[0]; original.Terrains.RemoveAt(5); original.Name="Working"; object[] save={dir,true,"My saved set",null}; Check((bool)Call(w,"TrySaveWorkingPreset",save),"Save new failed: "+save[3]); string id=Map(w).ActivePresetId; Check(id!=PresetCatalog.DefaultId && Map(w).Presets.Last().Terrains.Count==5,"Copy not preserved/new identity missing"); string path=Directory.GetFiles(dir).Single(); string content=File.ReadAllText(path); string before=MapLogic.Serializer().Serialize(Map(w)); int count=Count(w,"undo"); object[] collision={dir,true,"MY SAVED SET",null}; Check(!(bool)Call(w,"TrySaveWorkingPreset",collision) && before==MapLogic.Serializer().Serialize(Map(w)) && count==Count(w,"undo") && File.ReadAllText(path)==content,"Name collision overwrote or changed map");
                    using(var dialog=new SavePresetDialog(Map(w).Presets.Last(),false)) Check(((Button)dialog.CancelButton).DialogResult==DialogResult.Cancel && dialog.AsNew,"Unsaved/cancel configuration");
                    Call(w,"Undo"); Check(Map(w).ActivePresetId==PresetCatalog.DefaultId && File.ReadAllText(path)==content,"Undo changed preset file"); Call(w,"Redo"); Check(Map(w).ActivePresetId==id,"Redo created new preset identity");
                });
                Test("explicit custom overwrite, dirty indicators and Undo never write disk",w=>
                {
                    string dir=Path.Combine(root,"overwrite"); object[] first={dir,true,"Custom",null}; Check((bool)Call(w,"TrySaveWorkingPreset",first),"New fixture"); object[] saveMap={Path.Combine(root,"project.hexmap"),true,null}; Check((bool)Call(w,"TrySaveTo",saveMap),"Project fixture"); var p=Map(w).Presets.Last(); var old=p.Terrains[1].Copy(); var next=old.Copy(); next.Color="#010203"; next.VariantId=Guid.NewGuid().ToString("N"); next.Revision++; Call(w,"UpdateTerrain",old,next,false); object[] overwrite={dir,false,null,null}; Check((bool)Call(w,"TrySaveWorkingPreset",overwrite),"Explicit overwrite failed"); Check((bool)Get(w,"dirty"),"Saving preset marked project saved"); string path=Directory.GetFiles(dir).Single(); string saved=File.ReadAllText(path); Call(w,"Undo"); Check(!(bool)Get(w,"dirty") && ((Label)Get(w,"presetStateLabel")).Text.Contains("różni") && File.ReadAllText(path)==saved,"Undo changed disk or incorrect project/preset status");
                    Call(w,"Redo"); Check(!((Label)Get(w,"presetStateLabel")).Text.Contains("różni"),"Redo did not return to stored palette");
                    using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None)) { string before=MapLogic.Serializer().Serialize(Map(w)); Check(!(bool)Call(w,"TrySaveWorkingPreset",overwrite) && before==MapLogic.Serializer().Serialize(Map(w)),"Write failure modified map"); }
                    Check(File.ReadAllText(path)==saved,"Write failure changed preset");
                });
                Test("unsaved custom cannot overwrite and new preset write error leaves identity/history",w=>
                {
                    var p=new TerrainPreset { Name="Unsaved" }; p.Terrains.Add(PresetCatalog.Neutral()); Map(w).Presets.Add(p); Map(w).ActivePresetId=p.Id; Call(w,"MarkDirty"); string before=MapLogic.Serializer().Serialize(Map(w)); object[] overwrite={Path.Combine(root,"unsaved"),false,null,null}; Check(!(bool)Call(w,"TrySaveWorkingPreset",overwrite) && before==MapLogic.Serializer().Serialize(Map(w)),"Unsaved overwrite allowed"); string blocked=Path.Combine(root,"blocked"); File.WriteAllText(blocked,"KEEP"); object[] failed={blocked,true,"New",null}; Check(!(bool)Call(w,"TrySaveWorkingPreset",failed) && before==MapLogic.Serializer().Serialize(Map(w)) && Count(w,"undo")==0 && File.ReadAllText(blocked)=="KEEP","New write failure altered identity/history");
                });
                foreach(string orientation in new[]{"PointyTop","FlatTop"})
                {
                    string o=orientation;
                    Test("four-sided mixed resize preserves IDs/parity/text and old paint history: "+o,w=>
                    {
                        UseMap(w,5,4,o); var m=Map(w); m.RowParity=m.ColumnParity=1; for(int i=0;i<m.Cells.Count;i++) { m.Cells[i].Name=MapLogic.Coordinate(i,m.Columns); m.Cells[i].Description="note "+i; } var original=m.Cells.ToArray(); var positions=original.ToDictionary(c=>c.Id,c=>{ int i=m.Cells.IndexOf(c); return (PointF)Call(w,"Center",i%m.Columns,i/m.Columns); }); Paint(w,1,5);
                        var plan=MapLogic.ResizeAll(m,-1,2,1,-2); Check(plan.RemovedIndices.Count==11 && plan.DataIndices.Count==11 && plan.RemovedIndices.Distinct().Count()==11,"Corner count incorrect"); string[] coordinates=plan.DataIndices.Select(i=>MapLogic.Coordinate(i,m.Columns)).ToArray(); Check(coordinates.Contains("A1") && coordinates.Contains("E4"),"Pre-operation coordinates wrong");
                        Call(w,"ApplyResize",plan.State); Check(m.Columns==4 && m.Rows==5 && m.Cells[1].Id==original[5].Id && m.Cells[1].Name=="A2","Wrong content/index mapping"); var anchor=(PointF)Call(w,"Center",1,0); var shift=new PointF(anchor.X-positions[original[5].Id].X,anchor.Y-positions[original[5].Id].Y);
                        foreach(var c in m.Cells.Where(c=>positions.ContainsKey(c.Id))) { int i=m.Cells.IndexOf(c); var now=(PointF)Call(w,"Center",i%m.Columns,i/m.Columns); Check(Math.Abs(now.X-positions[c.Id].X-shift.X)<.01 && Math.Abs(now.Y-positions[c.Id].Y-shift.Y)<.01,"Parity shifted preserved content"); }
                        Check(m.Cells.Where(c=>!positions.ContainsKey(c.Id)).All(c=>!MapLogic.HasUserData(c)),"New content not neutral"); original[5].Description="later"; Call(w,"Undo"); Check(m.Cells.SequenceEqual(original) && m.Rows==4 && original[5].Description=="later","Resize Undo lost data"); Call(w,"Undo"); Check(original[5].VariantId==PresetCatalog.NeutralVariant && original[5].Description=="later","Older paint Undo wrong cell/text"); Call(w,"Redo"); Call(w,"Redo"); Check(m.Cells[1].VariantId=="default-water-v1" && m.Cells[1].Description=="later","Mixed Redo failed");
                    });
                }
                Test("all-zero resize preserves Redo; invalid mixed cuts cannot erase an old axis",w=>
                {
                    UseMap(w,5,4,"PointyTop"); Paint(w,1,0); Call(w,"Undo"); string before=MapLogic.Serializer().Serialize(Map(w)); var empty=MapLogic.ResizeAll(Map(w),0,0,0,0); Call(w,"ApplyResize",empty.State); Check(empty.IsEmpty && Count(w,"redo")==1 && before==MapLogic.Serializer().Serialize(Map(w)),"Zero resize changed history/map");
                    foreach(var d in new[]{new[]{-4,4,0,0},new[]{0,0,-5,5},new[]{0,0,199,0}}) { bool rejected=false; try { MapLogic.ResizeAll(Map(w),d[0],d[1],d[2],d[3]); } catch(InvalidDataException) { rejected=true; } Check(rejected && before==MapLogic.Serializer().Serialize(Map(w)) && Count(w,"redo")==1,"Invalid compound operation partially applied"); }
                    using(var dialog=new ResizeDialog(5,4)) { Check(dialog.TopDelta==0 && dialog.BottomDelta==0 && dialog.LeftDelta==0 && dialog.RightDelta==0,"Dialog defaults not zero"); var field=(ScrubNumber)typeof(ResizeDialog).GetField("topDelta",Flags).GetValue(dialog); field.Value=-4; var result=(Label)typeof(ResizeDialog).GetField("result",Flags).GetValue(dialog); Check(result.Text.Contains("całej") && !((Button)dialog.AcceptButton).Enabled,"Invalid plan enabled OK"); }
                });
                Test("deletion warning retains complete long coordinate list and defaults to No",w=>
                {
                    UseMap(w,200,200,"FlatTop"); foreach(var c in Map(w).Cells) c.Name="data"; var plan=MapLogic.ResizeAll(Map(w),-199,0,0,0); var coordinates=plan.DataIndices.Select(i=>MapLogic.Coordinate(i,200)).ToArray();
                    using(var warning=new ResizeWarningDialog(plan.RemovedIndices.Count,coordinates)) { var handle=warning.coordinates.Handle; Check(plan.DataIndices.Count==39800 && warning.coordinates.Text==String.Join(", ",coordinates) && warning.coordinates.Text.Contains("GR199"),"Warning truncated full coordinate list"); Check(warning.coordinates.ScrollBars==ScrollBars.Vertical && ((Button)warning.AcceptButton).DialogResult==DialogResult.No && ((Button)warning.CancelButton).DialogResult==DialogResult.No,"Warning not scrollable/safe default"); }
                });
                Console.WriteLine("Passed: "+passed+"; 0.2.1 method/control tests, not native UI."); return 0;
            }
            catch(Exception ex) { Console.Error.WriteLine("FAIL "+ex); return 1; }
            finally { Directory.Delete(root,true); }
        }
    }
}
