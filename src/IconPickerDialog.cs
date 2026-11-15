using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class ResourceTileButton:Button
    {
        internal Action UseResource;
        protected override void WndProc(ref Message m){if(m.Msg==0x203&&UseResource!=null){UseResource();return;}base.WndProc(ref m);}
        protected override bool ProcessCmdKey(ref Message m,Keys keys){if(keys==Keys.Enter&&UseResource!=null){UseResource();return true;}return base.ProcessCmdKey(ref m,keys);}
    }
    internal sealed class IconPickerDialog:DarkForm
    {
        readonly Func<IEnumerable<LibraryIcon>> items;readonly Func<LibraryIcon,Action,Button> makeButton;
        readonly FlowLayoutPanel list=new BufferedFlowPanel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(12)};
        readonly Button edit=new EditorButton{Text="Edytuj",Width=100,Height=28},remove=new EditorButton{Text="Usuń",Width=100,Height=28,Destructive=true};
        string selected;
        internal Func<string> Import;internal Action<LibraryIcon> Edit,Delete;
        public LibraryIcon Chosen{get;private set;}
        LibraryIcon Selected{get{return items().FirstOrDefault(i=>i.Id==selected);}}
        internal IconPickerDialog(Func<IEnumerable<LibraryIcon>> items,Func<LibraryIcon,Action,Button> makeButton,Color background)
        {
            this.items=items;this.makeButton=makeButton;Text="Biblioteka ikon — wybierz";Size=new Size(520,420);MinimumSize=new Size(460,240);StartPosition=FormStartPosition.CenterParent;BackColor=background;
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=44,Padding=new Padding(6),WrapContents=false};
            var add=new EditorButton{Text="Importuj",Width=100,Height=28};var close=new EditorButton{Text="Zamknij",Width=100,Height=28,DialogResult=DialogResult.Cancel};
            buttons.Controls.AddRange(new Control[]{add,edit,remove,close});
            Controls.Add(list);Controls.Add(new Label{Text="Zaznacz: edycja/usuwanie. Dwuklik lub Enter: użyj zasobu.",Dock=DockStyle.Top,Height=26,TextAlign=ContentAlignment.MiddleLeft});Controls.Add(buttons);CancelButton=close;
            add.Click+=(s,e)=>Run(()=>Import==null?null:Import());edit.Click+=(s,e)=>Run(()=>{var item=Selected;if(item!=null&&Edit!=null)Edit(item);return selected;});remove.Click+=(s,e)=>Run(()=>{var item=Selected;if(item!=null&&Delete!=null)Delete(item);return selected;});Reload(null);
        }
        void Run(Func<string> action){try{Reload(action());}catch(Exception ex){MessageBox.Show(this,ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);Reload(selected);}}
        internal void Reload(string id)
        {
            selected=id;list.SuspendLayout();try{foreach(Control c in list.Controls.Cast<Control>().ToArray())c.Dispose();foreach(var item in items()){
                var entry=item;var button=makeButton(entry,()=>Select(entry.Id));button.Width=88;button.Height=76;button.Text=entry.Name;button.AutoEllipsis=true;button.TextImageRelation=TextImageRelation.ImageAboveText;
                var tile=button as ResourceTileButton;if(tile!=null)tile.UseResource=()=>Use(entry.Id);button.DoubleClick+=(s,e)=>Use(entry.Id);button.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){e.Handled=e.SuppressKeyPress=true;Use(entry.Id);}};button.AccessibleDescription="Zaznacz do edycji; dwuklik lub Enter używa zasobu";list.Controls.Add(button);
            }}finally{list.ResumeLayout(true);}Select(id);
        }
        protected override bool ProcessCmdKey(ref Message m,Keys keys){if(keys==Keys.Enter&&list.ContainsFocus&&Selected!=null){Use(selected);return true;}return base.ProcessCmdKey(ref m,keys);}
        void Select(string id){selected=id;edit.Enabled=remove.Enabled=Selected!=null;foreach(Button b in list.Controls)ChoiceStyle.Apply(b,(string)b.Tag==selected);}
        internal void Use(string id){var item=items().FirstOrDefault(i=>i.Id==id);if(item==null)return;Chosen=item;DialogResult=DialogResult.OK;}
    }
    public sealed partial class MainWindow
    {
        LibraryIcon PickAssetLibrary(IWin32Window owner,bool objects)
        {
            Func<IEnumerable<LibraryIcon>> items=()=>objects?libraryObjects.Select(o=>o.Graphic()):libraryIcons;
            string priorIcon=activeIcon,priorObject=activeObject;bool priorIcons=iconMaterial,priorObjects=objectMaterial,priorEmpty=noMaterial;var priorTool=currentTool;
            using(var dialog=new IconPickerDialog(items,IconButton,BackColor)){
                if(objects)dialog.Text="Biblioteka obiektów — wybierz";
                dialog.Import=()=>{var before=new System.Collections.Generic.HashSet<string>(items().Select(i=>i.Id));if(objects)AddObjectTemplate(dialog);else ImportIcon(dialog);return items().Where(i=>!before.Contains(i.Id)).Select(i=>i.Id).FirstOrDefault()??(objects?activeObject:activeIcon);};
                dialog.Edit=item=>{if(objects){SelectObjectTemplate(item.Id);EditObjectTemplate(dialog);}else{SelectIcon(item.Id);EditLibraryIcon(dialog);}};
                dialog.Delete=item=>{if(objects){SelectObjectTemplate(item.Id);DeleteObjectTemplate(dialog);}else{SelectIcon(item.Id);DeleteLibraryIcon(dialog);}};
                if(RunModal(()=>dialog.ShowDialog(owner))==DialogResult.OK)return dialog.Chosen;
                activeIcon=priorIcon;activeObject=priorObject;iconMaterial=priorIcons&&CurrentQuick.Icons.All.Contains(priorIcon);objectMaterial=priorObjects&&CurrentQuick.Objects.All.Contains(priorObject);noMaterial=priorEmpty||priorIcons&&!iconMaterial||priorObjects&&!objectMaterial;SelectTool(priorTool);UpdateMaterialIndicator();return null;
            }
        }
        internal static string ConvertedAssetId(string sourceId,bool toObject){return (toObject?"from-icon-":"from-object-")+MapLogic.Hash(sourceId);}
        bool TryImportCounterpart(LibraryIcon source,bool toObject,out string error)
        {
            error=null;string id=ConvertedAssetId(source.Id,toObject);
            if(toObject){var found=libraryObjects.FirstOrDefault(o=>o.Id==id);if(found!=null){CurrentQuick.ObjectRefs[id]=found.Copy();UsedObject(id,true);SelectObjectTemplate(id);return true;}
                if(!TryChangeObjectTemplate(null,new ObjectTemplate{Id=id,Name=source.Name,Asset=source.Asset},false,ObjectsDirectory,out error))return false;SelectObjectTemplate(id);
            }else{var found=libraryIcons.FirstOrDefault(i=>i.Id==id);if(found!=null){CurrentQuick.IconRefs[id]=found.Copy();UsedIcon(id,true);SelectIcon(id);return true;}
                if(!TryChangeLibraryIcon(null,new LibraryIcon{Id=id,Name=source.Name,Asset=source.Asset},false,System.IO.Path.Combine(LibraryPaths.Root,"Icons"),out error))return false;SelectIcon(id);
            }return true;
        }
    }
}