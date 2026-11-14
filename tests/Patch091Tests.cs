using System;using System.IO;using System.Linq;using System.Drawing;using System.Windows.Forms;using System.Reflection;
namespace HexMapEditor.Tests
{
 internal static class Patch091Tests
 {
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static object Get(object target,string field){return target.GetType().GetField(field,Flags).GetValue(target);}
  static object Call(object target,string name,params object[] args){return target.GetType().GetMethods(Flags).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);}
  static string Content(object value){return MapLogic.Serializer().Serialize(value);}
  static void Open(bool baseline)
  {
   LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory);var p=PresetCatalog.Default();p.Id=Guid.NewGuid().ToString("N");p.Name="Własny preset";p.Terrains[0].Name="Osadzone tereny";p.Icons[0].Name="Osadzona ikona";
   var map=MainWindow.CreateProjectData(15,10,"PointyTop","Kolejność zapisu",p);MapLogic.ApplyTerrain(map.Cells[0],p.Terrains[0]);map.Cells[0].IconId=p.Icons[0].AssetId;map.Cells[0].IconEntryId=p.Icons[0].Id;map.Cells[0].IconName=p.Icons[0].Name;map.Objects.Add(new MapObject{TemplateId=p.Objects[0].Id,Name="Osadzony obiekt",AssetId=p.Objects[0].AssetId,X=40,Y=40});var path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Kolejność.hexmap");MainWindow.WriteProjectData(map,path,false);
   var local=p.Copy();local.Terrains[0].Name="Aktualne tereny biblioteki";FillRules.SetPool(local.Terrains[0],new System.Collections.Generic.List<FillVariant>{new FillVariant{Color="#010203"}});local.Icons.Clear();local.Objects.Clear();PresetCatalog.Write(local,LibraryPaths.Presets,false);
   var read=MainWindow.ReadProjectData(path);string before=Content(new{read.Cells,read.Objects});
   using(var w=new MainWindow(read,path)){var active=(TerrainPreset)Call(w,"ActivePreset");Console.WriteLine("Plik mapy: "+p.Terrains[0].Name+"; biblioteka: "+local.Terrains[0].Name+"; panel: "+active.Terrains[0].Name+"; ikony panelu: "+active.Icons.Count);Check(before==Content(new{read.Cells,read.Objects}),"Panel changed map contents");if(!baseline){
    Check(active.Terrains[0].Name==local.Terrains[0].Name&&active.Icons.Count==0,"Panel ignored current local preset");active.Terrains[0].Name="Session draft";Call(w,"SelectPreset",PresetCatalog.DefaultId);Call(w,"SelectPreset",p.Id);Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name=="Session draft","Selection overwrote draft");
    local.Terrains[0].Name="Library after editor opened";PresetCatalog.Write(local,LibraryPaths.Presets,true);object[] load={path,null,false};Check((bool)Call(w,"TryLoadProject",load),"Shared Open/MRU read: "+load[1]);Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name==local.Terrains[0].Name,"Open didn't refresh library");Check(!(bool)Get(w,"dirty"),"Panel adoption dirtied map");var adopted=(MapData)Get(w,"map");Check(before==Content(new{adopted.Cells,adopted.Objects}),"Open repainted embedded appearances");var round=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"After-panel.hexmap");MainWindow.WriteProjectData(adopted,round,false);var roundRead=MainWindow.ReadProjectData(round);Check(before==Content(new{roundRead.Cells,roundRead.Objects})&&roundRead.IconAssets.Any(x=>x.Id==p.Objects[0].AssetId)&&roundRead.IconAssets.Any(x=>x.Id==p.Icons[0].AssetId),"Panel adoption lost embedded sources on round-trip");
    MainWindow.WriteProjectData(map,path,true);object[] second={path,null,false};Check((bool)Call(w,"TryLoadProject",second),"Reverse save order");Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name==local.Terrains[0].Name,"Map save overwrote panel rule");
   }}
   if(!baseline){string file=PresetCatalog.FindFile(LibraryPaths.Presets,p.Id),backup=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"saved-preset.json");File.Move(file,backup);
    using(var fallback=new MainWindow(MainWindow.ReadProjectData(path),path)){Check(((TerrainPreset)Call(fallback,"ActivePreset")).Terrains[0].Name==p.Terrains[0].Name,"Missing local file lost fallback");Check(!File.Exists(file),"Fallback imported a library file");
     File.WriteAllText(file,"broken");string warning;var library=PresetCatalog.Load(LibraryPaths.Presets,out warning);Check(warning.Length>0&&!library.Any(x=>x.Id==p.Id),"Corrupt file wasn't reported");var list=(System.Collections.Generic.List<TerrainPreset>)Get(fallback,"libraryPresets");list.Clear();list.AddRange(library);Call(fallback,"AdoptLibraryPanel",(MapData)Get(fallback,"map"));Check(((TerrainPreset)Call(fallback,"ActivePreset")).Terrains[0].Name==p.Terrains[0].Name,"Corrupt file cleared fallback");
    }
   }
   Console.WriteLine(baseline?"baseline recorded — method reproduction, not ordinary EXE":"open PASS");
  }
  static void Controls()
  {
   using(var w=new MainWindow(MainWindow.CreateProjectData(15,10,"PointyTop","Layout",PresetCatalog.Default()),null)){
    w.Show();var left=(Panel)Get(w,"left");var sections=(TableLayoutPanel)Get(w,"leftSections");var icons=(FlowLayoutPanel)Get(w,"iconPreview");var objects=(FlowLayoutPanel)Get(w,"objectPreview");
    using(var g=w.CreateGraphics())Console.WriteLine("Screen="+Screen.PrimaryScreen.Bounds+" DPI="+g.DpiX+"; minimum="+w.MinimumSize+"; column="+left.Width);
    Check(left.Width==276&&!left.AutoScroll,"Fixed compact panel / no outer scroll");var shape=(ComboBox)Get(w,"shapePicker");Call(w,"SelectTool",EditorTool.Brush);shape.SelectedIndex=3;w.Size=w.MinimumSize;
    Check(sections.Height==left.ClientSize.Height,"Column exceeds available height");Check(((Panel)Get(w,"terrainScroll")).ClientSize.Height>=36,"One full terrain missing at maximum layout minimum");Check(objects.PointToScreen(Point.Empty).Y+objects.Height<=left.PointToScreen(Point.Empty).Y+left.Height,"Objects clipped");
    Check(w.MinimumSize.Height<=1080-SystemInformation.CaptionHeight-SystemInformation.FrameBorderSize.Height,"Minimum doesn't fit FullHD100 target");
    foreach(string field in new[]{"sizePicker","apexPicker","rotationPicker"}){var n=(ScrubNumber)Get(w,field);Check(n.Width==((ScrubNumber)Get(w,"inkSize")).Width,"Different field widths");Check(n.Right<=n.Parent.ClientSize.Width,"Parameter clipped");}
    var rot=(ScrubNumber)Get(w,"rotationPicker");Check(rot.Width-26>=TextRenderer.MeasureText("-360",rot.Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width,"Rotation text area too small");
    var actions=(FlowLayoutPanel)Get(w,"presetActions");Check(actions.Controls.Count==5,"Preset action count");Check(actions.Controls[4].Right==icons.Controls[5].Right,"Function row and six tiles differ");Check(icons.Controls[0].Left==left.Width-12-icons.Controls[5].Right,"Quick margins differ");
    for(int height=w.MinimumSize.Height;height<=w.MinimumSize.Height+120;height+=20){w.Height=height;Check(!left.VerticalScroll.Visible&&sections.Height==left.ClientSize.Height,"Scrollbar/layout oscillation");}
   }
   using(var name=new NameDialog("Nazwa","Próba")){name.Show();var input=(TextBox)Get(name,"name");Check(Math.Abs(input.Top-(input.Parent.Height-input.Height)/2)<=1,"Text not vertically centered");name.Close();}
   using(var d=new FillVariantDialog(new FillVariant(),false)){d.Show();var solid=(RadioButton)Get(d,"solid");var gradient=(RadioButton)Get(d,"gradient");var graphic=(RadioButton)Get(d,"graphic");Call(solid,"OnClick",EventArgs.Empty);Check(solid.Checked&&!gradient.Checked&&!graphic.Checked,"Radio active click clears all");Call(graphic,"OnClick",EventArgs.Empty);Check(graphic.Checked&&!solid.Checked&&!gradient.Checked,"Exclusive kind");Check(!((Button)Get(d,"crop")).Enabled,"Crop enabled without source");d.Close();}
   using(var button=new EditorButton{Text="Kadruj",Enabled=false,Size=new Size(100,30)})using(var image=new Bitmap(100,30)){button.DrawToBitmap(image,new Rectangle(0,0,100,30));Check(Enumerable.Range(10,80).Any(x=>Enumerable.Range(6,18).Any(y=>image.GetPixel(x,y).R>120)),"Disabled text unreadable");}
   Console.WriteLine("controls PASS — control integration, not visual final EXE");
  }
  static void Quick()
  {
   using(var w=new MainWindow(MainWindow.CreateProjectData(15,10,"PointyTop","Quick",PresetCatalog.Default()),null)){
    w.Show();var p=(TerrainPreset)Call(w,"ActivePreset");var full=(QuickSnapshot)Call(w,"CaptureQuick");var icons=(FlowLayoutPanel)Get(w,"iconPreview");var objects=(FlowLayoutPanel)Get(w,"objectPreview");var sections=(TableLayoutPanel)Get(w,"leftSections");
    foreach(int count in new[]{0,1,6,7,12}){var s=new QuickSnapshot{Icons=full.Icons.Take(count).ToArray(),Objects=full.Objects.Take(count).ToArray(),IconRefs=full.IconRefs,ObjectRefs=full.ObjectRefs};Call(w,"ApplyQuick",p.Id,s);Check(icons.Controls.Count==count&&objects.Controls.Count==count,"Placeholders present");Check(sections.RowStyles[3].Height==56+((count+5)/6)*32,"Wrong section height");}
    var before=icons.Controls.Cast<Control>().ToArray();int removed=0,added=0;icons.ControlRemoved+=(s,e)=>removed++;icons.ControlAdded+=(s,e)=>added++;
    Call(w,"RefreshIconPreview");Check(before.SequenceEqual(icons.Controls.Cast<Control>())&&removed==0&&added==0,"Unchanged list rebuilt");object[] restore={LibraryPaths.Presets,PresetSections.Terrains,null};Check((bool)Call(w,"TryRestorePreset",restore),"Terrain restore failed");Check(before.SequenceEqual(icons.Controls.Cast<Control>())&&removed==0&&added==0,"Terrain-only restore rebuilt quick");
    string id=full.Icons[4];Call(w,"RemoveQuickEntry",id,false,false);Check(removed==1&&added==0&&icons.Controls.Count==11,"Removal rebuilt unaffected tiles");Check(!((TerrainPreset)Call(w,"ActivePreset")).Icons.All(i=>i.Id!=id),"Quick removal edited preset");
    Console.WriteLine("Quick delta: one removal="+removed+", creations="+added+"; terrain-only restore=0/0");
    var refs=full.IconRefs.ToDictionary(x=>x.Key,x=>x.Value.Copy());using(var source=new Bitmap(16,32)){using(var g=Graphics.FromImage(source))g.Clear(Color.Red);refs[full.Icons[0]].Asset=IconAssets.FromBitmap(source,"Tall source");}var oldButton=icons.Controls[0];Call(w,"ApplyQuick",p.Id,new QuickSnapshot{Icons=full.Icons,Objects=full.Objects,IconRefs=refs,ObjectRefs=full.ObjectRefs});var thumb=(Bitmap)((Button)icons.Controls[0]).Image;Check(Object.ReferenceEquals(oldButton,icons.Controls[0])&&thumb.GetPixel(1,12).A==0&&thumb.GetPixel(12,12).R>200,"Updated thumbnail stretched aspect/rebuilt button");
   }Console.WriteLine("quick PASS");
  }
  static void Crop()
  {
   string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Źródło alpha.png");using(var source=new Bitmap(640,320)){using(var g=Graphics.FromImage(source)){g.Clear(Color.Transparent);using(var b=new SolidBrush(Color.FromArgb(150,240,60,30)))g.FillRectangle(b,80,30,480,260);}source.Save(path,System.Drawing.Imaging.ImageFormat.Png);}
   byte[] original=File.ReadAllBytes(path);using(var source=IconAssets.ReadImport(path)){var state=new SquareCropState(source.Size);Check(state.Transform.Zoom==.5,"Initial contain");using(var image=state.Render(source)){Check(image.Size==new Size(512,512),"Square result");Check(image.GetPixel(256,30).A==0&&Math.Abs(image.GetPixel(256,256).A-150)<=1,"Alpha/contain lost");}
    state.Pan(75,32,source.Size,560);state.Zoom(1.7);state.Transform.Angle=37;using(var a=state.Render(source))using(var b=state.Render(source)){Check(IconAssets.FromBitmap(a,"x").Id==IconAssets.FromBitmap(b,"x").Id,"Nondeterministic crop");}
   }
   using(var dialog=new SquareImportDialog(path)){dialog.Show();Check(((Control)dialog.Controls.OfType<SquareCropSurface>().Single()).Width>512&&dialog.Controls.OfType<SquareCropSurface>().Single().Frame.Width>512,"Preview not larger than stored square");((Button)dialog.CancelButton).PerformClick();Check(dialog.Result==null,"Cancel created asset");}
   Check(original.SequenceEqual(File.ReadAllBytes(path)),"Import changed source file");Console.WriteLine("crop PASS — geometry/render/Cancel, not actual import clicks");
  }
  static void ClosePartial()
  {
   LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory);var p=PresetCatalog.Default();p.Id=Guid.NewGuid().ToString("N");p.Name="A";PresetCatalog.Write(p,LibraryPaths.Presets,false);
   using(var d=new SavePresetDialog(p,true,true)){var switches=new[]{"terrains","icons","objects"}.Select(n=>(SwitchBox)Get(d,n)).ToArray();Check(switches.All(s=>s.Enabled&&s.Checked),"Closing section switches disabled");foreach(var s in switches)s.Checked=false;Check(d.Controls[0].Controls.OfType<FlowLayoutPanel>().Any(panel=>panel.Controls.OfType<Button>().Any(b=>b.Text=="Zapisz"&&!b.Enabled)),"All Off allows save");}
   using(var w=new MainWindow(MainWindow.CreateProjectData(15,10,"PointyTop","Closing",p),null)){
    var wm=(MapData)Get(w,"map");var a=(TerrainPreset)Call(w,"ActivePreset");a.Terrains[0].Name="Omitted draft A";object[] create={LibraryPaths.Presets,"B",null};Check((bool)Call(w,"TryCreatePreset",create),"Create B");var b=(TerrainPreset)Call(w,"ActivePreset");b.Terrains.Add(PresetCatalog.Neutral());b.Terrains[0].Name="Draft B";
    var asked=new System.Collections.Generic.List<string>();w.ConfirmPresetForTest=x=>{asked.Add(x.Id);return x.Id==a.Id?DialogResult.Yes:DialogResult.Cancel;};w.SavePresetAllForTest=x=>{object[] save={LibraryPaths.Presets,false,x.Name,PresetSections.Icons,null};return (bool)Call(w,"TrySaveWorkingPreset",save);};Check(!(bool)Call(w,"ConfirmPresetsBeforeClose")&&asked.SequenceEqual(new[]{a.Id,b.Id}),"Partial close repeated A or lost cancel");Check((bool)Call(w,"WorkingPresetDirty",wm.Presets.Single(x=>x.Id==a.Id)),"Omitted section falsely clean");Check(wm.Presets.Single(x=>x.Id==a.Id).Terrains[0].Name=="Omitted draft A","Later cancel lost omitted draft");Check(PresetCatalog.ReadFile(PresetCatalog.FindFile(LibraryPaths.Presets,a.Id)).Terrains[0].Name==p.Terrains[0].Name,"Partial overwrite changed omitted section");
    asked.Clear();w.ConfirmPresetForTest=x=>{asked.Add(x.Id);return DialogResult.Cancel;};Check(!(bool)Call(w,"ConfirmPresetsBeforeClose")&&asked.Count==1&&asked[0]==a.Id,"Next close didn't reconsider omitted section");
    asked.Clear();w.ConfirmPresetForTest=x=>{asked.Add(x.Id);return DialogResult.Yes;};w.SavePresetAllForTest=x=>false;Check(!(bool)Call(w,"ConfirmPresetsBeforeClose")&&asked.Count==1,"Failed/cancelled save asked next preset");
   }
   Console.WriteLine("close PASS — active switches, partial persistence, omission/Cancel/session sequence");
  }
  static void LibraryDelete()
  {
   LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory);var p=PresetCatalog.Default();p.Id=Guid.NewGuid().ToString("N");p.Name="Saved copy";PresetCatalog.Write(p,LibraryPaths.Presets,false);var map=MainWindow.CreateProjectData(15,10,"PointyTop","Delete fixture",p);map.Cells[0].IconId=p.Icons[0].AssetId;map.Cells[0].IconEntryId=p.Icons[0].Id;map.IconAssets.Add(p.Icons[0].Asset);
   using(var w=new MainWindow(map,null)){
    var first=(TerrainPreset)Call(w,"ActivePreset");Call(w,"CaptureQuick");var b=first.Copy();b.Id=Guid.NewGuid().ToString("N");b.Name="Other draft";map.Presets.Add(b);Call(w,"SelectPreset",b.Id);Call(w,"CaptureQuick");Call(w,"SelectPreset",first.Id);
    string before=Content(map.Presets),cell=Content(map.Cells);var icon=((System.Collections.Generic.List<LibraryIcon>)Get(w,"libraryIcons")).Single(i=>i.Id==first.Icons[0].Id);byte[] bytes=File.ReadAllBytes(icon.FilePath);File.AppendAllText(icon.FilePath," ");object[] fail={icon,null,false,Path.GetDirectoryName(icon.FilePath),null};Check(!(bool)Call(w,"TryChangeLibraryIcon",fail),"External conflict accepted");Check(((QuickSnapshot)Call(w,"CaptureQuick")).Icons.Contains(icon.Id),"Failed delete cleared quick");File.WriteAllBytes(icon.FilePath,bytes);
    object[] remove={icon,null,false,Path.GetDirectoryName(icon.FilePath),null};Check((bool)Call(w,"TryChangeLibraryIcon",remove),"Delete icon: "+remove[4]);Check(!((QuickSnapshot)Call(w,"CaptureQuick")).Icons.Contains(icon.Id),"Active list kept deleted icon");map.ActivePresetId=b.Id;Call(w,"RefreshPresetPicker");Check(!((QuickSnapshot)Call(w,"CaptureQuick")).Icons.Contains(icon.Id),"Other draft kept deleted icon");Check(before==Content(map.Presets)&&cell==Content(map.Cells),"Library delete changed preset/map data");Call(w,"Undo");Check(((QuickSnapshot)Call(w,"CaptureQuick")).Icons.Contains(icon.Id)&&File.Exists(icon.FilePath),"Delete Undo lost other draft list/file");map.ActivePresetId=first.Id;Call(w,"RefreshPresetPicker");Check(((QuickSnapshot)Call(w,"CaptureQuick")).Icons.Contains(icon.Id),"Delete Undo lost active list");Call(w,"Redo");Check(!File.Exists(icon.FilePath),"Delete Redo did not remove file");Call(w,"SelectPreset",first.Id);object[] restore={LibraryPaths.Presets,PresetSections.Icons,null};Check((bool)Call(w,"TryRestorePreset",restore),"Restore preset copy");Check(((QuickSnapshot)Call(w,"CaptureQuick")).Icons.Contains(icon.Id)&&!File.Exists(icon.FilePath),"Restoring copy recreated library file");
    var template=((System.Collections.Generic.List<ObjectTemplate>)Get(w,"libraryObjects")).Single(o=>o.Id==first.Objects[0].Id);Call(w,"SelectObjectTemplate",template.Id);Call(w,"PlaceObject",new PointF(20,20));string objects=Content(map.Objects);object[] removeObject={template,null,false,Path.GetDirectoryName(template.FilePath),null};Check((bool)Call(w,"TryChangeObjectTemplate",removeObject),"Delete object: "+removeObject[4]);Check(!((QuickSnapshot)Call(w,"CaptureQuick")).Objects.Contains(template.Id)&&objects==Content(map.Objects),"Object delete damaged instance/list");Call(w,"Undo");Check(((QuickSnapshot)Call(w,"CaptureQuick")).Objects.Contains(template.Id)&&File.Exists(template.FilePath),"Object delete Undo lost list/file");
    using(var choice=new QuickRemovalDialog(false)){Check(!((RadioButton)Get(choice,"library")).Enabled,"Embedded copy allowed missing library delete");Check(!choice.FromLibrary,"Unexpected destructive default");}
   }Console.WriteLine("library-delete PASS — list cleanup only after checked file transition, embedded copies preserved");
  }
  static void Fixture()
  {
   LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory);var p=PresetCatalog.Default();p.Id=Guid.NewGuid().ToString("N");p.Name="Próba preset";p.Icons=p.Icons.Take(7).ToList();p.Objects=p.Objects.Take(7).ToList();p.Terrains[0].Name="Osadzony stary teren";
   var map=MainWindow.CreateProjectData(15,10,"PointyTop","Próba 0.9.1",p);string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Próba 0.9.1.hexmap");MainWindow.WriteProjectData(map,path,false);p.Terrains[0].Name="Biblioteczny aktualny teren";PresetCatalog.Write(p,LibraryPaths.Presets,false);RecentProjects.Shared.Touch(path);
   using(var source=new Bitmap(640,320)){using(var g=Graphics.FromImage(source)){g.Clear(Color.Transparent);using(var b=new SolidBrush(Color.FromArgb(160,230,70,35)))g.FillEllipse(b,80,20,480,280);g.DrawRectangle(Pens.White,180,70,280,180);}source.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Źródło alpha.png"),System.Drawing.Imaging.ImageFormat.Png);}
   Console.WriteLine("fixture prepared: "+path+" — not a test PASS");
  }
  [STAThread]static int Main(string[] args){try{if(args.Length!=1)throw new Exception("Case required");Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args[0]=="baseline"||args[0]=="open")Open(args[0]=="baseline");else if(args[0]=="controls")Controls();else if(args[0]=="quick"){Quick();LibraryDelete();}else if(args[0]=="crop")Crop();else if(args[0]=="close")ClosePartial();else if(args[0]=="fixture")Fixture();else throw new Exception("Case not implemented yet");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
