using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace HexMapEditor.Tests
{
    internal static class Patch062Tests
    {
        const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Get(object o,string n){return o.GetType().GetField(n,P).GetValue(o);}
        static void Set(object o,string n,object v){o.GetType().GetField(n,P).SetValue(o,v);}
        static object Call(object o,string n,params object[] args){return o.GetType().GetMethods(P).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);}
        static void Check(bool b,string message){if(!b)throw new Exception(message);}
        static string Json(object o){return MapLogic.Serializer().Serialize(o);}
        static MapData Model(MainWindow w){return (MapData)Get(w,"map");}
        static void Start(MainWindow w){Set(w,"libraryPresets",new List<TerrainPreset>{PresetCatalog.Default()});Call(w,"StartNewProject",15,10,"PointyTop","PrĂłba062",PresetCatalog.Default());}
        static void History(MainWindow w,bool redo){var a=new object[]{redo,null};Check((bool)Call(w,"TryHistory",a),"History: "+a[1]);}
        static InkStroke Stroke(Color color,float width,params PointF[] points){return new InkStroke{Color="#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2"),Width=width,Points=points.Select(p=>new InkPoint(p)).ToList()};}
        static Bitmap Render(List<InkStroke> ink,float zoom)
        {
            var b=new Bitmap((int)(320*zoom),(int)(240*zoom));using(var g=Graphics.FromImage(b)){g.Clear(Color.Teal);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(zoom,zoom);foreach(var s in ink)InkGeometry.Draw(g,s);}return b;
        }
        static void Eraser(string root)
        {
            var ink=new List<InkStroke>();for(int y=65;y<=175;y+=14)ink.Add(Stroke(Color.Gold,44,new PointF(65,y),new PointF(240,y)));ink.Add(Stroke(Color.Red,19,new PointF(150,120)));ink.Add(Stroke(Color.Blue,3,new PointF(20,120),new PointF(290,120)));
            foreach(float zoom in new[]{.73f,1.46f})
            {
                float diameter=InkSizeMath.World(40,zoom,96);var cut=new PointF(150,120);var after=InkGeometry.Erase(ink,cut,cut,diameter);
                using(var before=Render(ink,zoom))using(var image=Render(after,zoom))
                {
                    int changedOutside=0,checkedInside=0;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){double distance=Math.Sqrt(Math.Pow(x-150*zoom,2)+Math.Pow(y-120*zoom,2));if(distance>23&&before.GetPixel(x,y).ToArgb()!=image.GetPixel(x,y).ToArgb())changedOutside++;if(distance<17){Check(image.GetPixel(x,y).ToArgb()==Color.Teal.ToArgb(),"Hole does not expose underlying layer");checkedInside++;}}
                    Check(changedOutside<10,"Drawing outside eraser changed: "+changedOutside);Check(checkedInside>500,"No interior checks");image.Save(Path.Combine(root,"eraser-"+zoom+".png"));
                }
                Check(after.Any(s=>s.Surface!=null),"No surface result");var round=MapLogic.Serializer().Deserialize<List<InkStroke>>(Json(after));InkGeometry.Validate(round);using(var a=Render(after,zoom))using(var b=Render(round,zoom)){for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)Check(a.GetPixel(x,y)==b.GetPixel(x,y),"Surface roundtrip changed pixel");}
                var swept=InkGeometry.Erase(ink,new PointF(100,100),new PointF(210,140),diameter);using(var b=Render(swept,zoom))Check(b.GetPixel((int)(155*zoom),(int)(120*zoom)).ToArgb()==Color.Teal.ToArgb(),"Sparse sweep gap");
            }
            var end=Stroke(Color.Black,30,new PointF(10,30),new PointF(200,30));var trimmed=InkGeometry.Erase(new List<InkStroke>{end},new PointF(200,0),new PointF(200,60),80);Check(InkGeometry.Bounds(trimmed.Single()).Right<162,"Fit includes erased edge");
            using(var w=new MainWindow())
            {
                w.Show();Start(w);var m=Model(w);m.Ink=ink;string cells=Json(m.Cells);Set(w,"gestureTool",EditorTool.Eraser);Set(w,"gestureButton",MouseButtons.Left);Set(w,"scale",1f);Set(w,"pan",PointF.Empty);Set(w,"inkToolSize",40f);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,150,120,0));Call(w,"MoveInk",new Point(170,120));Call(w,"FinishInk",true);string result=Json(m.Ink);Check(result!=Json(ink),"Gesture unchanged");History(w,false);Check(Json(m.Ink)==Json(ink),"Undo surface");History(w,true);Check(Json(m.Ink)==result&&Json(m.Cells)==cells,"Redo rerender or cell change");
                string path=Path.Combine(root,"Surface062.hexmap");var args=new object[]{path,false,null};Check((bool)Call(w,"TrySaveTo",args),"Save "+args[2]);using(var loaded=new MainWindow()){args=new object[]{path,null,false};Check((bool)Call(loaded,"TryLoadProject",args),"Load "+args[1]);Check(Model(loaded).FormatVersion==11&&Json(Model(loaded).Ink)==result,"Map surface roundtrip");}
                Call(w,"ClearInk");Check(m.Ink.Count==0,"Clear");History(w,false);Check(Json(m.Ink)==result,"Clear undo");History(w,true);Check(m.Ink.Count==0,"Clear redo");
                var size=(NumericUpDown)Get(w,"inkSize");Check(size.Minimum==1&&size.Maximum==200&&size.DecimalPlaces==0&&size.Increment==1,"Integer UI range");Check(InkSizeMath.World(200,.1f,96)>200,"World width incorrectly capped");
            }
            Console.WriteLine("eraser PASS: rendered surfaces at two zooms, sparse sweep, outside preservation, bounds, gesture/history, map roundtrip, integer UI");
        }
        static void Comet(string root)
        {
            using(var renderer=new CometRenderer())using(var b=new Bitmap(640,100))using(var g=Graphics.FromImage(b))
            {
                var points=new[]{new CometPoint(new PointF(20,50),650),new CometPoint(new PointF(290,50),300),new CometPoint(new PointF(610,50),0)};
                g.Clear(Color.Transparent);renderer.Draw(g,new Rectangle(0,0,640,100),points,null,20,Color.Red);
                for(int x=40;x<610;x++)Check(b.GetPixel(x,50).A>0,"Sparse comet hole at "+x);
                for(int x=260;x<320;x++)Check(Math.Abs(b.GetPixel(x,50).A-b.GetPixel(x-1,50).A)<=1,"Alpha bead at sample/join");
                Check(b.GetPixel(285,44).A==0&&b.GetPixel(600,44).A>0,"Taper not faster near tail");b.Save(Path.Combine(root,"comet.png"));
                g.Clear(Color.Transparent);renderer.Draw(g,new Rectangle(0,0,640,100),new[]{new CometPoint(new PointF(20,50),900),new CometPoint(new PointF(610,50),750)},null,20,Color.Red);Check(b.GetPixel(610,50).A==0,"Not expired at 750");
            }
            using(var w=new MainWindow())
            {
                w.Show();Start(w);string map=Json(Model(w));int count=((System.Collections.ICollection)Get(w,"undo")).Count;
                Set(w,"gestureTool",EditorTool.Stylus);Set(w,"gestureButton",MouseButtons.Right);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Right,1,100,120,0));Check(((InkCanvas)Get(w,"canvas")).PointerHidden,"Pointer cursor visible");Call(w,"PointerAt",new Point(400,120));Call(w,"FinishInk",true);Check(!((InkCanvas)Get(w,"canvas")).PointerHidden,"Cursor not restored");
                var trail=(System.Collections.IList)Get(w,"pointerTrail");foreach(var p in trail)p.GetType().GetField("Time",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,0L);Call(w,"AgePointer");Check(trail.Count==0&&!((Timer)Get(w,"pointerTimer")).Enabled,"Expired timer/trail");
                Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Right,1,300,200,0));Check(((System.Collections.IList)Get(w,"pointerTrail")).Count==1,"Old connector survived expiry");Call(w,"ClearPointerContext");Check(!((InkCanvas)Get(w,"canvas")).PointerHidden,"Context cursor");Check(Json(Model(w))==map&&((System.Collections.ICollection)Get(w,"undo")).Count==count,"Pointer changed document/history");
            }
            Console.WriteLine("comet PASS: sparse continuous pixels, no alpha join peaks, taper, 750ms expiry, timer/cursor/no document changes; not a visual PPM gesture");
        }
        static void Panel(string root)
        {
            using(var w=new MainWindow())
            {
                w.Show();Start(w);var m=Model(w);var asset=IconAssets.Starters()[0];m.IconAssets.Add(asset);var first=new MapObject{TemplateId="test062",Name="Pierwszy",AssetId=asset.Id,X=170,Y=140};var second=new MapObject{TemplateId="test062",Name="Drugi",AssetId=asset.Id,X=500,Y=140};m.Objects.AddRange(new[]{first,second});Set(w,"scale",1f);Set(w,"pan",PointF.Empty);
                foreach(bool group in new[]{false,true})
                {
                    if(group){var selected=(HashSet<string>)Get(w,"selectedObjects");selected.Clear();selected.UnionWith(new[]{first.Id,second.Id});Check((bool)Call(w,"CreateObjectGroup","Grupa panelu"),"Group fixture creation");}
                    foreach(bool open in new[]{false,true})
                    {
                        Call(w,"CloseDetailsPanel");if(open)Call(w,"ChooseObjectMember",m.Objects[0],false,false);var obj=m.Objects[0];var start=new Point((int)obj.X,(int)obj.Y);Set(w,"gestureTool",EditorTool.Select);Set(w,"dragOrigin",start);Set(w,"dragging",false);
                        var watch=System.Diagnostics.Stopwatch.StartNew();Call(w,"ObjectMouseDown",new MouseEventArgs(MouseButtons.Left,1,start.X,start.Y,0));watch.Stop();Console.WriteLine("MouseDown group="+group+", open="+open+": "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");
                        if(!open)Check(!(bool)Get(w,"rightPanelOpen"),"Closed panel opened synchronously in MouseDown");
                        int count=((System.Collections.ICollection)Get(w,"undo")).Count;float x=obj.X;Call(w,"ObjectMouseMove",new MouseEventArgs(MouseButtons.Left,0,start.X+30,start.Y+10,0));Call(w,"ObjectMouseUp",new MouseEventArgs(MouseButtons.Left,1,start.X+30,start.Y+10,0));Call(w,"FinishObjectGesture",true);
                        Check((bool)Get(w,"rightPanelOpen")&&Math.Abs(m.Objects[0].X-x-30)<.01&&((System.Collections.ICollection)Get(w,"undo")).Count==count+1,"Drag position/details/history");History(w,false);Check(Math.Abs(m.Objects[0].X-x)<.01,"Drag undo");History(w,true);
                    }
                }
                Call(w,"CloseDetailsPanel");var target=m.Objects[0];var at=new Point((int)target.X,(int)target.Y);Set(w,"dragOrigin",at);Set(w,"gestureTool",EditorTool.Select);Set(w,"dragging",false);Call(w,"ObjectMouseDown",new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0));Call(w,"ObjectMouseUp",new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0));Call(w,"FinishObjectGesture",true);Check((bool)Get(w,"rightPanelOpen"),"Click did not open details");Set(w,"dragOrigin",at);Set(w,"dragging",false);Call(w,"ObjectMouseDown",new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0));Call(w,"ObjectMouseUp",new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0));Call(w,"FinishObjectGesture",true);Check(!(bool)Get(w,"rightPanelOpen"),"Repeat click did not close");
            }
            Console.WriteLine("panel PASS: no closed-panel synchronous Show in down, single/group closed/open, exact move and one history, click/toggle; not frame-level smoothness evidence");
        }
        static void BackgroundUI(string root)
        {
            string file=Path.Combine(root,"Tło próbne.png");using(var image=new Bitmap(1200,800)){using(var g=Graphics.FromImage(image)){g.Clear(Color.DarkSlateBlue);for(int x=0;x<1200;x+=40)g.FillRectangle(x%80==0?Brushes.Gold:Brushes.Coral,x,0,20,800);}image.Save(file);}var source=GraphicSources.Import(file);var bg=new MapBackground{Source=source,SourceId=source.Id,Left=.1,Top=.15,Width=.8,Height=.7,Opacity=75,Saturation=65,Blur=6};
            using(var w=new MainWindow())using(var output=new Bitmap(1600,1000))using(var g=Graphics.FromImage(output))
            {
                w.Show();Start(w);Call(w,"ChangeBackground",bg);Call(w,"CaptureSavedState");string document=Json(Model(w));int count=((System.Collections.ICollection)Get(w,"undo")).Count;
                var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<4;i++)using(var full=BackgroundRenderer.Render(bg,new Size(1300+i*13,800))){}watch.Stop();Console.WriteLine("Previous full decode/filter path, four sizes: "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");
                using(var prepared=GraphicSources.Decode(source)){watch.Restart();for(int i=0;i<4;i++)using(var light=BackgroundRenderer.Render(bg,new Size(1300+i*13,800),prepared,320)){}watch.Stop();Console.WriteLine("Cached-source lightweight render, same four viewports: "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");}
                Call(w,"OnResizeBegin",EventArgs.Empty);watch.Restart();for(int i=0;i<4;i++){w.Size=new Size(1350+i*13,900);Call(w,"DrawBackground",g);}watch.Stop();Console.WriteLine("Live resize prepared source/320px, four sizes: "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");
                Check(((Bitmap)Get(w,"backgroundBitmap")).Width<=320,"Heavy live buffer");var decoded=Get(w,"backgroundSource");Call(w,"DrawBackground",g);Check(ReferenceEquals(decoded,Get(w,"backgroundSource")),"Source decoded per frame");Call(w,"OnResizeEnd",EventArgs.Empty);Call(w,"DrawBackground",g);var canvas=(Control)Get(w,"canvas");
                using(var exact=BackgroundRenderer.Render(bg,canvas.ClientSize))
                {var actual=(Bitmap)Get(w,"backgroundBitmap");Check(actual.Size==exact.Size,"Final resolution");for(int y=0;y<actual.Height;y+=7)for(int x=0;x<actual.Width;x+=7)Check(actual.GetPixel(x,y)==exact.GetPixel(x,y),"Final geometry/filter differs");}
                Check(document==Json(Model(w))&&count==((System.Collections.ICollection)Get(w,"undo")).Count&&!(bool)Get(w,"dirty"),"Resize edits document");
                var layout=(TableLayoutPanel)Get(w,"groupLayout");var frame=(Control)Get(w,"groupFrameColor");var dissolve=layout.Controls.Cast<Control>().Single(c=>c.Text=="Rozwiąż grupę");var delete=layout.Controls.Cast<Control>().Single(c=>c.Text=="Usuń instancje grupy");Check(layout.GetRow(frame)<layout.GetRow(dissolve)&&layout.GetRow(dissolve)<layout.GetRow(delete),"Group bottom order");
                var start=MainWindow.LibraryFolderStartInfo();Check(start.UseShellExecute&&start.Verb=="open"&&start.FileName==LibraryPaths.Root&&String.IsNullOrEmpty(start.Arguments),"Unsafe folder command");
                using(var dialog=new Form())using(var list=new ListView{View=View.Details,Dock=DockStyle.Fill}){for(int i=0;i<7;i++)list.Columns.Add("Column",i==4?185:100);dialog.Controls.Add(list);Call(w,"StyleInstanceList",list);dialog.Show();dialog.Width+=150;Check(list.Columns[4].Width==185,"Fog column expands");}
            }
            var asset=new GraphicDefinition{Source=source,SourceId=source.Id,Name="Miniatura"};asset.Seal();var entries=Enumerable.Range(0,8).Select(i=>new TerrainGraphic{Id="item"+i,Name="Grafika "+i,Asset=asset}).ToList();
            using(var picker=new TerrainGraphicPicker(()=>entries,false,true)){picker.Show();picker.PerformLayout();Call(picker,"UpdateIconSpacing");picker.Update();var list=(ListView)Get(picker,"list");var a=list.GetItemRect(0);var d=list.GetItemRect(3);var e=list.GetItemRect(4);Console.WriteLine("4-column viewport="+list.ClientSize+"; item0="+a+"; item3="+d+"; item4="+e);Check(a.Top==d.Top&&e.Top>a.Top&&list.ClientSize.Width<530,"Compact four columns");}
            using(var dialog=new BackgroundDialog(bg,new Size(900,600))){dialog.Show();var buttons=dialog.Controls.OfType<Button>().ToList();var library=buttons.Single(b=>b.Text=="Z biblioteki…");var import=buttons.Single(b=>b.Text=="Z pliku…");var remove=buttons.Single(b=>b.Text=="Usuń tło");Check(library.Left<import.Left&&import.Left<remove.Left&&remove.ForeColor.R>remove.ForeColor.G&&remove.FlatAppearance.BorderColor==remove.ForeColor,"Background actions order/style");}
            using(var icons=new Bitmap(80,32))using(var g=Graphics.FromImage(icons)){g.Clear(Color.FromArgb(34,38,48));g.DrawImage(EditorIcons.Draw("dark"),8,5);g.DrawImage(EditorIcons.Draw("darkbucket"),45,5);icons.Save(Path.Combine(root,"eyes.png"));}
            Console.WriteLine("background-ui PASS: cache, resize quality/final equivalence, no document change, safe folder launch info, layouts/4 columns/fixed Fog/background actions");
        }
        static void Fixture(string root)
        {
            string sourceFile=Path.Combine(root,"Tło fixture.png");using(var image=new Bitmap(1000,650)){using(var g=Graphics.FromImage(image)){g.Clear(Color.DarkSlateBlue);for(int x=0;x<1000;x+=80)g.FillRectangle(Brushes.Teal,x,0,40,650);}image.Save(sourceFile);}var source=GraphicSources.Import(sourceFile);
            using(var w=new MainWindow())
            {
                Start(w);var m=Model(w);m.Background=new MapBackground{Source=source,SourceId=source.Id,Saturation=65,Blur=6,Opacity=75};
                for(int y=90;y<=230;y+=14)m.Ink.Add(Stroke(Color.Gold,42,new PointF(110,y),new PointF(350,y)));m.Ink.Add(Stroke(Color.Crimson,22,new PointF(230,160)));m.Ink.Add(Stroke(Color.Blue,3,new PointF(75,160),new PointF(385,160)));
                var asset=IconAssets.Starters()[0];m.IconAssets.Add(asset);m.Objects.Add(new MapObject{TemplateId="test062",Name="Próba pierwszego drag",AssetId=asset.Id,X=440,Y=130});m.Objects.Add(new MapObject{TemplateId="test062",Name="Grupa A",AssetId=asset.Id,X=490,Y=220});m.Objects.Add(new MapObject{TemplateId="test062",Name="Grupa B",AssetId=asset.Id,X=550,Y=260});((HashSet<string>)Get(w,"selectedObjects")).UnionWith(m.Objects.Skip(1).Select(o=>o.Id));Check((bool)Call(w,"CreateObjectGroup","Grupa testowa062"),"Fixture group");Call(w,"CloseDetailsPanel");
                var args=new object[]{Path.Combine(root,"Próba 0.6.2.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",args),"Fixture save "+args[2]);Console.WriteLine(args[0]);
            }
        }
        [STAThread] static int Main(string[] args){try{if(args.Length!=1)throw new Exception("One Case required");Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory;if(args[0]=="eraser")Eraser(root);else if(args[0]=="comet")Comet(root);else if(args[0]=="panel")Panel(root);else if(args[0]=="background-ui")BackgroundUI(root);else if(args[0]=="fixture")Fixture(root);else throw new Exception("Unknown Case");return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
    }
}

