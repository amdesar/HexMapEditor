using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class DetailCaption : Label
    {
        internal bool Contrast;
        internal DetailCaption()
        {
            AutoEllipsis=true;
            TextAlign=ContentAlignment.MiddleLeft;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if(!Contrast){base.OnPaint(e);return;}
            var bounds=ClientRectangle;
            var flags=TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding;
            var edge=ForeColor.GetBrightness()>.48f?Color.Black:Color.WhiteSmoke;
            foreach(var offset in new[] { new Point(-1,0),new Point(1,0),new Point(0,-1),new Point(0,1) })
            {
                var at=bounds;at.Offset(offset);
                TextRenderer.DrawText(e.Graphics,Text,Font,at,edge,flags);
            }
            TextRenderer.DrawText(e.Graphics,Text,Font,bounds,ForeColor,flags);
        }
    }

    internal sealed class DraftNameBox : DarkTextBox
    {
        readonly List<string> history=new List<string>();
        int cursor;
        bool restoring;
        internal bool CanStep(bool repeat)
        {
            return repeat?cursor+1<history.Count:cursor>0;
        }
        internal void ResetDraft(string value)
        {
            restoring=true;
            try { Text=value;history.Clear();history.Add(value);cursor=0;ClearUndo(); }
            finally { restoring=false; }
        }
        internal void Step(bool repeat)
        {
            if(!CanStep(repeat))return;
            restoring=true;
            try { cursor+=repeat?1:-1;Text=history[cursor];SelectionStart=Text.Length; }
            finally { restoring=false; }
        }
        protected override void OnTextChanged(EventArgs e)
        {
            if(!restoring&&history.Count>0&&history[cursor]!=Text)
            {
                history.RemoveRange(cursor+1,history.Count-cursor-1);
                history.Add(Text);
                if(history.Count>256)history.RemoveAt(0);
                cursor=history.Count-1;
            }
            base.OnTextChanged(e);
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keys)
        {
            if(keys==(Keys.Control|Keys.Z)||keys==(Keys.Control|Keys.Y))
            {
                Step(keys==(Keys.Control|Keys.Y));return true;
            }
            return base.ProcessCmdKey(ref message,keys);
        }
        protected override void WndProc(ref Message message)
        {
            // Native textbox context menu Undo.
            if(message.Msg==0x304||message.Msg==0xC7){Step(false);return;}
            base.WndProc(ref message);
        }
    }

    internal sealed class DetailNameHeader : BufferedPanel
    {
        readonly DetailCaption title = new DetailCaption { Dock=DockStyle.Fill, Margin=Padding.Empty };
        readonly Label suffix = new Label { AutoSize=true, Anchor=AnchorStyles.Left, ForeColor=Color.Silver };
        readonly DraftNameBox input = new DraftNameBox { MaxLength=200, Anchor=AnchorStyles.Left|AnchorStyles.Right };
        readonly Button pencil, accept, cancel;
        readonly Action<string> commit;
        string identity, value;
        bool editing, available, allowEmpty;

        internal DetailNameHeader(string label, Action<string> commit, ToolTip tips, bool allowEmpty=false)
        {
            this.commit=commit;this.allowEmpty=allowEmpty;
            Height=38;MinimumSize=new Size(0,38);Dock=DockStyle.Fill;
            title.Font=new Font("Segoe UI",15,FontStyle.Bold);
            Disposed+=(s,e)=>title.Font.Dispose();
            input.AccessibleName=label;
            pencil=ActionButton("",label+" — edytuj",ChoiceStyle.ActiveBorder,Begin,tips);
            pencil.Image=EditorIcons.Small("pencil");
            accept=ActionButton("✓","Zatwierdź nazwę",Color.LightGreen,Accept,tips);
            cancel=ActionButton("×","Anuluj edycję nazwy",Color.LightCoral,Cancel,tips);
            title.Dock=DockStyle.None;
            Controls.AddRange(new Control[]{title,input,suffix,pencil,accept,cancel});
            input.KeyDown+=(s,e)=>{
                if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Accept();}
                else if(e.KeyCode==Keys.Escape){e.SuppressKeyPress=true;Cancel();}
            };
            RefreshControls();
        }
        static Button ActionButton(string text,string label,Color color,Action action,ToolTip tips)
        {
            var button=new EditorButton{Text=text,AccessibleName=label,ForeColor=color,Width=28,Height=27,Anchor=AnchorStyles.Left,Margin=new Padding(1)};
            tips.SetToolTip(button,label);button.Click+=(s,e)=>action();return button;
        }
        internal void Display(string key,string raw,string heading,string coordinate,Color color,bool enabled,bool contrast=false)
        {
            if(identity!=key||!enabled)Cancel();
            identity=key;value=raw;available=enabled;title.Text=heading;title.ForeColor=color;title.Contrast=contrast;
            suffix.Text=coordinate??"";RefreshControls();
        }
        void RefreshControls()
        {
            title.Visible=!editing;input.Visible=editing;suffix.Visible=!editing&&suffix.Text.Length!=0;
            pencil.Visible=!editing&&available;accept.Visible=cancel.Visible=editing;
            PerformLayout();
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if(pencil==null||cancel==null||accept==null)return;
            int height=ClientSize.Height,right=ClientSize.Width;
            int buttonY=Math.Max(0,(height-27)/2);
            if(editing)
            {
                cancel.SetBounds(Math.Max(0,right-28),buttonY,28,27);
                accept.SetBounds(Math.Max(0,right-58),buttonY,28,27);
                input.SetBounds(1,Math.Max(0,(height-input.PreferredHeight)/2),Math.Max(1,right-62),input.PreferredHeight);
            }
            else
            {
                if(available)pencil.SetBounds(Math.Max(0,right-28),buttonY,28,27);
                int space=Math.Max(0,right-(available?32:0));
                int suffixWidth=suffix.Text.Length==0?0:Math.Min(space,TextRenderer.MeasureText(suffix.Text,suffix.Font).Width);
                int titleWidth=Math.Min(Math.Max(0,space-suffixWidth),TextRenderer.MeasureText(title.Text,title.Font,Size.Empty,TextFormatFlags.NoPadding).Width+4);
                title.SetBounds(0,0,titleWidth,height);
                suffix.SetBounds(titleWidth,Math.Max(0,(height-suffix.PreferredHeight)/2),suffixWidth,suffix.PreferredHeight);
            }
        }
        void Begin()
        {
            editing=true;input.ResetDraft(value??"");RefreshControls();input.Focus();input.SelectAll();
        }
        void Accept()
        {
            if(!editing)return;
            string proposed=allowEmpty?input.Text:input.Text.Trim();
            if(!allowEmpty&&proposed.Length==0)return;
            Cancel();commit(proposed);
        }
        internal void CancelIf(bool condition) { if(condition)Cancel(); }
        internal void Cancel()
        {
            if(!editing)return;
            editing=false;input.ClearUndo();RefreshControls();
        }
    }

    internal sealed class MatrixMember
    {
        internal string Id, Name;
        internal bool Hidden;
        internal GroupProperty Value;
    }

    internal sealed class MemberMatrix : TableLayoutPanel
    {
        readonly Dictionary<string, SwitchBox[]> switches=new Dictionary<string, SwitchBox[]>();
        readonly string actionName;
        bool syncing;
        internal Action<GroupProperty> ColumnClicked;
        internal Action<string> MemberClicked;
        internal Action<string,GroupProperty,bool> ValueChanged;

        internal MemberMatrix(string actionName)
        {
            this.actionName=actionName;
            AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;ColumnCount=6;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            foreach(int width in new[] { 42,52,42,56,54 }) ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,width));
            BackColor=Color.FromArgb(34,38,48);AccessibleName=actionName;
        }
        internal void SetMembers(MatrixMember[] members,bool keep=false)
        {
            syncing=true;SuspendLayout();
            try
            {
                foreach(Control c in Controls.Cast<Control>().ToArray())c.Dispose();
                RowStyles.Clear();switches.Clear();
                AddCell(new Label{Text="Członek",AutoEllipsis=true},0,0);
                for(int col=0;col<GroupRules.Columns.Length;col++)
                {
                    var property=GroupRules.Columns[col];
                    var head=new Label{Text=GroupRules.ColumnNames[col],TextAlign=ContentAlignment.MiddleCenter};
                    if(!keep)
                    {
                        var link=new LinkLabel{Text=head.Text,LinkColor=Color.LightSteelBlue,ActiveLinkColor=Color.White,TextAlign=ContentAlignment.MiddleCenter,
                            AccessibleName="Przełącz całą kolumnę "+head.Text};
                        link.LinkClicked+=(s,e)=>{if(ColumnClicked!=null)ColumnClicked(property);};
                        head.Dispose();head=link;
                    }
                    AddCell(head,col+1,0);
                }
                int row=0;
                foreach(var member in members)
                {
                    row++;
                    var items=new SwitchBox[5];switches.Add(member.Id,items);
                    Label name=new Label{Text=member.Name,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
                    if(!keep&&!member.Hidden)
                    {
                        var link=new LinkLabel{Text=member.Name,LinkColor=Color.LightSteelBlue,AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};
                        string id=member.Id;link.LinkClicked+=(s,e)=>{if(MemberClicked!=null)MemberClicked(id);};
                        name.Dispose();name=link;
                    }
                    AddCell(name,0,row);
                    for(int col=0;col<items.Length;col++)
                    {
                        var property=GroupRules.Columns[col];
                        var box=new SwitchBox { AccessibleName=actionName+" — "+member.Name+" — "+GroupRules.ColumnNames[col],
                            Enabled=!member.Hidden&&(!keep||(member.Value&property)!=0) };
                        if(keep&&(member.Value&property)==0){box.Dispose();AddCell(new Label(),col+1,row);continue;}
                        items[col]=box;
                        string id=member.Id;
                        box.CheckedChanged+=(s,e)=>{if(!syncing&&ValueChanged!=null)ValueChanged(id,property,box.Checked);};
                        AddCell(box,col+1,row);
                    }
                }
            }
            finally { ResumeLayout(true);syncing=false; }
        }
        void AddCell(Control c,int column,int row)
        {
            c.Margin=Padding.Empty;c.Dock=DockStyle.Fill;c.Height=25;
            c.MouseEnter+=(s,e)=>Highlight(column,row);
            c.MouseLeave+=(s,e)=>Highlight(-1,-1);
            Controls.Add(c,column,row);
        }
        void Highlight(int column,int row)
        {
            var tint=Color.FromArgb(46,56,72);
            foreach(Control c in Controls)
                c.BackColor=GetColumn(c)==column||GetRow(c)==row?tint:BackColor;
        }
        internal void SetValues(IEnumerable<MatrixMember> members)
        {
            syncing=true;
            try { foreach(var member in members) for(int i=0;i<5;i++) if(switches[member.Id][i]!=null)
                    switches[member.Id][i].Checked=!member.Hidden&&(member.Value&GroupRules.Columns[i])!=0; }
            finally { syncing=false; }
        }
        internal GroupProperty Read(string id)
        {
            GroupProperty result=0;
            for(int i=0;i<5;i++)if(switches[id][i]!=null&&switches[id][i].Checked)result|=GroupRules.Columns[i];
            return result;
        }
    }
}
