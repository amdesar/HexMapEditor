using System;
using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class GraphicSourceDialog : DarkForm
    {
        public bool FromLibrary { get; private set; }
        public GraphicSourceDialog(string title,string library)
        {
            Text=title;ClientSize=new Size(340,145);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14),FlowDirection=FlowDirection.TopDown};Controls.Add(panel);
            var file=new Button{Text="Z pliku graficznego…",Width=306,Height=30};var copy=new Button{Text="Z biblioteki "+library+"…",Width=306,Height=30};var cancel=new Button{Text="Anuluj",Width=306,Height=28,DialogResult=DialogResult.Cancel};
            panel.Controls.AddRange(new Control[]{file,copy,cancel});file.Click+=(s,e)=>{FromLibrary=false;DialogResult=DialogResult.OK;};copy.Click+=(s,e)=>{FromLibrary=true;DialogResult=DialogResult.OK;};CancelButton=cancel;
        }
    }
}
