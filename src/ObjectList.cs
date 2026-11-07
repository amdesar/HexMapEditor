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
            int fog=ObjectFogLevel(obj);bool masked=!previewWithoutFog&&fog>0;var effective=GroupRules.Effective(map,obj);var item=new ListViewItem(masked?"???":obj.Name){Tag=obj.Id,ImageKey=masked?"hidden":obj.AssetId};item.SubItems.Add(masked?"???":map.ObjectStates.Single(s=>s.Id==effective.Side).Name);item.SubItems.Add(masked?"???":ObjectGeometry.Hit(map,new PointF(obj.X,obj.Y))<0?"Poza mapą":"");item.SubItems.Add(fog==0?"Widoczny":fog==1?"Częściowo ukryty":"Ukryty");return item;
        }
        void OpenObjectInstances()
        {
            EndCanvasGesture(true);
            using(var dialog=new Form{Text="Instancje obiektów"+(previewWithoutFog?" — podgląd bez mgły (kolumna Mgła: stan rzeczywisty)":""),Size=new Size(650,440),MinimumSize=new Size(460,260),StartPosition=FormStartPosition.CenterParent,BackColor=BackColor,ForeColor=ForeColor})
            using(var images=new ImageList{ImageSize=new Size(36,36),ColorDepth=ColorDepth.Depth32Bit}){
                var thumbnails=new List<Bitmap>();dialog.Disposed+=(s,e)=>{foreach(var thumb in thumbnails)thumb.Dispose();};var hidden=new Bitmap(36,36);thumbnails.Add(hidden);using(var g=Graphics.FromImage(hidden)){g.Clear(Color.SlateGray);using(var f=new Font("Segoe UI",18,FontStyle.Bold))g.DrawString("?",f,Brushes.White,8,1);}images.Images.Add("hidden",hidden);
                var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,SmallImageList=images,HideSelection=false,ShowItemToolTips=false};list.Columns.Add("Nazwa",215);list.Columns.Add("Stan",125);list.Columns.Add("Pozycja",105);list.Columns.Add("Mgła",150);
                foreach(var o in map.Objects){var item=ObjectListItem(o);if(item.ImageKey!="hidden"&&!images.Images.ContainsKey(o.AssetId)){var asset=map.IconAssets.First(a=>a.Id==o.AssetId);using(var source=IconAssets.Decode(asset)){var thumb=new Bitmap(36,36);thumbnails.Add(thumb);using(var g=Graphics.FromImage(thumb)){float f=36f/Math.Max(source.Width,source.Height);g.DrawImage(source,(36-source.Width*f)/2,(36-source.Height*f)/2,source.Width*f,source.Height*f);}images.Images.Add(o.AssetId,thumb);}}list.Items.Add(item);}
                var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=38};var show=SmallButton(bar,"Pokaż na mapie",()=>{if(list.SelectedItems.Count>0){ShowObjectOnMap((string)list.SelectedItems[0].Tag);dialog.DialogResult=DialogResult.OK;}},150);show.Enabled=false;
                SmallButton(bar,"Zaznacz bez centrowania",()=>{if(list.SelectedItems.Count>0){selectedGroupId=null;selectedObjects.Clear();selectedObjects.Add((string)list.SelectedItems[0].Tag);selected=-1;rightPanelOpen=true;UpdateUI();dialog.DialogResult=DialogResult.OK;}},190);
                var cancel=SmallButton(bar,"Zamknij",()=>dialog.Close(),90);dialog.CancelButton=cancel;list.SelectedIndexChanged+=(s,e)=>show.Enabled=list.SelectedItems.Count>0;dialog.Controls.Add(list);dialog.Controls.Add(bar);RunModal(()=>dialog.ShowDialog(this));
            }
        }
    }
}
