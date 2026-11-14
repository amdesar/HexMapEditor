using System;using System.Linq;using System.Drawing;using System.Windows.Forms;
namespace HexMapEditor.Tests
{
 internal static class Patch090Tests
 {
  static void Check(bool yes,string message){if(!yes)throw new Exception(message);}
  static object Call(object target,string name,params object[] args){return target.GetType().GetMethods(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);}
  static void Presets()
  {
   string dir=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Presets");System.IO.Directory.CreateDirectory(dir);
   var asset=IconAssets.Starters()[0];var p=new TerrainPreset{Name="Trzy sekcje"};p.Terrains.Add(PresetCatalog.Neutral());p.Icons.Add(new PresetIcon{Id="i",Name="Ikona",Asset=asset,AssetId=asset.Id});p.Objects.Add(new PresetObject{Id="o",Name="Obiekt",Asset=asset,AssetId=asset.Id,LightRadius=2});
   PresetCatalog.Write(p,dir,false);string path=System.IO.Directory.GetFiles(dir,"*.json").Single();var read=PresetCatalog.ReadFile(path);Check(read.PresetFormatVersion==5&&read.Icons.Count==1&&read.Objects.Count==1&&Object.ReferenceEquals(read.Icons[0].Asset,read.Objects[0].Asset),"Preset round-trip / dedup");
   var raw=MapLogic.Serializer().DeserializeObject(System.IO.File.ReadAllText(path)) as System.Collections.Generic.Dictionary<string,object>;raw["PresetFormatVersion"]=4;raw.Remove("Icons");raw.Remove("Objects");raw.Remove("IconAssets");string old=System.IO.Path.Combine(dir,"old.json");System.IO.File.WriteAllText(old,MapLogic.Serializer().Serialize(raw));var oldPreset=PresetCatalog.ReadFile(old);Check(oldPreset.Icons.Count==0&&oldPreset.Objects.Count==0,"Old terrain-only format");System.IO.File.Delete(old);
   var working=p.Copy();working.Terrains[0].Name="Roboczy teren";working.Icons.Clear();var partial=PresetSets.Merge(working,read,PresetSections.Icons);Check(partial.Terrains[0].Name==read.Terrains[0].Name&&partial.Icons.Count==0&&partial.Objects.Count==1,"Partial overwrite");
   var fresh=PresetSets.Merge(working,null,PresetSections.Icons);Check(fresh.Terrains.Count==0&&fresh.Objects.Count==0,"Partial new");
   var map=MainWindow.CreateProjectData(15,10,"PointyTop","Preset map",p);string mapPath=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preset.hexmap");MainWindow.WriteProjectData(map,mapPath,false);var loaded=MainWindow.ReadProjectData(mapPath);Check(loaded.Presets[0].Icons.Count==1&&loaded.Presets[0].Objects.Count==1,"Map round-trip");
   var state=new SavedMapState();state.Capture(map);map.Presets[0].Terrains[0].Name="Draft";Check(!state.CheckAll(map),"Preset incorrectly dirties map");map.Cells[0].Name="Cell";Check(state.CheckAll(map),"Map dirty lost");
   using(var w=new MainWindow(MainWindow.CreateProjectData(15,10,"PointyTop","Empty",PresetCatalog.Default()),null)){
    object[] create={dir,"Nowy pusty",null};Check((bool)Call(w,"TryCreatePreset",create),"Actual new-preset path: "+create[2]);var active=(TerrainPreset)Call(w,"ActivePreset");Check(active.Terrains.Count==0&&active.Icons.Count==0&&active.Objects.Count==0,"New preset isn't empty");
    active.Terrains.Add(PresetCatalog.Neutral());active.Terrains[0].Name="Draft A";
    object[] save={dir,false,active.Name,PresetSections.Terrains,null};Check((bool)Call(w,"TrySaveWorkingPreset",save),"Partial save actual: "+save[4]);
    active=(TerrainPreset)Call(w,"ActivePreset");active.Terrains[0].Name="Not saved";object[] iconOnly={dir,false,active.Name,PresetSections.Icons,null};Check((bool)Call(w,"TrySaveWorkingPreset",iconOnly),"Icons-only save");Check((bool)Call(w,"WorkingPresetDirty",(TerrainPreset)Call(w,"ActivePreset")),"Partial save cleared terrain draft");
    var beforeCells=MapLogic.Serializer().Serialize(((MapData)w.GetType().GetField("map",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(w)).Cells);
    object[] restore={dir,PresetSections.Terrains,null};Check((bool)Call(w,"TryRestorePreset",restore),"Restore actual: "+restore[2]);Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name=="Draft A","Restored wrong snapshot");
    var wm=(MapData)w.GetType().GetField("map",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(w);Check(beforeCells==MapLogic.Serializer().Serialize(wm.Cells),"Restore changed painted cells");Call(w,"Undo");Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name=="Not saved","Restore Undo");Call(w,"Redo");Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name=="Draft A","Restore Redo");
    var first=(TerrainPreset)Call(w,"ActivePreset");first.Terrains[0].Name="Unsaved A";object[] createB={dir,"Preset B",null};Check((bool)Call(w,"TryCreatePreset",createB),"Create B");var second=(TerrainPreset)Call(w,"ActivePreset");second.Terrains.Add(PresetCatalog.Neutral());second.Terrains[0].Name="Unsaved B";
    var asked=new System.Collections.Generic.List<string>();w.ConfirmPresetForTest=x=>{asked.Add(x.Id);return DialogResult.Cancel;};Check(!(bool)Call(w,"ConfirmPresetsBeforeClose")&&asked.Count==1&&asked[0]==first.Id,"Cancel didn't stop close");
    asked.Clear();w.ConfirmPresetForTest=x=>{asked.Add(x.Id);return x.Id==first.Id?DialogResult.No:DialogResult.Cancel;};Check(!(bool)Call(w,"ConfirmPresetsBeforeClose")&&asked.Count==2&&wm.Presets.Single(x=>x.Id==first.Id).Terrains[0].Name=="Unsaved A","No lost draft");
    asked.Clear();w.ConfirmPresetForTest=x=>{asked.Add(x.Id);return DialogResult.Yes;};w.SavePresetAllForTest=x=>false;Check(!(bool)Call(w,"ConfirmPresetsBeforeClose")&&asked.Count==1,"Save failure didn't stop close");
    // A successful save of A remains even if B later cancels closing.
    asked.Clear();w.ConfirmPresetForTest=x=>{asked.Add(x.Id);return x.Id==first.Id?DialogResult.Yes:DialogResult.Cancel;};w.SavePresetAllForTest=x=>{object[] full={dir,false,x.Name,PresetSections.All,null};return (bool)Call(w,"TrySaveWorkingPreset",full);};Check(!(bool)Call(w,"ConfirmPresetsBeforeClose")&&asked.Count==2,"Later cancel sequence");Check(PresetCatalog.ReadFile(PresetCatalog.FindFile(dir,first.Id)).Terrains[0].Name=="Unsaved A","Earlier successful save rolled back");
    // Externally changed bytes must not be replaced by a stale session expectation.
    Call(w,"SelectPreset",first.Id);string file=PresetCatalog.FindFile(dir,first.Id);string original=System.IO.File.ReadAllText(file);System.IO.File.AppendAllText(file," ");object[] conflict={dir,false,first.Name,PresetSections.All,null};Check(!(bool)Call(w,"TrySaveWorkingPreset",conflict)&&System.IO.File.ReadAllText(file)==original+" ","Conflict overwritten");
    System.IO.File.WriteAllText(file,"broken");object[] badRestore={dir,PresetSections.All,null};string draft=PresetContentForTest((TerrainPreset)Call(w,"ActivePreset"));Check(!(bool)Call(w,"TryRestorePreset",badRestore)&&draft==PresetContentForTest((TerrainPreset)Call(w,"ActivePreset")),"Bad restore lost draft");
   }
   using(var w=new MainWindow(MainWindow.CreateProjectData(15,10,"PointyTop","Orphan draft",p),null)){
    var draft=(TerrainPreset)Call(w,"ActivePreset");string id=draft.Id;draft.Terrains[0].Name="Keep omitted draft";object[] saveCopy={System.IO.Path.Combine(dir,"partial-new"),true,"Partial copy",PresetSections.Icons,null};Check((bool)Call(w,"TrySaveWorkingPreset",saveCopy),"Partial first save: "+saveCopy[4]);
    var wm=(MapData)w.GetType().GetField("map",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(w);Check(wm.Presets.Single(x=>x.Id==id).Terrains[0].Name=="Keep omitted draft","Partial first save lost source draft");Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains.Count==0,"Partial new file included omitted terrains");
   }
   Console.WriteLine("presets PASS — serialization, sections, conflict-safe writer, actual new preset, map round-trip");
  }
  static string PresetContentForTest(TerrainPreset p){return MapLogic.Serializer().Serialize(p);}
  [STAThread]static int Main(string[] args){try{
   if(args.Length!=1||(args[0]!="geometry"&&args[0]!="quick"&&args[0]!="presets"&&args[0]!="controls"))throw new Exception("Wymagany Case: geometry / quick / presets / controls");
   if(args[0]=="controls"){
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);var data=MainWindow.CreateProjectData(15,10,"PointyTop","Próba 0.9.0",PresetCatalog.Default());MainWindow.WriteProjectData(data,System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Próba 0.9.0.hexmap"),false);
    using(var w=new MainWindow(data,null)){w.Show();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;Func<string,object> get=n=>w.GetType().GetField(n,flags).GetValue(w);var tools=(System.Collections.Generic.Dictionary<EditorTool,Button>)get("toolButtons");Check(tools.Values.Select(b=>b.Top).Distinct().Count()==3,"Tool rows");Check(tools.Values.Select(b=>b.Left).Distinct().Count()==4,"Tool columns");
     var active=(TerrainPreset)Call(w,"ActivePreset");string hidden=StarterSets.Icons()[19].Id;var initial=((TerrainPreset)Call(w,"WithVisibleSets",active)).Icons.Select(i=>i.Id).ToArray();Call(w,"SelectIcon",hidden);Check(((TerrainPreset)Call(w,"WithVisibleSets",active)).Icons.Select(i=>i.Id).SequenceEqual(initial),"Selection promoted hidden icon");Call(w,"SelectTool",EditorTool.Brush);Call(w,"BeginMaterialStroke",0,EditorTool.Brush);Call(w,"PaintMaterial",0);Call(w,"FinishStroke");Check(((TerrainPreset)Call(w,"WithVisibleSets",active)).Icons[0].Id==hidden,"Successful icon use did not promote");Check(((TerrainPreset)Call(w,"WithVisibleSets",active)).Icons.Count==12,"Preset included reserve");
     foreach(string name in new[]{"iconPreview","objectPreview"}){var panel=(FlowLayoutPanel)get(name);Check(panel.Controls.Count==12,"12 tiles "+name);Check(panel.Controls.Cast<Control>().Select(c=>c.Top).Distinct().Count()==2,"2 rows "+name);Check(panel.Controls.Cast<Control>().Select(c=>c.Left).Distinct().Count()==6,"6 columns "+name);}
     Call(w,"SetZoomPercent",12800f);var zoom=(Button)get("zoomButton");Check(zoom.Width-zoom.Padding.Horizontal-4>=TextRenderer.MeasureText("12800%",zoom.Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width,"Zoom inner width");
     var current=(MapData)get("map");var cellBefore=MapLogic.Serializer().Serialize(current.Cells);Call(w,"SelectTool",EditorTool.Brush);var picker=(ComboBox)get("shapePicker");picker.SelectedIndex=3;var rotation=(ScrubNumber)get("rotationPicker");rotation.BeginText();rotation.Text="400";rotation.CommitText();Check(rotation.Value==40,"Rotation integration");Call(w,"Undo");Check(rotation.Value==0,"Tool rotation Undo");Check(cellBefore==MapLogic.Serializer().Serialize(current.Cells),"Tool settings changed cells");
     w.Size=w.MinimumSize;Check(((Control)get("objectPreview")).Parent.Bottom<=((Control)get("leftSections")).Height,"Minimum controls clipped");Check(((Control)get("terrainScroll")).ClientSize.Height>=36,"Minimum terrain list inaccessible");
     Call(w,"SelectTool",EditorTool.Stylus);Check(((Control)get("inkSize")).Width==((Control)get("sizePicker")).Width,"Shared numeric width");
     var icon=((System.Collections.Generic.List<LibraryIcon>)get("libraryIcons")).First(i=>i.Id==initial[0]);var editedIcon=icon.Copy();editedIcon.Name="Edited quick icon";object[] editIcon={icon,editedIcon,false,System.IO.Path.GetDirectoryName(icon.FilePath),null};Check((bool)Call(w,"TryChangeLibraryIcon",editIcon),"Edit library icon: "+editIcon[4]);Check(((LibraryIcon)Call(w,"FindIcon",icon.Id)).Name==editedIcon.Name,"Quick icon kept stale library definition");Call(w,"Undo");Check(((LibraryIcon)Call(w,"FindIcon",icon.Id)).Name==icon.Name,"Quick icon library Undo");
     var obj=((System.Collections.Generic.List<ObjectTemplate>)get("libraryObjects")).First(i=>i.Id==StarterSets.Objects()[0].Id);var editedObj=obj.Copy();editedObj.Name="Edited quick object";object[] editObject={obj,editedObj,false,System.IO.Path.GetDirectoryName(obj.FilePath),null};Check((bool)Call(w,"TryChangeObjectTemplate",editObject),"Edit library object: "+editObject[4]);Check(((ObjectTemplate)Call(w,"FindTemplate",obj.Id)).Name==editedObj.Name,"Quick object kept stale library definition");Call(w,"Undo");Check(((ObjectTemplate)Call(w,"FindTemplate",obj.Id)).Name==obj.Name,"Quick object library Undo");
     w.Scale(new SizeF(1.25f,1.25f));Check(((Control)get("inkSize")).Width>=36,"Scaled tool field");Console.WriteLine("125% control-scaling simulation, not a physical DPI monitor test");
    }Console.WriteLine("controls PASS — layout/integration, not visual EXE");return 0;
   }
   if(args[0]=="presets"){Presets();return 0;}
   if(args[0]=="quick"){
    var q=new QuickAccess();foreach(int i in Enumerable.Range(0,30))q.Added(i.ToString());Check(q.All.Length==24&&q.Visible.Length==12,"Capacity");var visible=q.Visible;q.Used(visible[5]);Check(q.Visible.SequenceEqual(visible),"Visible moved");q.Used(q.All[20]);Check(q.Visible[0]=="9","Hidden not promoted");q.Added("9");Check(q.All.Count(x=>x=="9")==1,"Duplicate");string removed=q.Visible[0];q.Remove(removed);Check(q.Visible.Length==12&&!q.All.Contains(removed),"Reserve refill");q.Restore(new[]{"a","a","b"});Check(q.All.SequenceEqual(new[]{"a","b"}),"Restore reserve");Console.WriteLine("quick PASS — model only, UI integration pending");return 0;
   }
   foreach(string orientation in new[]{"PointyTop","FlatTop"}){
    var map=MainWindow.CreateProjectData(31,25,orientation,"Geometria",PresetCatalog.Default());
    foreach(BrushShape shape in new[]{BrushShape.Line,BrushShape.Circle,BrushShape.Triangle,BrushShape.Square,BrushShape.Rectangle,BrushShape.Pentagon,BrushShape.Hexagon,BrushShape.Octagon})foreach(int rotation in new[]{0,37,-90}){
     var fill=BrushGeometry.Stamp(map,new Point(15,12),shape,7,3,true,rotation,60);var outline=BrushGeometry.Stamp(map,new Point(15,12),shape,7,3,false,rotation,60);
     Check(fill.Count>0&&outline.All(fill.Contains),orientation+" fill/outline "+shape);Check(fill.Distinct().Count()==fill.Count,"Duplicates");
     var outside=BrushGeometry.Stamp(map,new Point(-1,0),shape,8,5,true,rotation,120);Check(outside.All(i=>i>=0&&i<map.Cells.Count),"Outside bounds");
    }
    Check(BrushGeometry.Stamp(map,new Point(-1,0),BrushShape.Circle,5,1,true,0,60).Count>0,"Outside center missing");
    var small=BrushGeometry.Stamp(map,new Point(15,12),BrushShape.Triangle,8,1,true,0,10);var wide=BrushGeometry.Stamp(map,new Point(15,12),BrushShape.Triangle,8,1,true,0,170);Check(!small.SequenceEqual(wide),"Apex ignored");
    Check(BrushGeometry.Stamp(map,new Point(-1000,-1000),BrushShape.Circle,100,1,true,0,60).Count==0,"Distant stamp");
   }
   string[] inputs={"361","-361","400","-390","720","-720","0","9999999999999999999999999999999999999999999999999999999999"};int[] expected={1,-1,40,-30,360,-360,0,279};
   for(int i=0;i<inputs.Length-1;i++)Check(BrushAngles.Parse(inputs[i],0)==expected[i],"Angle "+inputs[i]);Check(Math.Abs(BrushAngles.Parse(inputs.Last(),0))<=360,"Huge rotation");
   Check(BrushAngles.Parse("-",42)==42&&BrushAngles.Parse("1a5",42)==42,"Incomplete angle");
   using(var n=new ScrubNumber{Minimum=-360,Maximum=360,ValueNormalizer=BrushAngles.Wrap,TextNormalizer=BrushAngles.Parse,Suffix="°"}){n.Value=400;Check(n.Value==40,"Value wrap");n.Value=20;n.BeginScrub(0);n.MoveScrub(-8);n.EndScrub();Check(n.Value==19,"Reverse scrub");}
   Console.WriteLine("geometry PASS — methods only");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
