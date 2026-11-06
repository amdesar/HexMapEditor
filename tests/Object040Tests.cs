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
    internal static class Object040Tests
    {
        const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
        static string root;
        static object Get(MainWindow w,string n){return typeof(MainWindow).GetField(n,F).GetValue(w);}
        static void Set(MainWindow w,string n,object v){typeof(MainWindow).GetField(n,F).SetValue(w,v);}
        static object Call(MainWindow w,string n,params object[] args){return typeof(MainWindow).GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(w,args);}
        static MapData Map(MainWindow w){return (MapData)Get(w,"map");}
        static void Check(bool b,string why){if(!b)throw new Exception(why);}
        static int Count(MainWindow w,string n){return ((ICollection)Get(w,n)).Count;}
        static void History(MainWindow w,bool redo){object[] args={redo,null};Check((bool)Call(w,"TryHistory",args),"History: "+args[1]);}
        static MapData Grid(int cols=8,int rows=5){var m=new MapData{Columns=cols,Rows=rows};for(int i=0;i<cols*rows;i++)m.Cells.Add(new CellData());return m;}
        static void Setup(MainWindow w){Set(w,"map",Grid());Call(w,"CaptureSavedState");Call(w,"PositionOverlays");Call(w,"RefreshPresetPicker");Set(w,"scale",1f);Set(w,"pan",new PointF(150,120));}
        static ObjectTemplate Template(MainWindow w,string name="Namiot"){var t=new ObjectTemplate{Name=name,Asset=IconAssets.Starters()[1]};((List<ObjectTemplate>)Get(w,"libraryObjects")).Add(t);Call(w,"SelectObjectTemplate",t.Id);return t;}
        static MapObject Place(MainWindow w,int col,int row){return (MapObject)Call(w,"PlaceObject",BrushGeometry.Center(Map(w),col,row));}
        static MouseEventArgs Mouse(Point p,MouseButtons button=MouseButtons.Left){return new MouseEventArgs(button,1,p.X,p.Y,0);}
        static Point Screen(MainWindow w,PointF world){var p=(PointF)Get(w,"pan");float s=(float)Get(w,"scale");return new Point((int)Math.Round(p.X+world.X*s),(int)Math.Round(p.Y+world.Y*s));}
        static HashSet<string> Selection(MainWindow w){return (HashSet<string>)Get(w,"selectedObjects");}
        static void GeometrySave()
        {
            using(var w=new MainWindow()){
                Setup(w);Template(w);var a=Place(w,2,2);a.X+=5;a.Y-=3;var outside=(MapObject)Call(w,"PlaceObject",new PointF(-130,25));var m=Map(w);m.Cells[0].IconId=a.AssetId;m.Cells[0].IconEntryId="independent-icon";m.Cells[0].IconName="Kopia grafiki";
                foreach(string orientation in new[]{"PointyTop","FlatTop"}){
                    Call(w,"ChangeOrientation",orientation);a=m.Objects.First(o=>o.Id==a.Id);var cell=ObjectGeometry.Nearest(m,new PointF(a.X,a.Y));Check(cell==new Point(2,2),"Orientation lost hex");var c=BrushGeometry.Center(m,2,2);float ox=orientation=="PointyTop"?5:5*.8660254f+1.5f,oy=orientation=="PointyTop"?-3:2.5f-3*.8660254f;Check(Math.Abs(a.X-c.X-ox)<.01&&Math.Abs(a.Y-c.Y-oy)<.01,"Orientation lost local offset");var edge=ObjectGeometry.Transform(m,new MapData{Orientation=orientation=="FlatTop"?"PointyTop":"FlatTop",RowParity=m.RowParity,ColumnParity=m.ColumnParity},new PointF(c.X+ox,c.Y+oy),0,0);Check(ObjectGeometry.Nearest(new MapData{Orientation=orientation=="FlatTop"?"PointyTop":"FlatTop",RowParity=m.RowParity,ColumnParity=m.ColumnParity},edge)==new Point(2,2),"Edge anchor changed");
                    var before=m.Objects.Select(o=>o.Copy()).ToList();var plan=MapLogic.ResizeAll(m,1,-1,1,-1);Call(w,"ApplyResize",plan.State);a=m.Objects.First(o=>o.Id==a.Id);Check(ObjectGeometry.Nearest(m,new PointF(a.X,a.Y))==new Point(3,3),"Resize anchor/parity");History(w,false);Check(m.Objects.Zip(before,(x,y)=>x.Id==y.Id&&Math.Abs(x.X-y.X)<.01&&Math.Abs(x.Y-y.Y)<.01).All(b=>b),"Resize Undo positions");History(w,true);History(w,false);
                }
                a=m.Objects.First(o=>o.Id==a.Id);a.Side="Ally";a.RevealsFog=true;m.Cells.ForEach(c=>c.Fog=true);
                string path=Path.Combine(root,"portable.hexmap");object[] save={path,false,null};Check((bool)Call(w,"TrySaveTo",save),"Portable save: "+save[2]);var raw=MapLogic.Serializer().Deserialize<MapData>(File.ReadAllText(path));Check(raw.FormatVersion==4&&raw.Objects.Count==2&&raw.IconAssets.Count==1,"Format/used dedupe");((List<ObjectTemplate>)Get(w,"libraryObjects")).Clear();object[] open={path,null,false};Check((bool)Call(w,"TryLoadProject",open),"Portable open: "+open[1]);Check(Map(w).Objects.Any(o=>o.X<0)&&Map(w).Cells.All(c=>c.Fog),"Load re-revealed fog or lost outside object");
                var stable=MapLogic.Serializer().Serialize(Map(w));File.WriteAllText(path,File.ReadAllText(path).Replace("\"Side\":\"Ally\"","\"Side\":\"Bad\""));open=new object[]{path,null,false};Check(!(bool)Call(w,"TryLoadProject",open)&&MapLogic.Serializer().Serialize(Map(w))==stable,"Bad object destroyed document");
                Call(w,"CaptureSavedState");
            }
        }
        static void GesturesFog()
        {
            using(var w=new MainWindow()){
                Setup(w);Template(w);var world=BrushGeometry.Center(Map(w),1,1);Point p=Screen(w,world);int steps=Count(w,"undo");Call(w,"CanvasMouseDown",null,Mouse(p));Call(w,"CanvasMouseMove",null,Mouse(new Point(p.X+140,p.Y)));Call(w,"CanvasMouseUp",null,Mouse(new Point(p.X+140,p.Y)));Check(Map(w).Objects.Count==1&&Count(w,"undo")==steps+1,"Object drag stamped copies");
                var outside=(MapObject)Call(w,"PlaceObject",new PointF(-100,-50));Check(outside.X<0,"Outside placement");Call(w,"SelectTool",EditorTool.Bucket);steps=Count(w,"undo");Call(w,"CanvasMouseDown",null,Mouse(p));Call(w,"CanvasMouseMove",null,Mouse(new Point(p.X+60,p.Y)));Call(w,"CanvasMouseUp",null,Mouse(p));Check(Map(w).Objects.Count==2&&Count(w,"undo")==steps,"Bucket created object");
                var m=Map(w);var obj=m.Objects[0];m.Cells.ForEach(c=>c.Fog=true);int pre=0;m.Cells[pre].Fog=false;var next=obj.Copy();next.Side="Enemy";next.RevealsFog=true;Call(w,"ChangeObject",next);Check(!m.Objects[0].RevealsFog&&m.Cells.Count(c=>!c.Fog)==1,"Enemy reveal");next=m.Objects[0].Copy();next.Side="Ally";next.RevealsFog=true;next.RevealRadius=0;Call(w,"ChangeObject",next);Check(m.Cells.Count(c=>!c.Fog)==2,"Radius 0");next=m.Objects[0].Copy();next.RevealRadius=1;next.Size=140;Call(w,"ChangeObject",next);Check(m.Cells.Count(c=>!c.Fog)==8,"Radius 1/size independence");History(w,false);History(w,true);
                var locked=Place(w,2,2);locked.Locked=true;var lockedBefore=locked.Copy();Selection(w).Clear();Selection(w).Add(obj.Id);Selection(w).Add(locked.Id);Call(w,"SelectTool",EditorTool.Select);obj=m.Objects.First(o=>o.Id==obj.Id);p=Screen(w,new PointF(obj.X,obj.Y));var end=Screen(w,BrushGeometry.Center(m,6,1));var fogBefore=m.Cells.Select(c=>c.Fog).ToArray();float x=obj.X;steps=Count(w,"undo");Call(w,"CanvasMouseDown",null,Mouse(p));Call(w,"CanvasMouseMove",null,Mouse(end));Call(w,"CanvasMouseUp",null,Mouse(end));Check(Count(w,"undo")==steps+1&&m.Objects.First(o=>o.Id==locked.Id).X==lockedBefore.X,"Group step/lock");Check(!m.Cells[1*m.Columns+4].Fog,"Gap in reveal path");History(w,false);Check(m.Cells.Select(c=>c.Fog).SequenceEqual(fogBefore)&&m.Objects.First(o=>o.Id==obj.Id).X==x,"Movement Undo changed pre-existing fog");History(w,true);
                obj=m.Objects.First(o=>o.Id==obj.Id);next=obj.Copy();next.Side="Neutral";Call(w,"ChangeObject",next);next=m.Objects.First(o=>o.Id==obj.Id).Copy();next.Side="Ally";Call(w,"ChangeObject",next);Check(!m.Objects.First(o=>o.Id==obj.Id).RevealsFog,"Returning ally auto enables");
                // Rectangle and closed lasso use centers, topmost click and Ctrl toggle share the same selection set.
                m.Cells.ForEach(c=>c.Fog=false);Selection(w).Clear();Call(w,"ChooseObject",m.Objects[0],false);Call(w,"ChooseObject",m.Objects[1],true);Check(Selection(w).Count==2,"Ctrl add");Call(w,"ChooseObject",m.Objects[1],true);Check(Selection(w).Count==1,"Ctrl toggle");Set(w,"pan",new PointF(250,180));Call(w,"SelectTool",EditorTool.Select);Set(w,"selectionKind",0);Point start=Screen(w,new PointF(-160,-100)),finish=Screen(w,new PointF(500,300));Call(w,"CanvasMouseDown",null,Mouse(start));Call(w,"CanvasMouseMove",null,Mouse(finish));Call(w,"CanvasMouseUp",null,Mouse(finish));Check(Selection(w).Count==m.Objects.Count,"Rectangle centers");
                Set(w,"selectionKind",1);Call(w,"CanvasMouseDown",null,Mouse(start));foreach(var q in new[]{new PointF(500,-100),new PointF(500,300),new PointF(-160,300)})Call(w,"CanvasMouseMove",null,Mouse(Screen(w,q)));Call(w,"CanvasMouseUp",null,Mouse(start));Check(Selection(w).Count==m.Objects.Count,"Closed lasso");
                obj=m.Objects[0];obj.Size=8;obj.X=BrushGeometry.Center(m,0,0).X;obj.Y=0;m.Cells[0].Fog=true;Check(Call(w,"HitObject",new PointF(obj.X,obj.Y))==null&&!(bool)Call(w,"ObjectVisible",obj),"Hidden object hit/visibility");obj.X=-45;Check((bool)Call(w,"ObjectVisible",obj),"Outside object hidden by fog");
                Call(w,"CaptureSavedState");
            }
        }
        static void LibraryHistory()
        {
            using(var w=new MainWindow()){
                Setup(w);var asset=IconAssets.Starters()[1];string dir=Path.Combine(root,"Objects");var template=new ObjectTemplate{Name="Drużyna",Asset=asset};object[] change={null,template,false,dir,null};Check((bool)Call(w,"TryChangeObjectTemplate",change),"Add template: "+change[4]);var stored=((List<ObjectTemplate>)Get(w,"libraryObjects")).Single();Check(Path.GetFileName(stored.FilePath)=="object-Drużyna.json"&&!(bool)Get(w,"dirty"),"Name/false map dirty");byte[] original=File.ReadAllBytes(stored.FilePath);Call(w,"SelectObjectTemplate",stored.Id);var obj=Place(w,2,2);obj.Side="Enemy";obj.Locked=true;string id=obj.Id;
                var next=stored.Copy();next.Name="Drużyna nowa";next.Asset=IconAssets.Starters()[2];change=new object[]{stored,next,false,dir,null};Check((bool)Call(w,"TryChangeObjectTemplate",change)&&Map(w).Objects.Single().AssetId==asset.Id,"Edit changed instance without checkbox");stored=((List<ObjectTemplate>)Get(w,"libraryObjects")).Single();next=stored.Copy();next.Name="Drużyna finalna";change=new object[]{stored,next,true,dir,null};Check((bool)Call(w,"TryChangeObjectTemplate",change),"Update map failed");obj=Map(w).Objects.Single();Check(obj.Id==id&&obj.Name==next.Name&&obj.AssetId==next.Asset.Id&&obj.Side=="Enemy"&&obj.Locked,"Atomic template update lost fields");History(w,false);Check(Map(w).Objects.Single().Name=="Drużyna","Template update Undo");History(w,true);
                stored=((List<ObjectTemplate>)Get(w,"libraryObjects")).Single();byte[] saved=File.ReadAllBytes(stored.FilePath);change=new object[]{stored,null,false,dir,null};Check((bool)Call(w,"TryChangeObjectTemplate",change)&&!File.Exists(stored.FilePath)&&Map(w).Objects.Count==1,"Delete removed instance");File.WriteAllText(stored.FilePath,"foreign");int count=Count(w,"undo");object[] undo={false,null};Check(!(bool)Call(w,"TryHistory",undo)&&Count(w,"undo")==count&&File.ReadAllText(stored.FilePath)=="foreign","Restore conflict overwritten/moved history");File.Delete(stored.FilePath);History(w,false);Check(File.ReadAllBytes(stored.FilePath).SequenceEqual(saved),"Restore bytes");History(w,true);History(w,false);
                var copy=new LibraryIcon{Name="Kopia",Asset=stored.Asset};object[] icon={null,copy,false,Path.Combine(root,"Icons"),null};Check((bool)Call(w,"TryChangeLibraryIcon",icon),"Cross library graphic");Check(copy.Id!=stored.Id,"Copied logical identity");Call(w,"ClearIconMaterial");int before=Count(w,"undo");var data=MapLogic.Serializer().Serialize(Map(w));Call(w,"SelectTool",EditorTool.Brush);var p=Screen(w,BrushGeometry.Center(Map(w),0,0));Call(w,"CanvasMouseDown",null,Mouse(p));Call(w,"CanvasMouseMove",null,Mouse(new Point(p.X+50,p.Y)));Call(w,"CanvasMouseUp",null,Mouse(p));Check(Count(w,"undo")==before&&MapLogic.Serializer().Serialize(Map(w))==data,"No material paints");
                Call(w,"ShowObjectOnMap",id);var pan=(PointF)Get(w,"pan");var center=Screen(w,new PointF(Map(w).Objects.Single().X,Map(w).Objects.Single().Y));var canvas=(Control)Get(w,"canvas");var right=(Control)Get(w,"right");Check(Math.Abs(center.X-(canvas.Width-right.Width)/2)<2,"Show object available viewport");
                Call(w,"FitEverything");float all=(float)Get(w,"scale");Call(w,"FitMap");Check((float)Get(w,"scale")>0&&all>0,"Fit functions");Call(w,"StartNewProject",8,5,"PointyTop","Nowa",PresetCatalog.Default());Check(Count(w,"undo")==0&&Selection(w).Count==0&&File.Exists(stored.FilePath),"Reset removes active library file");Call(w,"CaptureSavedState");
            }
        }
        static void ViewAndBounds()
        {
            using(var w=new MainWindow()){
                Setup(w);var m=Map(w);IconAsset asset;using(var source=new Bitmap(100,100)){using(var g=Graphics.FromImage(source))g.Clear(Color.Red);asset=IconAssets.FromBitmap(source,"Mask");}m.IconAssets.Add(asset);var obj=new MapObject{TemplateId="mask",Name="Mask",AssetId=asset.Id,X=0,Y=0,Size=100};m.Objects.Add(obj);m.Cells.ForEach(c=>c.Fog=true);Set(w,"pan",new PointF(100,100));
                using(var image=new Bitmap(600,500))using(var g=Graphics.FromImage(image)){g.Clear(Color.Transparent);Call(w,"DrawObjects",g);Check(image.GetPixel(100,100).A==0&&image.GetPixel(60,100).R==255&&image.GetPixel(60,100).A==255,"Partial fog clipping/outside visibility");}
                var tall=new IconAsset{Width=80,Height=160};var bounds=ObjectGeometry.Bounds(obj,tall);Check(bounds.Width==50&&bounds.Height==100,"Aspect/longest side");
                obj.X=-250;Call(w,"FitEverything");var allBounds=RectangleF.Union((RectangleF)Call(w,"MapBounds"),(RectangleF)Call(w,"ObjectBounds",obj));var pan=(PointF)Get(w,"pan");float scale=(float)Get(w,"scale");var canvas=(Control)Get(w,"canvas");Check(pan.X+allBounds.Left*scale>=35&&pan.X+allBounds.Right*scale<=canvas.Width-35,"Fit all crops actual graphic");m.Objects.Clear();Call(w,"FitMap");pan=(PointF)Get(w,"pan");scale=(float)Get(w,"scale");Call(w,"FitEverything");Check((PointF)Get(w,"pan")==pan&&(float)Get(w,"scale")==scale,"Empty fit differs from map");
                m.Objects.Add(obj);obj.X=0;obj.Size=8;var second=obj.Copy();second.Id="second";second.X=90;var locked=obj.Copy();locked.Id="locked";locked.X=140;locked.Locked=true;m.Objects.Add(second);m.Objects.Add(locked);m.Cells.ForEach(c=>c.Fog=false);Selection(w).UnionWith(m.Objects.Select(o=>o.Id));Set(w,"pan",new PointF(150,120));Set(w,"scale",1f);Call(w,"SelectTool",EditorTool.Select);var start=Screen(w,new PointF(0,0));int count=Count(w,"undo");Call(w,"CanvasMouseDown",null,Mouse(start));Call(w,"CanvasMouseMove",null,Mouse(new Point(start.X+40,start.Y+25)));Call(w,"CanvasMouseUp",null,Mouse(new Point(start.X+40,start.Y+25)));Check(m.Objects[0].X==40&&m.Objects[1].X==130&&m.Objects[2].X==140&&m.Objects[0].Y==25&&m.Objects[1].Y==25&&Count(w,"undo")==count+1,"Group relative movement/locked");History(w,false);History(w,true);
                obj=m.Objects[0];obj.Size=8;obj.Side="Ally";obj.RevealsFog=true;obj.RevealRadius=0;m.Cells.ForEach(c=>c.Fog=true);int at=ObjectGeometry.Hit(m,new PointF(obj.X,obj.Y));m.Cells[at].Fog=false;Selection(w).Clear();Selection(w).Add(obj.Id);start=Screen(w,new PointF(obj.X,obj.Y));var end=Screen(w,BrushGeometry.Center(m,6,1));Call(w,"CanvasMouseDown",null,Mouse(start));Set(w,"objectPreviousCursor",null);Call(w,"CanvasMouseMove",null,Mouse(end));Call(w,"CanvasMouseUp",null,Mouse(end));Check(m.Cells[m.Columns+3].Fog&&!m.Cells[m.Columns+6].Fog,"Unknown route revealed connecting line");
                var far=obj.Copy();far.X=10000000;far.Y=0;Check(ObjectGeometry.Reveal(m,far,new PointF(-10000000,0)).Count>0,"Clipped distant crossing lost map");far.X=-1000;Check(ObjectGeometry.Reveal(m,far,null).Count==0,"Outside center reveals nearest hex");
                var before=m.Objects.Select(o=>o.Copy()).ToList();string orientation=m.Orientation;Call(w,"ChangeOrientation","FlatTop");History(w,false);Check(m.Orientation==orientation&&m.Objects.Zip(before,(a,b)=>a.X==b.X&&a.Y==b.Y).All(x=>x),"Orientation Undo object positions");
                Call(w,"CaptureSavedState");
            }
        }
        static void InputRouting()
        {
            using(var w=new MainWindow()){
                w.Show();Application.DoEvents();Setup(w);Template(w);var obj=Place(w,1,1);Selection(w).Add(obj.Id);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");
                var text=(TextBox)Get(w,"objectName");Check(text.Focus(),"Cannot focus object name");var key=new KeyEventArgs(Keys.Delete);Call(w,"OnKeyDown",w,key);Check(Map(w).Objects.Count==1&&!key.Handled,"Object Delete stole text input");
                var tile=((Control)Get(w,"paletteTiles")).Controls.Cast<TerrainTile>().First();Check(tile.Choose.Focus(),"Cannot focus terrain");key=new KeyEventArgs(Keys.Delete);Call(w,"OnKeyDown",w,key);Check(Map(w).Objects.Count==1&&!key.Handled,"Object Delete stole terrain shortcut");
                var canvas=(Control)Get(w,"canvas");Check(canvas.Focus(),"Cannot focus canvas");key=new KeyEventArgs(Keys.Delete);Call(w,"OnKeyDown",w,key);Check(Map(w).Objects.Count==0&&key.Handled,"Canvas Delete did not remove instance");History(w,false);Check(Map(w).Objects.Count==1,"Delete Undo");
                Call(w,"SelectObjectTemplate",((List<ObjectTemplate>)Get(w,"libraryObjects")).First().Id);Set(w,"requireFreshCanvasDown",true);Set(w,"waitingForMouseRelease",true);int count=Map(w).Objects.Count;Call(w,"CanvasMouseDown",null,Mouse(new Point(400,300)));Check(Map(w).Objects.Count==count,"Modal release placed object");Set(w,"waitingForMouseRelease",false);Call(w,"CanvasMouseDown",null,new MouseEventArgs(MouseButtons.Left,2,400,300,0));Check(Map(w).Objects.Count==count,"Dialog double-click placed object");Call(w,"CaptureSavedState");w.Close();
            }
        }
        static void Fixture(string directory)
        {
            string warning;var templates=ObjectLibrary.Load(Path.Combine(directory,"Library","Objects"),out warning);var map=Grid(8,5);for(int i=0;i<map.Cells.Count;i++)map.Cells[i].Fog=i%map.Columns>=3;File.WriteAllText(Path.Combine(directory,"Próba obiektów.hexmap"),MapLogic.Serializer().Serialize(map));
        }
        [STAThread]static int Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");Directory.CreateDirectory(root);try{if(args.Length==2&&args[0]=="fixture"){Fixture(args[1]);return 0;}if(args.Length!=1)throw new Exception("Choose geometry, gestures, library, view or input");switch(args[0]){case "geometry":GeometrySave();break;case "gestures":GesturesFog();break;case "library":LibraryHistory();break;case "view":ViewAndBounds();break;case "input":InputRouting();break;default:throw new Exception("Unknown case");}Console.WriteLine("PASS "+args[0]+"; targeted 0.4.0 scenario only.");return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    }
}
