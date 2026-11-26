using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class BackgroundEntry
    {
        public int BackgroundFormatVersion { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public long LibraryOrder { get; set; }
        public MapBackground Settings { get; set; }
        internal string Path, SourceFile;
        internal byte[] Bytes;
        public override string ToString(){return Name;}
    }
    internal static class BackgroundLibrary
    {
        internal static BackgroundEntry Read(string path,Dictionary<string,GraphicSource> sources)
        {
            if(new FileInfo(path).Length>1024*1024)throw new InvalidDataException("Za duży plik ustawień tła.");
            var bytes=File.ReadAllBytes(path);
            var serializer=MapLogic.Serializer();var raw=serializer.DeserializeObject(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF')) as Dictionary<string,object>;
            if(raw==null||!new[]{"BackgroundFormatVersion","Id","Name","LibraryOrder","Settings"}.All(raw.ContainsKey))throw new InvalidDataException("Niepełny wpis tła.");
            var parameters=raw["Settings"] as Dictionary<string,object>;
            if(parameters==null||!new[]{"SourceId","Left","Top","Width","Height","Opacity","Saturation","Blur"}.All(parameters.ContainsKey))throw new InvalidDataException("Brak wymaganych parametrów tła.");
            var item=serializer.ConvertToType<BackgroundEntry>(raw);
            if(item==null||item.BackgroundFormatVersion!=1||String.IsNullOrWhiteSpace(item.Id)||item.Id.Length>200||String.IsNullOrWhiteSpace(item.Name)||item.Name.Length>200||item.LibraryOrder<0||item.Settings==null)throw new InvalidDataException("Nieprawidłowy wpis biblioteki teł.");
            item.SourceFile=LibraryImageStore.ReadReference(raw);
            item.Settings.Source=TerrainGraphicLibrary.ReadSource(LibraryMetadata.Catalog(path),item.Settings.SourceId,item.SourceFile,sources);
            item.Settings.LibraryId=item.Id;BackgroundRenderer.Validate(item.Settings);
            item.Path=System.IO.Path.GetFullPath(path);item.Bytes=bytes;return item;
        }
        internal static List<BackgroundEntry> Load(string directory,out string warning)
        {
            var errors=new List<string>();var sources=new Dictionary<string,GraphicSource>();
            var items=LibraryMetadata.Read(directory,"background-*.json",path=>Read(path,sources),i=>i.Id,(i,path)=>i.Path=path,
                GraphicLimits.LibraryEntries,errors,
                (entries,item)=>GraphicSources.Collect(entries.Select(i=>i.Settings.Source).Concat(new[]{item.Settings.Source}),true));
            warning=String.Join(Environment.NewLine,errors);return LibrarySequence.Sort(items,i=>i.LibraryOrder,i=>i.Id).ToList();
        }
        internal static BackgroundEntry Prepare(string directory,BackgroundEntry before,string name,MapBackground settings,IEnumerable<BackgroundEntry> items)
        {
            name=(name??"").Trim();if(name.Length==0||name.Length>200)throw new InvalidDataException("Nazwa tła: 1–200 znaków.");
            if(before==null&&items.Count()>=GraphicLimits.LibraryEntries)throw new InvalidDataException("Limit 128 aktywnych teł.");
            BackgroundRenderer.Validate(settings);GraphicSources.Collect(items.Where(i=>before==null||i.Id!=before.Id).Select(i=>i.Settings.Source).Concat(new[]{settings.Source}),true);
            var entry=new BackgroundEntry{BackgroundFormatVersion=1,Id=before==null?Guid.NewGuid().ToString("N"):before.Id,Name=name,Settings=settings.Copy(),LibraryOrder=before==null?LibrarySequence.Next(items.Select(i=>i.LibraryOrder)):before.LibraryOrder};
            entry.Settings.Source=settings.Source.Pin();entry.Settings.LibraryId=entry.Id;
            entry.Path=LibraryMetadata.Destination(directory,"background-"+PresetFiles.Stem(name),before==null?null:before.Path);
            if(!System.IO.Path.GetFileName(entry.Path).StartsWith("background-",StringComparison.Ordinal))throw new IOException("Za długa ścieżka biblioteki teł.");
            entry.SourceFile=before!=null && before.Settings.SourceId==settings.SourceId ? before.SourceFile :
                LibraryImageStore.Store(directory,name,settings.Source);
            entry.Bytes=Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(new
                { entry.BackgroundFormatVersion, entry.Id, entry.Name, entry.LibraryOrder, entry.Settings, entry.SourceFile }));
            return entry;
        }
    }
    internal sealed class BackgroundFileChange
    {
        internal BackgroundEntry Before,After;
        internal void Write(bool forward)
        {
            var from=forward?Before:After;var to=forward?After:Before;
            if(to!=null)
            {
                string catalog=LibraryMetadata.Catalog(to.Path);
                foreach(var path in LibraryMetadata.Files(catalog,"background-*.json"))
                    if((from==null||!String.Equals(path,from.Path,StringComparison.OrdinalIgnoreCase))&&LibraryMetadata.ReadIdentity(path)==to.Id)throw new IOException("Inny plik ma już ID tego tła: "+path);
                TerrainGraphicLibrary.ReadSource(catalog,to.Settings.SourceId,to.SourceFile);
            }
            LibraryFiles.Transition(from==null?null:from.Path,from==null?null:from.Bytes,to==null?null:to.Path,to==null?null:to.Bytes);
        }
    }
    internal sealed class BackgroundSaveDialog : DarkForm
    {
        internal int Choice;
        internal BackgroundSaveDialog(bool overwrite,bool applyMap=true)
        {
            Text="Zapis tła";ClientSize=new Size(385,218);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;
            BackColor=Color.FromArgb(34,38,48);ForeColor=Color.WhiteSmoke;
            Controls.Add(new Label{Text="Zastosować ustawienia i zapisać je w bibliotece?",Bounds=new Rectangle(15,12,355,30)});
            string[] names={"Zapisz jako nowe tło","Nadpisz wybrane tło","Tylko zastosuj do mapy","Wróć do edycji"};
            for(int i=0;i<names.Length;i++){int choice=i+1;var b=new EditorButton{Text=names[i],Bounds=new Rectangle(15,45+i*40,355,32),Enabled=(i!=1||overwrite)&&(i!=2||applyMap)};b.Click+=(s,e)=>{Choice=choice;DialogResult=choice==4?DialogResult.Cancel:DialogResult.OK;};Controls.Add(b);if(i==0)AcceptButton=b;if(i==3)CancelButton=b;}
        }
    }
    public sealed partial class MainWindow
    {
        List<BackgroundEntry> backgroundLibrary;
        void EnsureBackgroundLibrary(IWin32Window owner)
        {
            if(backgroundLibrary!=null)return;string warning;backgroundLibrary=BackgroundLibrary.Load(LibraryPaths.Backgrounds,out warning);
            if(warning.Length>0)MessageBox.Show(owner,warning,"Pominięte tła",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
        void ApplyBackgroundFile(BackgroundFileChange change,bool forward)
        {
            change.Write(forward);var from=forward?change.Before:change.After;var to=forward?change.After:change.Before;
            if(from!=null)backgroundLibrary.RemoveAll(i=>i.Id==from.Id);if(to!=null)backgroundLibrary.Add(to);
            backgroundLibrary=LibrarySequence.Sort(backgroundLibrary,i=>i.LibraryOrder,i=>i.Id).ToList();
        }
        bool CommitBackground(MapBackground settings,BackgroundEntry before,BackgroundEntry after,bool applyMap)
        {
            BackgroundRenderer.Validate(settings);
            bool mapChange=applyMap&&json.Serialize(settings)!=json.Serialize(map.Background);
            if(mapChange)GraphicSources.Collect(DocumentGraphics().Where(g=>g!=null).Select(g=>g.Source).Concat(settings==null?new GraphicSource[0]:new[]{settings.Source}));
            if(!mapChange&&before==null&&after==null){if(applyMap&&settings!=null&&map.Background!=null)map.Background.LibraryId=settings.LibraryId;return false;}
            EndCanvasGesture(true);
            var action=new EditAction{Label="Tło / biblioteka teł",BackgroundChange=mapChange,BeforeBackground=map.Background==null?null:map.Background.Copy(),AfterBackground=settings==null?null:settings.Copy(),BackgroundFileChange=before==null&&after==null?null:new BackgroundFileChange{Before=before,After=after}};
            ApplyAction(action,true);PushAction(action);if(mapChange)MarkDirty();else UpdateUI();return true;
        }
        bool AcceptBackground(BackgroundDialog owner,MapBackground settings,string id,bool applyMap)
        {
            try
            {
                EnsureBackgroundLibrary(owner);
                var before=backgroundLibrary.FirstOrDefault(i=>i.Id==id);
                // Unchanged acceptance never creates a duplicate library entry.
                if(settings==null||json.Serialize(settings)==json.Serialize(owner.Initial)||(before!=null&&json.Serialize(settings)==json.Serialize(before.Settings)))
                {if(applyMap)CommitBackground(settings,null,null,true);return true;}
                using(var choice=new BackgroundSaveDialog(before!=null,applyMap))
                {
                    if(choice.ShowDialog(owner)!=DialogResult.OK)return false;
                    if(choice.Choice==3){if(applyMap)CommitBackground(settings,null,null,true);return true;}
                    string name=before==null?(owner.ImportName??"Tło"):before.Name;
                    if(choice.Choice==1){using(var d=new NameDialog("Nazwa nowego tła",name)){if(d.ShowDialog(owner)!=DialogResult.OK)return false;name=d.Value;}before=null;}
                    var after=BackgroundLibrary.Prepare(LibraryPaths.Backgrounds,before,name,settings,backgroundLibrary);
                    settings=settings.Copy();settings.LibraryId=after.Id;CommitBackground(settings,before,after,applyMap);return true;
                }
            }
            catch(Exception ex){MessageBox.Show(owner,ex.Message+"\nNie zastosowano zmiany. Możesz ponowić zapis albo wybrać Tylko zastosuj do mapy.","Nie zapisano tła",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}
        }
        BackgroundEntry ChooseBackground(IWin32Window owner)
        {
            EnsureBackgroundLibrary(owner);
            using(var picker=new BackgroundPicker(()=>backgroundLibrary))
            {
                picker.Rename=entry=>{using(var d=new NameDialog("Zmień nazwę tła",entry.Name))if(d.ShowDialog(picker)==DialogResult.OK&&d.Value!=entry.Name)CommitBackground(null,entry,BackgroundLibrary.Prepare(LibraryPaths.Backgrounds,entry,d.Value,entry.Settings,backgroundLibrary),false);};
                picker.Delete=entry=>{if(MessageBox.Show(picker,"Usunąć tło „"+entry.Name+"” z biblioteki? Mapy zachowają własne dane. Bez Kosza; Undo tylko w zachowanej historii sesji (300 wpisów, do Nowa/Otwórz lub zakończenia programu).","Usuń tło z biblioteki",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes)CommitBackground(null,entry,null,false);};
                picker.Edit=entry=>{using(var d=new BackgroundDialog(entry.Settings,canvas.ClientSize)){d.LibraryOnly=true;d.ChooseLibrary=()=>ChooseBackground(d);d.SaveAndApply=(settings,id)=>AcceptBackground(d,settings,id,false);d.ShowDialog(picker);}};
                picker.ImportClipboard=()=>{
                    using(var dialog=new BackgroundDialog(null,canvas.ClientSize){LibraryOnly=true})
                    {
                        if(!dialog.ImportClipboard(picker))return;
                        dialog.ChooseLibrary=()=>ChooseBackground(dialog);
                        dialog.SaveAndApply=(settings,id)=>AcceptBackground(dialog,settings,id,false);
                        dialog.ShowDialog(picker);
                    }
                };
                return picker.ShowDialog(owner)==DialogResult.OK?picker.Selected:null;
            }
        }
    }
}
