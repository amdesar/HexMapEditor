using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class ObjectStateDialog : EditorDialog
    {
        readonly TextBox name=new TextBox{MaxLength=200};
        readonly CheckBox reveal=new CheckBox{Text="Pozwala odkrywać mgłę",AutoSize=true};
        string color="#777F91";
        public string StateName{get{return name.Text.Trim();}}
        public string StateColor{get{return color;}}
        public bool Allows{get{return reveal.Checked;}}
        public ObjectStateDialog():base("Nowy stan obiektu",195){Row(0,"Nazwa",name,true);var button=new Button{Text=color,BackColor=ColorTranslator.FromHtml(color)};Row(1,"Kolor",button,true);button.Click+=(s,e)=>{using(var dialog=new ColorDialog{Color=ColorTranslator.FromHtml(color),FullOpen=true})if(dialog.ShowDialog(this)==DialogResult.OK){color=ColorTranslator.ToHtml(Color.FromArgb(dialog.Color.R,dialog.Color.G,dialog.Color.B));if(!MapLogic.ValidColor(color))color="#"+dialog.Color.R.ToString("X2")+dialog.Color.G.ToString("X2")+dialog.Color.B.ToString("X2");button.Text=color;button.BackColor=dialog.Color;}};Layout.Controls.Add(reveal,0,2);Layout.SetColumnSpan(reveal,3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));Buttons(3,()=>{if(StateName.Length>0)DialogResult=DialogResult.OK;});}
    }
    internal sealed class UngroupDialog : EditorDialog
    {
        readonly ComboBox mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
        readonly CheckBox[] categories;
        public bool[] Keep {get{return mode.SelectedIndex==0?Enumerable.Repeat(true,5).ToArray():mode.SelectedIndex==1?new bool[5]:categories.Select(c=>c.Checked).ToArray();}}
        public UngroupDialog(bool dissolve):base(dissolve?"Rozwiąż grupę":"Odłącz od grupy",365)
        {
            ClientSize=new Size(480,365);mode.Items.AddRange(new object[]{"Zachowaj wynikowe ustawienia","Przywróć indywidualne ustawienia","Wybierz dla każdej właściwości"});mode.SelectedIndex=1;Row(0,"Po odłączeniu",mode,true);
            categories=new[]{"Stan","Rozmiar","Blokada ruchu","Odkrywanie","Promień"}.Select(n=>new CheckBox{Text="Zachowaj wynikowe: "+n,AutoSize=true,Enabled=false}).ToArray();
            for(int i=0;i<categories.Length;i++){Layout.Controls.Add(categories[i],0,i+1);Layout.SetColumnSpan(categories[i],3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));}
            mode.SelectedIndexChanged+=(s,e)=>{foreach(var c in categories)c.Enabled=mode.SelectedIndex==2;};var note=new Label{Text="Pozycje i nazwy pozostaną. Odkryta wcześniej mgła nie zostanie zasłonięta. Całość można cofnąć.",Dock=DockStyle.Fill};Layout.Controls.Add(note,0,6);Layout.SetColumnSpan(note,3);Layout.RowStyles.Add(new RowStyle(SizeType.Absolute,70));Buttons(7,()=>DialogResult=DialogResult.OK);
        }
    }
}
