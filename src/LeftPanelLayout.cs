using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        const int LeftPanelWidth=276,FunctionWidth=48,FunctionHeight=26;
        const int MaxToolSection=192,PresetSection=114,MinTerrainSection=126,MaxQuickSection=120;
        const int TabsHeadingHeight=26;
        int ParameterWidth{get{return Math.Max(54,TextRenderer.MeasureText("-360",Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width+26);}}
        void LayoutFunctions(FlowLayoutPanel row)
        {int i=0;foreach(Control b in row.Controls){b.Size=new Size(FunctionWidth,FunctionHeight);b.Margin=new Padding(i==0?2:0,0,i==4?0:5,0);i++;}row.WrapContents=false;}
        void LayoutShapeParameters(Control toolbox)
        {
            if(sizeCaption==null)return;int width=toolbox.ClientSize.Width;
            foreach(Control b in paletteTools.Controls){int column=paletteTools.Controls.GetChildIndex(b)%4;b.Size=new Size(48,26);b.Margin=new Padding(0,1,column==3?0:Math.Max(0,(width-192)/3),1);}
            shapeCaption.SetBounds(0,85,52,26);shapeCaption.TextAlign=ContentAlignment.MiddleLeft;
            shapePicker.SetBounds(52,85,Math.Max(80,width-52),24);
            bool second=brushShape==BrushShape.Rectangle||brushShape==BrushShape.Triangle;
            sizeCaption.Text=brushShape==BrushShape.Rectangle?"A":brushShape==BrushShape.Triangle?"Rozm.":"Rozmiar";
            secondCaption.Text=brushShape==BrushShape.Triangle?"Kąt":"B";rotationCaption.Text="Obrót";
            var labels=second?new[]{sizeCaption,secondCaption,rotationCaption}:new[]{sizeCaption,rotationCaption};
            int total=labels.Sum(l=>TextRenderer.MeasureText(l.Text,Font,Size.Empty,TextFormatFlags.NoPadding).Width+3+ParameterWidth+5)-5;
            if(total>width){rotationCaption.Text="↻";total=labels.Sum(l=>TextRenderer.MeasureText(l.Text,Font,Size.Empty,TextFormatFlags.NoPadding).Width+3+ParameterWidth+5)-5;if(total>width&&brushShape==BrushShape.Triangle)secondCaption.Text="∠";}
            int x=0;for(int i=0;i<labels.Length;i++){
                var label=labels[i];int text=TextRenderer.MeasureText(label.Text,Font,Size.Empty,TextFormatFlags.NoPadding).Width+3;
                label.TextAlign=ContentAlignment.MiddleLeft;label.SetBounds(x,112,text,26);x+=text;
                ScrubNumber field=label==sizeCaption?sizePicker:label==rotationCaption?rotationPicker:brushShape==BrushShape.Triangle?apexPicker:heightPicker;
                field.SetBounds(x,112,ParameterWidth,26);x+=ParameterWidth+5;
            }
            fillPicker.SetBounds(0,142,Math.Min(width,fillPicker.PreferredSize.Width),22);
            tips.SetToolTip(rotationCaption,"Obrót w prawo, w stopniach");rotationCaption.AccessibleName="Obrót";
            tips.SetToolTip(secondCaption,brushShape==BrushShape.Triangle?"Kąt wierzchołkowy trójkąta":"Wysokość prostokąta B");secondCaption.AccessibleName=brushShape==BrushShape.Triangle?"Kąt wierzchołkowy":"Wysokość B";
            if(inkSettings!=null)inkSettings.SetBounds(0,86,width,62);
        }
        void LayoutQuickSections()
        {
            if(leftSections==null)return;
            if(iconPreview!=null)leftSections.RowStyles[3].Height=56+((iconPreview.Controls.Count+5)/6)*32;
            if(objectPreview!=null)leftSections.RowStyles[4].Height=56+((objectPreview.Controls.Count+5)/6)*32;
        }
        void ConfigureLeftMinimum()
        {
            int client=MaxToolSection+PresetSection+MinTerrainSection+2*MaxQuickSection+TabsHeadingHeight+shellToolbar.Height+BottomHeight;
            MinimumSize=new Size(MinimumSize.Width,SizeFromClientSize(new Size(1,client)).Height);
        }
    }
}
