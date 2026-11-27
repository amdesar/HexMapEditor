using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0134Tests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static T Get<T>(object owner,string name){return (T)owner.GetType().GetField(name,Private).GetValue(owner);}
        static object Call(object owner,string name,params object[] args){return owner.GetType().GetMethod(name,Private).Invoke(owner,args);}
        static byte[] Png()
        {
            using(var bitmap=new Bitmap(48,32))
            using(var stream=new MemoryStream())
            {
                bitmap.SetPixel(6,6,Color.FromArgb(150,55,160,90));bitmap.Save(stream,ImageFormat.Png);return stream.ToArray();
            }
        }
        static string FixtureMap(string name)
        {
            string path=Path.Combine(Root,name+".hexmap");
            MainWindow.WriteProjectData(MainWindow.CreateProjectData(5,4,"PointyTop",name,PresetCatalog.Default()),path,false);
            return path;
        }
        static void Tools()
        {
            string path=FixtureMap("Narzędzia");
            using(var editor=new MainWindow(MainWindow.ReadProjectData(path),path))
            {
                Exception failure=null;
                editor.Shown+=(sender,args)=>editor.BeginInvoke(new Action(()=>
                {
                    try{VerifyTools(editor,path);}
                    catch(Exception error){failure=error;}
                    finally{editor.Close();}
                }));
                Application.Run(editor);
                if(failure!=null)throw failure;
            }
        }
        static void VerifyTools(MainWindow editor,string path)
        {
                var menu=Get<ToolStripMenuItem[]>(editor,"layerItems");
                var quick=Get<ToolStripMenuItem[]>(editor,"quickLayerItems");
                var layerButton=Get<ToolStripDropDownButton>(editor,"quickLayers");
                var strip=Get<ToolStrip>(editor,"quickStrip");
                var quality=Get<Button>(editor,"qualityButton");
                int index=strip.Items.IndexOf(layerButton);
                Check(((ToolStripControlHost)strip.Items[index+1]).Control==quality,"Layers are not before quality");
                foreach(int width in new[]{editor.Width,editor.MinimumSize.Width})
                {
                    editor.Width=width;Application.DoEvents();Check(layerButton.Available,"Layers disappear at width "+width);
                }
                var layerShortcut=Keys.Control|Keys.Shift|Keys.L;
                Check((bool)Call(editor,"HandleLayerShortcut",layerShortcut)&&layerButton.DropDown.Visible,"Keyboard shortcut did not open layers");
                layerButton.HideDropDown();
                strip.AutoSize=false;strip.Width=125;strip.PerformLayout();
                Check(layerButton.Placement==ToolStripItemPlacement.Overflow,"Layers lack the existing overflow path");
                Check((bool)Call(editor,"HandleLayerShortcut",layerShortcut)&&layerButton.DropDown.Visible&&strip.OverflowButton.DropDown.Visible,"Keyboard shortcut did not open layers in overflow");
                layerButton.HideDropDown();strip.OverflowButton.HideDropDown();
                var source=GraphicSources.Import(Png());
                var before=File.ReadAllBytes(path);
                quick[(int)MapLayer.Background].PerformClick();
                menu[(int)MapLayer.Fog].PerformClick();
                Check(!menu[(int)MapLayer.Background].Checked&&!quick[(int)MapLayer.Fog].Checked&&
                    quick.All(i=>menu.Single(m=>m.Text==i.Text).Checked==i.Checked),"Layer switches disagree");
                Check(!(bool)Call(editor,"get_HasUnsavedChanges") && File.ReadAllBytes(path).SequenceEqual(before),"Layer switches affect the document");
                Call(editor,"SelectTool",EditorTool.Stylus);
                var ink=Get<ScrubNumber>(editor,"inkSize");
                var height=Get<ScrubNumber>(editor,"heightPicker");
                Check(ink.UseLocalHistory&&new[]{"sizePicker","heightPicker","apexPicker","rotationPicker"}.All(name=>Get<ScrubNumber>(editor,name).UseLocalHistory),"Tool fields lack local history");
                ink.BeginText();ink.Text="15";ink.CommitText();
                ink.BeginText();ink.Text="18";Check((bool)Call(editor,"HandleHistoryShortcut",Keys.Control|Keys.Z),"Text shortcut missed the tool field");
                Check(ink.Text=="15"&&ink.Value==15,"Undo changed the model during typing");
                ink.StepLocal(true);Check(ink.Text=="18","Text redo did not restore draft");
                ink.Finish(true);Check(ink.Value==15,"Escape did not cancel draft");
                ink.BeginText();ink.Text="-,";ink.CommitText();Check(ink.Value==15,"Invalid number changed value");
                ink.BeginScrub(0);ink.MoveScrub(40);ink.EndScrub();decimal dragged=ink.Value;
                Check(dragged>15&&!height.CanStepLocal(false),"Scrub or isolated field history failed");
                ink.StepLocal(false);Check(ink.Value==15,"Undo scrub did not restore value");
                ink.StepLocal(true);Check(ink.Value==dragged,"Redo scrub did not restore value");
                Check(!(bool)Call(editor,"get_HasUnsavedChanges"),"Tool settings marked the map dirty");
                Call(editor,"CommitBackground",new MapBackground{Source=source,SourceId=source.Id},null,null,true);
                var canvas=Get<Panel>(editor,"canvas");canvas.Focus();
                Check((bool)Call(editor,"HandleHistoryShortcut",Keys.Control|Keys.Z),"Global shortcut was not routed");
                Check(!(bool)Call(editor,"get_HasUnsavedChanges")&&ink.Value==dragged,"Global Undo affected the field or did not undo one map edit");
                Check(((ICollection)Call(editor,"get_undo")).Count==0,"Tool entries leaked into document history");
                Call(editor,"ShowAbout");
                var about=editor.OwnedForms.OfType<AboutDialog>().Single();
                Check(!about.Modal&&about.Controls.OfType<Button>().Any(b=>b.Text=="Repozytorium projektu")&&
                    about.Controls.OfType<Label>().Any(l=>l.Text.Contains("Licencja MIT")),"About contents or modality");
                Call(editor,"ShowAbout");Check(ReferenceEquals(about,editor.OwnedForms.OfType<AboutDialog>().Single()),"Duplicate About window");
                ((Button)about.CancelButton).PerformClick();Check(about.IsDisposed,"About Cancel did not close");
        }
        static void Flow()
        {
            var offer=new ReleaseOffer(new Version(1,2,3),null,"HexMapEditor-1.2.3-Setup.exe",
                new Uri("https://github.com/amdesar/HexMapEditor/releases/tag/v1.2.3"),
                new Uri("https://github.com/amdesar/HexMapEditor/releases/download/v1.2.3/HexMapEditor-1.2.3-Setup.exe"),
                new string('0',64),32,true);
            using(var dialog=new UpdateOfferDialog(offer))
            {
                var buttons=dialog.Controls.OfType<EditorButton>().ToArray();
                var install=buttons.Single(b=>b.Text.StartsWith("Zainstaluj"));
                var cancel=buttons.Single(b=>b.Text=="Pomiń");
                Check(buttons.All(b=>b.Top==install.Top)&&install.BackColor==ChoiceStyle.ActiveBack&&cancel.Destructive,"Offer action row or colors");
                var promise=new TaskCompletionSource<VerifiedInstaller>();
                bool cancelled=false;
                dialog.Download=(token,progress)=>{
                    token.Register(()=>{cancelled=true;promise.TrySetCanceled();});
                    progress.Report(31);
                    return promise.Task;
                };
                dialog.Shown+=(s,e)=>dialog.BeginInvoke((Action)(()=>{
                    install.PerformClick();Check(!install.Enabled&&cancel.Text=="Anuluj"&&cancel.Enabled,"Transfer controls did not change");
                    cancel.PerformClick();
                }));
                Check(dialog.ShowDialog()==DialogResult.Cancel&&cancelled&&dialog.TakeInstaller()==null&&dialog.Error==null,"Cancellation reopened or returned an installer");
            }
            string transfer=Directory.CreateDirectory(Path.Combine(Root,"Downloads")).FullName;
            using(var token=new System.Threading.CancellationTokenSource())
            {
                var updates=new ReleaseUpdates("amdesar","HexMapEditor",(url,read)=>read(new MemoryStream(new byte[32])));
                try
                {
                    updates.DownloadSetup(offer,transfer,percent=>token.Cancel(),token.Token);
                    throw new InvalidOperationException("Cancelled transfer returned an installer.");
                }
                catch(OperationCanceledException){Check(!Directory.EnumerateFileSystemEntries(transfer).Any(),"Cancelled transfer left a partial download");}
            }
            var original=Png();
            var clipboard=new System.Windows.DataObject();clipboard.SetData("PNG",original);
            var fallback=new System.Windows.DataObject(); fallback.SetText("nie obraz");
            var directory=Directory.CreateDirectory(Path.Combine(Root,"Library","Backgrounds")).FullName;
            using(var dialog=new BackgroundDialog(null,new Size(640,480)))
            {
                try{dialog.SetClipboardSource(fallback);throw new Exception("Accepted non-raster clipboard");}catch(InvalidDataException){}
                dialog.SetClipboardSource(clipboard);
                Check(!Directory.EnumerateFiles(directory).Any(),"Clipboard changed Library before acceptance");
                dialog.Show();((Button)dialog.AcceptButton).PerformClick();
                Check(dialog.Result!=null&&dialog.ImportName=="Tło ze schowka","Missing clipboard draft");
                var entry=BackgroundLibrary.Prepare(directory,null,dialog.ImportName,dialog.Result,new BackgroundEntry[0]);
                new BackgroundFileChange{After=entry}.Write(true);
                var read=BackgroundLibrary.Read(entry.Path,new Dictionary<string,GraphicSource>());
                Check(File.ReadAllBytes(read.Settings.Source.LibraryFile).SequenceEqual(original)&&Path.GetDirectoryName(entry.Path)==Path.Combine(directory,"Metadata"),"Clipboard original or metadata was lost");
            }
        }
        static void Drop()
        {
            string first=FixtureMap("Pierwsza"),second=FixtureMap("Druga");
            string import=Directory.CreateDirectory(Path.Combine(LibraryPaths.Root,"Import","Backgrounds")).FullName;
            string excluded=Path.Combine(import,"Nie otwieraj.hexmap");File.Copy(first,excluded);
            string bad=Path.Combine(Root,"Zły.txt");File.WriteAllText(bad,"bad");
            var errors=new List<string>();
            var valid=MapDrop.Validate(new[]{first,first.ToUpperInvariant(),second,bad,excluded,Path.Combine(Root,"Brak.hexmap")},LibraryPaths.Root,errors);
            Check(valid.SequenceEqual(new[]{first,second})&&errors.Count==3,"Extension, input folder, duplicates or unavailable path");
            Check(MapDrop.Validate(Enumerable.Repeat(first,MapDrop.Limit+1).ToArray(),LibraryPaths.Root,errors).Count==0,"Drop budget failed");
            using(var editor=new MainWindow(MainWindow.ReadProjectData(first),first))
            {
                editor.Show();Application.DoEvents();
                var canvas=Get<Panel>(editor,"canvas");
                Check(canvas.AllowDrop&&editor.AllowDrop,"Main map targets are not registered");
                var payload=new DataObject(DataFormats.FileDrop,new[]{second,first});
                var args=new DragEventArgs(payload,0,0,0,DragDropEffects.Copy,DragDropEffects.None);
                typeof(Control).GetMethod("OnDragDrop",Private).Invoke(canvas,new object[]{args});
                Application.DoEvents();
                Check(((ICollection)Get<object>(editor,"documents")).Count==2&&RecentProjects.Shared.Entries.Contains(second),"Batch/duplicate/MRU integration failed");
            }
            using(var welcome=new StartWindow((map,path)=>{throw new InvalidOperationException("Unexpected transition");}))
            {
                Exception failure=null;
                welcome.Shown+=async (s,e)=>{
                    try
                    {
                        string[] received=null;
                        welcome.DroppedProjects=paths=>received=paths;
                        var payload=new DataObject(DataFormats.FileDrop,new[]{first,second});
                        var args=new DragEventArgs(payload,0,0,0,DragDropEffects.Copy,DragDropEffects.None);
                        welcome.Transition=true;
                        typeof(Control).GetMethod("OnDragDrop",Private).Invoke(welcome,new object[]{args});
                        await Task.Delay(40);Check(received==null,"A blocked welcome accepted a drop");
                        welcome.Transition=false;
                        typeof(Control).GetMethod("OnDragDrop",Private).Invoke(welcome,new object[]{args});
                        await Task.Delay(150);Check(received!=null&&received.SequenceEqual(new[]{first,second}),"Welcome drop did not preserve file order");
                    }
                    catch(Exception error){failure=error;}
                    finally{welcome.Close();}
                };
                Application.Run(welcome);
                if(failure!=null)throw failure;
            }
        }
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length!=1)throw new ArgumentException("Specify one Case.");
                switch(args[0])
                {
                    case "tools":Tools();break;
                    case "flow":Flow();break;
                    case "drop":Drop();break;
                    default:throw new ArgumentException("Unknown Case.");
                }
                Console.WriteLine(args[0]+" PASS");return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
        }
    }
}
