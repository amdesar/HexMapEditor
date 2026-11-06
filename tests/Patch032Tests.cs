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
    internal static class Patch032Tests
    {
        const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance; static string root;
        static object Get(MainWindow w,string n){return typeof(MainWindow).GetField(n,F).GetValue(w);}
        static void Set(MainWindow w,string n,object v){typeof(MainWindow).GetField(n,F).SetValue(w,v);}
        static object Call(MainWindow w,string n,params object[] args){return typeof(MainWindow).GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(w,args);}
        static MapData Map(MainWindow w){return (MapData)Get(w,"map");}
        static int Count(MainWindow w,string n){return ((ICollection)Get(w,n)).Count;}
        static void Check(bool b,string text){if(!b)throw new Exception(text);}
        static void Click(Button b){typeof(Button).GetMethod("OnClick",F).Invoke(b,new object[]{EventArgs.Empty});}
        static void History(MainWindow w,bool redo){object[] a={redo,null};Check((bool)Call(w,"TryHistory",a),"History: "+a[1]);}
        static void SavePreset(MainWindow w,string d,bool asNew,string name){object[] a={d,asNew,name,null};Check((bool)Call(w,"TrySaveWorkingPreset",a),"Save preset: "+a[3]);}
        static void Controls()
        {
            using(var w=new MainWindow()){
                var own=PresetCatalog.Default();own.Id="own";for(int i=0;i<15;i++)own.Terrains.Add(new TerrainDefinition());Map(w).Presets.Add(own);Map(w).ActivePresetId=own.Id;Call(w,"RefreshPresetPicker");w.Size=w.MinimumSize;w.Show();Application.DoEvents();
                var up=(Button)Get(w,"terrainUp");var down=(Button)Get(w,"terrainDown");var row=((Control)Get(w,"paletteTiles")).Controls.Cast<TerrainTile>().First();int step=row.Height+row.Margin.Vertical;int y=((Control)Get(w,"iconPreview")).PointToScreen(Point.Empty).Y;
                down.PerformClick();Check((int)Get(w,"terrainOffset")==step,"Down is not one row");up.PerformClick();Check((int)Get(w,"terrainOffset")==0,"Up is not one row");Call(w,"ScrollTerrains",100000);int max=(int)Get(w,"terrainOffset");up.PerformClick();Check((int)Get(w,"terrainOffset")==Math.Max(0,max-step),"Up from end wrong");down.PerformClick();Check((int)Get(w,"terrainOffset")==max&&!down.Enabled&&((Control)Get(w,"iconPreview")).PointToScreen(Point.Empty).Y==y,"End clamp/sections changed");
                Check(up.Size==down.Size&&up.Margin==down.Margin&&up.Padding==down.Padding&&up.ImageAlign==down.ImageAlign,"Arrow layouts differ");var a=(Bitmap)up.Image;var b=(Bitmap)down.Image;for(int x=0;x<a.Width;x++)for(int j=0;j<a.Height;j++)Check(a.GetPixel(x,j)==b.GetPixel(x,a.Height-1-j),"Arrows are not exact mirrored geometry");
                var entry=new LibraryIcon {Name="Icon A",Asset=IconAssets.Starters()[0]};var other=new LibraryIcon {Name="Icon B",Asset=IconAssets.Starters()[1]};((List<LibraryIcon>)Get(w,"libraryIcons")).Add(entry);((List<LibraryIcon>)Get(w,"libraryIcons")).Add(other);
                Set(w,"brushShape",BrushShape.Rectangle);Set(w,"brushSize",4);Set(w,"brushHeight",3);Set(w,"brushFilled",false);string state=MapLogic.Serializer().Serialize(Map(w));int undo=Count(w,"undo");bool dirty=(bool)Get(w,"dirty");
                foreach(var tool in new[]{EditorTool.FogBrush,EditorTool.FogBucket,EditorTool.Brush,EditorTool.Bucket,EditorTool.Select,EditorTool.Hand}){
                    Set(w,"currentTool",tool);Call(w,"SelectTerrain",own.Terrains[0].TerrainId);var expected=tool==EditorTool.FogBrush||tool==EditorTool.FogBucket?EditorTool.Brush:tool;Check((EditorTool)Get(w,"currentTool")==expected,"Terrain switches wrong tool");Set(w,"currentTool",tool);Call(w,"SelectIcon",entry.Id);Check((EditorTool)Get(w,"currentTool")==expected,"Icon switches wrong tool");
                }
                Set(w,"currentTool",EditorTool.FogBucket);Call(w,"RefreshPresetPicker");Call(w,"RefreshIconPreview");Check((EditorTool)Get(w,"currentTool")==EditorTool.FogBucket,"Refresh switched fog");Set(w,"updating",true);Call(w,"SelectIcon",entry.Id);Check((EditorTool)Get(w,"currentTool")==EditorTool.FogBucket,"Sync switched tool");Set(w,"updating",false);
                string active=(string)Get(w,"activeIcon");var current=(EditorTool)Get(w,"currentTool");LibraryIcon chosen=null;using(var edit=new IconEditDialog(entry,owner=>chosen)){
                    var choose=edit.Controls.Find("unused",true);var button=All(edit).OfType<Button>().Single(v=>v.Text=="Wybierz z biblioteki");Click(button);Check(edit.Asset==entry.Asset,"Cancel changed preview");chosen=other;Click(button);Check(edit.Asset==other.Asset&&!edit.UpdateMap,"Visual choice/checkbox");edit.DialogResult=DialogResult.Cancel;
                }
                Check((string)Get(w,"activeIcon")==active&&(EditorTool)Get(w,"currentTool")==current&&MapLogic.Serializer().Serialize(Map(w))==state&&Count(w,"undo")==undo&&(bool)Get(w,"dirty")==dirty,"Selection/edit cancellation changed main state");
                Check((int)Get(w,"brushSize")==4&&(int)Get(w,"brushHeight")==3&&!(bool)Get(w,"brushFilled")&&(BrushShape)Get(w,"brushShape")==BrushShape.Rectangle,"Selection reset brush settings");
                using(var picker=new IconPickerDialog(new[]{entry,other},(i,choose)=>{var button=new Button();button.Click+=(s,e)=>choose();return button;},Color.Gray)){var target=All(picker).OfType<Button>().Single(v=>v.Text==other.Name);Click(target);Check(picker.Chosen==other&&picker.DialogResult==DialogResult.OK,"Shared picker did not return entry");}
                Call(w,"CaptureSavedState");w.Close();
            }
        }
        static IEnumerable<Control> All(Control parent){foreach(Control c in parent.Controls){yield return c;foreach(var child in All(c))yield return child;}}
        static void Library()
        {
            string dir=Path.Combine(root,"fresh-icons");string warning;var icons=IconLibrary.Load(dir,out warning);var names=Directory.GetFiles(dir,"*.json").Select(Path.GetFileName).OrderBy(n=>n).ToArray();
            Check(warning==""&&icons.Count==5&&new[]{"Czaszka","Gwiazda","Krzyż","Namiot","Wieża"}.All(n=>names.Contains("icon-"+n+".json")),"Starter filenames wrong");
            string marker=Path.Combine(dir,".starter-icons-v1");byte[] bytes=File.ReadAllBytes(marker);File.Delete(icons[0].FilePath);icons=IconLibrary.Load(dir,out warning);Check(icons.Count==4&&File.ReadAllBytes(marker).SequenceEqual(bytes),"Deleted starter recreated/marker reset");
            using(var w=new MainWindow()){
                var entry=new LibraryIcon {Name="Shared engine",Asset=IconAssets.Starters()[0]};object[] add={null,entry,false,Path.Combine(root,"icon-history"),null};Check((bool)Call(w,"TryChangeLibraryIcon",add),"Shared file transition for icon failed");var saved=((List<LibraryIcon>)Get(w,"libraryIcons")).Single();History(w,false);Check(!File.Exists(saved.FilePath),"Icon Undo after extraction");History(w,true);Check(File.ReadAllBytes(saved.FilePath).SequenceEqual(saved.DiskBytes),"Icon Redo bytes");
            }
        }
        static void Presets()
        {
            string dir=Path.Combine(root,"presets");using(var w=new MainWindow()){
                string original=MapLogic.Serializer().Serialize(Map(w));SavePreset(w,dir,true,"Własny");string id=Map(w).ActivePresetId;var stored=((List<TerrainPreset>)Get(w,"libraryPresets")).Single(p=>p.Id==id);string path=stored.FilePath;byte[] first=File.ReadAllBytes(path);Check(Count(w,"undo")==1,"Copy not one step");History(w,false);Check(!File.Exists(path)&&MapLogic.Serializer().Serialize(Map(w))==original,"Copy Undo did not remove file/restore project");History(w,true);Check(File.ReadAllBytes(path).SequenceEqual(first)&&Map(w).ActivePresetId==id,"Copy Redo changed bytes/id");
                Call(w,"AddTerrain",new TerrainDefinition {Name="Roboczy"});string project=Path.Combine(root,"saved.hexmap");object[] save={project,false,null};Check((bool)Call(w,"TrySaveTo",save),"Save map fixture");string working=MapLogic.Serializer().Serialize(Map(w));int steps=Count(w,"undo");SavePreset(w,dir,false,null);byte[] overwritten=File.ReadAllBytes(path);Check(!first.SequenceEqual(overwritten)&&!(bool)Get(w,"dirty")&&Count(w,"undo")==steps+1,"Overwrite marks map dirty or misses step");History(w,false);Check(File.ReadAllBytes(path).SequenceEqual(first)&&MapLogic.Serializer().Serialize(Map(w))==working&&!(bool)Get(w,"dirty"),"Overwrite Undo changed working palette/project");History(w,true);
                Call(w,"ChangePalette","Rename",new Action(()=>Map(w).Presets.Single(p=>p.Id==id).Name="Nowa nazwa"),null);Check(File.Exists(path)&&File.ReadAllBytes(path).SequenceEqual(overwritten),"Working rename touched disk");History(w,false);Check(File.Exists(path),"Working rename Undo touched disk");History(w,true);SavePreset(w,dir,false,null);string renamed=PresetCatalog.FindFile(dir,id);byte[] named=File.ReadAllBytes(renamed);Check(renamed!=path&&!File.Exists(path),"Saved rename duplicated source");History(w,false);Check(File.Exists(path)&&!File.Exists(renamed)&&Map(w).Presets.Single(p=>p.Id==id).Name=="Nowa nazwa","Saved rename Undo changed working name");History(w,true);Check(File.ReadAllBytes(renamed).SequenceEqual(named),"Rename Redo bytes");
                Map(w).Cells[0].Name="Keep name";Map(w).Cells[0].Description="Keep text";Map(w).Cells[0].Fog=true;string before=MapLogic.Serializer().Serialize(Map(w));object[] delete={dir,null};steps=Count(w,"undo");Check((bool)Call(w,"TryDeletePreset",delete)&&!File.Exists(renamed)&&Count(w,"undo")==steps+1,"Delete failed");History(w,false);Check(File.ReadAllBytes(renamed).SequenceEqual(named)&&MapLogic.Serializer().Serialize(Map(w))==before&&((List<TerrainPreset>)Get(w,"libraryPresets")).Any(p=>p.Id==id)&&!((Label)Get(w,"presetStateLabel")).Text.Contains("niezapisany"),"Delete Undo is not saved/restored state");History(w,true);
                File.WriteAllText(renamed,"FOREIGN");string deleted=MapLogic.Serializer().Serialize(Map(w));steps=Count(w,"undo");object[] undo={false,null};Check(!(bool)Call(w,"TryHistory",undo)&&Count(w,"undo")==steps&&File.ReadAllText(renamed)=="FOREIGN"&&MapLogic.Serializer().Serialize(Map(w))==deleted,"Conflict overwritten or advanced history");File.Delete(renamed);History(w,false);Check(File.ReadAllBytes(renamed).SequenceEqual(named),"Retry restoration failed");
                File.WriteAllText(renamed,"EXTERNAL");object[] overwrite={dir,false,null,null};steps=Count(w,"undo");Check(!(bool)Call(w,"TrySaveWorkingPreset",overwrite)&&Count(w,"undo")==steps&&File.ReadAllText(renamed)=="EXTERNAL"&&MapLogic.Serializer().Serialize(Map(w))==before,"External change overwritten");File.WriteAllBytes(renamed,named);
                using(var locked=new FileStream(renamed,FileMode.Open,FileAccess.Read,FileShare.Read)){delete=new object[]{dir,null};Check(!(bool)Call(w,"TryDeletePreset",delete)&&Count(w,"undo")==steps&&MapLogic.Serializer().Serialize(Map(w))==before,"Write failure changed memory/history");}
                object[] create={dir,"Neutralny nowy",null};Check((bool)Call(w,"TryCreatePreset",create),"Create preset");string neutral=Map(w).ActivePresetId;string neutralPath=PresetCatalog.FindFile(dir,neutral);Check(Map(w).Presets.Single(p=>p.Id==neutral).Terrains.Count==1,"New not neutral");History(w,false);Check(!File.Exists(neutralPath)&&Map(w).ActivePresetId==id,"Create Undo");History(w,true);Check(File.Exists(neutralPath),"Create Redo");
                string bad=Path.Combine(root,"bad.hexmap");File.WriteAllText(bad,"broken");object[] open={bad,null,false};steps=Count(w,"undo");Check(!(bool)Call(w,"TryLoadProject",open)&&Count(w,"undo")==steps,"Failed open reset history");open=new object[]{project,null,false};Check((bool)Call(w,"TryLoadProject",open)&&Count(w,"undo")==0&&Count(w,"redo")==0&&File.Exists(neutralPath)&&File.Exists(renamed),"Open reset removed active files");
                create=new object[]{dir,"Późniejszy",null};Check((bool)Call(w,"TryCreatePreset",create),"Second create");Call(w,"StartNewProject",3,2,"PointyTop","Fresh",PresetCatalog.Default());Check(Count(w,"undo")==0&&File.Exists(neutralPath)&&File.Exists(renamed),"New deleted active presets");object[] protectedDelete={dir,null};Check(!(bool)Call(w,"TryDeletePreset",protectedDelete),"Default deletion allowed");
            }
        }
        static void Fixture(string dir)
        {
            Directory.CreateDirectory(dir);var preset=new TerrainPreset {Name="Próba usunięcia 0.3.2"};for(int i=0;i<18;i++)preset.Terrains.Add(new TerrainDefinition {Name="Teren "+(i+1).ToString("00"),Color=i%2==0?"#287DB3":"#72A65B"});PresetCatalog.Write(preset,Path.Combine(dir,"Library","Terrains","Presets"),false);
            var map=new MapData {Columns=8,Rows=5,Presets=new List<TerrainPreset>{preset},ActivePresetId=preset.Id};for(int i=0;i<40;i++)map.Cells.Add(new CellData());File.WriteAllText(Path.Combine(dir,"Próba.hexmap"),MapLogic.Serializer().Serialize(map));
        }
        [STAThread] static int Main(string[] args)
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Length==2&&args[0]=="--fixture"){Fixture(args[1]);return 0;}root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");Directory.CreateDirectory(root);
            try{if(args.Length!=1)throw new Exception("Choose controls, library or presets");switch(args[0]){case "controls":Controls();break;case "library":Library();break;case "presets":Presets();break;default:throw new Exception("Unknown case");}Console.WriteLine("PASS "+args[0]+"; one targeted 0.3.2 scenario, no full campaign.");return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}