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
    internal static class Patch063Tests
    {
        const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Get(object o,string n){return o.GetType().GetField(n,P).GetValue(o);}
        static void Set(object o,string n,object v){o.GetType().GetField(n,P).SetValue(o,v);}
        static object Call(object o,string n,params object[] args){return o.GetType().GetMethods(P).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);}
        static void Check(bool b,string message){if(!b)throw new Exception(message);}
        static string Json(object o){return MapLogic.Serializer().Serialize(o);}
        static MapData Model(MainWindow w){return (MapData)Get(w,"map");}
        static void Start(MainWindow w){Set(w,"libraryPresets",new List<TerrainPreset>{PresetCatalog.Default()});Call(w,"StartNewProject",15,10,"PointyTop","Próba063",PresetCatalog.Default());}
        static void History(MainWindow w,bool redo){var a=new object[]{redo,null};Check((bool)Call(w,"TryHistory",a),"History: "+a[1]);}
        static double Time(Action action){var t=Stopwatch.StartNew();action();return t.Elapsed.TotalMilliseconds;}
        static List<InkStroke> Drawing()
        {
            return Enumerable.Range(0,12).Select(i=>new InkStroke{Color=i%2==0?"#FFD04A":"#DC143C",Width=28,Points=Enumerable.Range(0,240).Select(j=>new InkPoint(new PointF(40+j*3,50+i*23+(float)Math.Sin(j*.35)*12))).ToList()}).ToList();
        }
        static void InkProfile(string root)
        {
            var ink=Drawing();using(var image=new Bitmap(850,420))using(var g=Graphics.FromImage(image))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                Action<List<InkStroke>,string> profile=(list,label)=>
                {
                    double validation=Time(()=>{for(int n=0;n<25;n++)InkGeometry.Validate(list);});
                    double paint=Time(()=>{for(int n=0;n<25;n++){g.Clear(Color.Teal);foreach(var s in list)InkGeometry.Draw(g,s);}});
                    Console.WriteLine(label+": strokes="+list.Count+", controls="+list.Sum(s=>InkGeometry.PointCost(s))+", Validate25="+validation.ToString("F2")+"ms, Paint25="+paint.ToString("F2")+"ms");
                };
                profile(ink,"before");var original=ink;double erase=Time(()=>{PointF previous=new PointF(80,40);for(int n=1;n<=90;n++){var next=new PointF(80+n*7,40+n*3+(float)Math.Sin(n*.2)*20);ink=InkGeometry.Erase(ink,previous,next,38);previous=next;}});Console.WriteLine("long erase90="+erase.ToString("F2")+"ms");
                profile(ink,"after erase");var fresh=new InkStroke{Points=new List<InkPoint>{new InkPoint(new PointF(30,360))}};ink.Add(fresh);double append=Time(()=>{for(int n=1;n<=90;n++){fresh.Points.Add(new InkPoint(new PointF(30+n*7,360)));InkGeometry.Validate(ink);}});Console.WriteLine("new stroke90 validations="+append.ToString("F2")+"ms");profile(new List<InkStroke>(),"after clear");
                File.WriteAllText(Path.Combine(root,"surface.json"),Json(ink));using(var w=new MainWindow()){Start(w);Model(w).Ink=ink;var a=new object[]{Path.Combine(root,"Surface063.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",a),"Save "+a[2]);using(var loaded=new MainWindow()){a=new object[]{a[0],null,false};Check((bool)Call(loaded,"TryLoadProject",a),"Load "+a[1]);Check(Json(Model(loaded).Ink)==Json(ink),"Surface roundtrip");}}
                Check(original.All(s=>s.Surface==null),"Original mutated");g.Clear(Color.Teal);foreach(var s in ink)InkGeometry.Draw(g,s);image.Save(Path.Combine(root,"after.png"));
            }
            Console.WriteLine("ink-profile PASS (local timed model/render, not visual smoothness)");
        }
        static void Gradient(string root)
        {
            var definition=GradientRules.Two("#000000");GradientRules.Validate(definition);Check(GradientRules.At(definition,.5).R==128,"RGB interpolation");definition.Stops[1].Color="#FF0000";var end=definition.Stops[1].Color;Check(GradientRules.Cycle(definition,.999999).R<2&&GradientRules.Cycle(definition,0).R==0&&definition.Stops[1].Color==end,"Smooth return overwrites end/jumps");var rainbow=GradientRules.Rainbow();Check(GradientRules.At(rainbow,0)==GradientRules.At(rainbow,1),"Rainbow wrap");
            using(var bar=new GradientBar{Draft=definition.Copy(),Size=new Size(300,55)}){Check(bar.Add(.5),"Add stop");Check(!bar.Add(.505),"Minimum spacing");bar.MoveSelected(.7);Check(Math.Abs(bar.Draft.Stops[1].Position-.7)<.0001,"Move stop");bar.RemoveSelected();Check(bar.Draft.Stops.Count==2,"Remove internal");bar.Selected=0;bar.RemoveSelected();Check(bar.Draft.Stops.Count==2,"Removed endpoint");}
            bool rejected=false;var bad=definition.Copy();bad.Stops[1].Position=Double.NaN;try{GradientRules.Validate(bad);}catch(InvalidDataException){rejected=true;}Check(rejected,"NaN accepted");
            using(var picker=new EditorColorDialog(Color.Black))
            {
                picker.Show();var hex=(TextBox)Get(picker,"hex");hex.Text="#aB12cd";Check(picker.Hex=="#AB12CD","HEX");var rgb=(NumericUpDown[])Get(picker,"rgb");Check(rgb[0].Value==171&&rgb[1].Value==18&&rgb[2].Value==205,"HEX to RGB");rgb[0].Value=32;Check(picker.Hex=="#2012CD","RGB to HEX/HSV");string valid=picker.Hex;hex.Text="#12";Check(!picker.ColorValid&&!((Button)Get(picker,"accept")).Enabled&&picker.Hex==valid,"Partial HEX changed valid data");picker.SetColor(Color.Lime);Check(hex.Text=="#00FF00"&&rgb[1].Value==255,"HSV/color sync");Check(!picker.Controls.Cast<Control>().Any(c=>c.Text=="Gradient"),"Solid context offers gradient");picker.CancelButton.PerformClick();
            }
            string before=Json(definition);using(var picker=new PaintPickerDialog(Color.Black,definition,true)){picker.Show();((GradientDefinition)Get(picker,"draft")).Stops[1].Color="#FFFFFF";picker.CancelButton.PerformClick();}Check(Json(definition)==before,"Picker cancel mutated definition");
            using(var picker=new PaintPickerDialog(Color.Black,definition,false)){picker.Show();picker.AcceptButton.PerformClick();Check(picker.DialogResult==DialogResult.OK&&GradientRules.Same(picker.Gradient,definition),"No-op gradient result");}
            Console.WriteLine("gradient PASS: linear RGB, cycle return/rainbow, stop add/move/remove/min spacing, HEX/RGB/HSV sync and invalid partial, solid context, cancel/no-op");
        }
        static Bitmap RenderInk(List<InkStroke> strokes){var image=new Bitmap(850,260);using(var g=Graphics.FromImage(image)){g.Clear(Color.Teal);g.SmoothingMode=SmoothingMode.AntiAlias;foreach(var stroke in strokes)InkGeometry.Draw(g,stroke);}return image;}
        static void InkGradient(string root)
        {
            var rainbow=GradientRules.Rainbow();var stroke=new InkStroke{Width=40,Gradient=rainbow.Copy(),CycleWorld=300,Points=new List<InkPoint>{new InkPoint(new PointF(30,100)),new InkPoint(new PointF(300,100)){Distance=270}}};
            using(var before=RenderInk(new List<InkStroke>{stroke}))
            {stroke.Points.Add(new InkPoint(new PointF(700,100)){Distance=670});stroke.CachedBounds=null;using(var extended=RenderInk(new List<InkStroke>{stroke})){for(int x=40;x<260;x++)Check(before.GetPixel(x,100)==extended.GetPixel(x,100),"Extension remaps old color");extended.Save(Path.Combine(root,"rainbow.png"));}}
            using(var zoomed=new Bitmap(1200,320))using(var g=Graphics.FromImage(zoomed)){g.Clear(Color.Teal);g.ScaleTransform(1.46f,1.46f);InkGeometry.Draw(g,stroke);int error=0;var sampler=new GradientSampler(stroke.Gradient);for(int x=70;x<1000;x++){var expected=sampler.Cycle(((x+.5)/1.46-30)/300);var actual=zoomed.GetPixel(x,146);error=Math.Max(error,Math.Max(Math.Abs(expected.R-actual.R),Math.Max(Math.Abs(expected.G-actual.G),Math.Abs(expected.B-actual.B))));}Console.WriteLine("Gradient zoom/tile maximum channel error="+error);Check(error<12,"Visible gradient tile seams");}
            InkGeometry.Validate(new List<InkStroke>{stroke});var erased=InkGeometry.Erase(new List<InkStroke>{stroke},new PointF(350,100),new PointF(420,100),60);Check(erased.Single().Points.Count==3&&ReferenceEquals(erased[0].Points,stroke.Points),"Eraser discarded original color trace");
            using(var before=RenderInk(new List<InkStroke>{stroke}))using(var after=RenderInk(erased)){Check(after.GetPixel(380,100).ToArgb()==Color.Teal.ToArgb(),"Gradient hole not transparent");for(int x=30;x<710;x++)if(x<315||x>455)Check(before.GetPixel(x,100)==after.GetPixel(x,100),"Eraser remaps surviving colors at "+x);after.Save(Path.Combine(root,"rainbow-erased.png"));}
            var restored=MapLogic.Serializer().Deserialize<List<InkStroke>>(Json(erased));InkGeometry.Validate(restored);using(var a=RenderInk(erased))using(var b=RenderInk(restored)){for(int x=30;x<710;x++)Check(a.GetPixel(x,100)==b.GetPixel(x,100),"Loaded trace changes color");}
            using(var w=new MainWindow())
            {
                w.Show();Start(w);Set(w,"scale",1f);Set(w,"pan",PointF.Empty);Set(w,"inkGradient",rainbow.Copy());Set(w,"inkToolSize",40f);Set(w,"gestureTool",EditorTool.Stylus);Set(w,"gestureButton",MouseButtons.Left);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,50,150,0));Call(w,"MoveInk",new Point(650,150));Call(w,"FinishInk",true);string original=Json(Model(w).Ink);Check(Model(w).Ink.Single().CycleWorld==300&&Model(w).Ink[0].Points.Last().Distance==600,"Gesture cycle/distance");
                Set(w,"gestureTool",EditorTool.Eraser);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,350,150,0));Call(w,"FinishInk",true);string cut=Json(Model(w).Ink);History(w,false);Check(Json(Model(w).Ink)==original,"Gradient erase undo");History(w,true);Check(Json(Model(w).Ink)==cut,"Gradient erase redo");
                var args=new object[]{Path.Combine(root,"GradientInk063.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",args),"Save "+args[2]);using(var loaded=new MainWindow()){args=new object[]{args[0],null,false};Check((bool)Call(loaded,"TryLoadProject",args),"Load "+args[1]);Check(Json(Model(loaded).Ink)==cut,"Map gradient ink roundtrip");}
                string map=Json(Model(w));int count=((System.Collections.ICollection)Get(w,"undo")).Count;Set(w,"pointerGradient",rainbow.Copy());Set(w,"pointerPalette",new GradientSampler(rainbow));Call(w,"PointerAt",new Point(100,100));Call(w,"PointerAt",new Point(700,100));var trail=(System.Collections.IList)Get(w,"pointerTrail");Check((double)trail[1].GetType().GetField("Phase",P).GetValue(trail[1])==2,"Pointer phase not distance");Call(w,"PointerAt",new Point(700,100));Check((double)trail[2].GetType().GetField("Phase",P).GetValue(trail[2])==2,"Idle changes pointer hue");Set(w,"pointerActive",true);foreach(var sample in trail)sample.GetType().GetField("Time",P).SetValue(sample,0L);Call(w,"AgePointer");Check((double)Get(w,"pointerPhase")==2,"Standing active pointer changes hue by clock");Call(w,"PointerAt",new Point(700,100));Call(w,"FinishInk",true);Call(w,"PointerAt",new Point(730,100));Check((double)trail[trail.Count-1].GetType().GetField("Phase",P).GetValue(trail[trail.Count-1])>2,"Live click resets phase");Call(w,"ClearPointerContext");Call(w,"PointerAt",new Point(100,100));Check((double)trail[0].GetType().GetField("Phase",P).GetValue(trail[0])==0,"Context phase not reset");Check(map==Json(Model(w))&&count==((System.Collections.ICollection)Get(w,"undo")).Count,"Pointer persisted");Call(w,"ClearPointerContext");
            }
            using(var comet=new CometRenderer())using(var image=new Bitmap(700,70))using(var g=Graphics.FromImage(image)){g.Clear(Color.Black);comet.DrawColored(g,new Rectangle(0,0,700,70),new[]{new CometPoint(new PointF(20,35),600,0,new GradientSampler(rainbow),Color.Red),new CometPoint(new PointF(680,35),0,2.2,new GradientSampler(rainbow),Color.Red)},null,20);for(int x=100;x<670;x++)Check(image.GetPixel(x,35).ToArgb()!=Color.Black.ToArgb(),"Sparse colored comet gap");image.Save(Path.Combine(root,"colored-comet.png"));}
            Console.WriteLine("ink-gradient PASS: incremental colors, two cycles/sparse stroke, eraser preserves surviving colors, undo/redo/map roundtrip, pointer phase/context/no document, continuous sparse colored comet render");
        }
        static void Fills(string root)
        {
            var gradient=GradientRules.Two("#FF0000");gradient.Stops[1].Color="#0000FF";var fills=new List<FillVariant>{new FillVariant{Gradient=gradient.Copy(),Color="#FF0000"},new FillVariant{Color="#00AA00"}};
            var history=new AppearanceHistory(fills);var change=history.Current;change[0].Gradient.Angle=90;Check(history.Commit(change),"Local gradient commit");history.Move(false);Check(history.Current[0].Gradient.Angle==0,"Local gradient undo");history.Move(true);Check(history.Current[0].Gradient.Angle==90&&fills[0].Gradient.Angle==0,"Local history mutable alias");
            foreach(bool flat in new[]{false,true})using(var image=new Bitmap(140,120))using(var g=Graphics.FromImage(image))using(var mask=TerrainTextures.Mask(new PointF(70,60),50,flat))
            {g.Clear(Color.Black);GradientRules.Fill(g,mask,gradient);Check(image.GetPixel(45,60).R>image.GetPixel(45,60).B&&image.GetPixel(95,60).B>image.GetPixel(95,60).R,"Gradient rotated/cross-hex axis");image.Save(Path.Combine(root,"hex-"+flat+".png"));}
            using(var w=new MainWindow())
            {
                w.Show();Start(w);var args=new object[]{LibraryPaths.Presets,"Gradient063",null};Check((bool)Call(w,"TryCreatePreset",args),"Actual new preset: "+args[2]);var terrain=new TerrainDefinition{Name="Gradient mieszany"};FillRules.SetPool(terrain,fills);Call(w,"AddTerrain",terrain);terrain=((TerrainPreset)Call(w,"ActivePreset")).Terrains.Last();var m=Model(w);for(int i=0;i<6;i++)MapLogic.ApplyTerrain(m.Cells[i],terrain,terrain.Variants[i%2]);string original=Json(m.Cells);var ids=m.Cells.Take(6).Select(c=>c.FillVariantId).ToArray();
                var next=terrain.Copy();next.Variants[0].Gradient.Angle=90;next.Variants.Add(new FillVariant{Color="#FFFF00"});typeof(MainWindow).GetMethod("VersionTerrain",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{terrain,next});Call(w,"UpdateTerrain",terrain,next,false,true);Check(Json(m.Cells)==original,"Unrequested retroactive gradient update");History(w,false);Call(w,"UpdateTerrain",terrain,next,true,true);Check(ids.SequenceEqual(m.Cells.Take(6).Select(c=>c.FillVariantId))&&m.Cells[0].Gradient.Angle==90&&m.Cells[1].Gradient==null,"Layout/solid position changed");string applied=Json(m.Cells);History(w,false);Check(Json(m.Cells)==original,"Gradient apply undo");History(w,true);Check(Json(m.Cells)==applied,"Gradient apply redo");
                Set(w,"appearanceRandom",new Random(63));for(int i=10;i<30;i++)Call(w,"PaintCell",i);Call(w,"FinishStroke");string painted=Json(m.Cells);using(var image=TerrainMosaic.Render(next,new Size(160,32),false)){image.Save(Path.Combine(root,"mosaic.png"));}Check(Json(m.Cells)==painted,"Mosaic repaints data");
                args=new object[]{LibraryPaths.Presets,false,null,null};Check((bool)Call(w,"TrySaveWorkingPreset",args),"Preset save "+args[3]);var preset=PresetCatalog.ReadFile(PresetCatalog.FindFile(LibraryPaths.Presets,m.ActivePresetId));Check(preset.PresetFormatVersion==4&&preset.Terrains.Last().Variants[0].Gradient.Angle==90,"Preset gradient roundtrip");
                var maskPool=new List<FillVariant>{new FillVariant{Color="#FFFFFF",Gradient=GradientRules.Two("#FFFFFF")}};m.Cells[0].Fog=true;Call(w,"ChangeFogAppearance",maskPool,true,true);Check((bool)Call(w,"CellMasked",0),"White gradient unmasks Fog");m.Cells[1].Darkness=true;Call(w,"ChangeDarkAppearance",maskPool,true,true);Check((bool)Call(w,"CellMasked",1),"Gradient unmasks restriction");
                args=new object[]{Path.Combine(root,"Fills063.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",args),"Map save "+args[2]);using(var loaded=new MainWindow()){var a=new object[]{args[0],null,false};Check((bool)Call(loaded,"TryLoadProject",a),"Map read "+a[1]);Check(Json(Model(loaded).Cells)==Json(m.Cells)&&Model(loaded).FormatVersion==11,"Map gradients/snapshots");string keep=Json(Model(loaded));string badPath=Path.Combine(root,"BadGradient.hexmap");File.WriteAllText(badPath,File.ReadAllText((string)args[0]).Replace("\"Angle\":90","\"Angle\":361"));a=new object[]{badPath,null,false};Check(!(bool)Call(loaded,"TryLoadProject",a)&&keep==Json(Model(loaded)),"Invalid gradient replaced current document");}
            }
            bool rejected=false;try{GradientRules.Budget(Enumerable.Repeat(GradientRules.Rainbow(),15000),100000,"próby");}catch(InvalidDataException){rejected=true;}Check(rejected,"Gradient budget ignored");Console.WriteLine("fills PASS: own per-hex gradient/orientations, local history/deep snapshots, actual preset creation, mixed pool/apply preserving layout/no retroactive updates, map11/preset4, masks independent of color, invalid read preserved map/budget");
        }
        static void UI(string root)
        {
            string file=Path.Combine(root,"źródło.png");using(var image=new Bitmap(40,40)){using(var g=Graphics.FromImage(image))g.Clear(Color.Coral);image.Save(file);}var source=GraphicSources.Import(file);var asset=new GraphicDefinition{Name="Próba",Source=source,SourceId=source.Id};asset.Seal();var entries=Enumerable.Range(0,20).Select(i=>new TerrainGraphic{Id="entry"+i,Name="Grafika "+i,Asset=asset}).ToList();
            using(var picker=new TerrainGraphicPicker(()=>entries,false,true))
            {picker.Show();Call(picker,"UpdateIconSpacing");var list=(ListView)Get(picker,"list");var first=list.GetItemRect(0);int step=list.GetItemRect(1).Left-first.Left;int columns=list.Items.Cast<ListViewItem>().Count(item=>item.Index<10&&list.GetItemRect(item.Index).Top==first.Top);Check(columns==4,"Default columns "+columns);picker.Width+=255;Call(picker,"UpdateIconSpacing");int wide=list.Items.Cast<ListViewItem>().Count(item=>item.Index<10&&list.GetItemRect(item.Index).Top==first.Top);Check(wide>=6&&list.GetItemRect(1).Left-list.GetItemRect(0).Left==step,"Spacing stretched/not responsive");Console.WriteLine("Library columns "+columns+" -> "+wide+", spacing="+step);}
            using(var dialog=new BackgroundDialog(null,new Size(800,500))){var remove=(Button)Get(dialog,"remove");Check(!remove.Enabled&&remove.ForeColor==Color.Gray&&remove.FlatAppearance.BorderColor==Color.Gray,"Disabled remove border active red");}
            using(var w=new MainWindow())using(var host=new Form{ClientSize=new Size(850,240)})using(var list=new DarkObjectList{Dock=DockStyle.Fill,View=View.Details})
            {w.Show();Start(w);list.Columns.Add("Nazwa",150);list.Columns.Add("Mgła",185);host.Controls.Add(list);Call(w,"StyleInstanceList",list);host.Show();list.Update();using(var image=new Bitmap(list.Width,list.Height)){list.DrawToBitmap(image,list.ClientRectangle);image.Save(Path.Combine(root,"dark-header.png"));var pixel=image.GetPixel(600,10);Check(pixel.R<100&&pixel.G<100&&pixel.B<100,"White residual header "+pixel);}host.Width+=80;Check(list.Columns[1].Width==185,"Fog expands");}
            Console.WriteLine("ui PASS: fixed spacing/responsive columns, disabled border, dark residual header, fixed Fog; panel/resize inspected, no risky composition change");
        }
        static void Fixture(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);var args=new object[]{LibraryPaths.Presets,"Próba gradientów063",null};Check((bool)Call(w,"TryCreatePreset",args),"Fixture preset "+args[2]);var m=Model(w);var gradient=GradientRules.Two("#FFB000");gradient.Stops[1].Color="#0044FF";var terrain=new TerrainDefinition{Name="Gradient heksa"};FillRules.SetPool(terrain,new List<FillVariant>{new FillVariant{Color="#FFB000",Gradient=gradient}});Call(w,"AddTerrain",terrain);terrain=((TerrainPreset)Call(w,"ActivePreset")).Terrains.Last();for(int i=0;i<30;i++)MapLogic.ApplyTerrain(m.Cells[i],terrain,terrain.Variants[0]);
                m.Ink=Drawing();foreach(var stroke in m.Ink)foreach(var point in stroke.Points)point.X*=.8f;
                m.Ink.Add(new InkStroke{Width=22,Gradient=GradientRules.Rainbow(),CycleWorld=300,Points=new List<InkPoint>{new InkPoint(new PointF(80,360)),new InkPoint(new PointF(620,360)){Distance=540}}});
                string sourceFile=Path.Combine(root,"Grafika biblioteki.png");using(var image=new Bitmap(64,64)){using(var g=Graphics.FromImage(image)){g.Clear(Color.Coral);g.FillEllipse(Brushes.DarkBlue,12,12,40,40);}image.Save(sourceFile);}var source=GraphicSources.Import(sourceFile);var asset=new GraphicDefinition{Name="Własny materiał",Source=source,SourceId=source.Id};asset.Seal();var entries=new List<TerrainGraphic>();Directory.CreateDirectory(LibraryPaths.TerrainGraphics);for(int i=0;i<20;i++){var item=TerrainGraphicLibrary.Prepare(LibraryPaths.TerrainGraphics,null,"Grafika "+(i+1),asset,entries);new TerrainGraphicChange{After=item}.Write(true);entries.Add(item);}
                args=new object[]{Path.Combine(root,"Próba 0.6.3.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",args),"Fixture map "+args[2]);Console.WriteLine(args[0]);
            }
        }
        [STAThread] static int Main(string[] args){try{if(args.Length!=1)throw new Exception("One Case required");Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory;if(args[0]=="ink-profile")InkProfile(root);else if(args[0]=="gradient")Gradient(root);else if(args[0]=="ink-gradient")InkGradient(root);else if(args[0]=="fills")Fills(root);else if(args[0]=="ui")UI(root);else if(args[0]=="fixture")Fixture(root);else throw new Exception("Unknown Case");return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
    }
}
