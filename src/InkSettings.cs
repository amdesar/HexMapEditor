using System;
using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        Panel inkSettings;
        NumericUpDown inkSize;
        Button inkColorButton,pointerColorButton;
        void BuildInkSettings(Control toolbox)
        {
            inkSettings=new Panel{Visible=false,BackColor=BackColor};toolbox.Controls.Add(inkSettings);inkSettings.BringToFront();
            var label=new Label{Text="Grubość / gumka",AutoSize=true,Location=new Point(0,3)};inkSettings.Controls.Add(label);
            inkSize=new NumericUpDown{Minimum=.5m,Maximum=200,DecimalPlaces=1,Increment=.5m,Value=5,Width=68,Location=new Point(115,0),AccessibleName="Grubość rysika / średnica gumki (logiczne piksele ekranu)"};inkSettings.Controls.Add(inkSize);
            var scrub=new NumberDrag(inkSize,.5m,4){Location=new Point(187,0)};inkSettings.Controls.Add(scrub);tips.SetToolTip(scrub,"Przeciągaj poziomo: 4 px = 0,5. Bez ruchu/brak przyspieszania; wspólny rozmiar rysika i gumki.");
            inkSize.ValueChanged+=(s,e)=>{if(updating)return;EndCanvasGesture(true);inkToolSize=(float)inkSize.Value;canvas.Invalidate();};
            inkColorButton=SmallButton(inkSettings,"Kolor kreski",()=>ChooseInkColor(false),112);inkColorButton.Location=new Point(0,28);
            pointerColorButton=SmallButton(inkSettings,"Wskaźnik",()=>ChooseInkColor(true),100);pointerColorButton.AccessibleName="Kolor chwilowego wskaźnika";tips.SetToolTip(pointerColorButton,pointerColorButton.AccessibleName);
            inkSettings.Layout+=(s,e)=>{int half=inkSettings.ClientSize.Width/2;inkColorButton.SetBounds(0,28,half-2,28);pointerColorButton.SetBounds(half+2,28,inkSettings.ClientSize.Width-half-2,28);};
            tips.SetToolTip(inkSize,"Wspólny rozmiar rysika/gumki 0,5–200 logicznych px ekranu (domyślnie 5). Nowy gest przelicza go na świat z zoomu i DPI. Obrys na płótnie pokazuje rozmiar. PPM rysika: chwilowy wskaźnik; PPM gumki: wyczyść rysunki z Undo.");
            toolbox.Layout+=(s,e)=>inkSettings.SetBounds(0,86,toolbox.ClientSize.Width,68);
        }
        void UpdateInkSettings()
        {
            if(inkSettings==null)return;bool visible=currentTool==EditorTool.Stylus||currentTool==EditorTool.Eraser;inkSettings.Visible=visible;
            if(!visible)return;bool was=updating;updating=true;
            try{bool eraser=currentTool==EditorTool.Eraser;inkSize.Value=(decimal)inkToolSize;inkColorButton.Enabled=pointerColorButton.Enabled=!eraser;inkSettings.BringToFront();}finally{updating=was;}
        }
        void ChooseInkColor(bool pointer)
        {
            using(var d=new EditorColorDialog(ColorTranslator.FromHtml(pointer?pointerColor:inkColor)))if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK){if(pointer)pointerColor=d.Hex;else inkColor=d.Hex;}
        }
    }
}
