using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed class ObjectTemplate
    {
        public int ObjectFormatVersion {get;set;}
        public string Id {get;set;}
        public string Name {get;set;}
        public IconAsset Asset {get;set;}
        public long LibraryOrder { get; set; }
        public bool LightEnabled {get;set;}
        public int LightRadius {get;set;}
        internal string FilePath; internal byte[] DiskBytes;
        public ObjectTemplate(){ObjectFormatVersion=1;LightRadius=1;Id=Guid.NewGuid().ToString("N");}
        internal ObjectTemplate Copy(){return (ObjectTemplate)MemberwiseClone();}
        internal LibraryIcon Graphic(){return new LibraryIcon{Id=Id,Name=Name,Asset=Asset};}
    }
    internal sealed class ObjectLibraryChange
    {
        internal QuickLibraryState RemovedLists;
        public ObjectTemplate Before,After;
        public int Index;
        public void Write(bool after){var a=after?Before:After;var b=after?After:Before;var directory=Path.GetDirectoryName(a==null?b.FilePath:a.FilePath);foreach(var path in Directory.GetFiles(directory,"*.json")){if(a!=null&&String.Equals(path,a.FilePath,StringComparison.OrdinalIgnoreCase))continue;var other=ObjectLibrary.Read(path);if((a!=null&&other.Id==a.Id)||(b!=null&&other.Id==b.Id))throw new IOException("Konflikt ID wzorca obiektu: "+path);}LibraryFiles.Transition(a==null?null:a.FilePath,a==null?null:a.DiskBytes,b==null?null:b.FilePath,b==null?null:b.DiskBytes);}
    }
    internal static class ObjectLibrary
    {
        public static void Validate(ObjectTemplate o){if(o==null||o.ObjectFormatVersion!=1||String.IsNullOrWhiteSpace(o.Id)||o.Id.Length>200||String.IsNullOrWhiteSpace(o.Name)||o.Name.Length>200||o.LightRadius<0||o.LightRadius>20)throw new InvalidDataException("Nieprawidłowy wzorzec obiektu.");using(var image=IconAssets.Decode(o.Asset)){} }
        public static ObjectTemplate Read(string path){if(new FileInfo(path).Length>3*1024*1024)throw new InvalidDataException("Za duży plik obiektu.");var bytes=File.ReadAllBytes(path);var o=MapLogic.Serializer().Deserialize<ObjectTemplate>(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'));Validate(o);o.FilePath=Path.GetFullPath(path);o.DiskBytes=bytes;return o;}
        public static string Destination(string directory,string name,string current){var path=PresetFiles.Destination(directory,"object-"+PresetFiles.Stem(name),current);if(!Path.GetFileName(path).StartsWith("object-",StringComparison.Ordinal))throw new IOException("Za długa ścieżka obiektu.");return path;}
        public static List<ObjectTemplate> Load(string directory,out string warning)
        {
            Directory.CreateDirectory(directory);string marker=Path.Combine(directory,".starter-objects-v2");
            if(!File.Exists(marker)){
                foreach(var starter in StarterSets.Objects()){
                    var o=starter;
                    if(!Directory.GetFiles(directory,"*.json").Any(p=>{try{return Read(p).Id==o.Id;}catch{return false;}}))MapLogic.AtomicWrite(Destination(directory,o.Name,null),MapLogic.Serializer().Serialize(o),false);
                }MapLogic.AtomicWrite(marker,"1",false);
            }
            var result=new List<ObjectTemplate>();var errors=new List<string>();
            foreach(var path in Directory.GetFiles(directory,"*.json").OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))try{var o=Read(path);if(result.Count>=128||result.Any(p=>p.Id==o.Id))throw new InvalidDataException("Limit 128 wzorców lub powtórzone ID.");result.Add(o);}catch(Exception ex){errors.Add(Path.GetFileName(path)+": "+ex.Message);}
            warning=String.Join("\n",errors);return LibrarySequence.Sort(result, o => o.LibraryOrder, o => o.Id);
        }
    }
    public sealed partial class MainWindow
    {
        readonly List<ObjectTemplate> libraryObjects=new List<ObjectTemplate>();
        FlowLayoutPanel objectPreview;
        Button deleteObjectButton;
        string activeObject;
        bool objectMaterial,noMaterial;
        string ObjectsDirectory {get{return Path.Combine(LibraryPaths.Root,"Objects");}}
        void LoadObjects(){try{string warning;var items=ObjectLibrary.Load(ObjectsDirectory,out warning);libraryObjects.Clear();libraryObjects.AddRange(items);RefreshObjectPreview();if(warning.Length>0)ShowEditorMessage(warning,"Pominięte wzorce obiektów",MessageBoxButtons.OK,MessageBoxIcon.Warning);}catch(Exception ex){ShowEditorMessage(ex.Message,"Biblioteka obiektów",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
        LibraryIcon PickObjectGraphic(IWin32Window owner)
        {return PickAssetLibrary(owner,true);}
        void SelectObjectTemplate(string id){if(FindTemplate(id)==null)return;EndCanvasGesture(true);activeObject=id;objectMaterial=true;noMaterial=false;iconMaterial=false;MaterialChosen();UpdateMaterialIndicator();canvas.Invalidate();}
        void RefreshObjectPreview(){UpdateQuickTiles(objectPreview,true);RefreshObjectSelection();}
        void RefreshObjectSelection(){if(deleteObjectButton!=null)deleteObjectButton.Enabled=objectMaterial&&FindTemplate(activeObject)!=null;if(objectPreview==null)return;foreach(Button b in objectPreview.Controls){bool selected=objectMaterial&&(string)b.Tag==activeObject;b.FlatAppearance.BorderSize=selected?3:1;b.FlatAppearance.BorderColor=selected?ChoiceStyle.ActiveBorder:Color.Gray;}}
        void OpenObjectLibrary(){var o=PickObjectGraphic(this);if(o!=null){var template=libraryObjects.First(x=>x.Id==o.Id);CurrentQuick.ObjectRefs[o.Id]=template.Copy();SelectObjectTemplate(o.Id);}}
        void AddObjectTemplate(){AddObjectTemplate(this);}
        void AddObjectTemplate(IWin32Window owner)
        {
            using(var choice=new GraphicSourceDialog("Dodaj wzorzec obiektu","ikon")){
                if(RunModal(()=>choice.ShowDialog(owner))!=DialogResult.OK)return;
                if(!choice.FromLibrary)
                {
                    try
                    {
                        var asset=choice.FromClipboard?CropClipboardIcon(owner,"Obiekt ze schowka"):ChooseSquareGraphicFile(owner,"Dodaj wzorzec obiektu");
                        if(asset!=null)SaveObjectTemplate(null,new ObjectTemplate{Name=asset.Name,Asset=asset},false,owner);
                    }
                    catch(Exception ex){ShowLibraryMessage(owner,ex.Message,"Import obiektu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
                    return;
                }
                var graphic=PickLibraryIcon(owner);if(graphic!=null){string error;if(!TryImportCounterpart(graphic,true,out error))ShowLibraryMessage(owner,error,"Nie można dodać obiektu",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            }
        }
        void EditObjectTemplate(){EditObjectTemplate(this);}
        void EditObjectTemplate(IWin32Window owner){var before=libraryObjects.FirstOrDefault(o=>o.Id==activeObject);if(before==null||!objectMaterial){ShowLibraryMessage(owner,"Zaznacz wzorzec obiektu","Edytuj obiekt",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}using(var d=new IconEditDialog(before.Graphic(),PickObjectGraphic,PickLibraryIcon,"Edytuj wzorzec obiektu")){
                d.SetLightDefaults(before.LightEnabled,before.LightRadius);
                d.CanAccept = () => !d.UpdateMap || !map.Objects.Any(obj => obj.TemplateId == before.Id) || GuardLayer(MapLayer.Objects, d);
                if (RunModal(() => d.ShowDialog(owner)) != DialogResult.OK) return;
                var next = before.Copy();
                next.Name = d.IconName;
                next.Asset = d.Asset; next.LightEnabled=d.DefaultLight; next.LightRadius=d.DefaultLightRadius;
                SaveObjectTemplate(before, next, d.UpdateMap,owner);
            }}
        void DeleteObjectTemplate(){DeleteObjectTemplate(this);}
        void DeleteObjectTemplate(IWin32Window owner){var before=libraryObjects.FirstOrDefault(o=>o.Id==activeObject);if(before==null||!objectMaterial)return;if(ShowLibraryMessage(owner,"Usunąć wzorzec „"+before.Name+"”? Instancje na mapie pozostaną.\nBez Kosza: kopia w pamięci Undo, do 300 wpisów. Nowa/Otwórz, wypadnięcie wpisu lub zamknięcie programu usuwa możliwość cofnięcia.","Usuń wzorzec",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes)SaveObjectTemplate(before,null,false,owner);}
        void SaveObjectTemplate(ObjectTemplate before,ObjectTemplate next,bool update,IWin32Window owner){string error;if(!TryChangeObjectTemplate(before,next,update,ObjectsDirectory,out error))ShowLibraryMessage(owner,error,"Nie można zmienić wzorca",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        bool TryChangeObjectTemplate(ObjectTemplate before,ObjectTemplate next,bool update,string directory,out string error)
        { return TryChangeObjectTemplate(before,next,update,directory,true,out error); }
        bool TryChangeObjectTemplate(ObjectTemplate before,ObjectTemplate next,bool update,string directory,bool addToQuick, out string error)
        {
            EndCanvasGesture(true);error=null;try{
                directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);
                if(before!=null&&(!libraryObjects.Any(o=>o.Id==before.Id)||!String.Equals(Path.GetDirectoryName(before.FilePath),directory,StringComparison.OrdinalIgnoreCase)))throw new IOException("Wzorzec zmienił się. Wybierz ponownie.");
                if(before==null&&libraryObjects.Count>=128)throw new IOException("Limit 128 wzorców.");
                if(next!=null){next=next.Copy();next.Name=(next.Name??"").Trim();ObjectLibrary.Validate(next);if(before!=null&&before.Id!=next.Id||before==null&&libraryObjects.Any(o=>o.Id==next.Id))throw new IOException("Konflikt tożsamości wzorca.");next.FilePath=ObjectLibrary.Destination(directory,next.Name,before==null?null:before.FilePath);next.LibraryOrder = before == null ? LibrarySequence.Next(libraryObjects.Select(o => o.LibraryOrder)) : before.LibraryOrder;next.DiskBytes=new UTF8Encoding(false).GetBytes(json.Serialize(next));}
                var action=new EditAction{Label=before==null?"Dodanie wzorca":next==null?"Usunięcie wzorca":"Edycja wzorca"};
                if(update&&before!=null&&next!=null)foreach(var o in map.Objects.Where(o=>o.TemplateId==before.Id)){
                    if(o.AssetId==next.Asset.Id)continue;var after=o.Copy();after.AssetId=next.Asset.Id;action.Objects.Add(new ObjectChange{Before=o.Copy(),After=after,Index=map.Objects.IndexOf(o)});
                }
                if(action.Objects.Count>0)RequireObjectAsset(action,next.Asset);
                bool file=before==null||next==null||before.FilePath!=next.FilePath||!before.DiskBytes.SequenceEqual(next.DiskBytes);
                if(!file&&action.Objects.Count==0)return true;
                if(file)action.ObjectLibraryChange=new ObjectLibraryChange{Before=before==null?null:before.Copy(),After=next,Index=before==null?libraryObjects.Count:libraryObjects.IndexOf(before)};
                ApplyAction(action,true);if(addToQuick && before==null)AddImportedToQuick(action,next.Id,true);PushAction(action);MarkDirty();return true;
            }catch(Exception ex){error=ex.Message;return false;}
        }
        void ApplyObjectLibrary(ObjectLibraryChange change,bool after)
        {
            change.Write(after);var removed=after?change.Before:change.After;var added=after?change.After:change.Before;
            if(removed!=null)libraryObjects.RemoveAll(o=>o.Id==removed.Id);if(added!=null)libraryObjects.Insert(Math.Min(change.Index,libraryObjects.Count),added.Copy());
            if(objectMaterial&&!libraryObjects.Any(o=>o.Id==activeObject)){objectMaterial=false;noMaterial=true;activeObject=null;}
            if(added!=null)CurrentQuick.ObjectRefs[added.Id]=added.Copy();
            if(added==null&&removed!=null){if(after&&change.After==null)change.RemovedLists=CaptureLibraryQuick();RemoveQuickEntry(removed.Id,true,true);}
            if(!after&&change.After==null)RestoreLibraryQuick(change.RemovedLists);
            RefreshObjectPreview();
        }
    }
}
