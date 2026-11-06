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
    internal static class Patch031Tests
    {
        const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
        static string root;
        static object Get(MainWindow w,string name){return typeof(MainWindow).GetField(name,F).GetValue(w);}
        static void Set(MainWindow w,string name,object value){typeof(MainWindow).GetField(name,F).SetValue(w,value);}
        static object Call(MainWindow w,string name,params object[] args){return typeof(MainWindow).GetMethods(F).Single(m=>m.Name==name && m.GetParameters().Length==args.Length).Invoke(w,args);}
        static MapData Map(MainWindow w){return (MapData)Get(w,"map");}
        static int Count(MainWindow w,string field){return ((ICollection)Get(w,field)).Count;}
        static List<LibraryIcon> Library(MainWindow w){return (List<LibraryIcon>)Get(w,"libraryIcons");}
        static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
        static MapData Small(int c,int r){var map=new MapData { Columns=c,Rows=r };for(int i=0;i<c*r;i++)map.Cells.Add(new CellData());return map;}
        static void Gesture(MainWindow w,int start,params int[] moves)
        {
            var canvas=(Control)Get(w,"canvas");Func<int,Point> p=i=>Point.Round((PointF)Call(w,"ScreenCenter",i%Map(w).Columns,i/Map(w).Columns));var at=p(start);
            Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0));
            foreach(int i in moves){at=p(i);Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.Left,0,at.X,at.Y,0));}
            Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,at.X,at.Y,0));
        }
        static LibraryIcon Add(MainWindow w,string name,IconAsset asset,string directory)
        {var entry=new LibraryIcon {Name=name,Asset=asset};object[] args={null,entry,false,directory,null};Check((bool)Call(w,"TryChangeLibraryIcon",args),"Add: "+args[4]);return Library(w).Single(i=>i.Id==entry.Id);}
        static void History(MainWindow w,bool redo){object[] args={redo,null};Check((bool)Call(w,"TryHistory",args),"History: "+args[1]);}
        static void Change(MainWindow w,LibraryIcon before,LibraryIcon next,bool update,string directory)
        {object[] args={before,next,update,directory,null};Check((bool)Call(w,"TryChangeLibraryIcon",args),"Change: "+args[4]);}
        static void FogAndDrag()
        {
            using(var w=new MainWindow()){
                var m=Small(7,1);Set(w,"map",m);foreach(var c in m.Cells){c.Terrain="Woda";c.Color="#112233";c.VariantId=Guid.NewGuid().ToString("N");}m.Cells[0].Fog=m.Cells[1].Fog=m.Cells[4].Fog=m.Cells[5].Fog=true;m.Cells[6].Terrain="Teren neutralny";
                Set(w,"currentTool",EditorTool.FogBucket);Call(w,"CaptureSavedState");Gesture(w,0,1,2,4,5,6,0);
                Check(!m.Cells[0].Fog&&!m.Cells[1].Fog&&!m.Cells[4].Fog&&!m.Cells[5].Fog&&!m.Cells[2].Fog&&!m.Cells[6].Fog && Count(w,"undo")==1,"Fog drag changed opposite state/retoggled/created multiple steps");History(w,false);Check(m.Cells[0].Fog&&m.Cells[1].Fog&&m.Cells[4].Fog&&m.Cells[5].Fog,"Fog Undo");History(w,true);
                Call(w,"FillRegion",2);Check(m.Cells.Take(6).All(c=>c.Fog)&&!m.Cells[6].Fog,"Same-color terrain names not separated or initial fog splits ignored");History(w,false);
                // A single click discovers only its own pre-existing fog island.
                m.Cells[0].Fog=m.Cells[1].Fog=m.Cells[4].Fog=m.Cells[5].Fog=true;Call(w,"FillRegion",0);Check(!m.Cells[0].Fog&&!m.Cells[1].Fog&&m.Cells[4].Fog&&m.Cells[5].Fog,"Separated fog islands merged");
            }
            using(var w=new MainWindow()){
                Set(w,"map",Small(6,1));var m=Map(w);for(int i=0;i<6;i++)m.Cells[i].VariantId=i<2?"left":i<4?"middle":"right";
                var a=Add(w,"A",IconAssets.Starters()[0],Path.Combine(root,"drag-icons"));Call(w,"SelectIcon",a.Id);Set(w,"currentTool",EditorTool.Bucket);int steps=Count(w,"undo");Gesture(w,0,2,4,0);
                Check(m.Cells.All(c=>c.IconEntryId==a.Id)&&Count(w,"undo")==steps+1,"Icon bucket drag not one step");History(w,false);Check(m.Cells.All(c=>c.IconId==null),"Icon drag Undo");History(w,true);
                Set(w,"iconMaterial",false);Set(w,"currentTool",EditorTool.Bucket);steps=Count(w,"undo");Gesture(w,0,2,4,0);Check(m.Cells.Select(c=>c.VariantId).Distinct().Count()==1&&Count(w,"undo")==steps+1,"Terrain drag region history");History(w,false);Check(m.Cells[0].VariantId=="left"&&m.Cells[5].VariantId=="right","Terrain undo lost boundaries");
            }
        }
        static void ControlsAndBrush()
        {
            using(var w=new MainWindow()){
                string before=MapLogic.Serializer().Serialize(Map(w));var original=Map(w).Presets[0].Terrains[0];var next=original.Copy();next.Name="Blocked";Call(w,"AddTerrain",next);Call(w,"UpdateTerrain",original,next,true);Call(w,"DeleteTerrain",original.TerrainId);Call(w,"RenamePreset");Call(w,"EditTerrainDialog",false,original.TerrainId);Call(w,"RemoveTerrainById",original.TerrainId);
                var tile=((Control)Get(w,"paletteTiles")).Controls.Cast<TerrainTile>().First();typeof(Control).GetMethod("OnKeyDown",F).Invoke(tile.Choose,new object[]{new KeyEventArgs(Keys.F2)});typeof(Control).GetMethod("OnKeyDown",F).Invoke(tile.Choose,new object[]{new KeyEventArgs(Keys.Delete)});
                Check(before==MapLogic.Serializer().Serialize(Map(w))&&Count(w,"undo")==0&&!tile.Edit.Enabled&&!tile.Delete.Enabled,"Default mutation via method/keyboard");
                var size=(NumericUpDown)Get(w,"sizePicker");var fill=(CheckBox)Get(w,"fillPicker");Check(!size.Enabled&&!fill.Enabled,"Point size still enabled");var picker=(ComboBox)Get(w,"shapePicker");picker.SelectedIndex=(int)BrushShape.Rectangle;Check(size.Enabled&&fill.Enabled,"Shape controls not restored");
                object[] save={Path.Combine(root,"presets"),true,"Edytowalny",null};Check((bool)Call(w,"TrySaveWorkingPreset",save),"Default copy: "+save[3]);Check(Map(w).ActivePresetId!=PresetCatalog.DefaultId,"Copy not selected");Call(w,"AddTerrain",new TerrainDefinition());Check(Map(w).Presets.Last().Terrains.Count==7,"Copy cannot edit");
                Call(w,"SelectTerrain",Map(w).Presets.Last().Terrains[0].TerrainId);Check(tile.Choose.FlatAppearance.BorderColor==Color.Gold&&tile.Choose.FlatAppearance.BorderSize==3,"Terrain not gold");
                w.Size=w.MinimumSize;w.Show();Application.DoEvents();for(int i=0;i<12;i++)Call(w,"AddTerrain",new TerrainDefinition());Application.DoEvents();var viewport=(Control)Get(w,"terrainScroll");var up=(Button)Get(w,"terrainUp");var down=(Button)Get(w,"terrainDown");int top=viewport.Top,height=viewport.Height;Call(w,"ScrollTerrains",100000);Check(up.Enabled&&!down.Enabled&&viewport.Top==top&&viewport.Height==height&&!((Panel)viewport).AutoScroll,"End arrow/viewport unstable");Call(w,"ScrollTerrains",-100000);Check(!up.Enabled&&down.Enabled&&viewport.Height==height,"Start arrow unstable");
                var canvas=(Control)Get(w,"canvas");var center=(PointF)Call(w,"ScreenCenter",0,0);float x=center.X+canvas.Left;((Button)Get(w,"leftToggle")).PerformClick();center=(PointF)Call(w,"ScreenCenter",0,0);Check(Math.Abs(x-center.X-canvas.Left)<.01,"Collapse changed final camera");
                Call(w,"CaptureSavedState");w.Close();
            }
            using(var w=new MainWindow()){
                Set(w,"map",Small(10,1));Set(w,"currentTool",EditorTool.FogBrush);Gesture(w,0,9);Check(Map(w).Cells.All(c=>c.Fog)&&Count(w,"undo")==1,"Fast brush segment has gaps");History(w,false);
                var canvas=(Control)Get(w,"canvas");Point p=Point.Round((PointF)Call(w,"ScreenCenter",0,0));Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.Left,0,-10,-10,0));p=Point.Round((PointF)Call(w,"ScreenCenter",9,0));Call(w,"CanvasMouseMove",canvas,new MouseEventArgs(MouseButtons.Left,0,p.X,p.Y,0));Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));Check(Map(w).Cells.Count(c=>c.Fog)==2,"Brush drew a line after leaving canvas");
            }
        }
        static void IconsAndFiles()
        {
            string dir=Path.Combine(root,"icons");Directory.CreateDirectory(dir);var image=IconAssets.Starters()[0];
            using(var w=new MainWindow()){
                var a=Add(w,"Namiot",image,dir);var b=Add(w,"Namiot",image,dir);Check(Path.GetFileName(a.FilePath)=="icon-Namiot.json"&&Path.GetFileName(b.FilePath)=="icon-Namiot (2).json"&&a.Id!=b.Id&&!(bool)Get(w,"dirty"),"Readable files/logical identity/library dirties map");
                var m=Map(w);m.IconAssets.Add(image);m.Cells[0].IconId=m.Cells[1].IconId=m.Cells[2].IconId=image.Id;m.Cells[0].IconEntryId=m.Cells[1].IconEntryId=a.Id;m.Cells[2].IconEntryId=b.Id;m.Cells[0].IconName=m.Cells[1].IconName=m.Cells[2].IconName="Namiot";m.Cells[0].Fog=true;m.Cells[0].Description="Keep description";Call(w,"CaptureSavedState");
                var candidate=a.Copy();candidate.Name="Żółta Wieża";candidate.Asset=IconAssets.Starters()[2];Change(w,a,candidate,false,dir);var edited=Library(w).Single(i=>i.Id==a.Id);Check(!File.Exists(a.FilePath)&&File.Exists(edited.FilePath)&&m.Cells[0].IconId==image.Id&&m.Cells[0].IconName=="Namiot"&&!(bool)Get(w,"dirty"),"Library-only edit changed map or duplicated file");History(w,false);Check(File.Exists(a.FilePath)&&Library(w).Single(i=>i.Id==a.Id).Name=="Namiot","Name/image Undo");History(w,true);
                edited=Library(w).Single(i=>i.Id==a.Id);candidate=edited.Copy();candidate.Name="Własna Gwiazda";candidate.Asset=IconAssets.Starters()[4];int count=Count(w,"undo");Change(w,edited,candidate,true,dir);var updated=Library(w).Single(i=>i.Id==a.Id);Check(m.Cells[0].IconId==candidate.Asset.Id&&m.Cells[1].IconName==candidate.Name&&m.Cells[2].IconId==image.Id&&m.Cells[0].Fog&&m.Cells[0].Description=="Keep description"&&Count(w,"undo")==count+1,"Atomic icon edit changed independent data or same-byte other entry");History(w,false);Check(m.Cells[0].IconId==image.Id&&m.Cells[0].IconName=="Namiot"&&Library(w).Single(i=>i.Id==a.Id).Name=="Żółta Wieża","Combined Undo failed");History(w,true);
                string mapFile=Path.Combine(root,"portable.hexmap");object[] save={mapFile,false,null};Check((bool)Call(w,"TrySaveTo",save),"Portable save: "+save[2]);var stored=MapLogic.Serializer().Deserialize<MapData>(File.ReadAllText(mapFile));Check(stored.FormatVersion==3&&stored.IconAssets.Count==2&&stored.Cells[0].IconEntryId==a.Id&&stored.Cells[1].IconName=="Własna Gwiazda","Save lost dedupe/metadata");
                string path=updated.FilePath;byte[] bytes=updated.DiskBytes;Change(w,updated,null,false,dir);Check(!File.Exists(path)&&!Library(w).Any(i=>i.Id==a.Id)&&m.Cells[0].IconId==candidate.Asset.Id,"Delete damaged map");
                File.WriteAllText(path,"FOREIGN");count=Count(w,"undo");object[] undo={false,null};Check(!(bool)Call(w,"TryHistory",undo)&&Count(w,"undo")==count&&File.ReadAllText(path)=="FOREIGN"&&!Library(w).Any(i=>i.Id==a.Id),"Restore overwrote conflict/advanced history");File.Delete(path);History(w,false);Check(File.ReadAllBytes(path).SequenceEqual(bytes)&&Library(w).Any(i=>i.Id==a.Id),"Delete undo did not restore exact bytes");History(w,true);History(w,false);
                File.WriteAllText(path,"EXTERNALLY CHANGED");undo=new object[]{false,null};count=Count(w,"undo");string state=MapLogic.Serializer().Serialize(m);Check(!(bool)Call(w,"TryHistory",undo)&&Count(w,"undo")==count&&MapLogic.Serializer().Serialize(m)==state&&File.ReadAllText(path)=="EXTERNALLY CHANGED","External update overwritten");File.WriteAllBytes(path,bytes);
                // A source locked against rename causes no partial memory/history operation.
                updated=Library(w).Single(i=>i.Id==a.Id);candidate=updated.Copy();candidate.Name="Locked";using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){object[] failed={updated,candidate,true,dir,null};Check(!(bool)Call(w,"TryChangeLibraryIcon",failed)&&Count(w,"undo")==count&&Library(w).Single(i=>i.Id==a.Id).Name==updated.Name&&MapLogic.Serializer().Serialize(m)==state,"Write failure changed state");}
                // Flat 0.3.0 icon file: no startup rename; only explicit edit normalizes.
                string old=Path.Combine(dir,"icon-oldhash.json");File.WriteAllText(old,MapLogic.Serializer().Serialize(IconAssets.Starters()[3]));var legacy=IconLibrary.Read(old);Check(legacy.Id==legacy.Asset.Id&&File.Exists(old),"Legacy file changed on read");Library(w).Add(legacy);candidate=legacy.Copy();candidate.Name="Czaszka Polska";Change(w,legacy,candidate,false,dir);Check(!File.Exists(old)&&File.Exists(Path.Combine(dir,"icon-Czaszka Polska.json")),"Explicit edit did not normalize old filename");
            }
        }
        static void ImportAndReset()
        {
            string dir=Path.Combine(root,"import");using(var w=new MainWindow()){
                string png=Path.Combine(root,"Polska Ikona.png");using(var bitmap=new Bitmap(24,40)){using(var g=Graphics.FromImage(bitmap))g.Clear(Color.Gold);bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png);}
                var imported=IconAssets.Import(png);File.Delete(png);var entry=Add(w,imported.Name,imported,dir);byte[] bytes=entry.DiskBytes;History(w,false);Check(!File.Exists(entry.FilePath)&&Library(w).Count==0&&!(bool)Get(w,"dirty"),"Import Undo changed map or left file");History(w,true);Check(File.ReadAllBytes(entry.FilePath).SequenceEqual(bytes),"Import Redo bytes");
                Set(w,"currentTool",EditorTool.Brush);Gesture(w,0,1);string file=Path.Combine(root,"roundtrip.hexmap");object[] save={file,false,null};Check((bool)Call(w,"TrySaveTo",save),"Save");int count=Count(w,"undo");string bad=Path.Combine(root,"bad.hexmap");File.WriteAllText(bad,"broken");object[] open={bad,null,false};Check(!(bool)Call(w,"TryLoadProject",open)&&Count(w,"undo")==count,"Failed open cleared history");open=new object[]{file,null,false};Check((bool)Call(w,"TryLoadProject",open)&&Count(w,"undo")==0&&Map(w).Cells[0].IconEntryId==entry.Id&&Map(w).Cells[1].IconName=="Polska Ikona"&&Map(w).IconAssets.Count==1,"Metadata roundtrip/open reset");
                var other=Add(w,"Druga",imported,dir);Call(w,"StartNewProject",3,2,"PointyTop","Fresh",PresetCatalog.Default());Check(Count(w,"undo")==0&&Count(w,"redo")==0&&Map(w).IconAssets.Count==0&&File.Exists(other.FilePath)&&File.Exists(entry.FilePath),"New cleared active files or kept history/resources");
            }
        }
        static void Names()
        {
            string dir=Path.Combine(root,"names");Directory.CreateDirectory(dir);
            foreach(string name in new[]{"Żółty Namiot","CON",".. / Czaszka.","???"}){string file=IconLibrary.Destination(dir,name,null);Check(Path.GetDirectoryName(file)==dir&&Path.GetFileName(file).StartsWith("icon-"),"Unsafe icon filename");}
            Check(Path.GetFileName(IconLibrary.Destination(dir,"Żółty Namiot",null))=="icon-Żółty Namiot.json","Case/polish name lost");
            string deep=Path.Combine(root,new string('p',196-root.Length-1));Directory.CreateDirectory(deep);string first=IconLibrary.Destination(deep,"Namiot",null);File.WriteAllText(first,"KEEP");bool failed=false;try{IconLibrary.Destination(deep,"Namiot",null);}catch(IOException){failed=true;}Check(failed&&File.ReadAllText(first)=="KEEP"&&Path.GetFileName(first).StartsWith("icon-"),"Prefix truncated or collision overwrote deep filename");
        }
        static void Fixture(string directory)
        {
            Directory.CreateDirectory(directory);var map=Small(8,5);var p=new TerrainPreset {Name="Próba przewijania"};for(int i=0;i<18;i++)p.Terrains.Add(new TerrainDefinition {Name="Teren "+(i+1).ToString("00"),Color=i%2==0?"#287DB3":"#72A65B"});map.Presets=new List<TerrainPreset>{p};map.ActivePresetId=p.Id;
            string icons=Path.Combine(directory,"Library","Icons");string warning;IconLibrary.Load(icons,out warning);var a=new LibraryIcon {Name="Próba edycji",Asset=IconAssets.Starters()[0]};File.WriteAllText(IconLibrary.Destination(icons,a.Name,null),MapLogic.Serializer().Serialize(a));map.IconAssets.Add(a.Asset);map.Cells[0].IconId=a.Asset.Id;map.Cells[0].IconEntryId=a.Id;map.Cells[0].IconName=a.Name;
            File.WriteAllText(Path.Combine(directory,"Próba.hexmap"),MapLogic.Serializer().Serialize(map));
        }
        [STAThread] static int Main(string[] args)
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            if(args.Length==2&&args[0]=="--fixture"){Fixture(args[1]);return 0;}
            root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");Directory.CreateDirectory(root);
            try {string filter=args.Length==0?"":args[0];int passed=0;foreach(var pair in new[]{Tuple.Create("regions",(Action)FogAndDrag),Tuple.Create("controls",(Action)ControlsAndBrush),Tuple.Create("icons",(Action)IconsAndFiles),Tuple.Create("import",(Action)ImportAndReset),Tuple.Create("names",(Action)Names)})if(pair.Item1==filter){pair.Item2();Console.WriteLine("PASS "+pair.Item1);passed++;}Check(passed>0,"Choose one case");Console.WriteLine("Passed: "+passed+" targeted 0.3.1 scenarios; no full campaign.");return 0;}
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
