using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0101Tests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static T Get<T>(object target,string field){return (T)target.GetType().GetField(field,Private).GetValue(target);}
        static void Set(object target,string field,object value){target.GetType().GetField(field,Private).SetValue(target,value);}
        static object Call(object target,string name,params object[] args)
        {return target.GetType().GetMethods(Private).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);}
        static IEnumerable<Control> Children(Control parent)
        {return parent.Controls.Cast<Control>().SelectMany(c=>new[]{c}.Concat(Children(c)));}
        static void AddStates(MapData map,int count)
        {
            for(int i=map.ObjectStates.Count;i<count;i++)
                map.ObjectStates.Add(new ObjectStateDefinition{Id="probe-"+i,Name="Stan "+(i-2),Color="#426A86",CanReveal=true});
        }
        static MapData Data(int states=8)
        {
            var map=MainWindow.CreateProjectData(8,7,"PointyTop","Próba 0.10.1",new TerrainPreset{Name="Próba panelu"});
            AddStates(map,states);
            var asset=IconAssets.Starters()[0];map.IconAssets.Add(asset);
            int[] cols={2,4,1},rows={2,2,4};
            for(int i=0;i<3;i++)
            {
                var point=BrushGeometry.Center(map,cols[i],rows[i]);
                map.Objects.Add(new MapObject{Name=new[]{"Pierwszy","Drugi","Poza grupą"}[i],TemplateId="probe",AssetId=asset.Id,
                    Side="Ally",Size=i==1?72:21,X=point.X,Y=point.Y});
            }
            var group=new ObjectGroup{Name="Grupa próbna",Members=map.Objects.Take(2).Select(o=>o.Id).ToList(),SizeMode="Offset",SizeValue=10};
            GroupRules.Set(group,map.Objects[0].Id,GroupProperty.Size|GroupProperty.Fog,true);
            GroupRules.Set(group,map.Objects[1].Id,GroupProperty.Lock,true);
            map.Groups.Add(group);
            map.Cells[2*map.Columns+2].IconId=asset.Id;
            map.Cells[2*map.Columns+2].IconName="Ikona pola";
            return map;
        }
        static void Metadata()
        {
            string path=Path.Combine(Root,"Projekt źródłowy.hexmap"),copy=Path.Combine(Root,"Kopia z Zapisz jako.hexmap");
            var map=Data();
            var version=MainWindow.Version;
            var old="0.10.0";
            Check(MainWindow.Version!=old,"Version constant");
            string raw=System.Text.Encoding.UTF8.GetString(MainWindow.PrepareProjectBytes(map))
                .Replace("\"LastSavedAppVersion\":\""+version+"\"","\"LastSavedAppVersion\":\""+old+"\"");
            File.WriteAllText(path,raw);
            var loaded=MainWindow.ReadProjectData(path);
            Check(ProjectMetadata.ReadPrefix(path)==old&&File.ReadAllText(path)==raw,"Opening changed metadata");
            bool available=false;
            using(var ready=new ManualResetEvent(false))
            {
                RecentAvailability.Refresh(new[]{path},()=>ready.Set());
                available=ready.WaitOne(5000);
            }
            Check(available&&RecentAvailability.Visible(new[]{path}).Contains(path)&&RecentAvailability.SavedWith(path)==old,"MRU prefix");
            foreach(string expected in new[]{null,"7.8.9"})
            {
                string artificial=Path.Combine(Root,(expected??"unknown")+".hexmap");
                string incompatible="{\"FormatVersion\":199"+(expected==null?"":",\"LastSavedAppVersion\":\""+expected+"\"")+"}";
                File.WriteAllText(artificial,incompatible);
                string error=null;
                try{MainWindow.ReadProjectData(artificial);}catch(InvalidDataException e){error=e.Message;}
                Check(error!=null&&error.Contains(expected??"nieznana")&&File.ReadAllText(artificial)==incompatible,"Incompatible format");
            }
            using(var w=new MainWindow(loaded,path))
            using(var timer=new System.Windows.Forms.Timer{Interval=25})
            {
                Exception error=null;
                var schedule=Get<AutoSaveSchedule>(w,"autoSchedule");
                long now=0;schedule.Clock=()=>now;
                var elapsed=Stopwatch.StartNew();
                w.Shown+=(s,e)=>w.BeginInvoke((Action)(()=>{
                    try
                    {
                        Check((bool)Call(w,"SaveProject")&&ProjectMetadata.ReadPrefix(path)==version,"SaveProject");
                        Check((bool)Call(w,"SaveAsTo",copy,new Func<string,DialogResult>(_=>DialogResult.No))&&ProjectMetadata.ReadPrefix(copy)==version,"SaveAs");
                        // Set up a prior writer version with its verified fingerprint, leaving the original file alone.
                        string prev=File.ReadAllText(copy).Replace("\"LastSavedAppVersion\":\""+version+"\"","\"LastSavedAppVersion\":\""+old+"\"");
                        File.WriteAllText(copy,prev);
                        Call(w,"ResetSaveClock",ProjectFileVersion.Bytes(File.ReadAllBytes(copy)));
                        loaded.Cells[0].Name="Zmienione przed autozapisem";Call(w,"MarkCellDirty",loaded.Cells[0]);
                        now=schedule.Due;Call(w,"PollAutoSave");
                        Check(Get<bool>(w,"autoSaving"),"Autosave did not start");
                        timer.Start();
                    }
                    catch(Exception ex){error=ex;w.Dispose();}
                }));
                timer.Tick+=(s,e)=>{
                    try
                    {
                        if(elapsed.ElapsedMilliseconds>12000)throw new Exception("Autosave timeout");
                        if(Get<bool>(w,"autoSaving"))return;
                        Check(w.AutoSaveCount==1&&ProjectMetadata.ReadPrefix(copy)==version&&
                            MainWindow.ReadProjectData(copy).Cells[0].Name==loaded.Cells[0].Name,"Autosave version or data");
                        string foreign="{\"FormatVersion\":12,\"LastSavedAppVersion\":\"0.10.0\"}";
                        File.WriteAllText(copy,foreign);
                        var attempted=new object[]{copy,true,null};
                        Check(!(bool)Call(w,"TrySaveTo",attempted)&&File.ReadAllText(copy)==foreign,"Failed write changed metadata");
                        Check(ProjectMetadata.ReadPrefix(path)==version,"Autosave touched wrong project");
                        File.WriteAllText(path,raw);
                        bool prompted=false;
                        Check(!(bool)Call(w,"SaveAsTo",path,new Func<string,DialogResult>(_=>{prompted=true;return DialogResult.Cancel;}))&&prompted&&
                            ProjectMetadata.ReadPrefix(path)==old,"Cancelled SaveAs changed metadata");
                        timer.Stop();w.Dispose();
                    }
                    catch(Exception ex){error=ex;timer.Stop();w.Dispose();}
                };
                Application.Run(w);
                if(error!=null)throw error;
            }
            Console.WriteLine("metadata PASS");
        }
        static void Panel()
        {
            foreach(int states in new[]{7,8,19})
            {
                var map=Data(states);
                using(var w=new MainWindow(map,Path.Combine(Root,"Próba układu.hexmap")))
                {
                    w.Show();
                    if(states==19)w.Height=760;
                    Call(w,"SelectPanelMember",map.Objects[states==19?0:2].Id);
                    var body=Get<DetailsOverlay>(w,"objectDetailPanel");
                    var remove=Get<Button>(w,"instanceDelete");
                    var table=Get<TableLayoutPanel>(w,"instanceLayout");
                    Check(Get<FlowLayoutPanel>(w,"stateTiles").Controls.Count==states+1,"Fixture state count "+states);
                    Check(remove.Height==30,"Unexpected first delete height: "+remove.Height);
                    foreach(int width in new[]{440,340,490,440})
                    {
                        Call(w,"SetRightWidth",width);
                        Check(remove.Height==30&&remove.Bottom<=table.Height,"Delete extends beyond measured table");
                        int baseline=table.Height;
                        if(states==8)
                        {
                            var fog=Get<CheckBox>(w,"objectReveal");
                            var sight=Get<CheckBox>(w,"objectLight");
                            var radius=Get<ScrubNumber>(w,"objectLightRadius");
                            foreach(int flag in new[]{1,2,3,0})
                            {
                                fog.Checked=(flag&1)!=0;sight.Checked=(flag&2)!=0;
                                Check(table.Height==baseline,"Radius changes row height: "+flag+" "+baseline+"->"+table.Height);
                                if(sight.Checked)Check(radius.Right<=radius.Parent.ClientSize.Width&&radius.Visible,"Radius remains within row");
                            }
                            Check(!body.VerticalScroll.Visible,"Eight states unexpectedly scroll");
                        }
                        if(states==19)
                        {
                            Check(body.VerticalScroll.Visible==(table.Height+body.Padding.Vertical>body.Height),"Scrollbar does not reflect content height");
                            if(width==340)
                            {
                                var tiles=Get<FlowLayoutPanel>(w,"stateTiles");
                                var positions=tiles.Controls.Cast<Control>().Select(c=>c.Top).Distinct().ToArray();
                                Check(tiles.Controls.Count==20&&positions.Length>=5,"State tiles did not wrap: "+tiles.Controls.Count+" / "+String.Join(",",positions));
                                Console.WriteLine("19 states: body {0}, viewport {1}, scroll {2}",table.Height+body.Padding.Vertical,body.Height,body.VerticalScroll.Visible);
                                AddStates(map,25);Call(w,"UpdateUI");
                                Check(body.VerticalScroll.Visible,"Additional states must scroll when they exceed the viewport");
                                map.ObjectStates.RemoveRange(19,6);Call(w,"UpdateUI");
                            }
                        }
                    }
                    var group=map.Groups[0];
                    Call(w,"SelectGroup",group);Call(w,"SetRightWidth",340);
                    var matrix=Get<MemberMatrix>(w,"groupMatrix");
                    var columns=matrix.GetColumnWidths();
                    Check(columns.Skip(1).Distinct().Count()==1&&columns[0]>40,"Unequal columns or clipped member names: "+String.Join(",",columns));
                    var groupTable=Get<TableLayoutPanel>(w,"groupLayout");
                    int totalHeight=groupTable.Height;
                    Get<CheckBox>(w,"groupFrame").Checked=true;
                    Check(groupTable.Height==totalHeight,"Frame colour changes row height");
                    Call(w,"SelectPanelMember",map.Objects[0].Id);
                    Check(remove.Visible&&remove.Height==30,"Link back to item");
                    Call(w,"CloseDetailsPanel");Set(w,"selected",2*map.Columns+2);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");
                    Check(Get<Label>(w,"hexIcon").Text=="Ikona pola","Hex icon refers to object");
                    if(states==8)
                    {
                        var right=Get<DetailsDock>(w,"right");
                        var hexName=Get<DetailNameHeader>(w,"hexDetailName");
                        var expand=Get<Button>(w,"expandDescription");
                        var pencil=hexName.Controls.OfType<Button>().Single(button=>button.AccessibleName.EndsWith("edytuj"));
                        int capturedWidth=right.Width;
                        Call(w,"BeginRightResize",right.Left);
                        int origin=Get<int>(w,"rightResizeOrigin");
                        foreach(int dx in new[]{-8,-24,-42,-80,-114,-90,-30,36})
                        {
                            Call(w,"MoveRightResize",origin+dx);
                            Check(right.Right==w.ClientSize.Width&&pencil.Right<=hexName.ClientSize.Width&&expand.Right<=expand.Parent.ClientSize.Width,
                                "Item clipped at resize offset "+dx);
                        }
                        Call(w,"EndRightResize");
                        Check(right.Width==capturedWidth,"Panel did not return to minimum");
                        Console.WriteLine("Measured right panel minimum {0} px",capturedWidth);
                    }
                    var former=map.Objects[0].X;
                    map.Objects[0].X=-900;
                    Call(w,"MarkCellDirty",map.Cells[0]);Call(w,"UpdateUI");
                    Check(Get<Label>(w,"hexIcon").Text=="Ikona pola","Object departure changed icon");
                    map.Objects[0].X=former;
                }
            }
            BulkChoices();
            Console.WriteLine("panel PASS");
        }
        static void BulkChoices()
        {
            var map=Data();var g=map.Groups[0];var first=map.Objects[0];var second=map.Objects[1];
            var third=map.Objects[2];g.Members.Add(third.Id);
            using(var dialog=new UngroupDialog(new[]{
                new MatrixMember{Id=first.Id,Name=first.Name,Value=GroupRules.Used(g,first.Id)},
                new MatrixMember{Id=second.Id,Name=second.Name,Value=GroupRules.Used(g,second.Id)},
                new MatrixMember{Id=third.Id,Name=third.Name,Value=0}},true))
            {
                dialog.Show();Get<ComboBox>(dialog,"mode").SelectedIndex=2;
                var matrix=Get<MemberMatrix>(dialog,"matrix");
                var cells=Get<Dictionary<string,SwitchBox[]>>(matrix,"switches");
                var total=Get<SwitchBox>(matrix,"all");
                var rows=Get<Dictionary<string,SwitchBox>>(matrix,"rowTotals");
                Check(!rows[third.Id].Enabled&&!total.Checked,"Disabled or default aggregate");
                Call(matrix.GetControlFromPosition(2,0),"OnLinkClicked",new LinkLabelLinkClickedEventArgs(new LinkLabel.Link()));
                Check(cells[first.Id][1].Checked&&!total.Checked,"Bulk column");
                Call(rows[first.Id],"OnClick",EventArgs.Empty);
                Check(rows[first.Id].Checked&&cells[first.Id][2].Checked,"Bulk row");
                Call(total,"OnClick",EventArgs.Empty);
                Check(total.Checked&&rows[second.Id].Checked,"Bulk all mixed");
                cells[first.Id][2].Checked=false;
                Check(!total.Checked&&!rows[first.Id].Checked,"Aggregate after manual edit");
                Call(total,"OnClick",EventArgs.Empty);
                Call(total,"OnClick",EventArgs.Empty);
                Check(dialog.Keep.Values.All(v=>v==0),"Bulk all off");
                Call(rows[first.Id],"OnClick",EventArgs.Empty);
                var keep=dialog.Keep;
                Check(keep[first.Id]==(GroupProperty.Size|GroupProperty.Fog)&&keep[second.Id]==0,"Keep mask includes wrong properties");
                ((Button)dialog.CancelButton).PerformClick();
                Check(dialog.DialogResult==DialogResult.Cancel&&map.Groups.Count==1,"Cancel changed group");
                using(var w=new MainWindow(map,null))
                {
                    Check((bool)Call(w,"DetachObjects",g.Id,keep),"Dissolve using table");
                    Check(map.Groups.Count==0&&map.Objects[0].Size==31&&!map.Objects[0].RevealsFog&&map.Objects[1].Size==72,"Preserve outputs");
                    Call(w,"Undo");
                    Check(map.Groups.Count==1&&map.Objects[0].Size==21,"Undo dissolve");
                }
            }
        }
        static byte[] Picture(int w,int h,bool large=false)
        {
            using(var image=new Bitmap(w,h,PixelFormat.Format32bppArgb))
            using(var output=new MemoryStream())
            {
                if(large)
                {
                    var region=new Rectangle(70,200,600,1260);
                    var data=image.LockBits(region,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
                    var row=new byte[region.Width*4];var random=new Random(19);
                    for(int y=0;y<region.Height;y++)
                    {
                        random.NextBytes(row);
                        for(int a=3;a<row.Length;a+=4)row[a]=255;
                        System.Runtime.InteropServices.Marshal.Copy(row,0,IntPtr.Add(data.Scan0,y*data.Stride),row.Length);
                    }
                    image.UnlockBits(data);
                }
                using(var g=Graphics.FromImage(image))
                using(var font=new Font("Segoe UI",large?20:12))
                {
                    g.FillRectangle(Brushes.Cyan,12,12,w-24,145);
                    g.DrawString("Mapa próbna z drobnym tekstem 0.10.1",font,Brushes.Black,18,28);
                }
                image.Save(output,ImageFormat.Png);return output.ToArray();
            }
        }
        static string Rtf(byte[] png)
        {
            return @"{\rtf1\ansi Start {\*\shppict{\pict{\*\picprop{\sp{\sn fLayoutInCell}{\sv 1}}}\pngblip{\*\blipuid 001122}"
                +BitConverter.ToString(png).Replace("-","")+@"}}{\nonshppict{\pict\wmetafile8 0000}} Koniec}";
        }
        static RichNode[] Flatten(IEnumerable<RichNode> nodes){return nodes.SelectMany(n=>new[]{n}.Concat(Flatten(n.Children))).ToArray();}
        static RichNode[] Note(RichNoteEditor editor){return Flatten(NoteCodec.Read(editor.ContentJson).Blocks);}
        static void Mixed(RichNoteEditor editor,string rtf,string html)
        {
            var data=new System.Windows.DataObject();
            data.SetData(System.Windows.DataFormats.Rtf,rtf);
            data.SetData(System.Windows.DataFormats.Html,html);
            editor.SetContent(null,"");editor.PasteData(data,"Start Koniec");
            var tokens=Note(editor);
            int image=Array.FindIndex(tokens,n=>n.Kind=="image");
            Check(image>=0&&tokens.Take(image).Any(n=>(n.Text??"").Contains("Start"))&&
                tokens.Skip(image+1).Any(n=>(n.Text??"").Contains("Koniec")),"Mixed paste order");
        }
        static void Notes()
        {
            var small=Picture(380,180);
            using(var editor=new RichNoteEditor())
            {
                editor.Error=s=>{throw new Exception(s);};
                Mixed(editor,Rtf(small),"<html><img src='file:///not-a-source.png'></html>");
                string word=Path.Combine(Root,"Rich Text Format.sample");
                if(File.Exists(word))
                {
                    Mixed(editor,File.ReadAllText(word),File.ReadAllText(Path.Combine(Root,"HTML Format.sample")));
                    editor.SetContent(null,"");
                    var data=new System.Windows.DataObject();
                    data.SetData(System.Windows.DataFormats.Rtf,File.ReadAllText(Path.Combine(Root,"Picture-only.rtf.sample")));
                    editor.PasteData(data,"");
                    Check(Note(editor).Count(n=>n.Kind=="image")==1,"Word picture-only RTF");
                    Console.WriteLine("Captured Word RTF/HTML PASS");
                }
                editor.SetContent(null,"");
                var bitmapOnly=new System.Windows.DataObject();
                bitmapOnly.SetData(System.Windows.DataFormats.Html,"<html><img src='file:///not-a-source.png'></html>");
                bitmapOnly.SetData("PNG",new MemoryStream(small));
                editor.PasteData(bitmapOnly,"");
                Check(Note(editor).Count(n=>n.Kind=="image")==1,"Empty HTML intercepted PNG");
                editor.SetContent(null,"");
                var path=Path.Combine(Root,"Źródło 2800×5400.png");
                byte[] bytes=Picture(2800,5400,true);
                Check(bytes.Length>2*1024*1024&&bytes.Length<3*1024*1024,"Unexpected fixture size "+bytes.Length);
                File.WriteAllBytes(path,bytes);
                editor.ImagePathRequested=()=>path;Call(editor,"InsertImageFile");
                var node=Note(editor).Single(n=>n.Kind=="image");
                using(var stream=new MemoryStream(NoteImages.Validate(node)))
                using(var image=new Bitmap(stream))
                    Check(image.Height==2048&&image.Width==1062&&image.GetPixel(0,2047).A==0,"Large source size/alpha");
                editor.InsertTable(2,2);editor.ChangeTable("addRow");
                Check(Note(editor).Count(n=>n.Kind=="row")==3,"Table formatting path");
                var map=Data();var cell=map.Cells[0];cell.RichDescription=editor.ContentJson;
                cell.Description=NoteCodec.Plain(cell.RichDescription);cell.NotesFormat="richDocument1";
                string file=Path.Combine(Root,"Opis.hexmap");MainWindow.WriteProjectData(map,file,false);
                string transfer=Path.Combine(Root,"transfer");Directory.CreateDirectory(transfer);
                string target=Path.Combine(transfer,"Opis.hexmap");File.Move(file,target);File.Delete(path);
                var reopened=MainWindow.ReadProjectData(target).Cells[0];editor.SetContent(reopened.RichDescription,reopened.Description);
                Check(Note(editor).Single(n=>n.Kind=="image").Png==node.Png,"Independent image roundtrip");
                Console.WriteLine("Source {0} bytes, 2800 by 5400 px",bytes.Length);
            }
            var clipboard=new System.Windows.DataObject();
            clipboard.SetData("PNG",new MemoryStream(small));
            byte[] graphic=ClipboardRaster.Read(clipboard,8*1024*1024,2048,2048L*2048);
            using(var square=new SquareImportDialog(IconAssets.ReadImport(graphic),"Ikona"))
            {
                square.Show();((Button)square.CancelButton).PerformClick();
                Check(square.Result==null&&square.DialogResult==DialogResult.Cancel,"Cancelled square");
            }
            foreach(string name in new[]{"Ikona","Obiekt"})
            {
                using(var square=new SquareImportDialog(IconAssets.ReadImport(graphic),name))
                {
                    square.Show();((Button)square.AcceptButton).PerformClick();
                    using(var output=IconAssets.Decode(square.Result))
                        Check(output.Size==new Size(512,512)&&output.GetPixel(0,0).A==0,"Square "+name);
                }
            }
            var source=GraphicSources.Import(graphic);
            var terrain=new GraphicDefinition{Source=source,SourceId=source.Id,Name="Teren"};terrain.Seal();
            using(var crop=new TerrainCropDialog(terrain,false,true))
            {
                crop.Show();((Button)crop.AcceptButton).PerformClick();
                Check(crop.Result!=null&&crop.Result.SourceId==source.Id,"Hex crop source");
            }
            Console.WriteLine("notes PASS");
        }
        static void Fixture()
        {
            var map=Data();
            PresetCatalog.Write(map.Presets[0],LibraryPaths.Presets,false);
            var path=Path.Combine(Root,"Próba 0.10.1.hexmap");
            MainWindow.WriteProjectData(map,path,false);RecentProjects.Shared.Touch(path);
            string states=Path.Combine(LibraryPaths.Root,"Objects","States");
            Directory.CreateDirectory(states);
            foreach(var state in map.ObjectStates.Where(s=>!StateLibrary.IsBuiltin(s.Id)))
                File.WriteAllText(Path.Combine(states,"state-"+state.Id+".json"),MapLogic.Serializer().Serialize(state));
            File.WriteAllBytes(Path.Combine(Root,"Obraz próbny.png"),Picture(380,180));
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                LibraryPaths.EnsureDirectories(Root);
                switch(args.Single())
                {
                    case "metadata":Metadata();break;
                    case "panel":Panel();break;
                    case "notes":Notes();break;
                    case "fixture":Fixture();break;
                    default:throw new ArgumentException("Unknown Case");
                }
                return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
