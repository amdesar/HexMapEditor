using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class ObjectStateDialog : EditorDialog
    {
        readonly TextBox name=new DarkTextBox{MaxLength=200};
        readonly CheckBox reveal=new SwitchBox{Text="Pozwala odkrywać mgłę",AutoSize=true};
        string color="#777F91";
        public string StateName{get{return name.Text.Trim();}}
        public string StateColor{get{return color;}}
        public bool Allows{get{return reveal.Checked;}}
        readonly CheckBox update = new SwitchBox { Text = "Zaktualizuj definicję na bieżącej mapie", AutoSize = true };
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

    internal sealed class UngroupDialog : DarkForm
    {
        readonly ComboBox mode = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, Dock=DockStyle.Top };
        readonly MemberMatrix matrix = new MemberMatrix("Zachowaj wartość wynikową");
        readonly MatrixMember[] members;
        public System.Collections.Generic.Dictionary<string,GroupProperty> Keep
        {
            get
            {
                return members.ToDictionary(m=>m.Id,m=>mode.SelectedIndex==0?GroupProperty.None:
                    mode.SelectedIndex==1?m.Value:matrix.Read(m.Id)&m.Value);
            }
        }
        public UngroupDialog(MatrixMember[] members,bool dissolve)
        {
            this.members=members;
            Text=dissolve?"Rozwiąż grupę":"Odłącz od grupy";
            StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(565,370);MinimumSize=new Size(535,260);
            var note=new Label { Dock=DockStyle.Top, Height=50, Text=(dissolve?"Wszystkie obiekty pozostaną na mapie. ":"Obiekt pozostanie na mapie. ")
                +"W tabeli zaznacz wartości wynikowe, które mają pozostać po operacji. Zmianę można cofnąć." };
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=43,FlowDirection=FlowDirection.RightToLeft};
            var ok=new EditorButton{Text=dissolve?"Rozwiąż":"Odłącz",Width=120,DialogResult=DialogResult.OK};
            var cancel=new EditorButton{Text="Anuluj",Width=90,DialogResult=DialogResult.Cancel};
            footer.Controls.AddRange(new Control[]{ok,cancel});
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
            matrix.Dock=DockStyle.Top;scroll.Controls.Add(matrix);
            Controls.Add(scroll);Controls.Add(mode);Controls.Add(note);Controls.Add(footer);
            matrix.SetMembers(members,true);
            mode.Items.AddRange(new object[]{"Przywróć wartości indywidualne","Zachowaj wartości wynikowe","Wybierz indywidualnie"});
            mode.SelectedIndexChanged+=(s,e)=>scroll.Visible=mode.SelectedIndex==2;
            mode.SelectedIndex=0;AcceptButton=ok;CancelButton=cancel;
        }
    }
}
