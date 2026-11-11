using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Imaging;using System.Diagnostics;using System.IO;using System.Linq;using System.Reflection;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace HexMapEditor.Tests {
internal static class Patch053Tests {
const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
static object Get(object w,string n){return w.GetType().GetField(n,P).GetValue(w);} static object Call(MainWindow w,string n,params object[] a){return typeof(MainWindow).GetMethods(P).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(w,a);}
static void Check(bool c,string s){if(!c)throw new Exception(s);} static MapData Map(MainWindow w){return (MapData)Get(w,"map");}
static GraphicDefinition Graphic(string root) {var path=Path.Combine(root,"Noise.bmp");using(var b=new Bitmap(1024,1024,PixelFormat.Format24bppRgb)){var bits=b.LockBits(new Rectangle(0,0,1024,1024),ImageLockMode.WriteOnly,b.PixelFormat);var bytes=new byte[bits.Stride*1024];new Random(53).NextBytes(bytes);Marshal.Copy(bytes,0,bits.Scan0,bytes.Length);b.UnlockBits(bits);b.Save(path,ImageFormat.Bmp);}var s=GraphicSources.Import(path);var g=new GraphicDefinition{Source=s,SourceId=s.Id,Name="Noise"};g.Seal();return g;}
static TerrainDefinition Terrain(){var t=new TerrainDefinition{Name="Różowy"};FillRules.SetPool(t,new List<FillVariant>{new FillVariant{Color="#FF66AA"}});return t;}
static void Start(MainWindow w,TerrainDefinition t){var p=new TerrainPreset{Name="Test"};p.Terrains.Add(t);Call(w,"StartNewProject",15,10,"PointyTop","Test",p);}
static List<int> Flood(MapData m,int at,bool fog=false){return MapLogic.Flood(m,at,m.Cells.Select(c=>new RegionState(c)).ToArray(),fog);}
static void Regions(string root){using(var w=new MainWindow()){var t=Terrain();Start(w,t);var m=Map(w);foreach(var c in m.Cells)MapLogic.ApplyTerrain(c,t);var v=t.Copy();v.VariantId="new-version";v.Revision++;FillRules.SetPool(v,new List<FillVariant>{new FillVariant{Color="#1144EE"}});for(int r=0;r<m.Rows;r++)MapLogic.ApplyTerrain(m.Cells[r*m.Columns+7],v);Check(Flood(m,0).Count==70,"Wiadro przekracza przegrodę historycznej wersji.");}}
static void Timed(string label,Action action){var sw=Stopwatch.StartNew();action();sw.Stop();Console.WriteLine(label+" ms="+sw.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));}
static void Perf(string root){var graphic=Graphic(root);var t=Terrain();FillRules.SetPool(t,Enumerable.Range(0,8).Select(i=>new FillVariant{Graphic=graphic,GraphicId=graphic.Id}).ToList());using(var w=new MainWindow()){Start(w,t);foreach(var c in Map(w).Cells){MapLogic.ApplyTerrain(c,t);c.Fog=true;}Timed("budget-150",()=>Call(w,"CheckTextureBudget",(object)new[]{graphic}));var next=t.Copy();next.Name="Edytowany";next.VariantId="perf-new";next.Revision++;Timed("accept-terrain-150",()=>Call(w,"UpdateTerrain",t,next,true));Timed("accept-fog-150",()=>Call(w,"ChangeFogAppearance",t.Variants,true));}Timed("pool-8-same-source",()=>{using(var d=new AppearancePoolDialog("Test","Test",t.Variants,true,false,"Test")){} });using(var d=new TerrainCropDialog(graphic)){foreach(string side in new[]{"pointy","flat"}){var c=(Control)Get(d,side);using(var b=new Bitmap(c.Width,c.Height)){Timed("crop-"+side+"-10",()=>{for(int i=0;i<10;i++)c.DrawToBitmap(b,c.ClientRectangle);});}}} }
static string Cells(MainWindow w){return MapLogic.Serializer().Serialize(Map(w).Cells);}
static object Invoke(object obj,string method,params object[] args){return obj.GetType().GetMethod(method,P).Invoke(obj,args);}
static void Version(TerrainDefinition old,TerrainDefinition next){typeof(MainWindow).GetMethod("VersionTerrain",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{old,next});}
static void History(MainWindow w,bool redo){var a=new object[]{redo,null};Check((bool)Call(w,"TryHistory",a),"History "+a[1]);}
static void SaveMap(MainWindow w,string path){var a=new object[]{path,false,null};Check((bool)Call(w,"TrySaveTo",a),"Save "+a[2]);}
static void LoadMap(MainWindow w,string path){var a=new object[]{path,null,false};Check((bool)Call(w,"TryLoadProject",a),"Load "+a[1]);}
sealed class Choices:Random{internal int Calls;public override int Next(int count){return Calls++%count;}}
static void Boundaries(string root){
 var graphic=Graphic(root);var t=Terrain();FillRules.SetPool(t,new List<FillVariant>{new FillVariant{Color="#FF0000"},new FillVariant{Graphic=graphic,GraphicId=graphic.Id}});
 using(var w=new MainWindow()){
  Call(w,"StartNewProject",5,3,"FlatTop","Test",new TerrainPreset{Name="Test",Terrains=new List<TerrainDefinition>{t}});
  var m=Map(w);for(int i=0;i<m.Cells.Count;i++)MapLogic.ApplyTerrain(m.Cells[i],t,t.Variants[i%2]);
  Check(Flood(m,0).Count==15,"Warianty jednej wersji dzielą region.");
  var newer=t.Copy();var frame=graphic.Copy();CropMath.Rotate(frame,false,90);frame.Seal();newer.Variants[1].Graphic=frame;newer.Variants[1].GraphicId=frame.Id;FillRules.SetPool(newer,newer.Variants);Version(t,newer);
  for(int r=0;r<3;r++)MapLogic.ApplyTerrain(m.Cells[r*5+2],newer,newer.Variants[1]);
  Check(Flood(m,0).Count==6,"Grafika nowej wersji nie jest przegrodą.");
  var distinct=t.Copy();distinct.TerrainId="other-kind";MapLogic.ApplyTerrain(m.Cells[0],distinct);Check(Flood(m,0).Count==1,"Różne TerrainId łączą się przez wygląd.");
  MapLogic.ApplyTerrain(m.Cells[0],t,t.Variants[0]);m.Cells[5].Fog=true;Check(!Flood(m,0,true).Contains(5),"Mgła nie respektuje stanu początkowego.");
  string before=Cells(w);typeof(MainWindow).GetField("currentTool",P).SetValue(w,EditorTool.Bucket);Call(w,"FillRegion",0);string after=Cells(w);History(w,false);Check(Cells(w)==before,"Undo regionu.");History(w,true);Check(Cells(w)==after,"Redo regionu losuje.");
  Call(w,"DeleteTerrain",t.TerrainId);Check(Flood(m,0).Count==6,"Usunięcie palety niszczy region.");
  string file=Path.Combine(root,"History.hexmap");SaveMap(w,file);using(var loaded=new MainWindow()){LoadMap(loaded,file);Check(Cells(loaded)==Cells(w) && Flood(Map(loaded),0).Count==6,"Roundtrip wersji regionu.");}
  var renamed=t.Copy();renamed.Name="Tylko nazwa";Version(t,renamed);Check(renamed.VariantId==t.VariantId,"Nazwa tworzy nową powierzchnię.");
  renamed.Variants.Reverse();Version(t,renamed);Check(renamed.VariantId==t.VariantId,"Kolejność tworzy nową powierzchnię.");
 }
}
static void Updates(string root){
 var graphic=Graphic(root);var t=Terrain();var fills=new List<FillVariant>{new FillVariant{Id="red",Color="#FF0000"},new FillVariant{Id="blue",Color="#001166"},new FillVariant{Id="removed",Graphic=graphic,GraphicId=graphic.Id},new FillVariant{Id="copy-image",Graphic=graphic,GraphicId=graphic.Id}};FillRules.SetPool(t,fills);
 using(var w=new MainWindow()){
  Start(w,t);var m=Map(w);for(int i=0;i<150;i++)MapLogic.ApplyTerrain(m.Cells[i],t,t.Variants[i%4]);m.Cells[148].VariantId="ancient";
  var next=t.Copy();next.Variants.RemoveAll(f=>f.Id=="removed");next.Variants.Reverse();next.Variants.Single(f=>f.Id=="blue").Color="#88BBFF";next.Variants.Add(new FillVariant{Id="new",Color="#FFFFFF"});Version(t,next);FillRules.SetPool(next,next.Variants);
  string before=Cells(w);Call(w,"UpdateTerrain",t,next,true,true);string after=Cells(w);
  for(int i=0;i<150;i++){
   var c=m.Cells[i];Check(c.FillVariantId==fills[i%4].Id,"Przemapowanie przez indeks.");
   if(i%4==1)Check(c.Color=="#88BBFF","Nie zaktualizowano wybranego ID.");
   if(i%4==0)Check(c.Color=="#FF0000","Przestawiono czerwony.");
   if(i%4==2)Check(c.TextureId==graphic.Id && c.VariantId==t.VariantId,"Usunięty wariant stracił migawkę.");
  }
  Check(m.Cells[148].VariantId=="ancient" && !m.Cells.Any(c=>c.FillVariantId=="new"),"Zmieniono historyczną wersję lub obsadzono nowy wariant.");
  History(w,false);Check(Cells(w)==before,"Undo aktualizacji.");History(w,true);Check(Cells(w)==after,"Redo aktualizacji.");
  var draft=next.Copy();draft.Name="Bez aktualizacji";Call(w,"UpdateTerrain",next,draft,false,true);Check(Cells(w)==after,"Wyłączony główny checkbox zmienia mapę.");
  var changed=draft.Copy();var blue=changed.Variants.Single(f=>f.Id=="blue");blue.Graphic=graphic;blue.GraphicId=graphic.Id;Version(draft,changed);FillRules.SetPool(changed,changed.Variants);Call(w,"UpdateTerrain",draft,changed,true,true);Check(m.Cells[1].FillVariantId=="blue" && m.Cells[1].TextureId==graphic.Id,"Zmiana typu nie zachowuje ID.");
  var reroll=changed.Copy();reroll.Variants[0].Color="#ABCDEF";Version(changed,reroll);FillRules.SetPool(reroll,reroll.Variants);var random=new Choices();typeof(MainWindow).GetField("appearanceRandom",P).SetValue(w,random);Call(w,"UpdateTerrain",changed,reroll,true,false);Check(random.Calls>0,"Jawne przelosowanie nie losuje.");string rolled=Cells(w);int draws=random.Calls;History(w,false);History(w,true);Check(Cells(w)==rolled && random.Calls==draws,"Redo przelosowuje.");
  var pool=new List<FillVariant>{new FillVariant{Id="fog-a",Color="#223344"},new FillVariant{Id="fog-b",Graphic=graphic,GraphicId=graphic.Id}};m.FogPalette=FillRules.Copy(pool);for(int i=0;i<150;i++){m.Cells[i].Fog=i!=149;m.Cells[i].FogFill=pool[i%2].Copy();}
  var fog=FillRules.Copy(pool);fog.Reverse();fog[0].Color="#8899AA";fog.RemoveAll(f=>f.Id=="fog-a");fog.Add(new FillVariant{Id="new-fog",Color="#FFFFFF"});string fogBefore=Cells(w);Call(w,"ChangeFogAppearance",fog,true,true);Check(m.Cells[0].FogFill.Color=="#223344" && m.Cells[1].FogFill.Color=="#8899AA" && !m.Cells[149].Fog,"Mgła nie zachowuje pozycji lub zmienia zakres.");string fogAfter=Cells(w);History(w,false);Check(Cells(w)==fogBefore,"Undo mgły.");History(w,true);Check(Cells(w)==fogAfter,"Redo mgły.");
  SaveMap(w,Path.Combine(root,"Updated.hexmap"));using(var l=new MainWindow()){LoadMap(l,Path.Combine(root,"Updated.hexmap"));Check(Cells(l)==Cells(w),"Roundtrip migawek/ID.");}
 }
 using(var w=new MainWindow()){Start(w,t);var rng=new Choices();typeof(MainWindow).GetField("appearanceRandom",P).SetValue(w,rng);Call(w,"BeginMaterialStroke",0,EditorTool.Brush);for(int i=0;i<4;i++){Call(w,"PaintMaterial",i);Call(w,"PaintMaterial",i);}Check(rng.Calls==4 && Map(w).Cells[2].FillVariantId=="removed" && Map(w).Cells[3].FillVariantId=="copy-image","Pozycje identycznej grafiki scalone albo losowane ponownie.");Call(w,"FinishStroke");}
}
static void Mouse(Control c,string method,int x,int y){c.GetType().GetMethod(method,P).Invoke(c,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});}
static void Local(string root){
 var graphic=Graphic(root);
 using(var d=new TerrainCropDialog(graphic)){
  var a=(Control)Get(d,"pointy");var b=(Control)Get(d,"flat");Check(((Bitmap)Get(a,"image")).Width<=GraphicLimits.CropPreviewSide,"Pełne źródło w podglądzie.");
  var draft=(GraphicDefinition)Get(d,"draft");draft.Linked=false;CropMath.Rotate(draft,false,90);draft.Linked=true;CropMath.Rotate(draft,true,90);Check(draft.Pointy.Angle==180 && draft.Flat.Angle==90,"Połączenie resetuje różnicę.");
  Mouse(b,"OnMouseDown",150,150);Mouse(b,"OnMouseMove",175,160);Mouse(b,"OnMouseUp",175,160);CropMath.Validate(draft.Pointy,draft.Source);CropMath.Validate(draft.Flat,draft.Source);
 }
 var background=new MapBackground{Source=graphic.Source,SourceId=graphic.SourceId,Left=.1,Top=.1,Width=.8,Height=.8};
 using(var c=new BackgroundCropSurface{Size=new Size(450,320)}){
  c.CreateControl();c.SetContent(background);var box=(RectangleF)c.GetType().GetProperty("ImageBounds",P).GetValue(c,null);
  var points=new[]{new PointF(.5f,.1f),new PointF(.9f,.5f),new PointF(.5f,.9f),new PointF(.1f,.5f)};
  for(int i=0;i<4;i++){
   background.Left=background.Top=.1;background.Width=background.Height=.8;c.SetContent(background);
   int x=(int)Math.Round(box.X+points[i].X*box.Width), y=(int)Math.Round(box.Y+points[i].Y*box.Height);
   Mouse(c,"OnMouseDown",x,y);Mouse(c,"OnMouseMove",x+(i%2==1?-15:0),y+(i%2==0?15:0));Mouse(c,"OnMouseUp",x,y);BackgroundRenderer.Validate(background);
   if(i%2==0)Check(background.Left==.1 && background.Width==.8,"Krawędź góra/dół zmienia poziom.");else Check(background.Top==.1 && background.Height==.8,"Krawędź lewo/prawo zmienia pion.");
  }
 }
 using(var d=new BackgroundDialog(background,new Size(1200,600))){var image=((PictureBox)Get(d,"preview")).Image;Check(Math.Abs((double)image.Width/image.Height-2)<.02,"Podgląd nie odpowiada canvas.");var slider=(TrackBar)Get(d,"saturation");slider.Value=0;var reset=d.Controls.OfType<Button>().Single(c=>c.AccessibleName=="Przywróć nasycenie 100%");typeof(Button).GetMethod("OnClick",P).Invoke(reset,new object[]{EventArgs.Empty});Check(slider.Value==100,"Reset nasycenia.");}
 using(var d=new AppearancePoolDialog("Test","Test",new List<FillVariant>{new FillVariant()},true,false,"Zastosuj")){var apply=(CheckBox)Get(d,"apply");var preserve=(CheckBox)Get(d,"preserve");Check(preserve.Checked && !preserve.Enabled,"Domyślne zachowanie puli.");apply.Checked=true;Check(preserve.Enabled,"Zależność checkboxa.");}
 using(var tile=new TerrainTile(id=>{},id=>{},id=>{})){
  var t=Terrain();FillRules.SetPool(t,new List<FillVariant>{new FillVariant{Color="#FFFFFF"}});tile.UpdateTerrain(t,true);Check(tile.Choose.ForeColor==Color.Black,"Jasny tekst.");t.Variants[0].Color="#000000";FillRules.SetPool(t,t.Variants);tile.UpdateTerrain(t,false);Check(tile.Choose.ForeColor==Color.White,"Ciemny tekst.");t.Variants[0].Graphic=graphic;t.Variants[0].GraphicId=graphic.Id;FillRules.SetPool(t,t.Variants);tile.UpdateTerrain(t,true);var color=tile.Choose.ForeColor;tile.UpdateTerrain(t,false);Check(tile.Choose.ForeColor==color,"Kolor nazwy niestabilny.");using(var bitmap=new Bitmap(tile.Width,tile.Height))tile.DrawToBitmap(bitmap,tile.ClientRectangle);
 }
 using(var w=new MainWindow()){
  Start(w,Terrain());string before=Cells(w);var run=typeof(MainWindow).GetMethod("RunModal",P).MakeGenericMethod(typeof(int));run.Invoke(w,new object[]{(Func<int>)(()=>1)});int barrier=(int)Get(w,"canvasBarrier");Check(w.OldCanvasInput(barrier) && !w.OldCanvasInput(unchecked(barrier+1)),"Bariera czasu gestu."); typeof(MainWindow).GetField("closingClick",P).SetValue(w,(Point?)new Point(500,500));typeof(MainWindow).GetField("closingClickTime",P).SetValue(w,barrier);Check(w.RejectCanvasInput(barrier+1,new Point(500,500)) && !w.RejectCanvasInput(barrier+1,new Point(550,550)) && !w.RejectCanvasInput(barrier+SystemInformation.DoubleClickTime+1,new Point(500,500)),"Ogon dwukliku / nowy gest w innym miejscu.");
  var canvas=(Control)Get(w,"canvas");var pt=(PointF)Call(w,"ScreenCenter",0,0);typeof(MainWindow).GetField("currentTool",P).SetValue(w,EditorTool.Brush);
  Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,2,(int)pt.X,(int)pt.Y,0));Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,(int)pt.X,(int)pt.Y,0));Check(Cells(w)==before,"Klik kończący modal edytuje mapę.");
  Call(w,"CanvasMouseDown",canvas,new MouseEventArgs(MouseButtons.Left,1,(int)pt.X,(int)pt.Y,0));Call(w,"CanvasMouseUp",canvas,new MouseEventArgs(MouseButtons.Left,1,(int)pt.X,(int)pt.Y,0));Check(Cells(w)!=before,"Nowy świadomy gest zablokowany.");
 }
}
static void Safety(string root){
 Check(GraphicLimits.SourceBytes==32*1024*1024 && GraphicLimits.SourceSide==4096 && GraphicLimits.Pool==32,"Limity źródła/puli.");
 var fake=Enumerable.Range(0,4).Select(i=>new GraphicSource{Id="budget-"+i,LibraryEncodedLength=GraphicLimits.Encoded(32L*1024*1024)}).ToArray();bool rejected=false;try{GraphicSources.Collect(fake);}catch(InvalidDataException){rejected=true;}Check(rejected,"Limit źródeł dokumentu.");Check(GraphicSources.Collect(fake,true).Count==4,"Limit biblioteki nie jest niezależny.");
 Check(GraphicLimits.PresetBytes>GraphicLimits.Encoded(GraphicLimits.DocumentSources) && GraphicLimits.MapBytes>GraphicLimits.PresetBytes,"Niespójne limity serializacji.");
 var graphic=Graphic(root);string padded=Path.Combine(root,"Boundary.bmp");File.Copy(Path.Combine(root,"Noise.bmp"),padded);using(var stream=new FileStream(padded,FileMode.Open,FileAccess.Write))stream.SetLength(GraphicLimits.SourceBytes);
 var imported=GraphicSources.Import(padded);Check(imported.EncodedLength==GraphicLimits.Encoded(GraphicLimits.SourceBytes),"Dokładna granica32MiB.");
 var large=new GraphicDefinition{Source=imported,SourceId=imported.Id,Name="Boundary"};large.Seal();var terrain=Terrain();FillRules.SetPool(terrain,new List<FillVariant>{new FillVariant{Graphic=large,GraphicId=large.Id}});var preset=new TerrainPreset{Name="Boundary",Terrains=new List<TerrainDefinition>{terrain}};string dir=Path.Combine(root,"presets");PresetCatalog.Save(preset,dir);var read=PresetCatalog.ReadFile(Directory.GetFiles(dir).Single());Check(GraphicSources.Same(read.Terrains[0].Texture.Source,imported),"Poprawny import nie daje się odczytać.");
 using(var stream=new FileStream(padded,FileMode.Open,FileAccess.Write))stream.SetLength(GraphicLimits.SourceBytes+1L);rejected=false;try{GraphicSources.Import(padded);}catch(InvalidDataException){rejected=true;}Check(rejected,"Źródło przekracza32MiB.");
 string graphics=Path.Combine(root,"graphics");Directory.CreateDirectory(graphics);string old=Path.Combine(graphics,"terrain-old.json");string text="{\"Id\":\"synthetic-old-graphic\",\"Name\":\"Stary\",\"GraphicFormatVersion\":1}";File.WriteAllText(old,text);string warning;var entries=TerrainGraphicLibrary.Load(graphics,out warning);Check(entries.Count==0 && warning.Length>0,"Stary wpis zaimportowany.");
 using(var w=new MainWindow()){
  typeof(MainWindow).GetField("terrainGraphics",P).SetValue(w,entries);var item=(TerrainGraphic)Call(w,"CommitGraphicImport",graphic,(Func<TerrainGraphic,GraphicDefinition,DialogResult>)((a,b)=>DialogResult.No),graphics);Check(File.Exists(item.Path) && File.ReadAllText(old)==text,"Pominięta grafika blokuje nową lub jest zmieniona.");
  var loaded=TerrainGraphicLibrary.Read(item.Path);Check(loaded.Asset.Source.LibraryFile!=null,"Biblioteka trzyma pełne źródło w RAM.");var pinned=loaded.Asset.DocumentCopy();Check(pinned.Source.LibraryFile==null,"Dokument odwołuje się do pliku biblioteki.");
  History(w,false);Check(!File.Exists(item.Path),"Undo pliku grafiki.");History(w,true);Check(File.Exists(item.Path),"Redo pliku grafiki.");
  string presets=Path.Combine(root,"smoke");var args=new object[]{presets,"Nowy",null};Check((bool)Call(w,"TryCreatePreset",args),"Smoke nowy preset: "+args[2]);Call(w,"SelectPreset",PresetCatalog.DefaultId);args=new object[]{presets,true,"Kopia",null};Check((bool)Call(w,"TrySaveWorkingPreset",args),"Smoke kopii: "+args[3]);
 }
 // Own fixture for the final ordinary EXE, with sources embedded in the map.
 using(var w=new MainWindow()){
  var t=Terrain();Start(w,t);var m=Map(w);foreach(var c in m.Cells)MapLogic.ApplyTerrain(c,t);var blue=t.Copy();blue.Variants[0].Color="#2255DD";FillRules.SetPool(blue,blue.Variants);Version(t,blue);Call(w,"UpdateTerrain",t,blue,false,true);for(int row=0;row<10;row++)MapLogic.ApplyTerrain(m.Cells[row*15+7],blue);
  var mixed=new TerrainDefinition{Name="Pula testowa"};FillRules.SetPool(mixed,new List<FillVariant>{new FillVariant{Id="red",Color="#FF2222"},new FillVariant{Id="blue",Color="#001166"}});Call(w,"AddTerrain",mixed);
  var image=new TerrainDefinition{Name="Grafika testowa"};FillRules.SetPool(image,new List<FillVariant>{new FillVariant{Graphic=graphic,GraphicId=graphic.Id}});Call(w,"AddTerrain",image);typeof(MainWindow).GetField("brushIndex",P).SetValue(w,1);m.Background=new MapBackground{Source=graphic.Source,SourceId=graphic.SourceId};SaveMap(w,Path.Combine(root,"Proba053.hexmap"));
 }
}
[STAThread]static int Main(string[] args){try{if(args.Length!=1)throw new ArgumentException("Wymagany Case");var root=AppDomain.CurrentDomain.BaseDirectory;if(args[0]=="regions")Regions(root);else if(args[0]=="perf")Perf(root);else if(args[0]=="boundaries")Boundaries(root);else if(args[0]=="updates")Updates(root);else if(args[0]=="local")Local(root);else if(args[0]=="safety")Safety(root);else throw new ArgumentException("Case jeszcze nie wdrożony");Console.WriteLine("PASS "+args[0]);return 0;}catch(Exception ex){Console.WriteLine(ex);return 1;}}
}}
