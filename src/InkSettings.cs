using System;
using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        Panel inkSettings;
        ScrubNumber inkSize;
        Button inkColorButton,pointerColorButton;
        void BuildInkSettings(Control toolbox)
        {
            inkSettings=new Panel{Visible=false,BackColor=toolbox.BackColor};toolbox.Controls.Add(inkSettings);inkSettings.BringToFront();
            var label=new Label{Text="Rozmiar rysika i gumki",AutoSize=true,Location=new Point(0,3)};inkSettings.Controls.Add(label);
            inkSize=new ScrubNumber{Minimum=1,Maximum=200,DecimalPlaces=0,Increment=1,Value=5,Width=68,Location=new Point(0,24),AccessibleName="Grubość rysika / średnica gumki (logiczne piksele ekranu)"};inkSettings.Controls.Add(inkSize);
            inkSize.ValueChanged+=(s,e)=>{if(updating)return;EndCanvasGesture(true);inkToolSize=(float)inkSize.Value;canvas.Invalidate();};
            BindToolNumber(inkSize);
            inkColorButton=SmallButton(inkSettings,"Kolor rysika",()=>ChooseInkColor(false),112);inkColorButton.Location=new Point(0,28);
            pointerColorButton=SmallButton(inkSettings,"Kolor wskaźnika",()=>ChooseInkColor(true),100);pointerColorButton.AccessibleName="Kolor chwilowego wskaźnika";tips.SetToolTip(pointerColorButton,pointerColorButton.AccessibleName);
            inkSettings.Layout+=(s,e)=>{int half=inkSettings.ClientSize.Width/2;inkSize.SetBounds(0,24,Math.Max(36,(inkSettings.ClientSize.Width-8)/3),26);inkColorButton.SetBounds(0,54,half-2,28);pointerColorButton.SetBounds(half+2,54,inkSettings.ClientSize.Width-half-2,28);};
            tips.SetToolTip(inkSize,"Wspólny rozmiar rysika/gumki 1–200 logicznych px ekranu (domyślnie 5). Nowy gest przelicza go na świat z zoomu i DPI. Obrys na płótnie pokazuje rozmiar. PPM rysika: chwilowy wskaźnik; PPM gumki: wyczyść rysunki z Undo.");
            toolbox.Layout+=(s,e)=>inkSettings.SetBounds(0,86,toolbox.ClientSize.Width,86);
        }
        void UpdateInkSettings()
        {
            if(inkSettings==null)return;bool visible=currentTool==EditorTool.Stylus||currentTool==EditorTool.Eraser;inkSettings.Visible=visible;
            if(!visible)return;bool was=updating;updating=true;
            try{bool eraser=currentTool==EditorTool.Eraser;inkSize.Value=(decimal)inkToolSize;inkColorButton.Enabled=pointerColorButton.Enabled=!eraser;inkSettings.BringToFront();}finally{updating=was;}
        }
        void ChooseInkColor(bool pointer)
        {
            using(var d=new PaintPickerDialog(ColorTranslator.FromHtml(pointer?pointerColor:inkColor),pointer?pointerGradient:inkGradient,true))if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK){if(pointer){ClearPointerContext();pointerColor=d.Hex;pointerGradient=d.Gradient;pointerPalette=pointerGradient==null?null:new GradientSampler(pointerGradient);}else{inkColor=d.Hex;inkGradient=d.Gradient;}}
        }
    }
}
