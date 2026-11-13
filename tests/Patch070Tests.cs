using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace HexMapEditor.Tests
{
    internal static class Patch070Tests
    {
        const BindingFlags P=BindingFlags.NonPublic|BindingFlags.Instance;
        static object Get(object o,string n){return o.GetType().GetField(n,P).GetValue(o);}
        static void Set(object o,string n,object value){o.GetType().GetField(n,P).SetValue(o,value);}
        static object Call(object o,string n,params object[] args){return o.GetType().GetMethods(P).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);}
        static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
        static string Json(object o){return MapLogic.Serializer().Serialize(o);}
        static List<InkStroke> Drawing()
        {
            return Enumerable.Range(0,24).Select(i=>new InkStroke{Color=i%2==0?"#FFD04A":"#DC143C",Width=40,Points=Enumerable.Range(0,320).Select(j=>new InkPoint(new PointF(30+j*4,40+i*22+(float)Math.Sin(j*.24)*14))).ToList()}).ToList();
        }
        static void Start(MainWindow w){Call(w,"StartNewProject",15,10,"PointyTop","Own070",PresetCatalog.Default());}
        static void EraserProfile(string root)
        {
            var source=Drawing();var timing=new InkTiming();InkTiming.Current=timing;List<InkStroke> result;
            var watch=Stopwatch.StartNew();using(var gesture=new InkEraseGesture(42)){var a=new PointF(60,35);for(int n=0;n<140;n++){var b=new PointF(60+n*8,35+n*3.7f+(float)Math.Sin(n*.12)*50);gesture.Add(a,b);a=b;}timing.Build=watch.Elapsed.TotalMilliseconds;watch.Restart();result=gesture.Apply(source);Console.WriteLine("Synchronous apply="+watch.Elapsed.TotalMilliseconds.ToString("F2")+"ms "+timing);}
            InkTiming.Current=null;Check(source.All(s=>s.Surface==null),"Source mutated");InkGeometry.Validate(result);Console.WriteLine("24x320 points, width40, 140 curved samples diameter42; remaining controls="+result.Sum(s=>InkGeometry.PointCost(s)));
            using(var w=new MainWindow()){w.Show();Start(w);var map=(MapData)Get(w,"map");map.Ink=source;Set(w,"inkBefore",new List<InkStroke>(source));Set(w,"eraseGesture",new InkEraseGesture(42));var gesture=(InkEraseGesture)Get(w,"eraseGesture");var a=new PointF(60,35);for(int n=0;n<140;n++){var b=new PointF(60+n*8,35+n*3.7f+(float)Math.Sin(n*.12)*50);gesture.Add(a,b);a=b;}watch.Restart();Call(w,"FinishInk",true);Console.WriteLine("FinishInk call/enqueue (completion verified by eraser-async)="+watch.Elapsed.TotalMilliseconds.ToString("F2")+"ms (includes history/dirty/invalidation)");}
            Console.WriteLine("eraser-profile PASS (baseline completion, not preview timing)");
        }
        static void EraserAsync(string root)
        {
            Exception failure=null;var window=new MainWindow();var timer=new System.Windows.Forms.Timer{Interval=15};var clock=Stopwatch.StartNew();double previous=0,maxGap=0;int ticks=0,phase=0;List<InkStroke> original=null;string originalJson=null,afterJson=null;int history=0;MapData expected=null;
            Action begin=()=>
            {
                original=Drawing();var m=(MapData)Get(window,"map");m.Ink=original;originalJson=Json(original);expected=m;history=((System.Collections.ICollection)Get(window,"undo")).Count;
                if(phase==3)original[0].Color="bad";
                Set(window,"inkBefore",new List<InkStroke>(original));var gesture=new InkEraseGesture(42);var a=new PointF(60,35);for(int n=0;n<140;n++){var b=new PointF(60+n*8,35+n*3.7f+(float)Math.Sin(n*.12)*50);gesture.Add(a,b);a=b;}Set(window,"eraseGesture",gesture);
                clock.Restart();previous=0;maxGap=0;ticks=0;var enqueue=Stopwatch.StartNew();Call(window,"FinishInk",true);Console.WriteLine("phase="+phase+" enqueueUI="+enqueue.Elapsed.TotalMilliseconds.ToString("F2")+"ms");Check(Get(window,"pendingInk")!=null,"No pending operation");
                var save=new object[]{Path.Combine(root,"must-not-save.hexmap"),false,null};Check(!(bool)Call(window,"TrySaveTo",save)&&!File.Exists((string)save[0]),"Save bypasses pending");var undo=new object[]{false,null};Check(!(bool)Call(window,"TryHistory",undo),"Undo bypasses pending");
                if(phase==1)Call(window,"CancelInkCommit");
                if(phase==2){Set(window,"documentRevision",(long)Get(window,"documentRevision")+1);var replacement=new MapData{Name="Other document"};Set(window,"map",replacement);}
            };
            window.Shown+=(s,e)=>window.BeginInvoke((Action)(()=>{try{Start(window);begin();timer.Start();}catch(Exception ex){failure=ex;window.Dispose();}}));
            timer.Tick+=(s,e)=>
            {
                try
                {
                    double now=clock.Elapsed.TotalMilliseconds;maxGap=Math.Max(maxGap,now-previous);previous=now;ticks++;
                    if(now>20000)throw new Exception("Pending operation timeout");if(Get(window,"pendingInk")!=null)return;
                    var m=(MapData)Get(window,"map");Console.WriteLine("phase="+phase+" ticks="+ticks+" maxUITimerGap="+maxGap.ToString("F2")+"ms "+window.LastInkCommitTiming);
                    Check(((Control)Get(window,"canvas")).Enabled,"Canvas not restored");
                    if(phase==0)
                    {
                        Check(ticks>1,"No UI progress during work");Check(original.All(x=>x.Surface==null),"Shared source mutated");Check(m.Ink.Any(x=>x.Surface!=null)&&((System.Collections.ICollection)Get(window,"undo")).Count==history+1,"Atomic commit/history");afterJson=Json(m.Ink);
                        var args=new object[]{false,null};Check((bool)Call(window,"TryHistory",args)&&Json(m.Ink)==originalJson,"Undo mismatch");args=new object[]{true,null};Check((bool)Call(window,"TryHistory",args)&&Json(m.Ink)==afterJson,"Redo mismatch");
                        var save=new object[]{Path.Combine(root,"Exact070.hexmap"),false,null};Check((bool)Call(window,"TrySaveTo",save),"Save "+save[2]);using(var loaded=new MainWindow()){args=new object[]{save[0],null,false};Check((bool)Call(loaded,"TryLoadProject",args)&&Json(((MapData)Get(loaded,"map")).Ink)==afterJson,"Roundtrip mismatch");}
                    }
                    else if(phase==1)Check(Json(m.Ink)==originalJson&&((System.Collections.ICollection)Get(window,"undo")).Count==history,"Cancellation applied data/history");
                    else if(phase==2){Check(!ReferenceEquals(m,expected)&&m.Name=="Other document"&&m.Ink.Count==0,"Stale result applied to another document");Start(window);}
                    else Check(m.Ink[0].Color=="bad"&&m.Ink.All(x=>x.Surface==null)&&((System.Collections.ICollection)Get(window,"undo")).Count==history,"Failure partially applied result");
                    phase++;if(phase<4){begin();return;}timer.Stop();Call(window,"CaptureSavedState");window.Close();
                }
                catch(Exception ex){failure=ex;timer.Stop();window.Dispose();}
            };
            Application.Run(window);timer.Dispose();if(failure!=null)throw failure;Console.WriteLine("eraser-async PASS: UI message loop progress, exact atomic result/Undo/Redo/map11, blocked save/history, cancel/stale/error rollback");
        }
        static InkStroke Trace(IEnumerable<PointF> points,float width,bool gradient)
        {
            var s=new InkStroke{Width=width,Gradient=gradient?GradientRules.Rainbow():null,CycleWorld=gradient?1200:0};foreach(var p in points){var next=new InkPoint(p);if(gradient&&s.Points.Count>0){var last=s.Points.Last();next.Distance=last.Distance+Math.Sqrt(Math.Pow(p.X-last.X,2)+Math.Pow(p.Y-last.Y,2));}s.Points.Add(next);}return s;
        }
        static void Layer(Graphics g,IList<InkStroke> ink,float zoom,PointF pan,bool interaction)
        {
            g.ResetTransform();g.ResetClip();g.Clear(Color.Teal);g.SetClip(new Rectangle(0,0,1000,640));g.TranslateTransform(pan.X,pan.Y);g.ScaleTransform(zoom,zoom);
            var draw=typeof(InkLayerRenderer).GetMethod("Draw",BindingFlags.Static|BindingFlags.NonPublic);if(draw.GetParameters().Length==4)draw.Invoke(null,new object[]{g,ink,null,new Rectangle(0,0,1000,640)});else draw.Invoke(null,new object[]{g,ink,null,new Rectangle(0,0,1000,640),interaction,false});
        }
        static double Time(Action operation){var watch=Stopwatch.StartNew();operation();return watch.Elapsed.TotalMilliseconds;}
        static void Camera(string root)
        {
            Console.WriteLine("Map camera ONLY: 1000x640,8x240 points, width90world, x16 spacing; zoom .25/.5/1/1.46; pan8x(12,8), not system window move.");
            using(var image=new Bitmap(1000,640))using(var g=Graphics.FromImage(image))
            {
                foreach(bool gradient in new[]{false,true})
                {
                    var ink=Enumerable.Range(0,8).Select(i=>Trace(Enumerable.Range(0,240).Select(n=>new PointF(30+n*16,50+i*110+(float)Math.Sin(n*.17)*20)),90,gradient)).ToList();InkGeometry.Validate(ink);InkGradientRenderer.Clear();
                    double first=Time(()=>Layer(g,ink,.5f,PointF.Empty,false)),warm=Time(()=>Layer(g,ink,.5f,PointF.Empty,false));double pan=Time(()=>{for(int n=1;n<=8;n++)Layer(g,ink,.5f,new PointF(n*12,n*8),true);});double panExact=Time(()=>Layer(g,ink,.5f,new PointF(96,64),false));using(var shifted=new Bitmap(image)) { InkLayerRenderer.Clear();Layer(g,ink,.5f,new PointF(96,64),false);int max=0;for(int y=0;y<640;y++)for(int x=0;x<1000;x++){var a=shifted.GetPixel(x,y);var b=image.GetPixel(x,y);max=Math.Max(max,Math.Max(Math.Abs(a.R-b.R),Math.Max(Math.Abs(a.G-b.G),Math.Abs(a.B-b.B))));}shifted.Save(Path.Combine(root,"pan-shifted.png"));image.Save(Path.Combine(root,"pan-exact.png"));Console.WriteLine("pan exact maxRGBerror="+max);Check(max<12,"Pan cache alpha/position mismatch");}
                    double zoom=Time(()=>{foreach(float z in new[]{.25f,.5f,1f,1.46f})Layer(g,ink,z,new PointF(96,64),true);});double exact=Time(()=>Layer(g,ink,1.46f,new PointF(96,64),false));
                    var large=Trace(new[]{new PointF(-600,1800),new PointF(5000,1800)},400,gradient);ink.Add(large);double largeZoom=Time(()=>{Layer(g,ink,.25f,PointF.Empty,false);Layer(g,ink,1.46f,new PointF(96,64),true);Layer(g,ink,1.46f,new PointF(96,64),false);});
                    Console.WriteLine("gradient="+gradient+" first="+first.ToString("F2")+" warm="+warm.ToString("F2")+" pan8="+pan.ToString("F2")+" panFinal="+panExact.ToString("F2")+" zoom4="+zoom.ToString("F2")+" zoomFinal="+exact.ToString("F2")+" largeDrawZoom="+largeZoom.ToString("F2")+"ms");
                }
                Console.WriteLine("empty12="+Time(()=>{for(int n=0;n<12;n++)Layer(g,new InkStroke[0],1.46f,new PointF(n*12,n*8),false);}).ToString("F2")+"ms");
                var thick=Trace(Enumerable.Range(0,140).Select(i=>new PointF(90+i*5,250+(float)Math.Sin(i*.032)*140)),100,true);Layer(g,new[]{thick},1.12f,PointF.Empty,false);image.Save(Path.Combine(root,"thick-arc.png"));int outliers=0;for(int y=2;y<638;y++)for(int x=2;x<998;x++){var center=image.GetPixel(x,y);var neighbours=new[]{image.GetPixel(x-1,y),image.GetPixel(x+1,y),image.GetPixel(x,y-1),image.GetPixel(x,y+1)};if(neighbours.Any(c=>c.ToArgb()==Color.Teal.ToArgb()))continue;if(neighbours.All(c=>Math.Max(Math.Abs(c.R-center.R),Math.Max(Math.Abs(c.G-center.G),Math.Abs(c.B-center.B)))>90))outliers++;}Console.WriteLine("thick join isolated color pinholes="+outliers);Check(outliers==0,"Color field pinholes");var sparse=Trace(Enumerable.Range(0,22).Select(i=>new PointF(90+i*24,210+(float)Math.Sin(i*.3)*80)),100,true);Layer(g,new[]{sparse},1.137f,PointF.Empty,false);image.Save(Path.Combine(root,"thick-sparse-1137.png"));foreach(var point in sparse.Points)Check(image.GetPixel((int)(point.X*1.137f),(int)(point.Y*1.137f)).ToArgb()!=Color.Teal.ToArgb(),"Sparse join gap");
            }
            Console.WriteLine("camera PASS: measured rendering, not UI smoothness");
        }
        static GraphicDefinition Graphic(string root,bool transparent=false)
        {
            string path=Path.Combine(root,"Alpha070.png");using(var b=new Bitmap(100,100))using(var g=Graphics.FromImage(b)){g.Clear(transparent?Color.Transparent:Color.FromArgb(128,220,30,10));b.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
            var source=GraphicSources.Import(path);var graphic=new GraphicDefinition{Name="Alpha070",Source=source};graphic.Seal();return graphic;
        }
        static void Quality(string root)
        {
            var graphic=Graphic(root);var c=RepresentativeColors.Get("#000000",graphic,null,false);Check(Math.Abs(c.A-128)<=3&&c.R>210&&c.G<40,"Weighted alpha average: "+c);var transparent=Graphic(root,true);Check(RepresentativeColors.Get("#000000",transparent,null,false).A==0,"Transparent becomes black");
            var gradient=GradientRules.Two("#000000");gradient.Stops[1].Position=1;var average=RepresentativeColors.Get("#000000",null,gradient,false);Check(Math.Abs(average.R-128)<5,"Spatial gradient mean "+average);gradient=new GradientDefinition{Stops=new List<GradientStop>{new GradientStop{Position=0,Color="#000000"},new GradientStop{Position=.1,Color="#FFFFFF"},new GradientStop{Position=1,Color="#FFFFFF"}}};Check(RepresentativeColors.Get("#000000",null,gradient,true).R>220,"Stop average instead of spatial mean");
            using(var window=new MainWindow())
            {
                window.Show();Start(window);var map=(MapData)Get(window,"map");map.Ink.Add(Trace(new[]{new PointF(25,50),new PointF(260,50)},30,true));map.Background=new MapBackground{Source=graphic.Source,SourceId=graphic.SourceId};Call(window,"CaptureSavedState");string before=Json(map);int history=((System.Collections.ICollection)Get(window,"undo")).Count;
                Call(window,"SetLowQuality",true);Check((bool)Get(window,"lowQuality")&&Json(map)==before&&!(bool)Get(window,"dirty")&&history==((System.Collections.ICollection)Get(window,"undo")).Count,"Quality changes document");Check(!(bool)Get(window,"hideBackground"),"Low changes layer flag");
                using(var b=new Bitmap(400,140))using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);InkLayerRenderer.Draw(g,map.Ink,null,new Rectangle(0,0,400,140),false,true);var low=b.GetPixel(130,50);Check(low.R>245&&low.G<10,"Low gradient not first color");InkLayerRenderer.Draw(g,map.Ink,null,new Rectangle(0,0,400,140),false,false);var high=b.GetPixel(130,50);Check(high.ToArgb()!=low.ToArgb(),"High keeps Low cache");b.Save(Path.Combine(root,"quality-high.png"));}
                var canvas=(Control)Get(window,"canvas");Set(window,"gestureTool",EditorTool.Stylus);Set(window,"gestureButton",MouseButtons.Left);Set(window,"inkGradient",GradientRules.Rainbow());Call(window,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,100,100,0));Call(window,"MoveInk",new Point(300,100));Call(window,"FinishInk",true);Check(map.Ink.Last().Gradient!=null&&map.Ink.Last().Points.Last().Distance>0,"Low drawing lost phase");
                string path=Path.Combine(root,"Quality070.hexmap");var save=new object[]{path,false,null};Check((bool)Call(window,"TrySaveTo",save),"Quality save "+save[2]);Check(File.ReadAllText(path).IndexOf("lowQuality",StringComparison.OrdinalIgnoreCase)<0,"Quality serialized");var bad=new object[]{Path.Combine(root,"missing.hexmap"),null,false};Check(!(bool)Call(window,"TryLoadProject",bad)&&(bool)Get(window,"lowQuality"),"Failed open resets High");var load=new object[]{path,null,false};Check((bool)Call(window,"TryLoadProject",load)&&!(bool)Get(window,"lowQuality"),"Successful load not High");Call(window,"SetLowQuality",true);Start(window);Check(!(bool)Get(window,"lowQuality"),"New not High");Call(window,"CaptureSavedState");
            }
            for(int i=0;i<300;i++){var g=GradientRules.Two("#"+i.ToString("X6"));RepresentativeColors.Get("#000000",null,g,false);}Check(RepresentativeColors.Count<=256,"Unbounded representative cache");Console.WriteLine("quality PASS: alpha/spatial sampling, bounded256, session flags/data/history, gradient Low/High + Low gesture phase, map11 roundtrip/reset");
        }
        static void Picker(string root)
        {
            RecentColors.Clear();using(var cancelled=new EditorColorDialog(Color.Red)){cancelled.SetColor(Color.Blue);}Check(RecentColors.Values.Length==0,"Cancel added history");
            for(int i=0;i<14;i++)using(var d=new EditorColorDialog(Color.FromArgb(i,30,50))){d.Show();((Button)Get(d,"accept")).PerformClick();}Check(RecentColors.Values.Length==12&&RecentColors.Values[0].R==13,"Solid acceptance/history limit");RecentColors.Add(RecentColors.Values[5]);Check(RecentColors.Values.Length==12&&RecentColors.Values.Select(c=>c.ToArgb()).Distinct().Count()==12,"Duplicate history");
            string original=Json(RecentColors.Values.Select(c=>c.ToArgb()).ToArray());using(var d=new PaintPickerDialog(Color.Red,GradientRules.Rainbow(),true)){d.Show();var editor=(EditorColorDialog)Get(d,"color");var recent=(Button[])Get(editor,"recent");recent[3].PerformClick();Check(((GradientDefinition)Get(d,"draft")).Stops[0].Color==String.Format("#{0:X2}{1:X2}{2:X2}",RecentColors.Values[3].R,RecentColors.Values[3].G,RecentColors.Values[3].B),"Swatch replaced gradient instead of point");((Button)Get(d,"accept")).PerformClick();Check(d.Gradient!=null,"Gradient acceptance");var selected=(RadioButton)Get(d,"gradientMode");var other=(RadioButton)Get(d,"solidMode");Check(selected.BackColor!=other.BackColor&&selected.FlatAppearance.BorderColor!=other.FlatAppearance.BorderColor&&selected.FlatAppearance.CheckedBackColor==Color.FromArgb(38,65,96),"Unclear selected style");}
            Check(original==Json(RecentColors.Values.Select(c=>c.ToArgb()).ToArray()),"Gradient filled history");using(var d=new EditorColorDialog(Color.Green)){var buttons=(Button[])Get(d,"recent");Check(buttons[0].Left==buttons[6].Left&&buttons[0].Top<buttons[6].Top,"Not 2x6");}RecentColors.Clear();using(var d=new EditorColorDialog(Color.Red))Check(((Button[])Get(d,"recent")).All(b=>!b.Enabled),"Empty selects black");Console.WriteLine("picker PASS: one shared12 MRU/duplicate/move/cancel, gradient excluded + swatch point,2x6/empty/style");
        }
        static void Startup(string root)
        {
            string path=Path.Combine(root,"own-recent.json");var recent=new RecentProjects(path);Check(recent.Entries.Length==0,"Initial history");for(int i=0;i<35;i++)recent.Touch(Path.Combine(root,"Mapa "+i+" ą.hexmap"));Check(recent.Entries.Length==30,"MRU30");recent.Touch(recent.Entries[10].ToUpperInvariant());Check(recent.Entries.Length==30&&recent.Entries.Distinct(StringComparer.OrdinalIgnoreCase).Count()==30,"Case duplicate");var again=new RecentProjects(path);Check(Json(again.Entries)==Json(recent.Entries),"Restart order");
            var available=new System.Collections.Generic.HashSet<string>(recent.Entries.Skip(3).Take(8),StringComparer.OrdinalIgnoreCase);var shown=recent.Available(available.Contains);Check(shown.Length==5&&shown[0]==recent.Entries[3],"Skip missing, scan remainder");available.Add(recent.Entries[0]);Check(recent.Available(available.Contains)[0]==recent.Entries[0]&&recent.Entries.Length==30,"Restoration/history loss");File.WriteAllText(path,"broken");Check(new RecentProjects(path).Entries.Length==0,"Corrupt blocks start");
            using(var w=new MainWindow()){Start(w);var args=new object[]{Path.Combine(root,"Argument ą.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",args),"Save argument fixture");string bytes=File.ReadAllText((string)args[0]);var data=MainWindow.ReadProjectData((string)args[0]);using(var initial=new MainWindow(data,(string)args[0])){Check(ReferenceEquals(Get(initial,"map"),data),"Temporary map replaced argument");Check(!(bool)Get(initial,"lowQuality"),"Startup not High");}Check(File.ReadAllText((string)args[0])==bytes,"Read modifies map");}
            bool opened=false;using(var start=new StartWindow((m,p)=>opened=true)){Check(!opened&&start.Controls.OfType<FlowLayoutPanel>().Single().Controls.Count==0,"Premature editor");Set(start,"choosing",true);Call(start,"ChooseNew");Call(start,"ChooseOpen");Check(!start.OpenPath("missing.hexmap")&&!opened,"Reentrant start operation");}Console.WriteLine("start PASS: MRU30/5/missing/restore/case/restart/corrupt, shared map11 read argument without placeholder; dialog cancellation exercised in final EXE");
        }
        static void Integration(string root)
        {
            // New assertions only; previously passed eraser/camera/picker cases are not repeated.
            string sourcePath=Path.Combine(root,"crop.png");using(var b=new Bitmap(200,100))using(var g=Graphics.FromImage(b)){g.Clear(Color.Red);g.FillRectangle(Brushes.Blue,100,0,100,100);b.Save(sourcePath);}
            var source=GraphicSources.Import(sourcePath);var graphic=new GraphicDefinition{Name="Two crops",Source=source,Pointy=new CropTransform{X=.25,Y=.5},Flat=new CropTransform{X=.75,Y=.5}};graphic.Seal();var a=RepresentativeColors.Get("#000000",graphic,null,false);var bcolor=RepresentativeColors.Get("#000000",graphic,null,true);Check(a.R>240&&bcolor.B>240,"Wrong orientation/crop mean "+a+" "+bcolor);
            var recent=new RecentProjects(Path.Combine(root,"boundary.json"));recent.Touch(Path.Combine(root,"known.hexmap"));string before=File.ReadAllText(Path.Combine(root,"boundary.json"));for(int i=0;i<30;i++)recent.Touch(Path.Combine(root,new String('ą',12000)+i+".hexmap"));Check(recent.Warning!=null&&File.ReadAllText(Path.Combine(root,"boundary.json")).Length>0,"No failure for unsupported long path");var read=new RecentProjects(Path.Combine(root,"boundary.json"));Check(read.Warning==null&&read.Entries.Length>0&&read.Entries.Length<30,"Failed path write damaged history");
            using(var window=new MainWindow()){window.Show();Start(window);var map=(MapData)Get(window,"map");map.Cells[0].Fog=true;map.Cells[1].Darkness=true;map.Cells[1].DarkFill=new FillVariant{Gradient=GradientRules.Rainbow()};Call(window,"CaptureSavedState");var first=(bool)Call(window,"CellMasked",0);var second=(bool)Call(window,"CellMasked",1);Call(window,"SetLowQuality",true);Check(first&&(bool)Call(window,"CellMasked",0)&&second&&(bool)Call(window,"CellMasked",1),"Low reveals protected data");using(var tile=new TerrainTile(id=>{},id=>{},id=>{})){var terrain=PresetCatalog.Default().Terrains[0];terrain.Variants=new List<FillVariant>{new FillVariant{Graphic=graphic,GraphicId=graphic.Id},new FillVariant{Gradient=GradientRules.Rainbow()}};tile.UpdateTerrain(terrain,true,false,true);using(var expected=TerrainMosaic.Render(terrain,tile.Choose.ClientSize,false,true)){var actual=(Bitmap)tile.Choose.FillImage;Check(actual.GetPixel(10,10)==expected.GetPixel(10,10),"Tile not Low mosaic");}}Call(window,"CaptureSavedState");}
            Console.WriteLine("integration PASS: actual opposite crops, MRU unsupported long-path write leaves readable history, logical masks unchanged in Low, tile uses same representative mosaic");
        }
        static void Fixture(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);var map=(MapData)Get(w,"map");map.Name="Próba 0.7.0";map.Ink=Enumerable.Range(0,16).Select(i=>Trace(Enumerable.Range(0,200).Select(n=>new PointF(30+n*5,30+i*22+(float)Math.Sin(n*.2)*12)),28,i%2!=0)).ToList();map.Ink.Add(Trace(Enumerable.Range(0,80).Select(i=>new PointF(70+i*7,420+(float)Math.Sin(i*.06)*60)),90,true));
                var graphic=Graphic(root);map.Background=new MapBackground{Source=graphic.Source,SourceId=graphic.SourceId,Saturation=40,Opacity=50};var terrain=map.Presets[0].Terrains[2];terrain.Variants=new List<FillVariant>{new FillVariant{Gradient=GradientRules.Rainbow()},new FillVariant{Graphic=graphic,GraphicId=graphic.Id}};FillRules.SetPool(terrain,terrain.Variants);
                foreach(var cell in map.Cells.Take(15)){var fill=terrain.Variants[map.Cells.IndexOf(cell)%2];cell.Terrain=terrain.Name;cell.TerrainId=terrain.TerrainId;cell.VariantId=terrain.VariantId;cell.TerrainRevision=terrain.Revision;cell.FillVariantId=fill.Id;cell.Color=fill.Color;cell.Texture=fill.Graphic;cell.TextureId=fill.GraphicId;cell.Gradient=fill.Gradient==null?null:fill.Gradient.Copy();}
                var save=new object[]{Path.Combine(root,"Próba 0.7.0.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",save),"Fixture save "+save[2]);Console.WriteLine("fixture ONLY "+save[0]);
            }
        }
        [STAThread]static int Main(string[] args)
        {
            try{if(args.Length!=1)throw new Exception("One explicit Case required");Application.EnableVisualStyles();switch(args[0]){case "eraser-profile":EraserProfile(AppDomain.CurrentDomain.BaseDirectory);break;case "eraser-async":EraserAsync(AppDomain.CurrentDomain.BaseDirectory);break;case "camera":Camera(AppDomain.CurrentDomain.BaseDirectory);break;case "quality":Quality(AppDomain.CurrentDomain.BaseDirectory);break;case "picker":Picker(AppDomain.CurrentDomain.BaseDirectory);break;case "start":Startup(AppDomain.CurrentDomain.BaseDirectory);break;case "integration":Integration(AppDomain.CurrentDomain.BaseDirectory);break;case "fixture":Fixture(AppDomain.CurrentDomain.BaseDirectory);break;default:throw new Exception("Case not implemented yet: "+args[0]);}return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
