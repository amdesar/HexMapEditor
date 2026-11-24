using System;
using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class GraphicSourceDialog : DarkForm
    {
        sealed class SourceChoices : FlowLayoutPanel
        {
            internal SourceChoices(){SetStyle(ControlStyles.Selectable,true);TabStop=false;}
        }
        public bool FromLibrary { get; private set; }
        public bool FromClipboard { get; private set; }
        public GraphicSourceDialog(string title,string library)
        {
            Text=title;ClientSize=new Size(340,174);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
            var panel=new SourceChoices{Dock=DockStyle.Fill,Padding=new Padding(14),FlowDirection=FlowDirection.TopDown};Controls.Add(panel);
            var file=new Button{Text="Z pliku graficznego…",Width=306,Height=30};var copy=new Button{Text="Z biblioteki "+library+"…",Width=306,Height=30};var cancel=new Button{Text="Anuluj",Width=306,Height=30,DialogResult=DialogResult.Cancel};
            var clipboard=new Button{Text="Ze schowka",Width=306,Height=30};
            panel.Controls.AddRange(new Control[]{clipboard,file,copy,cancel});
            clipboard.Click+=(s,e)=>{FromClipboard=true;DialogResult=DialogResult.OK;};
            file.Click+=(s,e)=>DialogResult=DialogResult.OK;
            copy.Click+=(s,e)=>{FromLibrary=true;DialogResult=DialogResult.OK;};CancelButton=cancel;
            Shown+=(s,e)=>panel.Focus();
        }
    }
}
