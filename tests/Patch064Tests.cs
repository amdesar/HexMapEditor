using System;

using System.Collections.Generic;

using System.Diagnostics;

using System.Drawing;

using System.Drawing.Drawing2D;

using System.IO;

using System.Linq;

using System.Reflection;

using System.Windows.Forms;

namespace HexMapEditor.Tests

{

    internal static class Patch064Tests

    {

        const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;

        static object Get(object o,string n){return o.GetType().GetField(n,P).GetValue(o);}

        static void Set(object o,string n,object v){o.GetType().GetField(n,P).SetValue(o,v);}

        static object Call(object o,string n,params object[] args){return o.GetType().GetMethods(P).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);}

        static void Check(bool ok,string text){if(!ok)throw new Exception(text);}

        static string Json(object value){return MapLogic.Serializer().Serialize(value);}

        static double Time(Action action){var watch=Stopwatch.StartNew();action();return watch.Elapsed.TotalMilliseconds;}

        static InkStroke Stroke(IEnumerable<PointF> points,float width,bool gradient)

        {

            var s=new InkStroke{Width=width,Color="#FFD04A",Gradient=gradient?GradientRules.Rainbow():null,CycleWorld=gradient?1200:0};

            foreach(var point in points){double d=0;if(s.Points.Count>0){var last=s.Points.Last();d=last.Distance+Math.Sqrt(Math.Pow(point.X-last.X,2)+Math.Pow(point.Y-last.Y,2));}s.Points.Add(new InkPoint(point){Distance=d});}return s;

        }

        static List<InkStroke> Drawing(bool gradient,bool crossing)

        {

            return Enumerable.Range(0,6).Select(i=>Stroke(Enumerable.Range(0,160).Select(j=>crossing?new PointF(1800+(float)Math.Sin(j*.17+i*.2)*1600,1100+(float)Math.Sin(j*.31)*850+i*12):new PointF(j*24-200,250+i*290+(float)Math.Sin(j*.07)*50)),80,gradient)).ToList();

        }

        static void Render(Graphics g,List<InkStroke> list,float scale)

        {g.ResetTransform();g.ResetClip();g.SetClip(new Rectangle(0,0,1000,640));g.Clear(Color.Teal);g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;InkLayerRenderer.Draw(g,list,list.LastOrDefault(),new Rectangle(0,0,1000,640));}

        static void Profile(string root)

        {

            Console.WriteLine("Viewport=1000x640; 6 strokes x160 points, width80 world (20px at .25); zoom .25,.5,1,1.46,2; fixed input, renderer times not UI FPS");

            using(var image=new Bitmap(1000,640))using(var g=Graphics.FromImage(image))

            {

                foreach(bool gradient in new[]{false,true})

                {

                    var list=Drawing(gradient,false);InkGradientRenderer.Clear();InkGradientRenderer.ResetMetrics();

                    double zoom=Time(()=>{foreach(float scale in new[]{.25f,.5f,1f,1.46f,2f})Render(g,list,scale);});

                    Console.WriteLine("zoom gradient="+gradient+" ms="+zoom.ToString("F2")+" tiles="+InkGradientRenderer.BuiltTiles+" segments="+InkGradientRenderer.SegmentVisits+" pixels="+InkGradientRenderer.PixelVisits+" pathDraws="+InkGradientRenderer.PathDraws);

                    InkGradientRenderer.ResetMetrics();double warm=Time(()=>{for(int n=0;n<3;n++)Render(g,list,2);});

                    var fresh=Stroke(new[]{new PointF(40,270),new PointF(450,270)},20,gradient);list.Add(fresh);double append=Time(()=>{for(int n=0;n<12;n++){var p=fresh.Points.Last();fresh.Points.Add(new InkPoint(new PointF(p.X+8,p.Y+1)){Distance=p.Distance+Math.Sqrt(65)});fresh.CachedBounds=null;Render(g,list,2);}});

                    Console.WriteLine("warm3="+warm.ToString("F2")+" append12="+append.ToString("F2")+" tiles="+InkGradientRenderer.BuiltTiles+" pixels="+InkGradientRenderer.PixelVisits);

                    foreach(bool cross in new[]{false,true}){list=Drawing(gradient,cross);InkGradientRenderer.Clear();InkGradientRenderer.ResetMetrics();double elapsed=Time(()=>Render(g,list,.25f));Console.WriteLine("cross="+cross+" gradient="+gradient+" render="+elapsed.ToString("F2")+" tiles="+InkGradientRenderer.BuiltTiles+" pixels="+InkGradientRenderer.PixelVisits);}

                }

                var ink=Enumerable.Range(0,10).Select(i=>Stroke(Enumerable.Range(0,180).Select(j=>new PointF(50+j*4,40+i*25+(float)Math.Sin(j*.3)*12)),28,i%2==1)).ToList();

                var saved=ink;double erase=Time(()=>{using(var gesture=new InkEraseGesture(36)){var a=new PointF(80,30);for(int n=1;n<=70;n++){var b=new PointF(80+n*8,30+n*4+(float)Math.Sin(n*.13)*18);gesture.Add(a,b);a=b;}ink=gesture.Apply(ink);}});

                Console.WriteLine("erase70 mixed10x180="+erase.ToString("F2")+" controls="+ink.Sum(s=>InkGeometry.PointCost(s)));Render(g,ink,1);image.Save(Path.Combine(root,"erased.png"));Check(saved.All(s=>s.Surface==null),"Mutated source");Console.WriteLine("emptyPaint12="+Time(()=>{for(int n=0;n<12;n++)Render(g,new List<InkStroke>(),2);}).ToString("F2"));

                foreach(bool gradient in new[]{false,true})using(var comet=new CometRenderer()){var palette=gradient?new GradientSampler(GradientRules.Rainbow()):null;var points=Enumerable.Range(0,80).Select(i=>new CometPoint(new PointF(30+i*11,300+(float)Math.Sin(i*.18)*100),700-i*8,i*.04,palette,Color.Gold)).ToList();g.ResetTransform();Console.WriteLine("comet20 gradient="+gradient+" ms="+Time(()=>{for(int n=0;n<20;n++)comet.DrawColored(g,new Rectangle(0,0,1000,640),points,null,20);}).ToString("F2"));}

            }

            Console.WriteLine("profile PASS");

        }

        static void Library(string root)

        {

            var path=Path.Combine(root,"Own source.png");using(var source=new Bitmap(2048,1024))using(var g=Graphics.FromImage(source)){g.Clear(Color.Coral);for(int y=0;y<1024;y+=7)g.DrawLine(Pens.Navy,0,y,2048,y);source.Save(path);}

            var bytes=GraphicSources.Import(path);var asset=new GraphicDefinition{Name="Own",Source=bytes};asset.Seal();var entries=Enumerable.Range(0,20).Select(i=>new TerrainGraphic{Id="own"+i,Name="Own "+i,Asset=asset}).ToList();

            for(int n=0;n<2;n++){double elapsed=Time(()=>{using(var dialog=new TerrainGraphicPicker(()=>entries,false,true)){dialog.Show();Check(((ListView)Get(dialog,"list")).Items.Count==20,"Entries missing");dialog.Close();}});Console.WriteLine((n==0?"cold":"warm")+" library20 same2048x1024source, no ink, ctor+Show="+elapsed.ToString("F2")+"ms");}

        }

        static Bitmap Picture(IList<InkStroke> strokes,float zoom=1)

        {

            var image=new Bitmap(1000,640);using(var g=Graphics.FromImage(image)){g.Clear(Color.Teal);g.SmoothingMode=SmoothingMode.AntiAlias;g.SetClip(new Rectangle(0,0,image.Width,image.Height));g.ScaleTransform(zoom,zoom);foreach(var stroke in strokes)InkGeometry.Draw(g,stroke);}return image;

        }

        static int Error(Color a,Color b){return Math.Max(Math.Abs(a.R-b.R),Math.Max(Math.Abs(a.G-b.G),Math.Abs(a.B-b.B)));}

        static void SameImage(Bitmap a,Bitmap b,string why,int tolerance=0)

        {int maximum=0;for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)maximum=Math.Max(maximum,Error(a.GetPixel(x,y),b.GetPixel(x,y)));Check(maximum<=tolerance,why+" error="+maximum);}

        static void ImageCase(string root)

        {

            var four=Stroke(new[]{new PointF(100,170),new PointF(420,170),new PointF(300,60),new PointF(300,280)},40,true);four.CycleWorld=500;

            using(var before=Picture(new[]{four}))

            {

                var b=four.Points.Last();double distance=four.Points[2].Distance+110;var expected=new GradientSampler(four.Gradient).Cycle(distance/four.CycleWorld);Check(Error(before.GetPixel(300,170),expected)<8,"Later vertical segment does not win");before.Save(Path.Combine(root,"four.png"));

                InkGradientRenderer.Clear();using(var other=Picture(Drawing(true,true))){}using(var rebuilt=Picture(new[]{four}))SameImage(before,rebuilt,"Eviction changes image");

                List<InkStroke> erased;using(var erase=new InkEraseGesture(36)){erase.Add(new PointF(290,170),new PointF(310,170));erased=erase.Apply(new[]{four});}

                using(var cut=Picture(erased)){Check(cut.GetPixel(300,170).ToArgb()==Color.Teal.ToArgb(),"Old hidden segment exposed in hole");for(int y=0;y<640;y++)for(int x=0;x<1000;x++)if(x<270||x>330||y<150||y>190)if(x>3&&y>3&&x<996&&y<636&&Enumerable.Range(-3,7).All(dx=>Enumerable.Range(-3,7).All(dy=>before.GetPixel(x+dx,y+dy).ToArgb()!=Color.Teal.ToArgb()&&cut.GetPixel(x+dx,y+dy).ToArgb()!=Color.Teal.ToArgb())))Check(Error(before.GetPixel(x,y),cut.GetPixel(x,y))<4,"Erase changed interior color outside hole at "+x+","+y+" error="+Error(before.GetPixel(x,y),cut.GetPixel(x,y)));cut.Save(Path.Combine(root,"four-erased.png"));}

                var read=MapLogic.Serializer().Deserialize<List<InkStroke>>(Json(erased));InkGeometry.Validate(read);using(var a=Picture(erased))using(var c=Picture(read))SameImage(a,c,"Serialization changes order/colors");

            }

            var straight=Stroke(new[]{new PointF(30,400),new PointF(650,400)},24,true);straight.CycleWorld=300;

            using(var zoomed=Picture(new[]{straight},1.46f)){var sampler=new GradientSampler(straight.Gradient);int maximum=0;for(int x=60;x<940;x++){var expected=sampler.Cycle(((x+.5)/1.46-30)/300);maximum=Math.Max(maximum,Error(expected,zoomed.GetPixel(x,584)));}Check(maximum<14,"Noninteger zoom seams/error="+maximum);Console.WriteLine("native gradient146% maxRGBerror="+maximum);zoomed.Save(Path.Combine(root,"zoom146.png"));}

            var dense=Enumerable.Range(0,10).Select(i=>Stroke(Enumerable.Range(0,100).Select(j=>new PointF(80+j*6,120+i*12+(float)Math.Sin(j*.4)*25)),34,i%2==0)).ToList();dense.Add(Stroke(new[]{new PointF(350,180)},12,false));dense.Last().Color="#FF0000";

            var route=Enumerable.Range(0,45).Select(n=>new PointF(150+n*9,100+n*3+(float)Math.Sin(n*.14)*18)).ToList();

            var sequential=dense;for(int i=0;i<route.Count;i++)sequential=InkGeometry.Erase(sequential,route[Math.Max(0,i-1)],route[i],38);

            List<InkStroke> batched;using(var eraser=new InkEraseGesture(38)){for(int i=0;i<route.Count;i++)eraser.Add(route[Math.Max(0,i-1)],route[i]);batched=eraser.Apply(dense);}

            using(var expected=Picture(sequential))using(var actual=Picture(batched)){int different=0,large=0;for(int y=0;y<640;y++)for(int x=0;x<1000;x++){int error=Error(expected.GetPixel(x,y),actual.GetPixel(x,y));if(error>0)different++;if(error>32)large++;}Console.WriteLine("Batch/sequential: changedAApixels="+different+" >32="+large);Check(large<60,"Batch erase materially changes surface");actual.Save(Path.Combine(root,"batch-erase.png"));}

            using(var original=Picture(dense))using(var cut=Picture(batched))for(int y=0;y<640;y++)for(int x=0;x<1000;x++){bool outside=route.All(p=>Math.Pow(x+.5-p.X,2)+Math.Pow(y+.5-p.Y,2)>25*25);if(outside&&x>700)Check(original.GetPixel(x,y)==cut.GetPixel(x,y),"Outside erased footprint changed");}

            // Dense circle matches the same vector silhouette in both modes. Sparse input remains a polygon.

            foreach(int samples in new[]{8,64})foreach(float scale in new[]{.5f,1.46f,2f})

            {

                var pts=Enumerable.Range(0,samples+1).Select(n=>new PointF(180+32*(float)Math.Cos(n*2*Math.PI/samples),160+32*(float)Math.Sin(n*2*Math.PI/samples))).ToArray();var solid=Stroke(pts,8,false);var gradient=Stroke(pts,8,true);

                using(var a=Picture(new[]{solid},scale))using(var b=Picture(new[]{gradient},scale)){for(int y=0;y<640;y++)for(int x=0;x<1000;x++)Check((a.GetPixel(x,y).ToArgb()==Color.Teal.ToArgb())==(b.GetPixel(x,y).ToArgb()==Color.Teal.ToArgb()),"Gradient changes circle silhouette");b.Save(Path.Combine(root,"circle-"+samples+"-"+scale+".png"));}

            }

            using(var w=new MainWindow())

            {

                w.Show();Call(w,"StartNewProject",15,10,"PointyTop","Own064",PresetCatalog.Default());var map=(MapData)Get(w,"map");map.Ink=new List<InkStroke>{four};string before=Json(map.Ink);Set(w,"scale",1f);Set(w,"pan",PointF.Empty);Set(w,"inkToolSize",36f);Set(w,"gestureTool",EditorTool.Eraser);Set(w,"gestureButton",MouseButtons.Left);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,300,160,0));Call(w,"MoveInk",new Point(300,190));Check(Json(map.Ink)==before,"Preview destructively mutates model");Call(w,"FinishInk",true);string after=Json(map.Ink);Check(after!=before,"Eraser commit missing");

                var history=new object[]{false,null};Check((bool)Call(w,"TryHistory",history),"Undo failed");Check(Json(map.Ink)==before,"Undo not exact");history=new object[]{true,null};Check((bool)Call(w,"TryHistory",history)&&Json(map.Ink)==after,"Redo not exact");

                var save=new object[]{Path.Combine(root,"Own064.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",save),"Map save "+save[2]);using(var loaded=new MainWindow()){var args=new object[]{save[0],null,false};Check((bool)Call(loaded,"TryLoadProject",args),"Read "+args[1]);Check(Json(((MapData)Get(loaded,"map")).Ink)==after,"map11 changed ink");}

            }

            // Production compositor: partial invalidation must not rebuild completed ink, and must honor preview holes.

            using(var expected=Picture(new[]{four}))using(var cache=new Bitmap(1000,640))using(var g=Graphics.FromImage(cache))

            {

                g.Clear(Color.Teal);g.SmoothingMode=SmoothingMode.AntiAlias;InkLayerRenderer.Clear();InkGradientRenderer.ResetMetrics();InkLayerRenderer.Draw(g,new[]{four},null,new Rectangle(0,0,1000,640));SameImage(expected,cache,"Layer cache changes picture",3);long passes=InkGradientRenderer.BuiltTiles;

                g.SetClip(new Rectangle(200,100,200,200));InkLayerRenderer.Draw(g,new[]{four},null,new Rectangle(0,0,1000,640));Check(InkGradientRenderer.BuiltTiles==passes,"Dirty region rebuilds immutable ink");g.ResetClip();

                using(var erase=new InkEraseGesture(36)){erase.Add(new PointF(300,150),new PointF(300,190));g.Clear(Color.Teal);g.SetClip(erase.Preview,CombineMode.Exclude);InkLayerRenderer.Draw(g,new[]{four},null,new Rectangle(0,0,1000,640));g.ResetClip();Check(cache.GetPixel(300,170).ToArgb()==Color.Teal.ToArgb(),"Preview ignores cut");Check(InkGradientRenderer.BuiltTiles==passes,"Erase preview rebuilds layer");}

            }

            using(var empty=new Bitmap(1000,640))using(var g=Graphics.FromImage(empty)){Console.WriteLine("empty compositor12="+Time(()=>{for(int n=0;n<12;n++)InkLayerRenderer.Draw(g,new InkStroke[0],null,new Rectangle(0,0,1000,640));}).ToString("F2")+"ms");Check(typeof(InkLayerRenderer).GetField("image",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)==null,"Empty ink retains backing image");}

            var edge=Stroke(new[]{new PointF(30,30),new PointF(100,30)},10,false);List<InkStroke> remaining;using(var erase=new InkEraseGesture(40)){erase.Add(new PointF(85,30),new PointF(120,30));remaining=erase.Apply(new[]{edge});}Check(InkGeometry.Bounds(remaining.Single()).Right<75,"Fit retains removed extreme");

            Console.WriteLine("image PASS: later-order, exact holes/colors, batch vs sequential, silhouette dense/sparse circles, 146% seams, eviction/redraw/read, real erase gesture Undo/Redo/map11/Fit");

        }

        static void Picker(string root)

        {

            using(var solid=new EditorColorDialog(Color.Black))

            {solid.Show();var rgb=(ScrubNumber[])Get(solid,"rgb");Check(rgb[0].Left==rgb[1].Left&&rgb[1].Left==rgb[2].Left&&rgb[0].Bottom<rgb[1].Top&&rgb[1].Bottom<rgb[2].Top,"RGB not vertical");var hex=(TextBox)Get(solid,"hex");hex.Text="#Ab12CD";Check(rgb[0].Value==171&&rgb[1].Value==18&&rgb[2].Value==205,"HEX sync");rgb[1].Value=250;Check(solid.Hex=="#ABFACD","RGB sync");hex.Text="#12";Check(!solid.ColorValid&&!((Button)Get(solid,"accept")).Enabled,"Invalid HEX accepted");solid.SetColor(Color.Blue);Check(solid.Hex=="#0000FF"&&rgb[2].Value==255,"HSV sync");Check(!solid.Controls.OfType<RadioButton>().Any(),"Gradient shown in solid context");solid.Close();}

            var definition=GradientRules.Rainbow();definition.Angle=70;string input=Json(definition);

            using(var dialog=new PaintPickerDialog(Color.Red,definition,true))

            {dialog.Show();var buttons=dialog.Controls.OfType<RadioButton>().ToArray();Check(buttons.Length==2&&buttons.Single(b=>b.Text=="Gradient").Checked,"Direct switches missing");buttons.Single(b=>b.Text=="Jednolity").Checked=true;Check(!buttons.Single(b=>b.Text=="Gradient").Checked,"Not exclusive");buttons.Single(b=>b.Text=="Gradient").Checked=true;var draft=(GradientDefinition)Get(dialog,"draft");var positions=draft.Stops.Select(p=>p.Position).ToArray();dialog.Randomize(new Random(64));Check(positions.SequenceEqual(draft.Stops.Select(p=>p.Position))&&draft.Angle==70&&draft.Stops.Count==definition.Stops.Count,"Random changed geometry");string once=Json(draft);using(var picture=new Bitmap(dialog.Width,dialog.Height))dialog.DrawToBitmap(picture,new Rectangle(Point.Empty,picture.Size));Check(once==Json(draft),"Repaint randomizes");dialog.CancelButton.PerformClick();}

            Check(Json(definition)==input,"Cancel mutated original");using(var dialog=new PaintPickerDialog(Color.Red,definition,false)){dialog.Show();dialog.AcceptButton.PerformClick();Check(GradientRules.Same(definition,dialog.Gradient),"No-op changed palette");}

            Console.WriteLine("picker PASS vertical RGB/sync/validation, exclusive switches, Random own deterministic RNG preserves positions/angle/repaint/cancel/no-op");

        }

        static void Fixture(string root)

        {

            using(var w=new MainWindow())

            {

                Call(w,"StartNewProject",15,10,"PointyTop","Próba 0.6.4",PresetCatalog.Default());var map=(MapData)Get(w,"map");

                map.Ink=Enumerable.Range(0,9).Select(i=>Stroke(Enumerable.Range(0,120).Select(j=>new PointF(40+j*5,40+i*25+(float)Math.Sin(j*.23)*15)),28,i%2==0)).ToList();

                map.Ink.Add(Stroke(new[]{new PointF(100,350),new PointF(420,350),new PointF(300,260),new PointF(300,460)},40,true));

                foreach(int samples in new[]{8,64})map.Ink.Add(Stroke(Enumerable.Range(0,samples+1).Select(n=>new PointF(520+32*(float)Math.Cos(n*2*Math.PI/samples),370+(samples==8?0:100)+32*(float)Math.Sin(n*2*Math.PI/samples))),8,true));

                var save=new object[]{Path.Combine(root,"Próba 0.6.4.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",save),"fixture save "+save[2]);

                string path=Path.Combine(root,"Own source.png");using(var bmp=new Bitmap(180,180))using(var g=Graphics.FromImage(bmp)){g.Clear(Color.Coral);g.DrawLine(Pens.Navy,0,0,180,180);bmp.Save(path);}var source=GraphicSources.Import(path);var asset=new GraphicDefinition{Name="Own source",Source=source};asset.Seal();Directory.CreateDirectory(LibraryPaths.TerrainGraphics);var entries=new List<TerrainGraphic>();for(int n=0;n<20;n++){var entry=TerrainGraphicLibrary.Prepare(LibraryPaths.TerrainGraphics,null,"Own "+n,asset,entries);new TerrainGraphicChange{After=entry}.Write(true);entries.Add(entry);}

                Console.WriteLine("fixture only: "+save[0]);

            }

        }

        static void Input(string root)
        {
            using(var w=new MainWindow())
            {
                w.Show();Call(w,"StartNewProject",15,10,"PointyTop","Own input",PresetCatalog.Default());Call(w,"SelectTool",EditorTool.Stylus);var size=(ScrubNumber)Get(w,"inkSize");size.Focus();size.Controls.OfType<TextBox>().Single().Text="40";Check((float)Get(w,"inkToolSize")==5,"Fixture prematurely committed numeric value");
                var canvas=(Control)Get(w,"canvas");Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,1,150,100,0));Check((MouseButtons)Get(w,"gestureButton")==MouseButtons.Left,"Numeric blur ended fresh Down");Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.Left,0,450,100,0));Call(w,"EndCanvasGesture",true);var strokes=((MapData)Get(w,"map")).Ink;Check(strokes.Count==1&&strokes[0].Points.Count>=2,"First drag after size edit became dot");
            }
            Console.WriteLine("input PASS numeric blur completes before starting a fresh stroke");
        }
        static void Comparison(string root)
        {
            var old=Assembly.LoadFile(File.ReadAllText(Path.Combine(root,"baseline-path.txt")));
            var type=old.GetType("HexMapEditor.InkStroke");var listType=typeof(List<>).MakeGenericType(type);var draw=old.GetType("HexMapEditor.InkGeometry").GetMethod("Draw",BindingFlags.Static|BindingFlags.NonPublic);var clear=old.GetType("HexMapEditor.InkGradientRenderer").GetMethod("Clear",BindingFlags.Static|BindingFlags.NonPublic);
            var sampler=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=Int32.MaxValue};
            foreach(bool cross in new[]{false,true})
            {
                var vertices=new List<PointF>();for(int row=0;row<10;row++){float x=row%2==0?100:900;float y=cross?(row%2==0?100+row*20:600-row*20):100+row*50;vertices.Add(new PointF(x,y));}
                var points=new List<PointF>();for(int n=1;n<vertices.Count;n++)for(int k=0;k<16;k++)points.Add(new PointF(vertices[n-1].X+(vertices[n].X-vertices[n-1].X)*k/16f,vertices[n-1].Y+(vertices[n].Y-vertices[n-1].Y)*k/16f));points.Add(vertices.Last());
                var stroke=Stroke(points,30,true);stroke.CycleWorld=300;var list=new List<InkStroke>{stroke};var baseline=(System.Collections.IEnumerable)sampler.Deserialize(Json(list),listType);
                using(var bitmap=new Bitmap(1000,640))using(var g=Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode=SmoothingMode.AntiAlias;g.SetClip(new Rectangle(0,0,1000,640));clear.Invoke(null,null);double before=Time(()=>{for(int frame=0;frame<3;frame++){g.Clear(Color.Teal);foreach(var s in baseline)draw.Invoke(null,new object[]{g,s});}});InkGradientRenderer.Clear();double after=Time(()=>{for(int frame=0;frame<3;frame++){g.Clear(Color.Teal);InkLayerRenderer.Draw(g,list,null,new Rectangle(0,0,1000,640));}});
                    Console.WriteLine("Comparable route cross="+cross+" points="+points.Count+" worldLength="+stroke.Points.Last().Distance.ToString("F1")+" width30 zoom1 viewport1000x640 render3 incl.cold: 063="+before.ToString("F2")+"ms 064="+after.ToString("F2")+"ms");
                }
            }
            Console.WriteLine("comparison PASS (same JSON rendered by reconstructed 0cf9c24 baseline assembly; not UI FPS)");
        }
        [STAThread]static int Main(string[] args)

        {

            try{if(args.Length!=1)throw new Exception("One Case required");Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory;switch(args[0]){case "profile":Profile(root);break;case "comparison":Comparison(root);break;case "library":Library(root);break;case "image":ImageCase(root);break;case "picker":Picker(root);break;case "fixture":Fixture(root);break;case "input":Input(root);break;default:throw new Exception("Case not implemented: "+args[0]);}return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}

        }

    }

}
