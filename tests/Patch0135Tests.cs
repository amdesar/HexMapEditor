using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WpfModifiers=System.Windows.Input.ModifierKeys;

namespace HexMapEditor.Tests
{
    internal static class Patch0135Tests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
        static T Get<T>(object owner,string name){return (T)owner.GetType().GetField(name,Private).GetValue(owner);}
        static void Set(object owner,string name,object value){owner.GetType().GetField(name,Private).SetValue(owner,value);}
        static object Call(object owner,string name,params object[] args)
        {return owner.GetType().GetMethods(Private).Single(method=>method.Name==name&&method.GetParameters().Length==args.Length).Invoke(owner,args);}
        static string Json(object value){return MapLogic.Serializer().Serialize(value);}
        static void History(MainWindow window,bool redo){Check((bool)Call(window,"TryHistory",redo,null),"Document history failed");}
        static T Find<T>(Control root) where T:Control
        {
            foreach(Control child in root.Controls){var match=child as T??Find<T>(child);if(match!=null)return match;}
            return null;
        }
        static void WithEditor(Action<MainWindow,MapData> test)
        {
            var data=MainWindow.CreateProjectData(5,4,"PointyTop","Próba 0.13.5",PresetCatalog.Default());
            string path=Path.Combine(Root,"Test.hexmap");MainWindow.WriteProjectData(data,path,false);
            using(var editor=new MainWindow(MainWindow.ReadProjectData(path),path))
            {
                Exception failure=null;
                editor.Shown+=(s,e)=>editor.BeginInvoke(new Action(()=>{
                    try{test(editor,(MapData)Call(editor,"get_map"));}
                    catch(Exception error){failure=error;}
                    finally{Call(editor,"CaptureSavedState");editor.Close();}
                }));
                Application.Run(editor);
                if(failure!=null)throw failure;
            }
        }
        static List<FillVariant> Pool(){return new List<FillVariant>{new FillVariant{Id="z",Weight=1},new FillVariant{Id="b",Color="#333333",Gradient=GradientRules.Two("#333333"),Weight=200}};}
        sealed class LastTicket:Random
        {
            internal int Calls;
            public override int Next(int maxValue){Calls++;return maxValue-1;}
        }
        static GraphicSource Sample()
        {
            using(var bitmap=new Bitmap(48,32))using(var stream=new MemoryStream())
            {using(var g=Graphics.FromImage(bitmap))g.Clear(Color.SteelBlue);bitmap.Save(stream,ImageFormat.Png);return GraphicSources.Import(stream.ToArray());}
        }
        static void Weights()
        {
            var serializer=MapLogic.Serializer();
            Check(serializer.Deserialize<FillVariant>("{\"Id\":\"old\",\"Color\":\"#778899\"}").Weight==1,"Legacy fill did not default to one");
            var pool=Pool();var random=new Random(9);
            int dominant=Enumerable.Range(0,20000).Count(i=>FillRules.WeightedPick(pool,random).Id=="b");
            Check(dominant>19800&&dominant<20000,"Weighted pick is not proportional");
            var terrain=PresetCatalog.Default().Terrains[0];FillRules.SetPool(terrain,pool);
            var first=TerrainMosaic.Assignments(terrain,12);
            Check(first.Count(n=>n==0)==11&&first.Count(n=>n==1)==1&&first.SequenceEqual(TerrainMosaic.Assignments(terrain,12)),"Mosaic allocation was not weighted and deterministic");
            string surface=FillRules.SurfaceSignature(terrain.Variants),mosaic=TerrainMosaic.Key(terrain);
            terrain.Variants[1].Weight=75;
            Check(surface==FillRules.SurfaceSignature(terrain.Variants)&&mosaic!=TerrainMosaic.Key(terrain),"Weights changed the surface or did not refresh the mosaic");
            var preset=PresetCatalog.Default();preset.Id="weight-fixture";preset.Name="Wagi";preset.Terrains[0]=terrain;
            Directory.CreateDirectory(LibraryPaths.Presets);PresetCatalog.Write(preset,LibraryPaths.Presets,false);
            string presetFile=PresetCatalog.FindFile(LibraryPaths.Presets,preset.Id);
            Check(PresetCatalog.ReadFile(presetFile).Terrains[0].Variants[1].Weight==75,"Preset lost weights");
            var map=MainWindow.CreateProjectData(5,4,"PointyTop","Wagi",preset);map.FogPalette=Pool();map.DarkPalette=Pool();map.DarkPalette[1].Weight=150;
            string path=Path.Combine(Root,"Wagi.hexmap");MainWindow.WriteProjectData(map,path,false);
            var read=MainWindow.ReadProjectData(path);
            Check(read.FormatVersion==12&&read.Presets.Single(p=>p.Id==preset.Id).Terrains[0].Variants[1].Weight==75&&read.FogPalette[1].Weight==200&&read.DarkPalette[1].Weight==150,"Embedded pools lost weights");
            WithEditor((editor,doc)=>{
                doc.FogPalette=Pool();doc.DarkPalette=Pool();doc.Cells[0].Fog=doc.Cells[0].Darkness=true;
                doc.Cells[0].FogFill=FillRules.Painted(pool[0]);doc.Cells[0].DarkFill=FillRules.Painted(pool[0]);
                string cells=Json(doc.Cells);var changed=Pool();changed[1].Weight=125;
                Call(editor,"ChangeFogAppearance",changed,true,true);Call(editor,"ChangeDarkAppearance",changed,true,true);
                Check(cells==Json(doc.Cells)&&doc.FogPalette[1].Weight==125&&doc.DarkPalette[1].Weight==125,"Preserve-layout altered painted fields");
                History(editor,false);History(editor,false);
                Check(doc.FogPalette[1].Weight==200&&doc.DarkPalette[1].Weight==200,"Undo did not restore weights");
                History(editor,true);History(editor,true);Check(cells==Json(doc.Cells),"Redo affected actual fields");
                var later=terrain.Copy();later.Variants[1].Weight=33;
                typeof(MainWindow).GetMethod("VersionTerrain",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{terrain,later});
                Check(later.VariantId==terrain.VariantId&&later.Revision==terrain.Revision,"Weights alone advanced terrain version");
                var lottery=new LastTicket();Set(editor,"appearanceRandom",lottery);
                var working=(TerrainPreset)Call(editor,"ActivePreset");var originalTerrain=working.Terrains[0];
                working.Terrains[0]=originalTerrain.Copy();FillRules.SetPool(working.Terrains[0],changed);
                try
                {
                    Set(editor,"brushIndex",0);
                    foreach(var tool in new[]{EditorTool.Brush,EditorTool.FogBucket,EditorTool.DarkBucket})
                    {
                        doc.Cells[2].Fog=doc.Cells[2].Darkness=false;
                        if(tool==EditorTool.Brush)
                        {
                            Call(editor,"BeginMaterialStroke",2,tool);Call(editor,"PaintMaterial",2);Call(editor,"FinishStroke");
                        }
                        else{Set(editor,"currentTool",tool);Call(editor,"FillRegion",2);}
                        Func<string> variant=()=>tool==EditorTool.Brush?doc.Cells[2].FillVariantId:tool==EditorTool.FogBucket?doc.Cells[2].FogFill.Id:doc.Cells[2].DarkFill.Id;
                        int draws=lottery.Calls;
                        Check(variant()=="b","Painting did not use the weighted selection for "+tool);
                        History(editor,false);History(editor,true);
                        Check(variant()=="b"&&lottery.Calls==draws,"Undo or Redo rerolled "+tool);
                    }
                }
                finally{working.Terrains[0]=originalTerrain;}
                using(var dialog=new FillVariantDialog(pool[0],false))
                {
                    dialog.Show(editor);
                    var field=Find<ScrubNumber>(dialog);Check(field.UseLocalHistory&&field.Minimum==1&&field.Maximum==200,"Variant weight field limits/history");
                    field.BeginText();field.Text="37";field.CommitText();field.StepLocal(false);Check(field.Value==1,"Local weight undo");
                    field.StepLocal(true);Check(field.Value==37,"Local weight redo");
                }
            });
        }
        static void Libraries()
        {
            var icons=new List<LibraryIcon>{new LibraryIcon{Id="first",Name="Żółw",LibraryOrder=9},new LibraryIcon{Id="second",Name="Zolw",LibraryOrder=2},new LibraryIcon{Id="last",Name="Alfa",LibraryOrder=6}};
            foreach(string query in new[]{"zolw","ŻÓŁW","Zółw"})
                Check(LibraryListing.Filter(icons,query,0,i=>i.Name,i=>i.LibraryOrder,i=>i.Id).Select(i=>i.Id).SequenceEqual(new[]{"first","second"}),"Search is not folded in both directions");
            Check(LibraryListing.Filter(icons,"",3,i=>i.Name,i=>i.LibraryOrder,i=>i.Id).First().Id=="first"&&
                LibraryListing.Filter(icons,"",4,i=>i.Name,i=>i.LibraryOrder,i=>i.Id).First().Id=="second"&&
                LibraryListing.Filter(icons,"",1,i=>i.Name,i=>i.LibraryOrder,i=>i.Id).First().Id=="last","Sort changed or ignored insertion order");
            Func<LibraryIcon,Action,Button> tile=(item,choose)=>{var button=new Button{Tag=item.Id};button.Click+=(s,e)=>choose();return button;};
            using(var dialog=new IconPickerDialog(()=>icons,tile,Color.Black))
            {
                var listing=Find<LibraryListing>(dialog);var entry=Find<TextBox>(listing);var order=Find<ComboBox>(listing);
                dialog.Show();entry.Text="żółw";order.SelectedIndex=4;
                var list=Find<FlowLayoutPanel>(dialog.Controls.OfType<Panel>().First(p=>p.Dock==DockStyle.Fill));
                var expected=(Button)list.Controls[0];Check((string)expected.Tag=="second","Selection uses filtered row order instead of ID");
                expected.PerformClick();string deleted=null;dialog.Delete=item=>{deleted=item.Id;icons.Remove(item);};
                Get<Button>(dialog,"remove").PerformClick();Check(deleted=="second"&&list.Controls.Count==1,"Remove targeted the wrong ID under filter");
                entry.Text="brak";Check(list.Controls.Count==0&&!Get<Button>(dialog,"remove").Enabled,"Empty view still permits deletion");
            }
            var source=Sample();var graphic=new GraphicDefinition{Name="Żółw",Source=source};graphic.Seal();
            var terrainsData=new List<TerrainGraphic>{new TerrainGraphic{Id="terrain",Name="Żółw",Asset=graphic,LibraryOrder=17}};
            var backgroundsData=new List<BackgroundEntry>{new BackgroundEntry{Id="background",Name="Zolw",LibraryOrder=20,
                Settings=new MapBackground{Source=source,SourceId=source.Id}}};
            using(var again=new IconPickerDialog(()=>icons,tile,Color.Black))
            using(var objects=new IconPickerDialog(()=>icons,tile,Color.Black,true))
            using(var terrains=new TerrainGraphicPicker(()=>terrainsData,false))
            using(var backgrounds=new BackgroundPicker(()=>backgroundsData))
            {
                Check(Find<ComboBox>(Find<LibraryListing>(again)).SelectedIndex==4&&Find<TextBox>(Find<LibraryListing>(again)).Text=="","Query or sort lifetime incorrect");
                foreach(Form dialog in new Form[]{objects,terrains,backgrounds})
                {
                    var listing=Find<LibraryListing>(dialog);Check(listing!=null&&Find<ComboBox>(listing).SelectedIndex==0,"Library sort is shared across categories");
                    dialog.Size=dialog.MinimumSize;dialog.Show();Check(listing.Width<=dialog.ClientSize.Width&&Find<TextBox>(listing).Width>Find<ComboBox>(listing).Width,"Library search layout at minimum");
                    var search=Find<TextBox>(listing);Find<ComboBox>(listing).SelectedIndex=3;
                    search.Text="żółw";
                    var nativeList=Find<ListView>(dialog);
                    var tiles=nativeList==null?Find<FlowLayoutPanel>(dialog.Controls.OfType<Panel>().First(p=>p.Dock==DockStyle.Fill)):null;
                    Check((nativeList==null?tiles.Controls.Count:nativeList.Items.Count)==1,"Category did not filter to the matching display name");
                    search.Text="żuraw";
                    Check((nativeList==null?tiles.Controls.Count:nativeList.Items.Count)==0,"Category did not show an empty result");
                }
            }
        }
        static void WaitForInk(MainWindow editor)
        {
            var until=DateTime.UtcNow.AddSeconds(8);
            while((bool)Call(editor,"get_InkCommitBusy")&&DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(10);}
            Check(!(bool)Call(editor,"get_InkCommitBusy"),"Exact ink worker did not finish");
        }
        static void Gesture(MainWindow editor,Point point,EraserTargets flags,bool commit)
        {
            Set(editor,"eraserTargets",flags);Set(editor,"gestureTool",EditorTool.Eraser);Set(editor,"gestureButton",MouseButtons.Left);
            Call(editor,"BeginInk",new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0));
            Call(editor,"EndCanvasGesture",commit);WaitForInk(editor);
        }
        static void Eraser()
        {
            WithEditor((editor,map)=>{
                var center=BrushGeometry.Center(map,1,1);Point mouse=Point.Round(center);int index=map.Columns+1;var cell=map.Cells[index];
                Set(editor,"scale",1f);Set(editor,"pan",PointF.Empty);Set(editor,"inkToolSize",40f);
                MapLogic.ApplyTerrain(cell,PresetCatalog.Default().Terrains[0]);cell.Name="Nazwa";cell.Description="Opis";
                cell.NotesFormat="richDocument1";cell.RichDescription=NoteCodec.Write(NoteCodec.FromPlain(cell.Description));
                IconAsset asset;using(var bitmap=new Bitmap(16,16))asset=IconAssets.FromBitmap(bitmap,"Próba");
                map.IconAssets.Add(asset);cell.IconId=asset.Id;cell.IconEntryId="icon";cell.IconName="Ikona";
                cell.Fog=cell.Darkness=true;var first=new MapObject{X=center.X,Y=center.Y,Locked=true,TemplateId="o",AssetId=asset.Id};
                var second=new MapObject{X=center.X+95,Y=center.Y,TemplateId="o",AssetId=asset.Id};map.Objects.AddRange(new[]{first,second});
                var group=new ObjectGroup{SizeValue=47,Members=new List<string>{first.Id,second.Id}};map.Groups.Add(group);
                map.Ink.Add(new InkStroke{Width=5,Points=new List<InkPoint>{new InkPoint(new PointF(center.X-45,center.Y)),new InkPoint(new PointF(center.X+45,center.Y))}});
                Call(editor,"CaptureSavedState");string original=Json(new{cell,map.Objects,map.Groups,map.Ink});
                foreach(var flags in new[]{EraserTargets.Terrain,EraserTargets.Name,EraserTargets.Description,EraserTargets.Icons,EraserTargets.Darkness,EraserTargets.Fog,EraserTargets.Ink})
                {
                    Gesture(editor,mouse,flags,true);
                    Check(MapLogic.IsUnpainted(cell)==(flags==EraserTargets.Terrain)&&(cell.Name=="")== (flags==EraserTargets.Name)&&
                        (cell.RichDescription==null)==(flags==EraserTargets.Description)&&(cell.IconId==null)==(flags==EraserTargets.Icons)&&
                        (cell.IconEntryId==null)==(flags==EraserTargets.Icons)&&(cell.IconName==null)==(flags==EraserTargets.Icons)&&
                        cell.Fog==(flags!=EraserTargets.Fog)&&cell.Darkness==(flags!=EraserTargets.Darkness)&&map.Objects.Count==2,"Wrong layer erased: "+flags);
                    History(editor,false);Check(original==Json(new{cell,map.Objects,map.Groups,map.Ink}),"Undo did not restore "+flags);
                }
                Gesture(editor,mouse,EraserTargets.Objects,true);Check(map.Objects.Count==2&&cell.Fog,"Masked object erased or fog changed");
                Set(editor,"previewWithoutFog",true);Set(editor,"hideDarkness",true);
                Gesture(editor,mouse,EraserTargets.Objects,true);
                Check(map.Objects.Single().Id==second.Id&&map.Groups.Single().Members.SequenceEqual(new[]{second.Id})&&map.Groups[0].SizeValue==47&&cell.Fog,"Visible locked member not erased or group/fog changed");
                History(editor,false);Check(original==Json(new{cell,map.Objects,map.Groups,map.Ink}),"Object undo failed");
                var all=(EraserTargets)255;
                Gesture(editor,mouse,all,false);Check(original==Json(new{cell,map.Objects,map.Groups,map.Ink}),"Cancelled erase partially mutated the map");
                var work=editor.RunInkWork;editor.RunInkWork=(request,token)=>{throw new IOException("Próba błędu gumki");};
                Gesture(editor,mouse,all,true);Check(original==Json(new{cell,map.Objects,map.Groups,map.Ink}),"Error left a partial transaction");
                editor.RunInkWork=work;Gesture(editor,mouse,all,true);
                Check(MapLogic.IsUnpainted(cell)&&cell.Name==""&&cell.Description==""&&!cell.Fog&&!cell.Darkness&&cell.IconId==null&&map.Objects.Count==1&&original!=Json(new{cell,map.Objects,map.Groups,map.Ink}),"Combined action failed");
                History(editor,false);Check(original==Json(new{cell,map.Objects,map.Groups,map.Ink}),"Combined action is not one Undo");
                History(editor,true);Check(map.Objects.Count==1&&cell.Name=="","Combined Redo failed");
                var other=Point.Round(new PointF(second.X,second.Y));Gesture(editor,other,EraserTargets.Objects,true);
                Check(map.Objects.Count==0&&map.Groups.Count==0,"Empty group remained");
            });
        }
        [DllImport("user32.dll")]static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]static extern int ToUnicodeEx(uint key,uint scan,byte[] state,StringBuilder result,int count,uint flags,IntPtr layout);
        static void ControlsCase()
        {
            Check(!RichNoteEditor.AllowsControlShortcut(WpfModifiers.Control|WpfModifiers.Alt)&&!RichNoteEditor.AllowsControlShortcut(WpfModifiers.Control|WpfModifiers.Alt|WpfModifiers.Shift)&&
                RichNoteEditor.AllowsControlShortcut(WpfModifiers.Control)&&RichNoteEditor.AllowsControlShortcut(WpfModifiers.Control|WpfModifiers.Shift),"Description keyboard modifier gate");
            var layout=GetKeyboardLayout(0);var strings=new List<string>();
            foreach(bool upper in new[]{false,true})
            {
                var state=new byte[256];state[0x11]=state[0x12]=state[0xA5]=0x80;if(upper)state[0x10]=0x80;
                var letters=new StringBuilder();
                foreach(char key in "ACELNOSXZ")
                {var buffer=new StringBuilder(8);if(ToUnicodeEx(key,0,state,buffer,buffer.Capacity,0,layout)>0)letters.Append(buffer);}
                strings.Add(letters.ToString());
            }
            Console.WriteLine("Keyboard layout "+layout.ToInt64().ToString("X")+" native translation "+String.Join(" / ",strings));
            if((layout.ToInt64()&0xffffffff)==0x04150415)
                Check(strings.SequenceEqual(new[]{"ąćęłńóśźż","ĄĆĘŁŃÓŚŹŻ"}),"Polish Programmer keyboard translation");
            var left=new Rectangle(-1600,70,840,600);var work=new Rectangle(-1920,0,1920,1040);
            Check(AboutDialog.Position(left,work,new Size(400,160)).Location==new Point(-1380,290)&&
                work.Contains(AboutDialog.Position(new Rectangle(-1910,5,15,10),work,new Size(600,240))),"About centering on a negative-coordinate monitor");
            WithEditor((editor,map)=>{
                Check(!(bool)Call(editor,"HandleHistoryShortcut",Keys.Control|Keys.Alt|Keys.Z),"WinForms consumed AltGr+Z");
                var strip=Get<ToolStrip>(editor,"quickStrip");var layers=Get<ToolStripDropDownButton>(editor,"quickLayers");
                var quality=Get<Button>(editor,"qualityButton");editor.Width=editor.MinimumSize.Width;Application.DoEvents();
                Check(layers.Height==quality.Height&&layers.Width-quality.Height==12&&layers.Placement==ToolStripItemPlacement.Main,"Layer button geometry at minimum width");
                foreach(var item in strip.Items.OfType<ToolStripSeparator>())
                {int index=strip.Items.IndexOf(item);Check(item.Bounds.Left-strip.Items[index-1].Bounds.Right==strip.Items[index+1].Bounds.Left-item.Bounds.Right,"Separator margins are asymmetric");}
                strip.Width=125;strip.PerformLayout();Check(layers.Placement==ToolStripItemPlacement.Overflow,"No Layers overflow");
                AboutDialog.ShowFor(editor);var about=editor.OwnedForms.OfType<AboutDialog>().Single();
                Check(about.Controls.OfType<Button>().Any(b=>b.Text=="Repozytorium projektu")&&!about.Modal,"About design");about.Close();
            });
            using(var background=new BackgroundDialog(null,new Size(100,100)))
            {
                var row=background.Controls.OfType<Button>().Where(b=>b.Top==10).OrderBy(b=>b.Left).ToArray();
                Check(row.Select(b=>b.Text).SequenceEqual(new[]{"Importuj ze schowka","Importuj z pliku","Biblioteka","Usuń tło"})&&!row.Last().Enabled,"Background controls order or states");
            }
        }
        [STAThread]static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                switch(args.Single())
                {
                    case "weights":Weights();break;
                    case "libraries":Libraries();break;
                    case "eraser":Eraser();break;
                    case "controls":ControlsCase();break;
                    default:throw new ArgumentException("Unknown Case.");
                }
                Console.WriteLine(args[0]+" PASS");return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
        }
    }
}
