using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class AppearancePoolDialog : Form
    {
        readonly TextBox name = new TextBox { MaxLength = 200 };
        readonly CheckBox apply = new CheckBox { AutoSize = true };
        readonly CheckBox preserve = new CheckBox { Text = "Zachowaj rozmieszczenie wariantów", AutoSize = true, Checked = true, Enabled = false };
        readonly FlowLayoutPanel tiles = new FlowLayoutPanel { AutoScroll = true, Dock = DockStyle.Fill, Padding = new Padding(8) };
        readonly bool flat;
        readonly Dictionary<string, Bitmap> thumbnails = new Dictionary<string, Bitmap>();
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
            Text = title; this.flat = flat; Fills = FillRules.Copy(fills);
            ClientSize = new Size(470, 350); MinimumSize = new Size(440, 300);
            StartPosition = FormStartPosition.CenterParent; Font = new Font("Segoe UI", 9);
            BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.WhiteSmoke;
            var header = new Panel { Dock = DockStyle.Top, Width = ClientSize.Width, Height = initialName == null ? 30 : 66 };
            if (initialName != null)
            {
                name.Text = initialName; name.Bounds = new Rectangle(78, 10, 365, 25); name.Anchor |= AnchorStyles.Right;
                header.Controls.Add(name); header.Controls.Add(new Label { Text = "Nazwa", Location = new Point(12, 13), AutoSize = true });
            }
            header.Controls.Add(new Label { Text = "Warianty wyglądu — kliknij kafelek, aby go edytować.", Location = new Point(12, initialName == null ? 8 : 44), AutoSize = true });
            var footer = new Panel { Dock = DockStyle.Bottom, Width = ClientSize.Width, Height = 110 };
            apply.Text = applyCaption; apply.Enabled = existing; apply.Location = new Point(12, 8);
            preserve.Location = new Point(12, 33); apply.CheckedChanged += (s,e) => preserve.Enabled = apply.Checked;
            var accept = new Button { Text = "Zatwierdź", Bounds = new Rectangle(240, 72, 105, 28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            var cancel = new Button { Text = "Anuluj", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(352, 72, 105, 28), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
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
            Disposed += (s,e) => { foreach (var image in thumbnails.Values) image.Dispose(); thumbnails.Clear(); };
        }
        void Rebuild()
        {
            tiles.SuspendLayout();
            try
            {
                foreach (Control child in tiles.Controls.Cast<Control>().ToArray()) child.Dispose();
                var needed = new HashSet<string>(Fills.Where(f => f.Graphic != null).Select(f => f.GraphicId));
                foreach (var key in thumbnails.Keys.Where(k => !needed.Contains(k)).ToArray()) { thumbnails[key].Dispose(); thumbnails.Remove(key); }
                for (int i = 0; i < Fills.Count; i++)
                {
                    int index = i; var fill = Fills[i];
                    var tile = new Button { Width = 96, Height = 80, BackColor = ColorTranslator.FromHtml(fill.Color), ForeColor = Color.White,
                        Text = fill.Graphic == null ? fill.Color : "", AccessibleName = "Wariant " + (i + 1), FlatStyle = FlatStyle.Flat };
                    if (fill.Graphic != null)
                    {
                        Bitmap thumbnail;
                        if (!thumbnails.TryGetValue(fill.GraphicId, out thumbnail)) thumbnails.Add(fill.GraphicId, thumbnail = TerrainTextures.Decode(fill.Graphic, flat));
                        tile.BackgroundImage = thumbnail; tile.BackgroundImageLayout = ImageLayout.Zoom;
                    }
                    var menu = new ContextMenuStrip();
                    var remove = menu.Items.Add("Usuń wariant"); remove.Enabled = Fills.Count > 1;
                    remove.Click += (s, e) => { Fills.RemoveAt(index); Rebuild(); };
                    tile.ContextMenuStrip = menu;
                    tile.Disposed += (s, e) => menu.Dispose();
                    tile.Click += (s, e) => Edit(index);
                    tiles.Controls.Add(tile);
                }
                var add = new Button { Text = "+", Width = 60, Height = 80, Enabled = Fills.Count < GraphicLimits.Pool, AccessibleName = "Dodaj wariant koloru lub grafiki" };
                add.Click += (s, e) => Edit(-1); tiles.Controls.Add(add);
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
                if (index < 0) Fills.Add(dialog.Result); else Fills[index] = dialog.Result;
                Rebuild();
            }
        }
    }

    internal sealed class FillVariantDialog : EditorDialog
    {
        readonly ComboBox kind = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly Button color = new Button();
        readonly FlowLayoutPanel graphicButtons = new FlowLayoutPanel { WrapContents = false };
        readonly TerrainPreview preview;
        readonly FillVariant draft;
        internal Func<IWin32Window, bool, GraphicDefinition> ChooseGraphic;
        internal Func<IWin32Window, GraphicDefinition, GraphicDefinition> EditCrop;
        readonly Button crop = new Button { Text = "Kadruj…", Width = 80 };
        internal FillVariant Result { get; private set; }
        internal FillVariantDialog(FillVariant initial, bool flat) : base("Wariant wyglądu", 285)
        {
            BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.WhiteSmoke;
            draft = initial.Copy();
            kind.Items.AddRange(new object[] { "Kolor", "Grafika" }); kind.SelectedIndex = draft.Graphic == null ? 0 : 1;
            var library = new Button { Text = "Biblioteka…", Width = 88 };
            var import = new Button { Text = "Importuj…", Width = 80 };
            graphicButtons.Controls.AddRange(new Control[] { library, import, crop });
            preview = new TerrainPreview(flat) { Dock = DockStyle.Fill };
            Row(0, "Typ", kind, true); Row(1, "Kolor", color, true); Row(2, "Grafika", graphicButtons, true);
            Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Layout.Controls.Add(preview, 0, 3); Layout.SetColumnSpan(preview, 3);
            Buttons(4, () =>
            {
                if (kind.SelectedIndex == 1 && draft.Graphic == null) return;
                if (kind.SelectedIndex == 0) { draft.Graphic = null; draft.GraphicId = null; }
                Result = draft.Copy(); DialogResult = DialogResult.OK;
            });
            color.Click += (s, e) =>
            {
                using (var picker = new EditorColorDialog(ColorTranslator.FromHtml(draft.Color)))
                    if (picker.ShowDialog(this) == DialogResult.OK) { draft.Color = picker.Hex; RefreshPreview(); }
            };
            library.Click += (s, e) => Pick(false); import.Click += (s, e) => Pick(true);
            crop.Click += (s, e) =>
            {
                if (draft.Graphic == null || EditCrop == null) return;
                var next = EditCrop(this, draft.Graphic);
                if (next != null) { draft.Graphic = next; draft.GraphicId = next.Id; RefreshPreview(); }
            };
            kind.SelectedIndexChanged += (s, e) => RefreshPreview();
            RefreshPreview();
        }
        void Pick(bool import)
        {
            var next = ChooseGraphic(this, import);
            if (next != null) { draft.Graphic = next; draft.GraphicId = next.Id; RefreshPreview(); }
        }
        void RefreshPreview()
        {
            color.Text = draft.Color; color.BackColor = ColorTranslator.FromHtml(draft.Color);
            color.Enabled = kind.SelectedIndex == 0; graphicButtons.Enabled = kind.SelectedIndex == 1;
            crop.Enabled = draft.Graphic != null;
            preview.ShowFill(ColorTranslator.FromHtml(draft.Color), kind.SelectedIndex == 1 ? draft.Graphic : null);
            Ok.Enabled = kind.SelectedIndex == 0 || draft.Graphic != null;
        }
    }
}
