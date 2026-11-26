using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class BackgroundPicker : DarkForm
    {
        readonly Func<List<BackgroundEntry>> entries;
        readonly ListView list=new ListView{Dock=DockStyle.Fill,View=View.LargeIcon,MultiSelect=false,HideSelection=false};
        readonly ImageList images=new ImageList{ImageSize=new Size(112,76),ColorDepth=ColorDepth.Depth32Bit};
        readonly List<Bitmap> thumbnails=new List<Bitmap>();
        internal Action<BackgroundEntry> Rename,Delete,Edit;
        internal Action ImportClipboard;
        internal BackgroundEntry Selected {get{return list.SelectedItems.Count==1?(BackgroundEntry)list.SelectedItems[0].Tag:null;}}
        internal BackgroundPicker(Func<List<BackgroundEntry>> entries)
        {
            this.entries=entries;Text="Biblioteka teł";ClientSize=new Size(620,390);MinimumSize=new Size(450,260);StartPosition=FormStartPosition.CenterParent;
            BackColor=Color.FromArgb(34,38,48);ForeColor=Color.WhiteSmoke;list.BackColor=BackColor;list.ForeColor=ForeColor;list.LargeImageList=images;
            var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(5)};
            var select=ActionButton(bar,"Wybierz",()=>{if(Selected!=null)DialogResult=DialogResult.OK;});
            var rename=ActionButton(bar,"Zmień nazwę",()=>Run(Rename));var edit=ActionButton(bar,"Edytuj / zastąp",()=>Run(Edit));var delete=ActionButton(bar,"Usuń",()=>Run(Delete));delete.Destructive=true;
            ActionButton(bar,"Importuj ze schowka",()=>{if(ImportClipboard!=null)ImportClipboard();RefreshEntries(null);});
            var close=ActionButton(bar,"Anuluj",()=>{DialogResult=DialogResult.Cancel;});CancelButton=close;AcceptButton=select;
            list.SelectedIndexChanged+=(s,e)=>select.Enabled=rename.Enabled=edit.Enabled=delete.Enabled=Selected!=null;
            list.DoubleClick+=(s,e)=>select.PerformClick();Controls.Add(list);Controls.Add(bar);RefreshEntries(null);
            select.Enabled=rename.Enabled=edit.Enabled=delete.Enabled=false;
        }
        EditorButton ActionButton(Control bar,string title,Action action){var b=new EditorButton{Text=title,AutoSize=true,Height=28};b.Click+=(s,e)=>action();bar.Controls.Add(b);return b;}
        void Run(Action<BackgroundEntry> action){var selected=Selected;if(selected==null)return;try{action(selected);RefreshEntries(selected.Id);}catch(Exception ex){MessageBox.Show(this,ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
        void RefreshEntries(string id)
        {
            list.BeginUpdate();try{list.Items.Clear();images.Images.Clear();foreach(var image in thumbnails)image.Dispose();thumbnails.Clear();foreach(var entry in entries())
            {
                var thumb=BackgroundRenderer.Render(entry.Settings,new Size(112,76),null,112);thumbnails.Add(thumb);images.Images.Add(thumb);
                var row=list.Items.Add(entry.Name,images.Images.Count-1);row.Tag=entry;if(entry.Id==id)row.Selected=true;
            }}finally{list.EndUpdate();}
        }
        protected override void Dispose(bool disposing){if(disposing){list.LargeImageList=null;images.Dispose();foreach(var image in thumbnails)image.Dispose();}base.Dispose(disposing);}
    }
}
