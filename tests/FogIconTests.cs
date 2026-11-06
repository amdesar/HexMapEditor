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
    internal static class FogIconTests
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
        static int passed; static string root;
        static object Get(MainWindow w, string n) { return typeof(MainWindow).GetField(n, F).GetValue(w); }
        static void Set(MainWindow w, string n, object value) { typeof(MainWindow).GetField(n, F).SetValue(w, value); }
        static object Call(MainWindow w, string n, params object[] args) { return typeof(MainWindow).GetMethods(F).Single(m => m.Name == n && m.GetParameters().Length == args.Length).Invoke(w, args); }
        static MapData Map(MainWindow w) { return (MapData)Get(w, "map"); }
        static int Count(MainWindow w, string n) { return ((ICollection)Get(w,n)).Count; }
        static void Check(bool b, string why) { if (!b) throw new Exception(why); }
        static void Test(string name, Action test) { if (!String.IsNullOrEmpty(filter) && !name.StartsWith(filter, StringComparison.Ordinal)) return; test(); passed++; Console.WriteLine("PASS " + name); }
        static MapData New(int columns, int rows, string orientation) { var m = new MapData { Columns = columns, Rows = rows, Orientation = orientation }; for(int i=0;i<columns*rows;i++)m.Cells.Add(new CellData()); return m; }
        static IconAsset AddIcon(MainWindow w) { var a = IconAssets.Starters()[0]; ((List<LibraryIcon>)Get(w,"libraryIcons")).Add(new LibraryIcon {Id=a.Id,Name=a.Name,Asset=a}); Call(w,"SelectIcon",a.Id);return a; }
        static void Gesture(MainWindow w, int start, params int[] moves)
        {
            var canvas=(Control)Get(w,"canvas");Func<int,Point> point=i=>Point.Round((PointF)Call(w,"ScreenCenter",i%Map(w).Columns,i/Map(w).Columns));var p=point(start);
            Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));
            foreach(int index in moves){p=point(index);Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.Left,0,p.X,p.Y,0));}
            Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));
        }
        static string filter;
        [STAThread] static int Main(string[] args)
        {
            filter = args.Length == 0 ? "" : args[0];
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");Directory.CreateDirectory(root);
            try
            {
                Test("eight deterministic brush geometries, rings/outline and screen axes in both orientations",()=>{
                    foreach(string orientation in new[]{"PointyTop","FlatTop"}) {
                        var map=New(31,31,orientation);int center=15*31+15;
                        foreach(BrushShape shape in Enum.GetValues(typeof(BrushShape)))foreach(int n in new[]{1,3})foreach(bool filled in new[]{false,true}){
                            var a=BrushGeometry.Stamp(map,center,shape,n,2,filled);Check(a.Count>0 && a.Distinct().Count()==a.Count && a.SequenceEqual(BrushGeometry.Stamp(map,center,shape,n,2,filled)),"Empty/unstable brush "+shape);
                            if(n==3 && shape!=BrushShape.Point)Check(a.Count>1,"Size does not grow for "+shape);
                        }
                        Check(BrushGeometry.Stamp(map,center,BrushShape.Hexagon,3,1,true).Count==19 && BrushGeometry.Stamp(map,center,BrushShape.Hexagon,3,1,false).Count==12,"Not full hex rings");
                        var h=BrushGeometry.Stamp(map,center,BrushShape.Horizontal,5,1,true).Select(i=>BrushGeometry.Center(map,i%31,i/31)).ToArray();var v=BrushGeometry.Stamp(map,center,BrushShape.Vertical,5,1,true).Select(i=>BrushGeometry.Center(map,i%31,i/31)).ToArray();
                        Check(h.Max(p=>p.X)-h.Min(p=>p.X)>h.Max(p=>p.Y)-h.Min(p=>p.Y) && v.Max(p=>p.Y)-v.Min(p=>p.Y)>v.Max(p=>p.X)-v.Min(p=>p.X),"Lines ignore visible axes");
                    }
                });
                Test("icon overlap is one gesture; Undo preserves terrain/text; modal tail cannot edit",()=>{
                    using(var w=new MainWindow()){
                        var a=AddIcon(w);Set(w,"currentTool",EditorTool.Brush);Map(w).Cells[0].Name="Later name";Map(w).Cells[0].Description="Later description";Call(w,"CaptureSavedState");Gesture(w,0,0,1,0,1);
                        Check(Count(w,"undo")==1 && Map(w).Cells[0].IconId==a.Id && Map(w).Cells[1].IconId==a.Id,"Overlapping stamp toggled twice");Call(w,"Undo");Check(Map(w).Cells[0].IconId==null && Map(w).Cells[0].Name=="Later name" && Map(w).Cells[0].Description=="Later description","Undo lost independent data");Call(w,"Redo");Gesture(w,0,0,1,0);Check(Map(w).Cells[0].IconId==null && Map(w).Cells[1].IconId==null,"Remove mode not fixed at start");Call(w,"Undo");
                        Set(w,"requireFreshCanvasDown",true);var canvas=(Control)Get(w,"canvas");var p=Point.Round((PointF)Call(w,"ScreenCenter",2,0));string before=MapLogic.Serializer().Serialize(Map(w));Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,2,p.X,p.Y,0));Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.Left,0,p.X,p.Y,0));Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));Check(before==MapLogic.Serializer().Serialize(Map(w)),"Dialog double-click tail edited icon layer");
                    }
                });
                Test("fog gesture masks details immediately, never damages text, one Undo/Redo",()=>{
                    using(var w=new MainWindow()){
                        Map(w).Cells[0].Name="SECRET";Map(w).Cells[0].Description="HIDDEN DESCRIPTION";Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");Set(w,"currentTool",EditorTool.FogBrush);Gesture(w,0,1,0,1);
                        Check(Map(w).Cells[0].Fog && Map(w).Cells[1].Fog && Count(w,"undo")==1,"Fog retoggled within gesture");Check(((TextBox)Get(w,"cellName")).Text=="???" && !((Control)Get(w,"cellName")).Enabled && !((Control)Get(w,"cellDescription")).Enabled && !((RichNoteEditor)Get(w,"cellDescription")).ContentJson.Contains("HIDDEN"),"Details leaked hidden text");
                        Call(w,"TextUndo","name",false);Check(Map(w).Cells[0].Name=="SECRET" && Map(w).Cells[0].Description=="HIDDEN DESCRIPTION","Hidden edit damaged stored content");Call(w,"Undo");Check(!Map(w).Cells[0].Fog && ((TextBox)Get(w,"cellName")).Text=="SECRET","Fog Undo failed to reveal");Call(w,"Redo");Check(Map(w).Cells[0].Fog,"Fog Redo failed");
                    }
                });
                Test("icon bucket uses terrain variant; fog bucket reuses region by underlying color",()=>{
                    using(var w=new MainWindow()){
                        Set(w,"map",New(3,3,"PointyTop"));var a=AddIcon(w);var water=PresetCatalog.Default().Terrains[1];foreach(int i in new[]{0,1,3,4})MapLogic.ApplyTerrain(Map(w).Cells[i],water);
                        Map(w).Cells[1].IconId=a.Id;Map(w).IconAssets.Add(a);Set(w,"currentTool",EditorTool.Bucket);Call(w,"FillRegion",0);Check(Map(w).Cells.Count(c=>c.IconId==a.Id)==4 && Count(w,"undo")==1,"Icon bucket boundary/action wrong");Call(w,"FillRegion",0);Check(Map(w).Cells.All(c=>c.IconId==null),"Bucket erase not uniform");Call(w,"Undo");
                        Map(w).Cells[1].VariantId="separate-same-color";Map(w).Cells[0].Description="Keep";Set(w,"currentTool",EditorTool.FogBucket);Call(w,"FillRegion",0);Check(Map(w).Cells.Count(c=>c.Fog)==4 && Map(w).Cells[0].Description=="Keep" && Map(w).Cells.Count(c=>c.IconId==a.Id)==4,"Fog uses fog/icons/variant as border");Call(w,"Undo");Check(Map(w).Cells.All(c=>!c.Fog),"Fog bucket Undo failed");
                    }
                });
                Test("portable format 3 stores one used asset, import releases source, invalid open preserves map",()=>{
                    string source=Path.Combine(root,"source.png");using(var bitmap=new Bitmap(40,20)){using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Gold);bitmap.Save(source,System.Drawing.Imaging.ImageFormat.Png);}var imported=IconAssets.Import(source);File.Delete(source);
                    using(var w=new MainWindow()){
                        ((List<LibraryIcon>)Get(w,"libraryIcons")).Add(new LibraryIcon {Id=imported.Id,Name=imported.Name,Asset=imported});Call(w,"SelectIcon",imported.Id);Set(w,"currentTool",EditorTool.Brush);Gesture(w,0,1);Map(w).Cells[0].Fog=true;Map(w).IconAssets.Add(IconAssets.Starters()[1]);
                        string file=Path.Combine(root,"portable.hexmap");object[] save={file,false,null};Check((bool)Call(w,"TrySaveTo",save),"Save failed: "+save[2]);var stored=MapLogic.Serializer().Deserialize<MapData>(File.ReadAllText(file));Check(stored.FormatVersion==3 && stored.IconAssets.Count==1 && stored.Cells[0].Fog && stored.Cells[1].IconId==imported.Id,"Saved whole library or lost layer");
                        object[] open={file,null,false};Check((bool)Call(w,"TryLoadProject",open),"Portable load failed: "+open[1]);Check(Count(w,"undo")==0 && Count(w,"redo")==0 && !(bool)Get(w,"iconMaterial") && Map(w).IconAssets.Count==1,"Open mixed document history/material/resources");using(var b=IconAssets.Decode(Map(w).IconAssets.Single()))Check(b.Width==40 && b.Height==20,"Asset proportions lost");
                        string before=MapLogic.Serializer().Serialize(Map(w));stored.IconAssets[0].Png="broken";File.WriteAllText(file,MapLogic.Serializer().Serialize(stored));Check(!(bool)Call(w,"TryLoadProject",open) && before==MapLogic.Serializer().Serialize(Map(w)),"Bad resource destroyed open map");stored.FormatVersion=2;File.WriteAllText(file,MapLogic.Serializer().Serialize(stored));Check(!(bool)Call(w,"TryLoadProject",open) && before==MapLogic.Serializer().Serialize(Map(w)),"Old format unexpectedly migrated");
                        Call(w,"StartNewProject",2,2,"FlatTop","Fresh",PresetCatalog.Default());Check(Map(w).IconAssets.Count==0 && Map(w).Cells.All(c=>!c.Fog && c.IconId==null) && Count(w,"undo")==0 && Count(w,"redo")==0,"New mixed document history/resources");
                    }
                });
                Test("crop warns icons/fog once and restores layers with original IDs through shared history",()=>{
                    using(var w=new MainWindow()){
                        var a=AddIcon(w);Set(w,"currentTool",EditorTool.Brush);Gesture(w,0);Map(w).Cells[0].Fog=true;string id=Map(w).Cells[0].Id;var plan=MapLogic.ResizeAll(Map(w),-1,0,-1,0);var groups=RemovalGroup.For(Map(w),plan.RemovedIndices);Check(plan.DataIndices.Count==1 && groups.Sum(g=>g.Coordinates.Length)==1 && groups[0].Title.Contains("Ikona") && groups[0].Title.Contains("Mgła"),"Icon/fog excluded or double counted");
                        Call(w,"ApplyResize",plan.State);Call(w,"Undo");Check(Map(w).Cells[0].Id==id && Map(w).Cells[0].IconId==a.Id && Map(w).Cells[0].Fog,"Resize Undo lost layer/ID");Call(w,"Undo");Check(Map(w).Cells[0].IconId==null && Map(w).Cells[0].Fog,"Old icon Undo changed fog");
                    }
                });
                Test("rendering hides color/icon/label with opaque fog and keeps a large proportional icon",()=>{
                    using(var w=new MainWindow())using(var image=new Bitmap(500,400))using(var graphics=Graphics.FromImage(image)){
                        var a=AddIcon(w);Map(w).IconAssets.Add(a);Map(w).Cells[0].IconId=a.Id;Map(w).Cells[0].Color="#FF0000";Set(w,"pan",new PointF(100,100));Set(w,"scale",2f);Set(w,"selected",-1);Set(w,"previewCell",-1);Map(w).LabelMode="InCells";
                        Call(w,"PaintMap",Get(w,"canvas"),new PaintEventArgs(graphics,new Rectangle(0,0,500,400)));Color visible=image.GetPixel(100,100);Map(w).Cells[0].Fog=true;Call(w,"PaintMap",Get(w,"canvas"),new PaintEventArgs(graphics,new Rectangle(0,0,500,400)));
                        Color fog=Color.FromArgb(101,107,117);Check(image.GetPixel(100,100).ToArgb()==fog.ToArgb() && image.GetPixel(110,115).ToArgb()==fog.ToArgb() && visible.R>200 && visible.G>200,"Underlying data bleeds through fog or icon is missing");
                    }
                });
                Test("minimum sidebar has fixed sections, terrain-only scroll and real canvas/Fit bounds",()=>{
                    using(var w=new MainWindow()){
                        var own=PresetCatalog.Default();own.Id="test-own";Map(w).Presets.Add(own);Map(w).ActivePresetId=own.Id;Call(w,"RefreshPresetPicker");w.Size=w.MinimumSize;w.Show();Application.DoEvents();for(int i=0;i<20;i++)Call(w,"AddTerrain",new TerrainDefinition());Application.DoEvents();var left=(Panel)Get(w,"left");var scroll=(Panel)Get(w,"terrainScroll");var canvas=(Control)Get(w,"canvas");var sections=(TableLayoutPanel)Get(w,"leftSections");
                        Check(!left.AutoScroll && !scroll.AutoScroll && ((Button)Get(w,"terrainDown")).Enabled && sections.Controls.Cast<Control>().All(c=>c.Bottom<=left.Height) && canvas.Left==238,"Sidebar pushes fixed sections/clips canvas");var icon=(Control)Get(w,"iconPreview");int y=icon.PointToScreen(Point.Empty).Y;Call(w,"ScrollTerrains",100);Application.DoEvents();Check(icon.PointToScreen(Point.Empty).Y==y,"Terrain scroll moved icons");
                        Call(w,"FitMap");var bounds=(RectangleF)Call(w,"MapBounds");var pan=(PointF)Get(w,"pan");float scale=(float)Get(w,"scale");Check(pan.X+bounds.Left*scale>=31 && pan.X+bounds.Right*scale<=canvas.Width-31,"Fit counts wrong sidebar width");string before=MapLogic.Serializer().Serialize(Map(w));int undo=Count(w,"undo");((Button)Get(w,"leftToggle")).PerformClick();Check(canvas.Left==18 && MapLogic.Serializer().Serialize(Map(w))==before && Count(w,"undo")==undo,"Collapse edits document or overlays canvas");Call(w,"FitMap");Call(w,"CaptureSavedState");w.Close();
                    }
                });
                Test("preset json extension, readable collision-safe naming and deliberate rename",()=>{
                    string directory=Path.Combine(root,"presets");Directory.CreateDirectory(directory);
                    var preset=new TerrainPreset { Name="Kosmos" };preset.Terrains.Add(PresetCatalog.Neutral());PresetCatalog.Save(preset,directory);
                    Check(File.Exists(Path.Combine(directory,"Kosmos.json")),"Unexpected preset extension");
                    string id=preset.Id;preset.Name="Świat / nowy";PresetCatalog.Write(preset,directory,true);
                    string warning;var loaded=PresetCatalog.Load(directory,out warning);Check(warning=="" && loaded.Count==2 && loaded.Single(p=>p.Id==id).Name==preset.Name && Directory.GetFiles(directory,"*.json").Length==1,"Rename duplicated/lost preset");
                    var other=new TerrainPreset { Name="Świat : nowy" };other.Terrains.Add(PresetCatalog.Neutral());PresetCatalog.Save(other,directory);
                    Check(Directory.GetFiles(directory,"*.json").Length==2 && Path.GetFileName(PresetCatalog.FindFile(directory,other.Id)).Contains("(2)"),"Sanitized filename collision overwrote preset");
                });
                Check(passed>0,"Case filter did not select a test.");Console.WriteLine("Passed: "+passed+"; selective 0.3.0 method/control cases only. No full regression suite.");return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
            finally{Directory.Delete(root,true);}
        }
    }
}
