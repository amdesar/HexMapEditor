using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        Panel objectDetailPanel;
        Control hexDetailScroll;
        Label detailTitle;
        readonly Label detailIcon=new Label();
        readonly TextBox objectName=new TextBox{MaxLength=200};
        readonly ComboBox objectSide=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
        readonly NumericUpDown objectSize=new NumericUpDown{Minimum=8,Maximum=1024,Value=48},objectRadius=new NumericUpDown{Minimum=0,Maximum=20,Value=1};
        readonly CheckBox objectLocked=new CheckBox{Text="Blokada ruchu",AutoSize=true},objectReveal=new CheckBox{Text="Odkrywa mgłę",AutoSize=true};
        Label objectCount;
        string objectEditorId;
        bool clearAfterIconEdit;
        static bool TextInputFocused(Control parent){if(parent.ContainsFocus&&(parent is TextBoxBase||parent is NumericUpDown||parent is RichNoteEditor))return true;return parent.Controls.Cast<Control>().Any(TextInputFocused);}
        void ClearIconMaterial(){activeIcon=null;iconMaterial=false;objectMaterial=false;noMaterial=true;UpdateMaterialIndicator();canvas.Invalidate();}
        void UpdateBrushAvailability()
        {
            if(shapePicker==null)return;bool applicable=currentTool==EditorTool.FogBrush||currentTool==EditorTool.Brush&&!objectMaterial&&!noMaterial;
            shapePicker.Enabled=applicable;sizePicker.Enabled=applicable&&brushShape!=BrushShape.Point;heightPicker.Enabled=applicable;fillPicker.Enabled=applicable&&brushShape!=BrushShape.Point&&brushShape!=BrushShape.Horizontal&&brushShape!=BrushShape.Vertical;
        }
        void BuildObjectDetails()
        {
            objectDetailPanel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16),AutoScroll=true,Visible=false};right.Controls.Add(objectDetailPanel);objectDetailPanel.BringToFront();
            var layout=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(4)};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));objectDetailPanel.Controls.Add(layout);
            objectCount=new Label{AutoSize=true,Text="Obiekt"};layout.Controls.Add(objectCount,0,0);layout.SetColumnSpan(objectCount,2);
            objectSide.Items.AddRange(new object[]{"Sojusznik","Neutralny","Wróg"});
            Control[] inputs={objectName,objectSide,objectSize,objectLocked,objectReveal,objectRadius};string[] captions={"Nazwa","Stan","Rozmiar","Ruch","Mgła","Promień"};
            for(int i=0;i<inputs.Length;i++){layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));layout.Controls.Add(new Label{Text=captions[i],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,i+1);inputs[i].Dock=DockStyle.Fill;inputs[i].Margin=new Padding(4,5,4,5);inputs[i].AccessibleName="Obiekt: "+captions[i];layout.Controls.Add(inputs[i],1,i+1);}
            objectSide.SelectedIndexChanged+=(s,e)=>{if(!updating&&objectSide.SelectedIndex!=0)objectReveal.Checked=false;ObjectRevealControls();};objectReveal.CheckedChanged+=(s,e)=>ObjectRevealControls();
            var apply=new Button{Text="Zastosuj właściwości",Dock=DockStyle.Fill,Height=32};StyleButton(apply);layout.Controls.Add(apply,0,8);layout.SetColumnSpan(apply,2);apply.Click+=(s,e)=>{
                if(selectedObjects.Count!=1||String.IsNullOrWhiteSpace(objectName.Text))return;var obj=map.Objects.First(o=>selectedObjects.Contains(o.Id)).Copy();obj.Name=objectName.Text.Trim();obj.Side=new[]{"Ally","Neutral","Enemy"}[objectSide.SelectedIndex];obj.Size=(float)objectSize.Value;obj.Locked=objectLocked.Checked;obj.RevealsFog=objectReveal.Checked&&obj.Side=="Ally";obj.RevealRadius=(int)objectRadius.Value;ChangeObject(obj);objectEditorId=null;UpdateUI();
            };
            var help=new Label{Text="Rozmiar: najdłuższy bok grafiki w jednostkach świata (8–1024). Promień: 0–20 pierścieni; 0 odkrywa tylko heks pod środkiem. Zmiany zatwierdź przyciskiem.",AutoSize=true,MaximumSize=new Size(300,0),Margin=new Padding(4,12,4,4)};layout.Controls.Add(help,0,9);layout.SetColumnSpan(help,2);
            objectDetailPanel.Tag=apply;
        }
        void ObjectRevealControls(){bool ally=objectSide.SelectedIndex==0;objectReveal.Enabled=ally&&selectedObjects.Count==1;objectRadius.Enabled=ally&&objectReveal.Checked&&selectedObjects.Count==1;}
        void UpdateObjectDetails()
        {
            if(objectDetailPanel==null)return;objectCount.Text=selectedObjects.Count==1?"Instancja obiektu":"Wybrane obiekty: "+selectedObjects.Count;
            bool single=selectedObjects.Count==1;foreach(var c in new Control[]{objectName,objectSide,objectSize,objectLocked,objectReveal,objectRadius,(Control)objectDetailPanel.Tag})c.Enabled=single;
            if(single){var obj=map.Objects.First(o=>selectedObjects.Contains(o.Id));string key=json.Serialize(obj);if(objectEditorId!=key){objectEditorId=key;objectName.Text=obj.Name;objectSide.SelectedIndex=Array.IndexOf(new[]{"Ally","Neutral","Enemy"},obj.Side);objectSize.Value=(decimal)obj.Size;objectLocked.Checked=obj.Locked;objectReveal.Checked=obj.RevealsFog;objectRadius.Value=obj.RevealRadius;}}
            else objectEditorId=null;ObjectRevealControls();
        }
        void OpenObjectInstances()
        {
            EndCanvasGesture(true);
            using(var dialog=new Form{Text="Instancje obiektów na bieżącej mapie",Size=new Size(530,440),MinimumSize=new Size(360,260),StartPosition=FormStartPosition.CenterParent,BackColor=BackColor,ForeColor=ForeColor})
            using(var images=new ImageList{ImageSize=new Size(36,36),ColorDepth=ColorDepth.Depth32Bit}){
                var thumbnails=new System.Collections.Generic.List<Bitmap>();dialog.Disposed+=(s,e)=>{foreach(var thumb in thumbnails)thumb.Dispose();};
                var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,SmallImageList=images,HideSelection=false};list.Columns.Add("Nazwa",260);list.Columns.Add("Stan / pozycja",210);
                foreach(var o in map.Objects){var asset=map.IconAssets.First(a=>a.Id==o.AssetId);using(var source=IconAssets.Decode(asset)){var thumb=new Bitmap(36,36);thumbnails.Add(thumb);using(var g=Graphics.FromImage(thumb)){float f=36f/Math.Max(source.Width,source.Height);g.DrawImage(source,(36-source.Width*f)/2,(36-source.Height*f)/2,source.Width*f,source.Height*f);}images.Images.Add(o.Id,thumb);}var item=new ListViewItem(o.Name){Tag=o.Id,ImageKey=o.Id};item.SubItems.Add((o.Side=="Ally"?"Sojusznik":o.Side=="Enemy"?"Wróg":"Neutralny")+(ObjectGeometry.Hit(map,new PointF(o.X,o.Y))<0?" / Poza mapą":""));list.Items.Add(item);}
                var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=38};var show=SmallButton(bar,"Pokaż na mapie",()=>{if(list.SelectedItems.Count>0){ShowObjectOnMap((string)list.SelectedItems[0].Tag);dialog.DialogResult=DialogResult.OK;}},150);show.Enabled=false;
                SmallButton(bar,"Zaznacz bez centrowania",()=>{if(list.SelectedItems.Count>0){selectedObjects.Clear();selectedObjects.Add((string)list.SelectedItems[0].Tag);selected=-1;rightPanelOpen=true;UpdateUI();dialog.DialogResult=DialogResult.OK;}},190);
                var cancel=SmallButton(bar,"Zamknij",()=>dialog.Close(),90);dialog.CancelButton=cancel;list.SelectedIndexChanged+=(s,e)=>show.Enabled=list.SelectedItems.Count>0;dialog.Controls.Add(list);dialog.Controls.Add(bar);RunModal(()=>dialog.ShowDialog(this));
            }
        }
    }
}
