using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch054Tests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Get(object owner,string name) { return owner.GetType().GetField(name,Private).GetValue(owner); }
        static void Set(object owner,string name,object value) { owner.GetType().GetField(name,Private).SetValue(owner,value); }
        static object Call(object owner,string method,params object[] args) { return owner.GetType().GetMethods(Private).Single(m=>m.Name==method && m.GetParameters().Length==args.Length).Invoke(owner,args); }
        static void Check(bool value,string why) { if(!value)throw new Exception(why); }
        static MapData Map(MainWindow w) { return (MapData)Get(w,"map"); }
        static string Cells(MainWindow w) { return MapLogic.Serializer().Serialize(Map(w).Cells); }
        static void Start(MainWindow w,int cols=5,int rows=3) { Set(w,"libraryPresets",new List<TerrainPreset>{PresetCatalog.Default()});var p=PresetCatalog.Default().Copy(); p.Id="own"; p.Name="Własny"; Call(w,"StartNewProject",cols,rows,"PointyTop","Próba054",p); }
        static void History(MainWindow w,bool redo) { var args=new object[]{redo,null}; Check((bool)Call(w,"TryHistory",args),"History: "+args[1]); }
        static void Save(MainWindow w,string file) { var args=new object[]{file,false,null}; Check((bool)Call(w,"TrySaveTo",args),"Save: "+args[2]); }
        static void Load(MainWindow w,string file) { var args=new object[]{file,null,false}; Check((bool)Call(w,"TryLoadProject",args),"Load: "+args[1]); }
        static GraphicDefinition Graphic(string root,bool transparent=false)
        {
            var file=Path.Combine(root,transparent ? "Alpha.png" : "Stripes.png");
            using(var image=new Bitmap(1024,768))using(var g=Graphics.FromImage(image))
            {
                g.Clear(transparent ? Color.Transparent : Color.DarkBlue);
                if(!transparent)for(int y=0;y<768;y+=80)g.FillRectangle(Brushes.Gold,0,y,1024,30);
                image.Save(file,ImageFormat.Png);
            }
            var source=GraphicSources.Import(file);var graphic=new GraphicDefinition{Source=source,SourceId=source.Id,Name="Pasy"};graphic.Seal();return graphic;
        }
        static void Mouse(Control control,string method,int x=10,int y=10) { Call(control,method,new MouseEventArgs(MouseButtons.Left,1,x,y,0)); }
        static void Perf(string root)
        {
            var graphic=Graphic(root);
            using(var dialog=new BackgroundDialog(new MapBackground{Source=graphic.Source,SourceId=graphic.SourceId,Blur=8},new Size(1185,832)))
            {
                var slider=(TrackBar)Get(dialog,"saturation"); Mouse(slider,"OnMouseDown");
                var watch=Stopwatch.StartNew();for(int i=0;i<20;i++)slider.Value=10+i*9;watch.Stop();
                Mouse(slider,"OnMouseUp");Console.WriteLine("20 saturation updates ms="+watch.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
                Check(slider.Value==181,"Latest saturation missing.");
            }
        }
        static void Empty(string root)
        {
            using(var w=new MainWindow())
            {
                Start(w);var m=Map(w);var neutral=m.Presets.Single(p=>p.Id==m.ActivePresetId).Terrains[0];
                Check(m.Cells.All(c=>MapLogic.IsUnpainted(c) && !MapLogic.HasUserData(c)),"New map is a painted preset.");
                var version=neutral.Copy();version.Color="#0044FF";version.Variants[0].Color=version.Color;version.VariantId="neutral-v2";version.Revision++;
                string empty=Cells(w);Call(w,"UpdateTerrain",neutral,version,true,true);Check(Cells(w)==empty,"Updating neutral paints empty fields.");History(w,false);
                MapLogic.ApplyTerrain(m.Cells[7],neutral);
                Check(m.Cells[7].Color==m.Cells[0].Color && MapLogic.Flood(m,7,m.Cells.Select(c=>new RegionState(c)).ToArray(),false).Count==1,"Painted neutral joins empty region.");
                string before=Cells(w);Call(w,"UpdateTerrain",neutral,version,true,true);Check(m.Cells[7].Color=="#0044FF" && m.Cells.Where((c,i)=>i!=7).All(MapLogic.IsUnpainted),"Update didn't distinguish painted neutral.");
                string after=Cells(w);History(w,false);Check(Cells(w)==before,"Undo loses unpainted IDs.");History(w,true);Check(Cells(w)==after,"Redo loses unpainted IDs.");
                Call(w,"SelectPreset",PresetCatalog.DefaultId);Check(Cells(w)==after,"Preset selection assigned empty cells.");
                Set(w,"currentTool",EditorTool.Bucket);Set(w,"brushIndex",1);Call(w,"FillRegion",0);
                Check(m.Cells.Count(c=>c.TerrainId=="default-water")==14 && m.Cells[7].VariantId=="neutral-v2","Bucket crosses painted neutral.");History(w,false);Check(Cells(w)==after,"Bucket undo.");History(w,true);History(w,false);
                var plan=MapLogic.ResizeAll(m,1,0,1,0);Call(w,"ApplyResize",plan.State);Check(m.Cells.Count(MapLogic.IsUnpainted)==23 && m.Cells.Count==24,"New resize fields are painted.");
                string grown=Cells(w);History(w,false);Check(Cells(w)==after,"Resize undo.");History(w,true);Check(Cells(w)==grown,"Resize redo.");
                string file=Path.Combine(root,"Empty.hexmap");Save(w,file);using(var loaded=new MainWindow()){Load(loaded,file);Check(Cells(loaded)==Cells(w),"Empty/painted roundtrip.");}
            }
        }
        static void Editor(string root)
        {
            var graphic=Graphic(root);var pool=new List<FillVariant>{new FillVariant{Id="a",Color="#CC0000"},new FillVariant{Id="b",Graphic=graphic,GraphicId=graphic.Id}};
            using(var w=new MainWindow())using(var d=new AppearancePoolDialog("Test","Nazwa",pool,true,false,"Zastosuj"))
            {
                Start(w);string initial=Cells(w);var tiles=(FlowLayoutPanel)Get(d,"tiles");
                Check(tiles.Controls.Cast<Control>().All(c=>c.Width==88&&c.Height==88),"Non-square tile/plus.");
                var close=tiles.Controls[0].Controls.OfType<VariantRemoveButton>().Single();Check(close.ForeColor.R>200,"Missing red remove action.");
                MouseClick(close);Check(d.Fills.Count==1 && d.Fills[0].Id=="b","Remove click opened editor or wrong position.");
                Check(!d.RemoveVariant(0),"Deleted last variant.");d.LocalHistory(false);Check(d.Fills[0].Id=="a"&&d.Fills[1].Id=="b","Undo ID/order.");
                var changed=AppearanceHistory.Snapshot(d.Fills);changed[1].Graphic=changed[1].Graphic.Copy();CropMath.Rotate(changed[1].Graphic,false,90);changed[1].Graphic.Seal();changed[1].GraphicId=changed[1].Graphic.Id;
                d.CommitFills(changed);d.LocalHistory(false);Check(d.Fills[1].Graphic.Pointy.Angle==0,"Crop undo.");d.LocalHistory(true);Check(d.Fills[1].Graphic.Pointy.Angle==90&&Object.ReferenceEquals(d.Fills[1].Graphic.Source,graphic.Source),"Crop/source snapshot.");
                d.RemoveVariant(1);d.LocalHistory(false);Check(d.Fills[1].Id=="b"&&d.Fills[1].Graphic.Pointy.Angle==90&&d.Fills[1].Graphic.Flat.Angle==90&&Object.ReferenceEquals(d.Fills[1].Graphic.Source,graphic.Source),"Delete undo lost graphic/crops/source.");
                d.LocalHistory(false);Check(!d.CommitFills(d.Fills) && ((AppearanceHistory)Get(d,"history")).CanRedo,"No-op discarded redo.");
                changed=AppearanceHistory.Snapshot(d.Fills);changed[0].Color="#00CC00";d.CommitFills(changed);Check(!d.LocalHistory(true),"New branch retains redo.");
                Check(Cells(w)==initial && ((Stack<EditAction>)Get(w,"undo")).Count==0,"Local history touched map.");
                var t=Map(w).Presets.Single(p=>p.Id==Map(w).ActivePresetId).Terrains[0];var next=t.Copy();FillRules.SetPool(next,d.Fills);next.VariantId="editor-v2";next.Revision++;Call(w,"UpdateTerrain",t,next,false,true);
                Check(((Stack<EditAction>)Get(w,"undo")).Count==1,"Accepted pool isn't one main step.");
                d.DialogResult=DialogResult.Cancel;Check(Cells(w)==initial,"Cancel modified cells.");
            }
            using(var d=new AppearancePoolDialog("Mgła",null,pool,true,true,"Zastosuj")){Check(!d.LocalHistory(false),"Reopen kept local history.");d.RemoveVariant(0);d.LocalHistory(false);Check(d.Fills.Count==2,"Fog local undo.");}
            var h=new AppearanceHistory(pool);for(int i=0;i<105;i++){var value=h.Current;value[0].Color="#"+i.ToString("X6");h.Commit(value);}int undo=0;while(h.Move(false))undo++;Check(undo==100,"Local limit not100.");
        }
        static void MouseClick(Button button) { Call(button,"OnClick",EventArgs.Empty); }
        static void Crop(string root)
        {
            var graphic=Graphic(root);
            using(var d=new TerrainCropDialog(graphic,true,true))
            {
                Check(d.Controls.OfType<CropSurface>().Count()==1,"Hidden second renderer.");
                var draft=(GraphicDefinition)Get(d,"draft");var rotation=(TrackBar)Get(d,"rotation");rotation.Value=90;
                Check(draft.Pointy.Angle==90&&draft.Flat.Angle==90,"New import doesn't initialize both crops.");
                var selector=(ComboBox)Get(d,"orientation");selector.SelectedIndex=0;
                Check(!draft.Linked && draft.Pointy.Angle==90&&draft.Flat.Angle==90,"First switch resets crops.");
                rotation.Value=135;Check(draft.Pointy.Angle==135&&draft.Flat.Angle==90,"Independent crop edited both.");
                selector.SelectedIndex=1;Check(draft.Pointy.Angle==135&&draft.Flat.Angle==90&&!draft.Linked,"Switch relinked.");
                var surface=(CropSurface)Get(d,"surface");Mouse(surface,"OnMouseDown",220,220);Mouse(surface,"OnMouseMove",245,230);Mouse(surface,"OnMouseUp",245,230);CropMath.Validate(draft.Flat,draft.Source);CropMath.Validate(draft.Pointy,draft.Source);
                MouseClick((Button)d.AcceptButton);Check(!d.Result.Linked && graphic.Pointy.Angle==0,"Accept changed source or persisted initialization.");
                var preset=new TerrainPreset{Name="Kadry"};var terrain=new TerrainDefinition();FillRules.SetPool(terrain,new List<FillVariant>{new FillVariant{Graphic=d.Result,GraphicId=d.Result.Id}});preset.Terrains.Add(terrain);string dir=Path.Combine(root,"Presets");PresetCatalog.Save(preset,dir);var read=PresetCatalog.ReadFile(Directory.GetFiles(dir).Single());
                Check(read.Terrains[0].Texture.Pointy.Angle==135&&read.Terrains[0].Texture.Flat.Angle==90&&GraphicSources.Same(read.Terrains[0].Texture.Source,graphic.Source),"Crop roundtrip.");
                var saved=d.Result.Copy();saved.Linked=true;
                using(var unchanged=new TerrainCropDialog(saved,false,false)){MouseClick((Button)unchanged.AcceptButton);Check(unchanged.Result.Id==saved.Id && unchanged.Result.Linked==saved.Linked,"No-op recrop rewrote legacy metadata/history.");}
                using(var edit=new TerrainCropDialog(saved,true,false)){var copy=(GraphicDefinition)Get(edit,"draft");((TrackBar)Get(edit,"rotation")).Value=180;Check(copy.Pointy.Angle==135&&copy.Flat.Angle==180&&!copy.Linked,"Legacy Linked controls re-edit.");edit.DialogResult=DialogResult.Cancel;Check(saved.Flat.Angle==90,"Cancel changed saved crop.");}
            }
        }
        sealed class NoDraws:Random { public override int Next(int n) { throw new Exception("Palette used painting Random"); } }
        static string Pixels(Bitmap image) { using(var stream=new MemoryStream()){image.Save(stream,ImageFormat.Png);return Convert.ToBase64String(stream.ToArray());} }
        static void Presentation(string root)
        {
            var graphic=Graphic(root);var terrain=new TerrainDefinition{Name="Mozaika",TerrainId="mosaic"};
            FillRules.SetPool(terrain,Enumerable.Range(0,6).Select(i=>new FillVariant{Id="fill"+i,Color=i%2==0?"#DD3355":"#33AA66",Graphic=i==3?graphic:null,GraphicId=i==3?graphic.Id:null}).ToList());
            Check(TerrainMosaic.Representatives(terrain).Count==6,"Small pool misses positions.");
            using(var a=TerrainMosaic.Render(terrain,new Size(152,32),false))using(var b=TerrainMosaic.Render(terrain,new Size(152,32),false))Check(Pixels(a)==Pixels(b),"Mosaic redraw changes.");
            var key=TerrainMosaic.Key(terrain);terrain.Name="Inna nazwa";terrain.Variants.Reverse();Check(key==TerrainMosaic.Key(terrain),"Name/order reshuffled.");FillRules.SetPool(terrain,terrain.Variants);
            using(var w=new MainWindow())
            {
                Start(w,15,10);Set(w,"appearanceRandom",new NoDraws());Call(w,"AddTerrain",terrain);Call(w,"RefreshPresetPicker");Call(w,"UpdateTerrainSelection");
                var m=Map(w);m.Background=new MapBackground{Source=graphic.Source,SourceId=graphic.SourceId};MapLogic.ApplyTerrain(m.Cells[0],terrain,terrain.Variants[0]);
                Save(w,Path.Combine(root,"Fixture054.hexmap"));
            }
            FillRules.SetPool(terrain,Enumerable.Range(0,32).Select(i=>new FillVariant{Id="many"+i}).ToList());Check(TerrainMosaic.Representatives(terrain).Count==TerrainMosaic.Capacity,"Large pool unbounded.");
            var bg=new MapBackground{Source=graphic.Source,SourceId=graphic.SourceId,Blur=3};
            using(var d=new BackgroundDialog(bg,new Size(1200,600)))
            {
                var left=(Control)Get(d,"crop");var right=(PictureBox)Get(d,"preview");Check(left.Size==right.Size&&left.Top==right.Top,"Background surfaces asymmetric.");
                var sat=(TrackBar)Get(d,"saturation");Mouse(sat,"OnMouseDown");sat.Value=175;Mouse(sat,"OnMouseUp");
                Check(Math.Abs((double)right.Image.Width/right.Image.Height-2)<.02,"Wrong canvas aspect.");
                var draft=(MapBackground)Get(d,"draft");using(var expected=BackgroundRenderer.Render(draft,new Size(1200,600),(Bitmap)Get(d,"previewSource"),Math.Min(right.Width,right.Height)))Check(Pixels((Bitmap)right.Image)==Pixels(expected),"Final saturation isn't exact renderer.");
            }
            var transparent=Graphic(root,true);
            using(var w=new MainWindow())using(var bitmap=new Bitmap(240,220))using(var g=Graphics.FromImage(bitmap))
            {
                Start(w,3,2);var m=Map(w);m.LabelMode="None";m.Cells[0].Color="#FF0000";m.Cells[0].Name="SECRET";m.Cells[0].Fog=true;m.Cells[0].FogFill=new FillVariant{Graphic=transparent,GraphicId=transparent.Id};Set(w,"pan",new PointF(90,90));Set(w,"scale",1f);
                Call(w,"PaintMap",null,new PaintEventArgs(g,new Rectangle(0,0,240,220)));Check(bitmap.GetPixel(90,90).ToArgb()==Color.Red.ToArgb()&&m.Cells[0].Fog,"Alpha has opaque base or clears Fog.");
                var icon=IconAssets.Starters()[0];m.IconAssets.Add(icon);var obj=new MapObject{Name="SECRET",TemplateId="own",AssetId=icon.Id,Side="Neutral",X=0,Y=0,Size=12};m.Objects.Add(obj);
                Check(((ObjectListRow)Call(w,"ObjectListItem",obj)).Text=="???" && Call(w,"HitObject",new PointF(0,0))==null,"Alpha leaks logical details/hit.");
                Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");Check(((TextBox)Get(w,"cellName")).Text=="???","Alpha leaks hex details.");
            }
        }
        [STAThread]static int Main(string[] args)
        {
            try { if(args.Length!=1)throw new ArgumentException("Wymagany Case");string root=AppDomain.CurrentDomain.BaseDirectory;
                if(args[0]=="background-perf")Perf(root);else if(args[0]=="empty")Empty(root);else if(args[0]=="editor")Editor(root);else if(args[0]=="crop")Crop(root);else if(args[0]=="presentation")Presentation(root);else throw new ArgumentException("Nieznany Case");
                Console.WriteLine("PASS "+args[0]);return 0;
            }catch(Exception ex){Console.WriteLine(ex);return 1;}
        }
    }
}
