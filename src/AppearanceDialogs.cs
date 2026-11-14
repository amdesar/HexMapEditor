using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class AppearancePoolDialog : DarkForm
    {
        readonly TextBox name = new DarkTextBox { MaxLength = 200 };
        readonly CheckBox apply = new SwitchBox { AutoSize = true };
        readonly CheckBox preserve = new SwitchBox { Text = "Zachowaj rozmieszczenie wariantów", AutoSize = true, Checked = true, Enabled = false };
        readonly FlowLayoutPanel tiles = new FlowLayoutPanel { AutoScroll = true, Dock = DockStyle.Fill, Padding = new Padding(8) };
        readonly bool flat;
        readonly Dictionary<string, Bitmap> thumbnails = new Dictionary<string, Bitmap>();
        readonly AppearanceHistory history;
        readonly Button undoLocal = new EditorButton { Image = EditorIcons.Small("undo"), Width = 36, Height = 26, AccessibleName = "Cofnij wariant" };
        readonly Button redoLocal = new EditorButton { Image = EditorIcons.Small("redo"), Width = 36, Height = 26, AccessibleName = "Ponów wariant" };
        readonly ToolTip tips = new ToolTip();
        internal Func<IWin32Window, bool, GraphicDefinition> ChooseGraphic;
        internal Func<IWin32Window, GraphicDefinition, GraphicDefinition> EditCrop;
        internal Func<bool> CanAccept;
        internal List<FillVariant> Fills { get; private set; }
        internal string TerrainName { get { return name.Text.Trim(); } }
        internal bool ApplyToMap { get { return apply.Checked; } }
        internal bool PreserveLayout { get { return preserve.Checked; } }
        internal Point? ClosingClick;
        internal int ClosingClickTime;
        internal AppearancePoolDialog(string title, string initialName, List<FillVariant> fills, bool existing, bool flat, string applyCaption)
        {
            Text = title; this.flat = flat; history = new AppearanceHistory(fills); Fills = history.Current;
            ClientSize = new Size(470, 350); MinimumSize = new Size(440, 300);
            StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 9);
            BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.WhiteSmoke;
            var header = new Panel { Dock = DockStyle.Top, Width = ClientSize.Width, Height = initialName == null ? 64 : 100 };
            if (initialName != null)
            {
                name.Text = initialName; name.Bounds = new Rectangle(78, 10, 365, name.PreferredHeight); name.Anchor |= AnchorStyles.Right;
                header.Controls.Add(name); header.Controls.Add(new Label { Text = "Nazwa", Bounds = new Rectangle(12, 8, 62, 26), TextAlign=ContentAlignment.MiddleLeft });
            }
            header.Controls.Add(new Label { Text = "Warianty wyglądu — kliknij kafelek, aby go edytować.", Location = new Point(12, initialName == null ? 8 : 44), AutoSize = true });
            undoLocal.Location = new Point(12, initialName == null ? 32 : 68); redoLocal.Location = new Point(54, undoLocal.Top);
            tips.SetToolTip(undoLocal, "Cofnij wariant (Ctrl+Z). Lokalna historia: 100 operacji; nie cofa plików biblioteki.");
            tips.SetToolTip(redoLocal, "Ponów wariant (Ctrl+Y). Historia tylko tego edytora.");
            undoLocal.Click += (s,e) => LocalHistory(false); redoLocal.Click += (s,e) => LocalHistory(true);
            header.Controls.AddRange(new Control[] { undoLocal, redoLocal });
            var footer = new Panel { Dock = DockStyle.Bottom, Width = ClientSize.Width, Height = 110 };
            apply.Text = applyCaption; apply.Enabled = existing; apply.Location = new Point(12, 8);
            apply.AutoSize=preserve.AutoSize=false;apply.Size=new Size(446,24);preserve.SetBounds(12,33,446,24);footer.Layout+=(s,e)=>{apply.Width=preserve.Width=footer.ClientSize.Width-24;}; apply.CheckedChanged += (s,e) => preserve.Enabled = apply.Checked;
            var accept = new EditorButton { Text = "Zatwierdź", Bounds = new Rectangle(240, 72, 105, 28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            var cancel = new EditorButton { Text = "Anuluj", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(352, 72, 105, 28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            foreach (var button in new[] { accept, cancel }) button.MouseDown += (s,e) =>
            { if (e.Button == MouseButtons.Left) { ClosingClick = ((Control)s).PointToScreen(e.Location); ClosingClickTime = MainWindow.GetMessageTime(); } };
            accept.Click += (s, e) =>
            {
                if (initialName != null && TerrainName.Length == 0) return;
                if (CanAccept == null || CanAccept()) DialogResult = DialogResult.OK;
            };
            footer.Controls.AddRange(new Control[] { apply, preserve, accept, cancel });
            Controls.Add(tiles); Controls.Add(header); Controls.Add(footer);
            AcceptButton = accept; CancelButton = cancel; Rebuild();
            Disposed += (s,e) => { foreach (var image in thumbnails.Values) image.Dispose(); thumbnails.Clear(); tips.Dispose(); };
        }
        void Rebuild()
        {
            tiles.SuspendLayout();
            try
            {
                foreach (Control child in tiles.Controls.Cast<Control>().ToArray()) child.Dispose();
                var needed = new HashSet<string>(Fills.Where(f => f.Graphic != null||f.Gradient!=null).Select(f => f.Graphic!=null?f.GraphicId:"gradient:"+MapLogic.Serializer().Serialize(f.Gradient)));
                foreach (var key in thumbnails.Keys.Where(k => !needed.Contains(k)).ToArray()) { thumbnails[key].Dispose(); thumbnails.Remove(key); }
                for (int i = 0; i < Fills.Count; i++)
                {
                    int index = i; var fill = Fills[i];
                    var tile = new Panel { Width = 88, Height = 88, Margin = new Padding(4) };
                    var choose = new TerrainChoiceButton { Dock = DockStyle.Fill, BackColor = ColorTranslator.FromHtml(fill.Color), ForeColor = Color.White,
                        Text = fill.Graphic == null ? fill.Color : "", AccessibleName = "Wariant " + (i + 1), FlatStyle = FlatStyle.Flat };
                    tile.Controls.Add(choose);
                    if (fill.Gradient != null)
                    {
                        string key="gradient:"+MapLogic.Serializer().Serialize(fill.Gradient);Bitmap image;if(!thumbnails.TryGetValue(key,out image))thumbnails.Add(key,image=GradientRules.Thumbnail(fill.Gradient,new Size(88,88)));choose.FillImage=image;choose.Text="";
                    }
                    else if (fill.Graphic != null)
                    {
                        Bitmap thumbnail;
                        if (!thumbnails.TryGetValue(fill.GraphicId, out thumbnail)) thumbnails.Add(fill.GraphicId, thumbnail = TerrainTextures.Decode(fill.Graphic, flat));
                        choose.FillImage = thumbnail;
                    }
                    var close = new VariantRemoveButton { ForeColor = Color.FromArgb(255,90,90), BackColor = Color.FromArgb(25,28,36),
                        Bounds = new Rectangle(62,2,24,24), FlatStyle = FlatStyle.Flat, AccessibleName = "Usuń wariant " + (i+1), Enabled = Fills.Count > 1 };
                    close.FlatAppearance.BorderColor = Color.WhiteSmoke;
                    tips.SetToolTip(close, Fills.Count > 1 ? "Usuń wariant" : "Nie można usunąć ostatniego wariantu");
                    close.AccessibleDescription = Fills.Count > 1 ? "Usuwa tylko tę pozycję puli" : "Nie można usunąć ostatniego wariantu";
                    tips.SetToolTip(choose,Fills.Count > 1 ? "Edytuj wariant; czerwony × usuwa pozycję" : "Edytuj wariant. Nie można usunąć ostatniego wariantu.");
                    close.Click += (s,e) => RemoveVariant(index); tile.Controls.Add(close); close.BringToFront();
                    var menu = new ContextMenuStrip();
                    var remove = menu.Items.Add("Usuń wariant"); remove.Enabled = Fills.Count > 1;
                    remove.Click += (s, e) => RemoveVariant(index);
                    choose.ContextMenuStrip = menu;
                    tile.Disposed += (s, e) => menu.Dispose();
                    choose.Click += (s, e) => Edit(index);
                    tiles.Controls.Add(tile);
                }
                var add = new EditorButton { Text = "+", Width = 88, Height = 88, Margin = new Padding(4), Enabled = Fills.Count < GraphicLimits.Pool, AccessibleName = "Dodaj wariant koloru lub grafiki" };
                add.Click += (s, e) => Edit(-1); tiles.Controls.Add(add);
                undoLocal.Enabled = history.CanUndo; redoLocal.Enabled = history.CanRedo;
            }
            finally { tiles.ResumeLayout(true); }
        }
        void Edit(int index)
        {
            using (var dialog = new FillVariantDialog(index < 0 ? new FillVariant() : Fills[index], flat))
            {
                dialog.ChooseGraphic = ChooseGraphic;
                dialog.EditCrop = EditCrop;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var next = AppearanceHistory.Snapshot(Fills);
                if (index < 0) next.Add(dialog.Result); else next[index] = dialog.Result;
                CommitFills(next);
            }
        }
        internal bool CommitFills(List<FillVariant> next)
        {
            if (!history.Commit(next)) return false;
            Fills = history.Current; Rebuild(); return true;
        }
        internal bool RemoveVariant(int index)
        {
            if (Fills.Count <= 1 || index < 0 || index >= Fills.Count) return false;
            var next = AppearanceHistory.Snapshot(Fills); next.RemoveAt(index); return CommitFills(next);
        }
        internal bool LocalHistory(bool redo)
        {
            if (!history.Move(redo)) return false;
            Fills = history.Current; Rebuild(); return true;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.Z)) return true;
            bool undo = keyData == (Keys.Control | Keys.Z);
            bool redo = keyData == (Keys.Control | Keys.Y);
            if (undo || redo)
            {
                // Owned modal editors route to themselves. A focused TextBox retains native text undo.
                if (ContainsFocus && name.Focused) return base.ProcessCmdKey(ref msg, keyData);
                if (ContainsFocus && Enabled) { LocalHistory(redo); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    internal sealed class VariantRemoveButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using(var pen=new Pen(Enabled ? ForeColor : Color.Gray,2.5f))
            {
                float x=(ClientSize.Width-1)/2f,y=(ClientSize.Height-1)/2f;
                e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawLine(pen,x-4,y-4,x+4,y+4); e.Graphics.DrawLine(pen,x+4,y-4,x-4,y+4);
            }
        }
    }

    internal sealed class FillVariantDialog : EditorDialog
    {
        readonly RadioSwitch solid=new RadioSwitch{Text="Kolor"},gradient=new RadioSwitch{Text="Gradient"},graphic=new RadioSwitch{Text="Grafika"};
        readonly Button color=new EditorButton();
        readonly FlowLayoutPanel graphicButtons=new FlowLayoutPanel{WrapContents=false};
        readonly TerrainPreview preview;
        readonly FillVariant draft;
        readonly Label valueLabel;
        internal Func<IWin32Window,bool,GraphicDefinition> ChooseGraphic;
        internal Func<IWin32Window,GraphicDefinition,GraphicDefinition> EditCrop;
        readonly Button crop=new EditorButton{Text="Kadruj…",Width=80};
        internal FillVariant Result{get;private set;}
        int Kind{get{return graphic.Checked?2:gradient.Checked?1:0;}}
        internal FillVariantDialog(FillVariant initial,bool flat):base("Wariant wyglądu",300)
        {
            draft=initial.Copy();var choices=new FlowLayoutPanel{WrapContents=false};choices.Controls.AddRange(new Control[]{solid,gradient,graphic});
            var library=new EditorButton{Text="Biblioteka…",Width=88};var import=new EditorButton{Text="Importuj…",Width=80};graphicButtons.Controls.AddRange(new Control[]{import,library,crop});
            Row(0,"Typ",choices,true);var value=new Panel();value.Controls.AddRange(new Control[]{color,graphicButtons});color.Dock=graphicButtons.Dock=DockStyle.Fill;Row(1,"Kolor",value,true);valueLabel=(Label)Layout.GetControlFromPosition(0,1);
            preview=new TerrainPreview(flat){Dock=DockStyle.Fill};Layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));Layout.Controls.Add(preview,0,2);Layout.SetColumnSpan(preview,3);
            Buttons(3,()=>{if(Kind==2&&draft.Graphic==null)return;if(Kind!=2){draft.Graphic=null;draft.GraphicId=null;}if(Kind!=1)draft.Gradient=null;Result=draft.Copy();DialogResult=DialogResult.OK;});
            color.Click+=(s,e)=>{using(var picker=new PaintPickerDialog(ColorTranslator.FromHtml(draft.Color),draft.Gradient,false,flat,Kind==1))if(picker.ShowDialog(this)==DialogResult.OK){draft.Color=picker.Hex;draft.Gradient=picker.Gradient;if(picker.Gradient==null)solid.Checked=true;else gradient.Checked=true;RefreshPreview();}};
            library.Click+=(s,e)=>Pick(false);import.Click+=(s,e)=>Pick(true);crop.Click+=(s,e)=>{if(draft.Graphic==null||EditCrop==null)return;var next=EditCrop(this,draft.Graphic);if(next!=null){draft.Graphic=next;draft.GraphicId=next.Id;RefreshPreview();}};
            foreach(var option in new[]{solid,gradient,graphic})option.CheckedChanged+=(s,e)=>RefreshPreview();
            if(draft.Graphic!=null)graphic.Checked=true;else if(draft.Gradient!=null)gradient.Checked=true;else solid.Checked=true;RefreshPreview();
        }
        void Pick(bool import){if(ChooseGraphic==null)return;var next=ChooseGraphic(this,import);if(next!=null){draft.Graphic=next;draft.GraphicId=next.Id;RefreshPreview();}}
        void RefreshPreview()
        {
            if(preview==null)return;int kind=Kind;valueLabel.Text=kind==2?"Grafika":kind==1?"Gradient":"Kolor";
            if(kind==1&&draft.Gradient==null)draft.Gradient=GradientRules.Two(draft.Color);
            color.Text=kind==1?"Edytuj gradient…":draft.Color;color.BackColor=ColorTranslator.FromHtml(draft.Color);color.Visible=kind!=2;graphicButtons.Visible=kind==2;
            crop.Enabled=draft.Graphic!=null;preview.ShowFill(ColorTranslator.FromHtml(draft.Color),kind==2?draft.Graphic:null);preview.Gradient=kind==1?draft.Gradient:null;preview.Invalidate();Ok.Enabled=kind!=2||draft.Graphic!=null;
        }
    }
}