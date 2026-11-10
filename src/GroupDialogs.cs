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
        readonly CheckBox update = new CheckBox { Text = "Zaktualizuj definicję na bieżącej mapie", AutoSize = true };
        public bool UpdateMap { get { return update.Checked; } }
        public Func<bool> CanAccept;

        public ObjectStateDialog(ObjectStateDefinition state = null)
            : base(state == null ? "Nowy stan obiektu" : "Edytuj stan biblioteczny", state == null ? 195 : 233)
        {
            if (state != null) { name.Text = state.Name; color = state.Color; reveal.Checked = state.CanReveal; }
            Row(0, "Nazwa", name, true);
            var button = new Button { Text = color, BackColor = ColorTranslator.FromHtml(color) };
            Row(1, "Kolor", button, true);
            button.Click += (sender, args) =>
            {
                using (var dialog = new EditorColorDialog(ColorTranslator.FromHtml(color)))
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        color = dialog.Hex;
                        button.Text = color;
                        button.BackColor = dialog.SelectedColor;
                    }
            };
            AddOption(reveal, 2);
            if (state != null) AddOption(update, 3);
            Buttons(state == null ? 3 : 4, () => { if (StateName.Length > 0 && (CanAccept == null || CanAccept())) DialogResult = DialogResult.OK; });
        }

        void AddOption(CheckBox option, int row)
        {
            Layout.Controls.Add(option, 0, row);
            Layout.SetColumnSpan(option, 3);
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        }
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
