using System;using System.Drawing;using System.Reflection;using System.Windows.Forms;
namespace HexMapEditor.Tests
{
 internal static class Patch083Tests
 {
  const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
  static object Get(object o,string n){return o.GetType().GetField(n,P).GetValue(o);}
  static void Set(object o,string n,object v){o.GetType().GetField(n,P).SetValue(o,v);}
  static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n,P).Invoke(o,a);}
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  [STAThread] static int Main(string[] args){try{
   if(args.Length!=1||args[0]!="controls")throw new Exception("Wymagany Case: controls");
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   var data=MainWindow.CreateProjectData(15,10,"PointyTop","Próba 0.8.3",PresetCatalog.Default());
   using(var w=new MainWindow(data,null)){w.Show();
    foreach(EditorTool t in Enum.GetValues(typeof(EditorTool))){Set(w,"currentTool",t);Call(w,"MaterialChosen");bool mask=t==EditorTool.FogBrush||t==EditorTool.FogBucket||t==EditorTool.DarkBrush||t==EditorTool.DarkBucket;Check((EditorTool)Get(w,"currentTool")== (mask?EditorTool.Brush:t),"Narzędzie "+t);}
    var icons=(System.Collections.Generic.List<LibraryIcon>)Get(w,"libraryIcons");var objects=(System.Collections.Generic.List<ObjectTemplate>)Get(w,"libraryObjects");
    Check(icons.Count>0&&objects.Count>0,"Brak syntetycznych zasobów testowych");
    string terrain=((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].TerrainId;
    foreach(string selector in new[]{"SelectTerrain","SelectIcon","SelectObjectTemplate"})foreach(EditorTool tool in new[]{EditorTool.FogBrush,EditorTool.FogBucket,EditorTool.DarkBrush,EditorTool.DarkBucket,EditorTool.Hand,EditorTool.Stylus}){
     Set(w,"currentTool",tool);string id=selector=="SelectTerrain"?terrain:selector=="SelectIcon"?icons[0].Id:objects[0].Id;Call(w,selector,id);
     bool mask=tool==EditorTool.FogBrush||tool==EditorTool.FogBucket||tool==EditorTool.DarkBrush||tool==EditorTool.DarkBucket;Check((EditorTool)Get(w,"currentTool")== (mask?EditorTool.Brush:tool),selector+" / "+tool);
    }
    var left=(Button)Get(w,"zoomButton");var right=(Button)Get(w,"zoomArrow");Call(w,"SetZoomPercent",12800f);Check(left.Text=="12800%"&&left.Width>=TextRenderer.MeasureText(left.Text,left.Font,Size.Empty,TextFormatFlags.NoPadding).Width,"Tekst zoomu");
    left.PerformClick();Check(Math.Abs((float)Get(w,"scale")-1)<0.0001f,"Reset zoomu");right.PerformClick();var popup=(ContextMenuStrip)Get(w,"zoomPopup");Check(popup.Visible,"Otwarcie listy");right.PerformClick();Check(!popup.Visible,"Zamknięcie listy");right.PerformClick();popup.Items[1].PerformClick();Check(Math.Abs((float)Get(w,"scale")-0.5f)<0.0001f,"Preset 50%");Check(popup.Items[popup.Items.Count-1].Text=="Niestandardowe…","Niestandardowy zoom");
    using(var rename=new RenameProjectDialog("Próba.hexmap",x=>true)){rename.Show();var name=(TextBox)Get(rename,"name");Check(name.Height==name.PreferredHeight&&name.Text=="Próba","Pole rename");rename.Close();}
    Check((Get(w,"objectName") as TextBox).MinimumSize.Height==0,"Pole obiektu");
    Check(((MenuStrip)Get(w,"mainMenu")).Renderer is FitMenuRenderer,"Renderer menu");
   }Console.WriteLine("controls PASS");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
