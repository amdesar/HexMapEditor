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
    internal static class Patch061Tests
    {
        const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
        static object Get(object o,string name){return o.GetType().GetField(name,P).GetValue(o);}
        static void Set(object o,string name,object value){o.GetType().GetField(name,P).SetValue(o,value);}
        static object Call(object o,string name,params object[] args){return o.GetType().GetMethods(P).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);}
        static void Check(bool value,string why){if(!value)throw new Exception(why);}
        static MapData Model(MainWindow w){return (MapData)Get(w,"map");}
        static string Json(object o){return MapLogic.Serializer().Serialize(o);}
        static void History(MainWindow w,bool redo){var a=new object[]{redo,null};Check((bool)Call(w,"TryHistory",a),"History: "+a[1]);}
        static void Start(MainWindow w){Set(w,"libraryPresets",new List<TerrainPreset>{PresetCatalog.Default()});Call(w,"StartNewProject",11,5,"PointyTop","Próba061",PresetCatalog.Default());}
        static List<MapObject> Trio(MainWindow w)
        {
            Start(w);var m=Model(w);m.Cells.ForEach(c=>c.Darkness=true);var asset=IconAssets.Starters()[0];m.IconAssets.Add(asset);
            for(int i=0;i<3;i++){var p=BrushGeometry.Center(m,2+i*3,2);m.Objects.Add(new MapObject{Name="Członek "+i,TemplateId="test",AssetId=asset.Id,X=p.X,Y=p.Y,LightEnabled=i!=1,LightRadius=1,Side=i==0?"Enemy":"Neutral"});}
            return m.Objects;
        }
        static HashSet<int> Lit(MainWindow w){return (HashSet<int>)Call(w,"LitCells");}
        static void Groups(string root)
        {
            using(var w=new MainWindow())
            {
                w.Show();var trio=Trio(w);var m=Model(w);var before=new HashSet<int>(Lit(w));string bases=Json(trio);
                ((HashSet<string>)Get(w,"selectedObjects")).UnionWith(trio.Select(o=>o.Id));
                Check((bool)Call(w,"CreateObjectGroup","Trójka"),"Actual group creation failed");
                Console.WriteLine("Before coverage="+before.Count+"; after="+Lit(w).Count+"; base="+Json(trio.Select(o=>o.LightEnabled))+"; group="+Json(m.Groups));
                Check(Json(m.Objects)==bases&&Lit(w).SetEquals(before),"Grouping changed individual sources");
                var g=m.Groups.Single().Copy();g.LightRadius=2;Call(w,"ChangeObjectGroup",g,null);Check(Lit(w).Count>before.Count,"Group radius ineffective");g=m.Groups.Single().Copy();g.LightEnabled=false;Call(w,"ChangeObjectGroup",g,null);Check(Lit(w).Count==0,"Group off ineffective");
                g=m.Groups.Single().Copy();g.LightEnabled=null;g.LightRadius=null;Call(w,"ChangeObjectGroup",g,null);Check(Lit(w).SetEquals(before),"Individual not restored");
                History(w,false);Check(Lit(w).Count==0,"Group undo");History(w,true);Check(Lit(w).SetEquals(before),"Group redo");
                Call(w,"DetachObjects",m.Groups[0].Id,new[]{trio[0].Id},new bool[7]);Check(Lit(w).SetEquals(before),"Detach base visibility");History(w,false);
                // New requirement: hiding Objects disables participation, not the saved settings/base/Fog.
                string cells=Json(m.Cells),objects=Json(m.Objects);Call(w,"SetLayerVisible",MapLayer.Objects,false);Check(Lit(w).Count==0,"Hidden objects still participate");Call(w,"SetLayerVisible",MapLayer.Objects,true);Check(Lit(w).SetEquals(before)&&cells==Json(m.Cells)&&objects==Json(m.Objects),"Show objects alters base/data");
            }
            Console.WriteLine("groups PASS");
        }
        static void Outside(string root)
        {
            using(var w=new MainWindow())
            {
                w.Show();var trio=Trio(w);var m=Model(w);var target=trio.Select(o=>new PointF(o.X,o.Y)).ToArray();
                foreach(var o in trio)o.Y-=420;
                Call(w,"InvalidateLight");Check(Lit(w).Count==0,"Outside coverage");
                ((HashSet<string>)Get(w,"selectedObjects")).UnionWith(trio.Select(o=>o.Id));
                Check((bool)Call(w,"CreateObjectGroup","Poza mapą"),"Outside group creation");
                Set(w,"gestureTool",EditorTool.Hand);Set(w,"dragOrigin",Point.Empty);Set(w,"scale",1f);Set(w,"pan",new PointF(200,0));
                Call(w,"BeginObjectMove",trio.Select(o=>o.Id),new PointF(0,0));
                // Populate the empty cache while the group is outside, then use the real move path.
                Check(Lit(w).Count==0,"Before move");
                Call(w,"ObjectMouseMove",new MouseEventArgs(MouseButtons.Left,0,200,420,0));Call(w,"FinishObjectGesture",true);
                Console.WriteLine("Moved positions="+Json(m.Objects)+"; cache="+Lit(w).Count+"; fresh="+LightGeometry.Coverage(m).Count);
                Check(m.Objects.Select((o,i)=>Math.Abs(o.Y-target[i].Y)<.001f).All(v=>v),"Move did not enter map");
                Check(Lit(w).Count==14,"Outside group failed to reveal on entering map");
                History(w,false);Check(Lit(w).Count==0,"Undo move");History(w,true);Check(Lit(w).Count==14,"Redo move");
            }
            Console.WriteLine("outside PASS");
        }
        static void Backgrounds(string root)
        {
            string sourcePath=Path.Combine(root,"Tło źródło.png");using(var image=new Bitmap(160,100)){using(var g=Graphics.FromImage(image)){g.Clear(Color.Coral);g.FillRectangle(Brushes.Blue,40,20,60,60);}image.Save(sourcePath);}
            var source=GraphicSources.Import(sourcePath);var settings=new MapBackground{Source=source,SourceId=source.Id,Left=.1,Top=.15,Width=.7,Height=.6,Opacity=57,Saturation=133,Blur=4};
            using(var w=new MainWindow())
            {
                w.Show();Start(w);Set(w,"backgroundLibrary",new List<BackgroundEntry>());
                var list=(List<BackgroundEntry>)Get(w,"backgroundLibrary");string directory=LibraryPaths.Backgrounds;Directory.CreateDirectory(directory);
                var item=BackgroundLibrary.Prepare(directory,null,"Próba Tła",settings,list);
                Call(w,"CommitBackground",item.Settings,null,item,true);Check(File.Exists(item.Path)&&Model(w).Background.Opacity==57,"Create background/map");
                string warning;var read=BackgroundLibrary.Load(directory,out warning);Check(warning==""&&read.Count==1,"Read library: "+warning);Check(Json(read[0].Settings)==Json(settings)&&read[0].Settings.Source.Data==source.Data,"All settings/full source roundtrip");
                Call(w,"CaptureSavedState");var renamed=BackgroundLibrary.Prepare(directory,item,"Tło po zmianie",item.Settings,read);Call(w,"CommitBackground",null,item,renamed,false);Check(!(bool)Get(w,"dirty")&&!File.Exists(item.Path)&&File.Exists(renamed.Path),"Rename caused dirty/duplicate");History(w,false);Check(File.Exists(item.Path)&&!File.Exists(renamed.Path),"Rename undo");History(w,true);
                string mapState=Json(Model(w).Background);Call(w,"CommitBackground",null,renamed,null,false);Check(!File.Exists(renamed.Path)&&mapState==Json(Model(w).Background),"Delete damaged map snapshot");
                File.WriteAllText(renamed.Path,"foreign test bytes");int count=((System.Collections.ICollection)Get(w,"undo")).Count;var failed=new object[]{false,null};Check(!(bool)Call(w,"TryHistory",failed)&&File.ReadAllText(renamed.Path)=="foreign test bytes"&&((System.Collections.ICollection)Get(w,"undo")).Count==count,"Conflict overwritten/advanced");File.Delete(renamed.Path);History(w,false);Check(File.Exists(renamed.Path),"Retry restore");History(w,true);
                string mapPath=Path.Combine(root,"Tło061.hexmap");var args=new object[]{mapPath,false,null};Check((bool)Call(w,"TrySaveTo",args),"Save: "+args[2]);File.Delete(sourcePath);
                using(var loaded=new MainWindow()){args=new object[]{mapPath,null,false};Check((bool)Call(loaded,"TryLoadProject",args),"Load: "+args[1]);Check(Json(Model(loaded).Background)==mapState&&Model(loaded).Background.Source.Data==source.Data,"Portable background settings/source");}
            }
            using(var host=new Form())using(var crop=new BackgroundCropSurface{Size=new Size(365,310)})
            {
                host.Controls.Add(crop);host.Show();var draft=settings.Copy();crop.SetContent(draft);Call(crop,"OnMouseDown",new MouseEventArgs(MouseButtons.Middle,1,180,150,0));Call(crop,"OnMouseMove",new MouseEventArgs(MouseButtons.Middle,0,210,165,0));Call(crop,"OnMouseUp",new MouseEventArgs(MouseButtons.Middle,1,210,165,0));Check(draft.Left>settings.Left&&draft.Top>settings.Top&&draft.Width==settings.Width&&draft.Height==settings.Height&&!crop.Capture,"MMB move resized/capture");BackgroundRenderer.Validate(draft);
            }
            Console.WriteLine("backgrounds PASS");
        }
        static void SizePointer(string root)
        {
            Check(InkSizeMath.World(40,1.46f,96)*1.46f>39.99f&&InkSizeMath.World(40,.73f,96)*.73f>39.99f,"Screen width math");Check(InkSizeMath.World(200,.01f,96)==20000,"World width capped at screen limit");
            using(var w=new MainWindow())
            {
                w.Show();Start(w);var canvas=(Control)Get(w,"canvas");Set(w,"pan",new PointF(100,100));Set(w,"scale",1.46f);Set(w,"inkToolSize",40f);Set(w,"gestureTool",EditorTool.Stylus);Set(w,"gestureButton",MouseButtons.Left);
                Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,200,200,0));Call(w,"MoveInk",new Point(500,200));Call(w,"FinishInk",true);float first=Model(w).Ink[0].Width;
                Set(w,"scale",.73f);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,200,300,0));Call(w,"MoveInk",new Point(500,300));Call(w,"FinishInk",true);Check(Math.Abs(Model(w).Ink[1].Width/first-2)<.001,"New stroke didn't adjust with zoom");
                string before=Json(Model(w).Ink);Set(w,"gestureTool",EditorTool.Eraser);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,275,150,0));Call(w,"MoveInk",new Point(275,250));Call(w,"FinishInk",true);string erased=Json(Model(w).Ink);Check(before!=erased&&Model(w).Ink.Count>=2,"Partial eraser failed");History(w,false);Check(Json(Model(w).Ink)==before,"Erase exact Undo");History(w,true);Check(Json(Model(w).Ink)==erased,"Erase exact Redo");
                Model(w).Ink.Add(new InkStroke{Width=InkSizeMath.World(200,.01f,96),Points=new List<InkPoint>{new InkPoint(new PointF(0,0))}});InkGeometry.Validate(Model(w).Ink);
                var save=new object[]{Path.Combine(root,"Rozmiar061.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",save),"Width save");using(var other=new MainWindow()){var load=new object[]{save[0],null,false};Check((bool)Call(other,"TryLoadProject",load)&&Model(other).Ink.Last().Width==20000,"World width roundtrip");}
                var size=(NumericUpDown)Get(w,"inkSize");Call(w,"SelectTool",EditorTool.Eraser);Check(size.Minimum==.5m&&size.Maximum==200&&size.Value==40,"Shared eraser setting");Call(w,"SelectTool",EditorTool.Stylus);Check(size.Value==40,"Shared stylus setting");
                int actions=((System.Collections.ICollection)Get(w,"undo")).Count;string document=Json(Model(w));Set(w,"gestureTool",EditorTool.Stylus);Set(w,"gestureButton",MouseButtons.Right);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Right,1,200,200,0));Check(((InkCanvas)canvas).PointerHidden,"Pointer cursor not hidden");Call(w,"PointerAt",new Point(250,220));Call(w,"FinishInk",true);Check(!((InkCanvas)canvas).PointerHidden,"Pointer cursor stuck");Check(((System.Collections.ICollection)Get(w,"undo")).Count==actions&&Json(Model(w))==document,"Pointer edited document");
                System.Threading.Thread.Sleep(760);Call(w,"AgePointer");Check(((System.Collections.ICollection)Get(w,"pointerTrail")).Count==0&&!((Timer)Get(w,"pointerTimer")).Enabled,"Pointer didn't expire/stop");Call(w,"ClearPointerContext");
            }
            using(var host=new Form())using(var number=new NumericUpDown{Minimum=1,Maximum=15,Value=5})using(var drag=new NumberDrag(number,1,8))
            {host.Controls.Add(number);host.Controls.Add(drag);host.Show();Call(drag,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,5,5,0));Check(number.Value==5,"Down changed value");drag.MoveBy(16);Check(number.Value==7,"Scrub displacement");drag.MoveBy(16);Check(number.Value==7,"Scrub time accelerated");drag.MoveBy(-8);Check(number.Value==4,"Scrub reverse");Call(drag,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,0,0,0));Check(!drag.Capture,"Scrub capture stuck");}
            Console.WriteLine("size-pointer PASS (continuous comet NOT visually verified)");
        }
        static void Presentation(string root)
        {
            var terrain=PresetCatalog.Default().Terrains[0].Copy();terrain.Variants=new List<FillVariant>();foreach(var color in new[]{"#FF0000","#00FF00","#0000FF","#0000FF","#0000FF"})terrain.Variants.Add(new FillVariant{Color=color});
            var assignments=TerrainMosaic.Assignments(terrain,48);Check(assignments.GroupBy(i=>i).Max(g=>g.Count())-assignments.GroupBy(i=>i).Min(g=>g.Count())<=1,"Mosaic unequal positions");Check(assignments.SequenceEqual(TerrainMosaic.Assignments(terrain,48)),"Mosaic unstable");
            foreach(bool flat in new[]{false,true})using(var mosaic=TerrainMosaic.Render(terrain,new Size(150,32),flat)){mosaic.Save(Path.Combine(root,flat?"mozaika-flat.png":"mozaika-pointy.png"));Check(mosaic.Width==150&&mosaic.Height==32,"Mosaic size");}
            using(var w=new MainWindow())
            {
                w.Show();Trio(w);foreach(var c in Model(w).Cells)c.Darkness=false;Call(w,"InvalidateLight");
                var times=new List<long>();for(int i=0;i<3;i++){Call(w,"CloseDetailsPanel");var watch=Stopwatch.StartNew();Call(w,"ChooseObjectMember",Model(w).Objects[0],false,true);((Control)Get(w,"right")).Update();watch.Stop();times.Add(watch.ElapsedMilliseconds);}
                Console.WriteLine("Open object panel, close/open, ms: "+String.Join(",",times));
                var layout=(TableLayoutPanel)Get(w,"instanceLayout");Check(layout.GetRow((Control)Get(w,"instanceDelete"))>layout.GetRow((Control)Get(w,"objectLightRadius")),"Delete not last");
                var controls=((FlowLayoutPanel)Get(w,"paletteTools")).Controls.Cast<Control>().Select(c=>c.AccessibleName).ToArray();Check(controls[0]=="Kursor"&&controls[1]=="Rączka"&&controls[2]=="Rysik"&&controls[3]=="Gumka"&&controls[11]=="Wiadro ograniczenia widoczności","Tool order");
            }
            var sourceFile=Path.Combine(root,"obraz.png");using(var image=new Bitmap(80,80)){using(var g=Graphics.FromImage(image))g.Clear(Color.Coral);image.Save(sourceFile);}var source=GraphicSources.Import(sourceFile);var asset=new GraphicDefinition{Source=source,SourceId=source.Id,Name="Miniatura"};asset.Seal();
            var entries=Enumerable.Range(0,8).Select(i=>new TerrainGraphic{Id="item"+i,Name="Grafika "+i,Asset=asset}).ToList();
            using(var picker=new TerrainGraphicPicker(()=>entries,false)){picker.Show();var list=(ListView)Get(picker,"list");Application.DoEvents();var a=list.GetItemRect(0);var d=list.GetItemRect(3);var e=list.GetItemRect(4);Console.WriteLine("Library item rectangles: "+a+"; "+d+"; "+e);Check(a.Top==d.Top&&e.Top>a.Top,"Default library not four columns");}
            Console.WriteLine("presentation PASS (no continuous rendering claim)");
        }
        static void PanelProfile(string root)
        {
            using(var w=new MainWindow())
            {
                w.Show();Trio(w);foreach(var c in Model(w).Cells)c.Darkness=false;Call(w,"InvalidateLight");
                ((HashSet<string>)Get(w,"selectedObjects")).Add(Model(w).Objects[0].Id);Set(w,"rightPanelOpen",true);Set(w,"selected",-1);Set(w,"updating",true);
                var watch=new Stopwatch();Action<string,Action> time=(label,action)=>{watch.Restart();action();watch.Stop();Console.WriteLine(label+": "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");};
                time("UpdateCaption",()=>Call(w,"UpdateCaption"));time("UpdateObjectDetails",()=>Call(w,"UpdateObjectDetails"));time("PositionOverlays",()=>Call(w,"PositionOverlays"));time("Show object subtree",()=>((Control)Get(w,"objectDetailPanel")).Show());time("Show right",()=>((Control)Get(w,"right")).Show());time("PaintTree",()=>((DetailsOverlay)Get(w,"objectDetailPanel")).PaintTree());Set(w,"updating",false);time("Full UpdateUI (already open)",()=>Call(w,"UpdateUI"));
            }
        }
        static void Acceptance(string root)
        {
            var file=Path.Combine(root,"Maska.png");using(var image=new Bitmap(100,100)){using(var g=Graphics.FromImage(image)){g.Clear(Color.Black);g.FillRectangle(Brushes.DarkSlateBlue,20,20,50,50);}image.Save(file);}
            var source=GraphicSources.Import(file);var graphic=new GraphicDefinition{Name="Maska",Source=source,SourceId=source.Id};graphic.Seal();
            using(var w=new MainWindow())
            {
                w.Show();Start(w);Call(w,"StartNewProject",15,10,"PointyTop","Konfiguracja użytkownika",PresetCatalog.Default());var m=Model(w);var asset=IconAssets.Starters()[0];m.IconAssets.Add(asset);
                m.DarkPalette=new List<FillVariant>{new FillVariant{Color="#000000"},new FillVariant{Graphic=graphic,GraphicId=graphic.Id}};
                for(int i=0;i<m.Cells.Count;i++){m.Cells[i].Darkness=true;m.Cells[i].DarkFill=m.DarkPalette[i%2].Copy();}
                for(int i=0;i<3;i++){var p=BrushGeometry.Center(m,2+i*3,3);m.Objects.Add(new MapObject{Name="Próba "+i,TemplateId="próba",AssetId=asset.Id,X=p.X,Y=p.Y-210,LightEnabled=i!=1,LightRadius=1});}
                Call(w,"InvalidateLight");var save=new object[]{Path.Combine(root,"Konfiguracja061.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",save),"Acceptance fixture");
                ((HashSet<string>)Get(w,"selectedObjects")).UnionWith(m.Objects.Select(o=>o.Id));Check((bool)Call(w,"CreateObjectGroup","Poza mapą"),"Actual create");
                Set(w,"pan",new PointF(200,250));Set(w,"scale",1f);Set(w,"gestureTool",EditorTool.Select);Set(w,"dragOrigin",new Point(200,250));Call(w,"BeginObjectMove",m.Objects.Select(o=>o.Id).ToArray(),new PointF(0,0));Call(w,"ObjectMouseMove",new MouseEventArgs(MouseButtons.Left,0,200,460,0));Call(w,"FinishObjectGesture",true);Check(Lit(w).Count==14,"Mixed mask/cursor move sources");
                Set(w,"gestureTool",EditorTool.Hand);Set(w,"dragOrigin",new Point(200,250));Call(w,"BeginObjectMove",m.Objects.Select(o=>o.Id).ToArray(),new PointF(0,0));Call(w,"ObjectMouseMove",new MouseEventArgs(MouseButtons.Left,0,248,250,0));Call(w,"FinishObjectGesture",true);Check(Lit(w).Count>0&&m.Objects.Count(o=>o.LightEnabled)==2,"Hand move loses sources");
                Console.WriteLine("15x10 mixed mask, group outside, cursor/hand move: coverage="+Lit(w).Count+"; bug NOT reproduced");
            }
        }
        static void BackgroundControls(string root)
        {
            string file=Path.Combine(root,"próba.png");using(var image=new Bitmap(100,80)){using(var g=Graphics.FromImage(image))g.Clear(Color.Turquoise);image.Save(file);}var source=GraphicSources.Import(file);var settings=new MapBackground{Source=source,SourceId=source.Id,Left=.1,Top=.2,Width=.7,Height=.6,Opacity=47,Saturation=81,Blur=3};
            using(var w=new MainWindow())
            {
                w.Show();Start(w);Set(w,"backgroundLibrary",new List<BackgroundEntry>());var list=(List<BackgroundEntry>)Get(w,"backgroundLibrary");Directory.CreateDirectory(LibraryPaths.Backgrounds);var entry=BackgroundLibrary.Prepare(LibraryPaths.Backgrounds,null,"Parametry",settings,list);Call(w,"CommitBackground",null,null,entry,false);
                string warning;var read=BackgroundLibrary.Load(LibraryPaths.Backgrounds,out warning);Check(warning==""&&read.Count==1,"Strict metadata load");
                using(var picker=new BackgroundPicker(()=>read)){picker.Show();Application.DoEvents();var listView=(ListView)Get(picker,"list");Check(listView.Items.Count==1&&listView.LargeImageList.Images.Count==1,"Picker thumbnails");using(var image=new Bitmap(picker.Width,picker.Height))picker.DrawToBitmap(image,picker.ClientRectangle);picker.Close();}
                using(var dialog=new BackgroundDialog(settings,new Size(900,600)))
                {
                    Check(((TrackBar)Get(dialog,"opacity")).Value==47&&((TrackBar)Get(dialog,"saturation")).Value==81&&((TrackBar)Get(dialog,"blur")).Value==3,"Sliders didn't restore");
                    int actions=((System.Collections.ICollection)Get(w,"undo")).Count;Check((bool)Call(w,"AcceptBackground",dialog,settings,null,true),"No-op dialog accept");Check(((System.Collections.ICollection)Get(w,"undo")).Count==actions+1,"Map apply single operation");
                    int now=((System.Collections.ICollection)Get(w,"undo")).Count;Check((bool)Call(w,"AcceptBackground",dialog,settings,null,true)&&((System.Collections.ICollection)Get(w,"undo")).Count==now&&Directory.GetFiles(LibraryPaths.Backgrounds,"background-*.json").Length==1,"Unchanged acceptance duplicates entry/history");
                }
            }
            Console.WriteLine("background-controls PASS");
        }
        static void PanelLayout(string root)
        {
            using(var w=new MainWindow())
            {
                w.Show();Trio(w);foreach(var c in Model(w).Cells)c.Darkness=false;Call(w,"InvalidateLight");Call(w,"ChooseObjectMember",Model(w).Objects[0],false,true);
                var button=(Control)Get(w,"instanceDelete");Check(button.Height>=26&&button.Height<=45,"Delete button expanded: "+button.Height);var table=(TableLayoutPanel)Get(w,"instanceLayout");Check(table.GetRow(button)==13&&table.RowStyles.Count>13&&table.RowStyles[13].SizeType==SizeType.AutoSize,"Final row not autosized");Console.WriteLine("Instance delete height="+button.Height+"; final row=AutoSize");
            }
        }
        static void PointerFocus(string root)
        {
            using(var w=new MainWindow())
            {
                w.Show();Start(w);var canvas=(InkCanvas)Get(w,"canvas");canvas.Focus();Set(w,"gestureTool",EditorTool.Stylus);Set(w,"gestureButton",MouseButtons.Right);Call(w,"BeginInk",new MouseEventArgs(MouseButtons.Right,1,200,200,0));
                Check(canvas.PointerHidden&&(bool)Get(w,"pointerActive"),"Pointer not active");string document=Json(Model(w));int count=((System.Collections.ICollection)Get(w,"undo")).Count;
                Call(canvas,"OnLostFocus",EventArgs.Empty);Check(!canvas.PointerHidden&&!(bool)Get(w,"pointerActive")&&!((Timer)Get(w,"pointerTimer")).Enabled&&((System.Collections.ICollection)Get(w,"pointerTrail")).Count==0,"Focus loss retained pointer/cursor");Check(document==Json(Model(w))&&count==((System.Collections.ICollection)Get(w,"undo")).Count,"Focus loss edited document");
            }
            Console.WriteLine("pointer-focus PASS");
        }
        static void BackgroundOverwrite(string root)
        {
            var path=Path.Combine(root,"źródło.png");using(var image=new Bitmap(64,64)){using(var g=Graphics.FromImage(image))g.Clear(Color.Coral);image.Save(path);}var source=GraphicSources.Import(path);
            using(var w=new MainWindow())
            {
                w.Show();Start(w);Directory.CreateDirectory(LibraryPaths.Backgrounds);Set(w,"backgroundLibrary",new List<BackgroundEntry>());var entries=(List<BackgroundEntry>)Get(w,"backgroundLibrary");
                var first=BackgroundLibrary.Prepare(LibraryPaths.Backgrounds,null,"Nadpisanie",new MapBackground{Source=source,SourceId=source.Id,Opacity=70},entries);Call(w,"CommitBackground",first.Settings,null,first,true);var before=Json(Model(w).Background);
                var settings=first.Settings.Copy();settings.Opacity=31;settings.Saturation=55;settings.Blur=6;var next=BackgroundLibrary.Prepare(LibraryPaths.Backgrounds,first,first.Name,settings,(List<BackgroundEntry>)Get(w,"backgroundLibrary"));Call(w,"CommitBackground",next.Settings,first,next,true);
                Check(next.Id==first.Id&&next.Path==first.Path&&Directory.GetFiles(LibraryPaths.Backgrounds,"background-*.json").Length==1&&File.ReadAllBytes(next.Path).SequenceEqual(next.Bytes),"Overwrite changed identity/duplicated file");Check(Directory.GetFiles(Path.Combine(LibraryPaths.Backgrounds,"Sources")).Length==1,"Source duplicated per entry");
                History(w,false);Check(Json(Model(w).Background)==before&&File.ReadAllBytes(first.Path).SequenceEqual(first.Bytes),"Overwrite Undo not atomic");History(w,true);Check(Model(w).Background.Opacity==31&&File.ReadAllBytes(next.Path).SequenceEqual(next.Bytes),"Overwrite Redo not exact");
                int count=((System.Collections.ICollection)Get(w,"undo")).Count;string state=Json(Model(w));bool accepted=false;
                using(var dialog=new BackgroundDialog(Model(w).Background,new Size(900,600))){dialog.SaveAndApply=(value,id)=>{accepted=true;return true;};dialog.Show();((TrackBar)Get(dialog,"opacity")).Value=92;dialog.CancelButton.PerformClick();}
                Check(!accepted&&Json(Model(w))==state&&count==((System.Collections.ICollection)Get(w,"undo")).Count&&File.ReadAllBytes(next.Path).SequenceEqual(next.Bytes),"Cancel changed map/library/history");
            }
            Console.WriteLine("background-overwrite PASS");
        }
        static void Fixture(string root,bool outside=false){using(var w=new MainWindow()){Trio(w);if(outside)foreach(var o in Model(w).Objects)o.Y-=210;var a=new object[]{Path.Combine(root,"Trójka061.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",a),"Fixture: "+a[2]);Console.WriteLine(a[0]);}}
        [STAThread]static int Main(string[] args){try{if(args.Length!=1||!new[]{"groups","fixture","outside","fixture-outside","backgrounds","size-pointer","presentation","panel-profile","acceptance","background-controls","panel-layout","pointer-focus","background-overwrite"}.Contains(args[0]))throw new ArgumentException("Wymagany pojedynczy Case; patrz Run-Patch061Tests.ps1");Application.EnableVisualStyles();if(args[0]=="groups")Groups(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="outside")Outside(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="backgrounds")Backgrounds(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="size-pointer")SizePointer(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="presentation")Presentation(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="panel-profile")PanelProfile(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="acceptance")Acceptance(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="background-controls")BackgroundControls(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="panel-layout")PanelLayout(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="pointer-focus")PointerFocus(AppDomain.CurrentDomain.BaseDirectory);else if(args[0]=="background-overwrite")BackgroundOverwrite(AppDomain.CurrentDomain.BaseDirectory);else Fixture(AppDomain.CurrentDomain.BaseDirectory,args[0]=="fixture-outside");return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
    }
}
