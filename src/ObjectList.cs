using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        ListViewItem ObjectListItem(MapObject obj)
        {
            double visible = FogVisibility.VisibleFraction(map, ObjectBounds(obj));
            bool masked = FogVisibility.MasksDetails(visible, previewWithoutFog);
            var effective = GroupRules.Effective(map, obj);
            var item = new ListViewItem(masked ? "???" : obj.Name) { Tag = obj.Id, ImageKey = masked ? "hidden" : obj.AssetId };
            item.SubItems.Add(masked ? "???" : map.ObjectStates.Single(s => s.Id == effective.Side).Name);
            item.SubItems.Add(masked ? "???" : ObjectGeometry.Hit(map, new PointF(obj.X, obj.Y)) < 0 ? "Poza mapą" : "");
            item.SubItems.Add(FogVisibility.Label(visible));
            return item;
        }
        void StyleInstanceList(ListView list)
        {
            list.BackColor = Color.FromArgb(34, 38, 48);
            list.ForeColor = ForeColor;
            list.OwnerDraw = true;
            list.SizeChanged += (sender, e) =>
            {
                int occupied = list.Columns[0].Width + list.Columns[1].Width + list.Columns[2].Width;
                list.Columns[3].Width = Math.Max(255, list.ClientSize.Width - occupied);
            };
            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            list.DrawColumnHeader += (sender, e) =>
            {
                using (var brush = new SolidBrush(Color.FromArgb(49, 54, 67))) e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, list.Font, Rectangle.Inflate(e.Bounds, -6, 0), list.ForeColor, flags);
            };
            list.DrawItem += (sender, e) => { }; // Details view paints each column below.
            list.DrawSubItem += (sender, e) =>
            {
                var background = e.Item.Selected ? Color.FromArgb(71, 87, 124) : list.BackColor;
                using (var brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
                var text = Rectangle.Inflate(e.Bounds, -6, 0);
                if (e.ColumnIndex == 0 && list.SmallImageList.Images.ContainsKey(e.Item.ImageKey))
                {
                    list.SmallImageList.Draw(e.Graphics, e.Bounds.Left + 3,
                        e.Bounds.Top + (e.Bounds.Height - list.SmallImageList.ImageSize.Height) / 2, list.SmallImageList.Images.IndexOfKey(e.Item.ImageKey));
                    text.X += 36;
                    text.Width -= 36;
                }
                if (text.Width > 0) TextRenderer.DrawText(e.Graphics, e.SubItem.Text, list.Font, text, list.ForeColor, flags);
            };
        }

        void OpenObjectInstances()
        {
            EndCanvasGesture(true);
            using(var dialog=new Form{Text="Instancje obiektów"+(previewWithoutFog?" — podgląd bez mgły (kolumna Mgła: stan rzeczywisty)":""),Size=new Size(735,440),MinimumSize=new Size(460,260),StartPosition=FormStartPosition.CenterParent,BackColor=BackColor,ForeColor=ForeColor})
            using(var images=new ImageList{ImageSize=new Size(36,36),ColorDepth=ColorDepth.Depth32Bit}){
                var thumbnails=new List<Bitmap>();dialog.Disposed+=(s,e)=>{foreach(var thumb in thumbnails)thumb.Dispose();};var hidden=new Bitmap(36,36);thumbnails.Add(hidden);using(var g=Graphics.FromImage(hidden)){g.Clear(Color.SlateGray);using(var f=new Font("Segoe UI",18,FontStyle.Bold))TextRenderer.DrawText(g,"?",f,new Rectangle(0,0,36,36),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);}images.Images.Add("hidden",hidden);
                var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,SmallImageList=images,HideSelection=false,ShowItemToolTips=false};list.Columns.Add("Nazwa",215);list.Columns.Add("Stan",125);list.Columns.Add("Pozycja",105);list.Columns.Add("Mgła",255); StyleInstanceList(list);
                foreach(var o in map.Objects){var item=ObjectListItem(o);if(item.ImageKey!="hidden"&&!images.Images.ContainsKey(o.AssetId)){var asset=map.IconAssets.First(a=>a.Id==o.AssetId);using(var source=IconAssets.Decode(asset)){var thumb=new Bitmap(36,36);thumbnails.Add(thumb);using(var g=Graphics.FromImage(thumb)){float f=36f/Math.Max(source.Width,source.Height);g.DrawImage(source,(36-source.Width*f)/2,(36-source.Height*f)/2,source.Width*f,source.Height*f);}images.Images.Add(o.AssetId,thumb);}}list.Items.Add(item);}
                var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=38};var show=SmallButton(bar,"Pokaż na mapie",()=>{if(list.SelectedItems.Count>0){ShowObjectOnMap((string)list.SelectedItems[0].Tag);dialog.DialogResult=DialogResult.OK;}},150);show.Enabled=false;
                SmallButton(bar,"Zaznacz bez centrowania",()=>{if(list.SelectedItems.Count>0){selectedGroupId=null;selectedObjects.Clear();selectedObjects.Add((string)list.SelectedItems[0].Tag);selected=-1;rightPanelOpen=true;UpdateUI();dialog.DialogResult=DialogResult.OK;}},190);
                var cancel=SmallButton(bar,"Zamknij",()=>dialog.Close(),90);dialog.CancelButton=cancel;list.SelectedIndexChanged+=(s,e)=>show.Enabled=list.SelectedItems.Count>0;dialog.Controls.Add(list);dialog.Controls.Add(bar);RunModal(()=>dialog.ShowDialog(this));
            }
        }
    }
}
