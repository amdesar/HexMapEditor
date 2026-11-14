using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class NumberEditEventArgs:EventArgs {internal readonly bool Cancelled,Changed;internal NumberEditEventArgs(bool cancel,bool changed){Cancelled=cancel;Changed=changed;}}
    // One control and transaction boundary for all application-owned numeric fields.
    internal sealed class ScrubNumber:UserControl
    {
        readonly NumberTextBox input=new NumberTextBox{BorderStyle=BorderStyle.None,TextAlign=HorizontalAlignment.Right,Visible=false,TabStop=false};
        decimal current,min,max=100,step=1,original;int precision,startX;float dragDpi=1;bool down,scrubbing,editing,finishing,hover;
        internal event EventHandler EditStarting;
        internal event EventHandler<NumberEditEventArgs> EditFinished;
        public event EventHandler ValueChanged;
        public decimal Minimum{get{return min;}set{min=value;if(max<min)max=min;Value=current;}}
        public decimal Maximum{get{return max;}set{max=value;if(min>max)min=max;Value=current;}}
        public decimal Increment{get{return step;}set{if(value<=0)throw new ArgumentOutOfRangeException();step=value;}}
        public int DecimalPlaces{get{return precision;}set{precision=Math.Max(0,Math.Min(6,value));Value=current;}}
        public int PixelsPerStep{get;set;}
        internal Func<string,decimal,decimal> TextNormalizer;
        internal Func<decimal,decimal> ValueNormalizer;
        internal string Suffix="";
        public decimal Value{get{return current;}set{decimal next=Math.Max(min,Math.Min(max,Decimal.Round(ValueNormalizer==null?value:ValueNormalizer(value),precision,MidpointRounding.AwayFromZero)));bool changed=next!=current;current=next;if(!editing||finishing)input.Text=Format();Invalidate();if(changed&&ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
        public override string Text{get{return editing?input.Text:Format();}set{if(input!=null)input.Text=value;}}
        internal bool IsEditing{get{return editing||scrubbing;}}
        internal void CancelDrag(){if(down||scrubbing){down=false;Finish(true);Capture=false;}}
        string Format(){return current.ToString("F"+precision,CultureInfo.CurrentCulture);}
        const NumberStyles InputStyle=NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingWhite|NumberStyles.AllowTrailingWhite;
        public ScrubNumber()
        {
            NumberInputRouter.Ensure();Disposed+=(s,e)=>NumberInputRouter.Leave(this);
            Size=new Size(80,26);MinimumSize=new Size(36,23);PixelsPerStep=8;TabStop=true;DoubleBuffered=true;BackColor=Color.FromArgb(41,46,58);ForeColor=Color.WhiteSmoke;Cursor=Cursors.SizeWE;
            input.BackColor=Color.FromArgb(39,55,78);input.ForeColor=ForeColor;Controls.Add(input);
            input.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){CommitText();e.Handled=e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Escape){Finish(true);e.Handled=e.SuppressKeyPress=true;}};
            input.Validating+=(s,e)=>CommitText();
            input.LostFocus+=(s,e)=>{if(editing&&!ContainsFocus)CommitText();};
            EnabledChanged+=(s,e)=>{if(!Enabled)Finish(true);Invalidate();};
            var tip=new ToolTip();tip.SetToolTip(this,"Kliknij, aby wpisać. Przeciągaj poziomo po przekroczeniu progu. Enter zatwierdza, Escape przywraca. Strzałki działają z fokusem.");Disposed+=(s,e)=>tip.Dispose();
        }
        internal static decimal Normalize(string text,decimal previous,decimal minimum,decimal maximum,int places)
        {
            if(!NumberTextBox.Draft(text)||String.IsNullOrEmpty(text)||text=="-"||text.EndsWith(".")||text.EndsWith(","))return previous;
            bool negative=text[0]=='-';string unsigned=negative?text.Substring(1):text;string[] parts=unsigned.Replace(',','.').Split('.');
            string whole=parts[0].TrimStart('0');if(whole.Length==0)whole="0";
            if(whole.Length>29)return negative?minimum:maximum;
            string fraction=parts.Length==1?"":parts[1].Substring(0,Math.Min(parts[1].Length,places+1));
            decimal parsed;if(!Decimal.TryParse((negative?"-":"")+whole+(fraction.Length==0?"":"."+fraction),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out parsed))return negative?minimum:maximum;
            return Math.Max(minimum,Math.Min(maximum,Decimal.Round(parsed,places,MidpointRounding.AwayFromZero)));
        }
        void Start(){if(editing||scrubbing)return;original=current;if(EditStarting!=null)EditStarting(this,EventArgs.Empty);}
        internal void BeginText(){if(!Enabled)return;if(editing)return;Start();editing=true;NumberInputRouter.Enter(this);input.Text=Format();input.Visible=true;input.Focus();input.SelectAll();Invalidate();}
        internal bool CommitText(){if(!editing)return true;Value=TextNormalizer==null?Normalize(input.Text,original,min,max,precision):TextNormalizer(input.Text,original);Finish(false);return true;}
        internal void Finish(bool cancel)
        {
            if(finishing||(!editing&&!scrubbing))return;finishing=true;bool changed=current!=original;
            try{if(cancel)Value=original;editing=false;NumberInputRouter.Leave(this);scrubbing=false;down=false;Capture=false;if(input.ContainsFocus){if(Parent!=null)Parent.Focus();else Focus();}input.Visible=false;input.BackColor=Color.FromArgb(39,55,78);if(EditFinished!=null)EditFinished(this,new NumberEditEventArgs(cancel,changed));}
            finally{finishing=false;Invalidate();}
        }
        internal void BeginScrub(int x){if(!Enabled)return;if(editing&&!CommitText())return;down=true;startX=x;using(var g=CreateGraphics())dragDpi=g.DpiX/96f;Focus();Capture=true;}
        internal void MoveScrub(int x){if(!down)return;int delta=x-startX;if(!scrubbing&&Math.Abs(delta)>=Math.Max(4,SystemInformation.DragSize.Width/2)){Start();scrubbing=true;}if(scrubbing)Value=original+Math.Truncate((decimal)(delta/dragDpi)/Math.Max(1,PixelsPerStep))*step;}
        internal void EndScrub(){if(!down)return;bool moved=scrubbing;down=false;Capture=false;if(moved)Finish(false);else BeginText();}
        protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left)BeginScrub(e.X);base.OnMouseDown(e);}
        protected override void OnMouseMove(MouseEventArgs e){MoveScrub(e.X);base.OnMouseMove(e);}
        protected override void OnMouseUp(MouseEventArgs e){if(e.Button==MouseButtons.Left)EndScrub();base.OnMouseUp(e);}
        protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture&&down){down=false;Finish(true);}base.OnMouseCaptureChanged(e);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnEnter(EventArgs e){base.OnEnter(e);if(!finishing&&!down&&!editing)BeginText();Invalidate();}
        protected override void OnLeave(EventArgs e){if(editing&&!ContainsFocus)CommitText();if(scrubbing)Finish(true);base.OnLeave(e);Invalidate();}
        protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);input.SetBounds(6,Math.Max(3,(Height-input.PreferredHeight)/2),Math.Max(10,Width-12-(Suffix.Length>0?14:0)),input.PreferredHeight);}
        protected override bool ProcessCmdKey(ref Message msg,Keys keys)
        {
            if(keys==Keys.Enter&&editing){CommitText();return true;}
            if((down||scrubbing)&&(keys==(Keys.Control|Keys.Z)||keys==(Keys.Control|Keys.Y)))return true;
            if(keys==Keys.Escape&&IsEditing){Finish(true);return true;}
            if(keys==Keys.Tab||keys==(Keys.Shift|Keys.Tab)){if(!CommitText())return true;return base.ProcessCmdKey(ref msg,keys);}
            if((keys==Keys.Up||keys==Keys.Down)&&ContainsFocus){if(editing&&!CommitText())return true;Start();scrubbing=true;Value=current+(keys==Keys.Up?step:-step);Finish(false);BeginText();return true;}
            return base.ProcessCmdKey(ref msg,keys);
        }
        protected override void OnMouseWheel(MouseEventArgs e){if(ContainsFocus){Start();scrubbing=true;Value=current+(e.Delta>0?step:-step);Finish(false);}base.OnMouseWheel(e);}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(!Enabled?Color.FromArgb(57,61,70):IsEditing?Color.FromArgb(89,136,183):hover?Color.FromArgb(112,123,144):Color.FromArgb(76,83,98)))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);if(!editing)TextRenderer.DrawText(e.Graphics,Format(),Font,new Rectangle(5,1,Width-10-(Suffix.Length>0?14:0),Height-2),Enabled?ForeColor:Color.Gray,TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);if(Suffix.Length>0)TextRenderer.DrawText(e.Graphics,Suffix,Font,new Rectangle(Width-17,1,14,Height-2),Enabled?ForeColor:Color.Gray,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);}
    }
}
