using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch060Tests
    {
        const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Get(object o,string name){return o.GetType().GetField(name,P).GetValue(o);}
        static void Set(object o,string name,object value){o.GetType().GetField(name,P).SetValue(o,value);}
        static object Call(object o,string name,params object[] args){return o.GetType().GetMethods(P).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);}
        static void Check(bool value,string why){if(!value)throw new Exception(why);}
        static MapData Model(MainWindow w){return (MapData)Get(w,"map");}
        static string Json(object o){return MapLogic.Serializer().Serialize(o);}
        static int UndoCount(MainWindow w){return ((Stack<EditAction>)Get(w,"undo")).Count;}
        static void Start(MainWindow w){Set(w,"libraryPresets",new List<TerrainPreset>{PresetCatalog.Default()});Call(w,"StartNewProject",7,5,"PointyTop","Próba060",PresetCatalog.Default());}
        static void History(MainWindow w,bool redo){var args=new object[]{redo,null};Check((bool)Call(w,"TryHistory",args),"History: "+args[1]);}
        static void Stroke(MainWindow w,EditorTool tool,params int[] cells){Call(w,"BeginMaterialStroke",cells[0],tool);foreach(int i in cells)Call(w,"PaintMaterial",i);Call(w,"FinishStroke");Set(w,"strokeStarted",false);}
        static InkStroke Line(float x0,float y0,float x1,float y1){return new InkStroke{Width=3,Points=new List<InkPoint>{new InkPoint(new PointF(x0,y0)),new InkPoint(new PointF(x1,y1))}};}
        static void InkGesture(MainWindow w,EditorTool tool,Point a,Point b)
        {
            Set(w,"currentTool",tool);Set(w,"gestureTool",tool);Set(w,"gestureButton",MouseButtons.Left);
            Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,a.X,a.Y,0));Call(w,"MoveInk",b);Call(w,"EndCanvasGesture",true);
        }
        static MapObject AddObject(MainWindow w,int cell,bool light,int radius)
        {
            var m=Model(w);var asset=IconAssets.Starters()[0];if(!m.IconAssets.Any(a=>a.Id==asset.Id))m.IconAssets.Add(asset);
            var center=BrushGeometry.Center(m,cell%m.Columns,cell/m.Columns);var o=new MapObject{TemplateId="test",AssetId=asset.Id,Name="Lampa",X=center.X,Y=center.Y,LightEnabled=light,LightRadius=radius};m.Objects.Add(o);Call(w,"InvalidateLight");return o;
        }
        static void Acceptance(string root)
        {
            var terrain=PresetCatalog.Neutral().Copy();terrain.Variants=Enumerable.Range(0,5).Select(i=>new FillVariant{Id="position"+i,Color=i==0?"#FF0000":i==1?"#00FF00":"#0000FF"}).ToList();
            var order=TerrainMosaic.Assignments(terrain,26);var counts=Enumerable.Range(0,5).Select(i=>order.Count(v=>v==i)).ToArray();Check(counts.Max()-counts.Min()<=1,"Mosaic positions aren't balanced");Check(order.SequenceEqual(TerrainMosaic.Assignments(terrain,26)),"Mosaic unstable");terrain.Name="Inna nazwa";Check(order.SequenceEqual(TerrainMosaic.Assignments(terrain,26)),"Name shuffled mosaic");
            using(var w=new MainWindow())using(var d=new AppearancePoolDialog("Próba",null,terrain.Variants,true,false,"Aktualizuj"))
            {
                Start(w);var random=new Random(2026);Set(w,"appearanceRandom",random);using(var b=TerrainMosaic.Render(terrain,new Size(195,32),false)){}Check(random.Next()==new Random(2026).Next(),"Mosaic uses map Random");
                d.RemoveVariant(0);string before=Json(d.Fills);var args=new object[]{new Message(),Keys.Control|Keys.Shift|Keys.Z};Call(d,"ProcessCmdKey",args);Check(Json(d.Fills)==before,"Local CtrlShiftZ ran history");Call(d,"LocalHistory",false);Check(d.Fills.Count==5,"CtrlZ undo path lost");
                args=new object[]{Keys.Control|Keys.Shift|Keys.Z};Check((bool)Call(w,"HandleHistoryShortcut",args),"Main modifier shortcut not consumed");Check(UndoCount(w)==0,"CtrlShiftZ changed history");
            }
            using(var picker=new TerrainGraphicPicker(()=>new List<TerrainGraphic>(),false,true)){Check(picker.BackColor.R<60&&picker.AcceptButton==null,"Management picker isn't dark / has selection action");}
            using(var source=new Bitmap(800,500))
            {
                using(var g=Graphics.FromImage(source)){g.Clear(Color.Black);g.FillRectangle(Brushes.Red,400,0,400,500);}
                var file=Path.Combine(root,"cover.png");source.Save(file,ImageFormat.Png);var asset=GraphicSources.Import(file);var bg=new MapBackground{Source=asset,SourceId=asset.Id};
                using(var a=BackgroundRenderer.Render(bg,new Size(1187,793),null,160))using(var b=BackgroundRenderer.Render(bg,new Size(1187,793),null,310))
                {Func<Bitmap,double> edge=image=>Enumerable.Range(0,image.Width).First(x=>image.GetPixel(x,image.Height/2).R>120)/(double)image.Width;Check(Math.Abs(edge(a)-edge(b))<.008,"Light/exact crop geometries differ");}
            }
            Console.WriteLine("acceptance PASS: balanced mosaic; no map Random; CtrlShiftZ; management picker; cover geometry.");
        }
        static void Ink(string root)
        {
            var strokes=new List<InkStroke>{Line(-100,0,100,0)};string original=Json(strokes);
            var cut=InkGeometry.Erase(strokes,new PointF(0,-20),new PointF(0,20),20);// 0.6.2: actual surface with holes, not split centreline fragments.
            Check(cut.Count==1&&cut[0].Id==strokes[0].Id&&cut[0].Surface!=null,"Eraser lost retained surface");var area=InkSurface.Geometry(cut[0]);Check(!area.FillContains(new System.Windows.Point(0,0))&&area.FillContains(new System.Windows.Point(-30,0))&&area.FillContains(new System.Windows.Point(30,0)),"Surface cut/retained sides");Check(Json(strokes)==original,"Eraser mutated snapshot");
            var unchanged=InkGeometry.Erase(strokes,new PointF(500,500),new PointF(501,500),20);Check(Object.ReferenceEquals(unchanged[0],strokes[0]),"Untouched stroke cloned");
            Check(InkGeometry.Erase(strokes,new PointF(-200,0),new PointF(0,0),20).Max(s=>InkGeometry.Bounds(s).Right)<=102,"Eraser bounds wrong");
            using(var w=new MainWindow())
            {
                Start(w);var m=Model(w);var canvas=(Control)Get(w,"canvas");canvas.Size=new Size(800,600);Set(w,"pan",new PointF(0,0));Set(w,"scale",1f);
                string cells=Json(m.Cells);InkGesture(w,EditorTool.Stylus,new Point(80,90),new Point(220,90));Check(m.Ink.Count==1&&UndoCount(w)==1,"Pen gesture/history");string drawn=Json(m.Ink);
                InkGesture(w,EditorTool.Eraser,new Point(150,70),new Point(150,110));Check(m.Ink.Count==2&&UndoCount(w)==2,"Partial eraser gesture/history");string erased=Json(m.Ink);History(w,false);Check(Json(m.Ink)==drawn,"Eraser undo");History(w,true);Check(Json(m.Ink)==erased,"Eraser redo");
                Call(w,"ClearInk");Check(m.Ink.Count==0&&UndoCount(w)==3,"Clear all");History(w,false);Check(Json(m.Ink)==erased,"Clear undo");History(w,true);int count=UndoCount(w);Call(w,"ClearInk");Check(count==UndoCount(w),"Empty clear creates action");History(w,false);
                Check(Json(m.Cells)==cells,"Ink changed cells");Set(w,"scale",2f);Set(w,"pan",new PointF(50,25));Check(Json(m.Ink)==erased,"Pan/zoom changed world ink");
            }
            Console.WriteLine("ink PASS: partial capsules, immutable snapshots, gesture undo/redo, clear/no-op, world geometry.");
        }
        static void Pointer(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);string before=Json(Model(w));bool dirty=(bool)Get(w,"dirty");Set(w,"currentTool",EditorTool.Stylus);Set(w,"gestureTool",EditorTool.Stylus);Set(w,"gestureButton",MouseButtons.Right);
                Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Right,1,100,100,0));for(int i=0;i<180;i++)Call(w,"PointerAt",new Point(100+i,100));
                Check(((System.Collections.ICollection)Get(w,"pointerTrail")).Count==128,"Pointer unbounded");Check(((System.Windows.Forms.Timer)Get(w,"pointerTimer")).Enabled,"Pointer timer not active");
                Thread.Sleep(1050);Call(w,"AgePointer");Check((bool)Get(w,"pointerActive"),"Held pointer stopped");
                using(var image=new Bitmap(400,200))using(var g=Graphics.FromImage(image)){Call(w,"DrawInk",g);Check(image.GetPixel(279,100).A>0,"Held pointer vanished after old trail expired");}
                Call(w,"EndCanvasGesture",true);Call(w,"AgePointer");Check(!((System.Windows.Forms.Timer)Get(w,"pointerTimer")).Enabled,"Idle pointer keeps repainting");Check(before==Json(Model(w))&&UndoCount(w)==0&&dirty==(bool)Get(w,"dirty"),"Pointer changed document/history");
            }
            Console.WriteLine("pointer PASS: bounded trail, held circle, age-out/idle timer, no document/history state.");
        }
        static void Darkness(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);var m=Model(w);Set(w,"appearanceRandom",new Random(6));
                var pool=new List<FillVariant>{new FillVariant{Id="dark-a",Color="#010203"},new FillVariant{Id="dark-b",Color="#110000"}};Call(w,"ChangeDarkAppearance",pool,false,true);
                Stroke(w,EditorTool.DarkBrush,0,1,1,2);Check(m.Cells.Take(3).All(c=>c.Darkness)&&m.Cells.Skip(3).All(c=>!c.Darkness),"Base painting");string baseCells=Json(m.Cells);History(w,false);Check(m.Cells.All(c=>!c.Darkness),"Dark undo");History(w,true);Check(Json(m.Cells)==baseCells,"Dark redo resampled");
                Set(w,"currentTool",EditorTool.DarkBucket);Call(w,"FillRegion",10);Check(m.Cells.All(c=>c.Darkness),"Dark bucket initial region");History(w,false);Check(Json(m.Cells)==baseCells,"Dark bucket crosses initial mask");History(w,true);
                var a=AddObject(w,16,true,0);var lit=(HashSet<int>)Call(w,"LitCells");Check(lit.Count==1&&lit.Contains(16),"Light radius0");string look=Json(m.Cells[16].DarkFill);
                var next=a.Copy();next.LightRadius=1;Call(w,"ChangeObject",next);Check(((HashSet<int>)Call(w,"LitCells")).Count==7,"Light radius1");Check(m.Cells.All(c=>c.Darkness),"Light destroyed base");
                var b=AddObject(w,16,true,0);var off=m.Objects.First(o=>o.Id==a.Id).Copy();off.LightEnabled=false;Call(w,"ChangeObject",off);Check(((HashSet<int>)Call(w,"LitCells")).SetEquals(new[]{16}),"Light union source disabled");
                // Edit base while lit, then move the remaining source. No old snapshot may return.
                Stroke(w,EditorTool.DarkBrush,16);Check(!m.Cells[16].Darkness,"Remove lit base");var move=b.Copy();var center=BrushGeometry.Center(m,5,3);move.X=center.X;move.Y=center.Y;Call(w,"ChangeObject",move);Check(!m.Cells[16].Darkness&&Json(m.Cells[16].DarkFill)==look,"Light overwrote newer base");
                Stroke(w,EditorTool.DarkBrush,16);string result=Json(m.Cells);var group=new ObjectGroup{Name="Lampy",Members=m.Objects.Select(o=>o.Id).ToList(),LightEnabled=true,LightRadius=1};m.Groups.Add(group);Call(w,"InvalidateLight");Check(GroupRules.Effective(m,m.Objects[0]).LightEnabled&&!m.Objects[0].LightEnabled,"Group destroys base light");
                var g2=group.Copy();g2.LightEnabled=false;Call(w,"ChangeObjectGroup",g2,null);Check(((HashSet<int>)Call(w,"LitCells")).Count==0,"Group off");History(w,false);Check(((HashSet<int>)Call(w,"LitCells")).Count>=7,"Group light undo");Check(Json(m.Cells)==result,"Light/group changed fog/base appearances");
                Call(w,"SetLayerVisible",MapLayer.Objects,false);Check(((HashSet<int>)Call(w,"LitCells")).Count==0,"0.6.1: hidden objects must not participate in field of view");
                foreach(string orientation in new[]{"PointyTop","FlatTop"}){m.Orientation=orientation;var p=BrushGeometry.Center(m,-1,2);m.Objects.Clear();m.Groups.Clear();m.Objects.Add(new MapObject{X=p.X,Y=p.Y,LightEnabled=true,LightRadius=1});Check(LightGeometry.Coverage(m).Count>0,"Outside source doesn't reach map "+orientation);}
            }
            Console.WriteLine("darkness PASS: base/bucket/history; stable appearance, light0/1/union, base edit while lit, group overrides, outside source both orientations.");
        }
        static void Visibility(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);var m=Model(w);var o=AddObject(w,16,false,1);o.Size=120;var bounds=(RectangleF)Call(w,"ObjectBounds",o);
                for(int i=0;i<m.Cells.Count;i++){if(i%m.Columns<=2)m.Cells[i].Fog=true;else m.Cells[i].Darkness=true;}
                double fog=FogVisibility.VisibleFraction(m,bounds),dark=FogVisibility.VisibleFraction(m,bounds,i=>m.Cells[i].Darkness),both=(double)Call(w,"MaskVisible",bounds);Check(both<=fog&&both<=dark&&both<.5,"Masks not unioned");var row=(ObjectListRow)Call(w,"ObjectListItem",o);Check(row.Text=="???"&&row.SubItems[5].Text=="???","Light column leaks masked source");
                bool dirty=(bool)Get(w,"dirty");int history=UndoCount(w);Call(w,"SetLayerVisible",MapLayer.Fog,false);Check((double)Call(w,"MaskVisible",bounds)==dark,"Fog flag disables darkness");Call(w,"SetLayerVisible",MapLayer.Darkness,false);Check((double)Call(w,"MaskVisible",bounds)==1,"Both previews still mask");Call(w,"SetLayerVisible",MapLayer.Fog,true);Check((double)Call(w,"MaskVisible",bounds)==fog,"Darkness flag disables Fog");Check(dirty==(bool)Get(w,"dirty")&&history==UndoCount(w),"View creates history/dirty");
                m.Ink.Add(Line(-1000,-1000,-900,-1000));Call(w,"FitEverything");float all=(float)Get(w,"scale");Call(w,"SetLayerVisible",MapLayer.Ink,false);o.X=2000;Call(w,"SetLayerVisible",MapLayer.Objects,true);Call(w,"FitEverything");float objects=(float)Get(w,"scale");Call(w,"SetLayerVisible",MapLayer.Objects,false);Call(w,"FitEverything");float grid=(float)Get(w,"scale");Check(grid>all&&grid>objects,"Fit ignores view flags");
                // Portable map: new fields and retained full graphic source, not a derived light cache.
                var file=Path.Combine(root,"alpha.png");using(var bitmap=new Bitmap(32,32)){bitmap.SetPixel(2,2,Color.Gold);bitmap.Save(file,ImageFormat.Png);}var source=GraphicSources.Import(file);var graphic=new GraphicDefinition{Source=source,SourceId=source.Id,Name="Alpha"};graphic.Seal();m.DarkPalette=new List<FillVariant>{new FillVariant{Id="alpha",Graphic=graphic,GraphicId=graphic.Id}};m.Cells[0].DarkFill=m.DarkPalette[0].Copy();o.LightEnabled=true;o.LightRadius=2;
                var path=Path.Combine(root,"Próba060.hexmap");var args=new object[]{path,false,null};Check((bool)Call(w,"TrySaveTo",args),"Save: "+args[2]);string raw=File.ReadAllText(path);Check(!raw.Contains("lightCells")&&!raw.Contains("pointerTrail"),"Derived/transient state serialized");
                using(var loaded=new MainWindow()){args=new object[]{path,null,false};Check((bool)Call(loaded,"TryLoadProject",args),"Load: "+args[1]);var n=Model(loaded);Check(n.FormatVersion==10&&Json(n.Ink)==Json(m.Ink)&&n.Objects[0].LightEnabled&&n.Objects[0].LightRadius==2,"New data roundtrip");Check(n.DarkPalette[0].Graphic.Source.Data==source.Data&&n.Cells[0].DarkFill.Graphic!=null,"Dark full source missing");Check(!n.Cells[0].Darkness||Json(n.Cells[0].DarkFill)==Json(m.Cells[0].DarkFill),"Appearance changed");Check(!(bool)Get(loaded,"hideInk")&&!(bool)Get(loaded,"hideDarkness"),"View wasn't reset on Open");}
            }
            Console.WriteLine("visibility PASS: geometric union + independent flags, masked light/list; visible fit; map9 roundtrip with ink/light/dark source.");
        }
        static void Gestures(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);var m=Model(w);var canvas=(Control)Get(w,"canvas");canvas.Size=new Size(800,600);Set(w,"pan",new PointF(60,80));Set(w,"scale",1f);
                var o=AddObject(w,16,true,0);m.Cells.ForEach(c=>c.Darkness=true);string baseState=Json(m.Cells);int count=UndoCount(w);
                var start=(PointF)Call(w,"Screen",new PointF(o.X,o.Y));var endWorld=BrushGeometry.Center(m,5,3);var end=(PointF)Call(w,"Screen",endWorld);
                Set(w,"gestureTool",EditorTool.Hand);Set(w,"gestureButton",MouseButtons.Left);Set(w,"dragOrigin",Point.Round(start));Call(w,"BeginObjectMove",new[]{o.Id},new PointF(o.X,o.Y));Call(w,"ObjectMouseMove",new MouseEventArgs(MouseButtons.Left,1,(int)end.X,(int)end.Y,0));Call(w,"EndCanvasGesture",true);
                Check(UndoCount(w)==count+1&&Json(m.Cells)==baseState,"Moving light changes base or multiple steps");Check((bool)Call(w,"EffectiveDarkness",16)&&!(bool)Call(w,"EffectiveDarkness",26),"Light didn't follow source");History(w,false);Check(!(bool)Call(w,"EffectiveDarkness",16),"Light undo cache");History(w,true);Check(!(bool)Call(w,"EffectiveDarkness",26),"Light redo cache");
                // The same stroke route must not resample visited darkness cells.
                var random=new Random(19);Set(w,"appearanceRandom",random);var pool=new List<FillVariant>{new FillVariant{Id="a",Color="#100000"},new FillVariant{Id="b",Color="#001000"}};Call(w,"ChangeDarkAppearance",pool,false,true);
                m.Cells[0].Darkness=false;Call(w,"BeginMaterialStroke",0,EditorTool.DarkBrush);Call(w,"PaintMaterial",0);var fill=Json(m.Cells[0].DarkFill);Call(w,"PaintMaterial",0);Call(w,"FinishStroke");Check(Json(m.Cells[0].DarkFill)==fill,"Visited dark cell resampled");var expected=new Random(19);expected.Next(2);Check(random.Next()==expected.Next(),"Dark duplicate consumed Random");
                // A protected fog discoverer remains independent of the light layer.
                var discoverer=m.Objects[0].Copy();discoverer.Side="Ally";discoverer.RevealsFog=true;discoverer.RevealRadius=0;Call(w,"ChangeObject",discoverer);int at=ObjectGeometry.Hit(m,new PointF(discoverer.X,discoverer.Y));Stroke(w,EditorTool.FogBrush,at);Check(!m.Cells[at].Fog,"Darkness disabled active fog protection");
                // Hidden group controls do not show effective/base values or edit through live events.
                var group=new ObjectGroup{Name="Tajna grupa",Members=new List<string>{discoverer.Id},SizeMode="Common",SizeValue=80,LightEnabled=true,LightRadius=3};m.Groups.Add(group);m.Cells.ForEach(c=>c.Fog=true);Call(w,"InvalidateLight");Set(w,"selectedGroupId",group.Id);((HashSet<string>)Get(w,"selectedObjects")).Add(discoverer.Id);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");Check(((TextBox)Get(w,"groupName")).Text=="???"&&!((Control)Get(w,"groupLayout")).Enabled,"Masked group details leak");
                // Ink culling cache must follow newly appended points; partial erase + budget path remain reversible.
                m.Cells.ForEach(c=>c.Fog=false);InkGesture(w,EditorTool.Stylus,new Point(100,100),new Point(250,100));var stroke=m.Ink.Single();Check(InkGeometry.Bounds(stroke).Width>149,"Cached ink bounds stale");string drawn=Json(m.Ink);InkGesture(w,EditorTool.Eraser,new Point(175,90),new Point(175,110));History(w,false);Check(Json(m.Ink)==drawn,"Ink cache changes snapshot");
                Set(w,"gestureTool",EditorTool.Stylus);Set(w,"gestureButton",MouseButtons.Right);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Right,1,30,30,0));Call(w,"EndCanvasGesture",true);Thread.Sleep(1050);Call(w,"AgePointer");Check(!((System.Windows.Forms.Timer)Get(w,"pointerTimer")).Enabled,"Pointer timer survives release");
                // Validate new field limits without giant allocations / campaign.
                var invalid=Line(0,0,1,1);invalid.Width=Single.PositiveInfinity;bool rejected=false;try{InkGeometry.Validate(new List<InkStroke>{invalid});}catch(InvalidDataException){rejected=true;}Check(rejected,"Ink width limit");var bad=group.Copy();bad.LightRadius=21;m.Groups=new List<ObjectGroup>{bad};rejected=false;try{GroupRules.Validate(m);}catch(InvalidDataException){rejected=true;}Check(rejected,"Light radius limit");
            }
            Console.WriteLine("gestures PASS: light movement one Undo/Redo, preserved darkness, visited appearance RNG, fog protection, masked group, ink bounds/history, pointer idle, limits.");
        }
        static void InkPreview(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);Set(w,"currentTool",EditorTool.Stylus);Set(w,"inkToolSize",20f);Set(w,"scale",2f);
                string before=Json(Model(w));int history=UndoCount(w);
                Call(w,"CanvasMouseMove",null,new MouseEventArgs(MouseButtons.None,0,100,100,0));
                Check((Point?)Get(w,"inkCursor")==new Point(100,100),"Stylus hover position missing");
                using(var image=new Bitmap(240,240))using(var g=Graphics.FromImage(image))
                {Call(w,"DrawInk",g);Check(image.GetPixel(110,100).A>0&&image.GetPixel(100,100).A==0,"World thickness preview radius / center");}
                Check(before==Json(Model(w))&&history==UndoCount(w),"Hover edited model/history");
                Set(w,"hideInk",true);using(var image=new Bitmap(240,240))using(var g=Graphics.FromImage(image)){Call(w,"DrawInk",g);Check(image.GetPixel(110,100).A==0,"Hidden ink preview still drawn");}
            }
            Console.WriteLine("ink-preview PASS: hover, world thickness scaled to screen, no data/history, hidden layer.");
        }
        static void Fixture(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);var m=Model(w);m.Name="Próba 0.6.0";m.LabelMode="InCells";m.Cells.ForEach(c=>c.Darkness=true);m.Cells[0].Fog=true;m.Cells[1].Fog=true;var o=AddObject(w,16,true,1);o.Name="Lampa próby 0.6.0";
                var args=new object[]{Path.Combine(root,"Próba 0.6.0.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",args),"Fixture save: "+args[2]);
                Console.WriteLine("Utworzono wyłącznie własną mapę wejściową UI: "+args[0]);
            }
        }
        [STAThread] static int Main(string[] args)
        {
            try{if(args.Length!=1||!new[]{"acceptance","ink","pointer","darkness","visibility","gestures","ink-preview","fixture"}.Contains(args[0]))throw new ArgumentException("Wymagany pojedynczy Case: acceptance|ink|pointer|darkness|visibility|gestures|ink-preview|fixture");Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory;switch(args[0]){case "acceptance":Acceptance(root);break;case "ink":Ink(root);break;case "pointer":Pointer(root);break;case "darkness":Darkness(root);break;case "visibility":Visibility(root);break;case "gestures":Gestures(root);break;case "ink-preview":InkPreview(root);break;case "fixture":Fixture(root);break;}return 0;}
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
