using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class ObjectListRow : ListViewItem
    {
        public int DocumentIndex;
        public double HiddenFraction;
        public Color? StateColor;
        public ObjectListRow(string name) : base(name) { }
    }

    internal sealed class ObjectListOrder : IComparer
    {
        public int Column { get; private set; }
        public int Direction { get; private set; }

        public void Advance(int column)
        {
            Direction = Column == column ? (Direction == 1 ? -1 : Direction == -1 ? 0 : 1) : 1;
            Column = column;
        }

        public int Compare(object x, object y)
        {
            var a = (ObjectListRow)x;
            var b = (ObjectListRow)y;
            if (Direction != 0)
            {
                int comparison = Column == 4
                    ? a.HiddenFraction.CompareTo(b.HiddenFraction)
                    : StringComparer.CurrentCultureIgnoreCase.Compare(a.SubItems[Column].Text, b.SubItems[Column].Text);
                if (comparison != 0) return Direction * Math.Sign(comparison);
            }
            return a.DocumentIndex.CompareTo(b.DocumentIndex);
        }
    }

    public sealed partial class MainWindow
    {
        ObjectListRow ObjectListItem(MapObject obj)
        {
            double visible = FogVisibility.VisibleFraction(map, ObjectBounds(obj));
            bool masked = FogVisibility.MasksDetails(visible, previewWithoutFog);
            var effective = GroupRules.Effective(map, obj);
            var state = map.ObjectStates.Single(s => s.Id == effective.Side);
            var group = GroupRules.For(map, obj.Id);
            var item = new ObjectListRow(masked ? "???" : obj.Name)
            {
                Tag = obj.Id, ImageKey = masked ? "hidden" : obj.AssetId,
                HiddenFraction = 1 - visible, StateColor = masked ? (Color?)null : ColorTranslator.FromHtml(state.Color)
            };
            item.SubItems.Add(masked ? "???" : state.Name);
            item.SubItems.Add(masked ? "???" : group == null ? "" : group.Name);
            item.SubItems.Add(masked ? "???" : ObjectGeometry.Hit(map, new PointF(obj.X, obj.Y)) < 0 ? "Poza mapą" : "");
            item.SubItems.Add(FogVisibility.Label(visible));
            return item;
        }

        static void DrawQuestionMarker(Graphics graphics, Rectangle bounds)
        {
            using (var background = new SolidBrush(Color.SlateGray)) graphics.FillRectangle(background, bounds);
            using (var glyph = new GraphicsPath())
            {
                glyph.AddString("?", FontFamily.GenericSansSerif, (int)FontStyle.Bold,
                    bounds.Height * .68f, PointF.Empty, StringFormat.GenericTypographic);
                var shape = glyph.GetBounds();
                using (var move = new Matrix())
                {
                    move.Translate(bounds.Left + (bounds.Width - shape.Width) / 2 - shape.Left,
                        bounds.Top + (bounds.Height - shape.Height) / 2 - shape.Top);
                    glyph.Transform(move);
                }
                graphics.FillPath(Brushes.White, glyph);
            }
        }

        void StyleInstanceList(ListView list)
        {
            list.BackColor = Color.FromArgb(34, 38, 48);
            list.ForeColor = ForeColor;
            list.OwnerDraw = true;
            var order = new ObjectListOrder();
            list.ColumnClick += (sender, e) =>
            {
                order.Advance(e.Column);
                list.ListViewItemSorter = order;
                list.Sort();
                for (int i = 0; i < list.Columns.Count; i++)
                {
                    var header = list.Columns[i];
                    header.Text = (string)header.Tag + (i != order.Column || order.Direction == 0 ? "" : order.Direction > 0 ? " ▲" : " ▼");
                }
            };
            list.SizeChanged += (sender, e) =>
            {
                int occupied = list.Columns.Cast<ColumnHeader>().Take(4).Sum(h => h.Width);
                list.Columns[4].Width = Math.Max(215, list.ClientSize.Width - occupied);
            };
            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            list.DrawColumnHeader += (sender, e) =>
            {
                using (var brush = new SolidBrush(Color.FromArgb(49, 54, 67))) e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, list.Font, Rectangle.Inflate(e.Bounds, -6, 0), list.ForeColor, flags);
            };
            list.DrawItem += (sender, e) => { }; // Details view paints each column below.
            list.DrawSubItem += (sender, e) =>
            {
                var item = (ObjectListRow)e.Item;
                var background = item.Selected ? Color.FromArgb(71, 87, 124) : list.BackColor;
                using (var brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
                var text = Rectangle.Inflate(e.Bounds, -6, 0);
                if (e.ColumnIndex == 0 && list.SmallImageList.Images.ContainsKey(item.ImageKey))
                {
                    list.SmallImageList.Draw(e.Graphics, e.Bounds.Left + 3,
                        e.Bounds.Top + (e.Bounds.Height - list.SmallImageList.ImageSize.Height) / 2, list.SmallImageList.Images.IndexOfKey(item.ImageKey));
                    text.X += 36;
                    text.Width -= 36;
                }
                if (e.ColumnIndex == 1)
                {
                    int side = Math.Min(14, e.Bounds.Height);
                    var marker = new Rectangle(text.X, text.Y + (text.Height - side) / 2, side, side);
                    if (item.StateColor.HasValue)
                        using (var brush = new SolidBrush(item.StateColor.Value)) e.Graphics.FillRectangle(brush, marker);
                    else DrawQuestionMarker(e.Graphics, marker);
                    text.X += side + 5;
                    text.Width -= side + 5;
                }
                if (text.Width > 0) TextRenderer.DrawText(e.Graphics, e.SubItem.Text, list.Font, text, list.ForeColor, flags);
            };
        }

        void OpenObjectInstances()
        {
            EndCanvasGesture(true);
            using (var dialog = new Form { Text = "Lista obiektów na mapie" + (previewWithoutFog ? " — podgląd bez mgły" : ""),
                Size = new Size(875, 440), MinimumSize = new Size(460, 260), StartPosition = FormStartPosition.CenterParent,
                BackColor = BackColor, ForeColor = ForeColor })
            using (var images = new ImageList { ImageSize = new Size(36, 36), ColorDepth = ColorDepth.Depth32Bit })
            {
                var thumbnails = new List<Bitmap>();
                dialog.Disposed += (s, e) => { foreach (var thumb in thumbnails) thumb.Dispose(); };
                var hidden = new Bitmap(36, 36);
                thumbnails.Add(hidden);
                using (var g = Graphics.FromImage(hidden))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    DrawQuestionMarker(g, new Rectangle(0, 0, 36, 36));
                }
                images.Images.Add("hidden", hidden);
                var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false,
                    SmallImageList = images, HideSelection = false, ShowItemToolTips = false };
                list.Columns.Add("Nazwa", 205);
                list.Columns.Add("Stan", 150);
                list.Columns.Add("Grupa", 145);
                list.Columns.Add("Pozycja", 100);
                list.Columns.Add("Mgła", 240);
                foreach (ColumnHeader header in list.Columns) header.Tag = header.Text;
                StyleInstanceList(list);
                foreach (var obj in map.Objects)
                {
                    var row = ObjectListItem(obj);
                    row.DocumentIndex = list.Items.Count;
                    if (row.ImageKey != "hidden" && !images.Images.ContainsKey(obj.AssetId))
                    {
                        var asset = map.IconAssets.First(a => a.Id == obj.AssetId);
                        using (var source = IconAssets.Decode(asset))
                        {
                            var thumb = new Bitmap(36, 36);
                            thumbnails.Add(thumb);
                            using (var g = Graphics.FromImage(thumb))
                            {
                                float f = 36f / Math.Max(source.Width, source.Height);
                                g.DrawImage(source, (36 - source.Width * f) / 2, (36 - source.Height * f) / 2,
                                    source.Width * f, source.Height * f);
                            }
                            images.Images.Add(obj.AssetId, thumb);
                        }
                    }
                    list.Items.Add(row);
                }
                var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38 };
                var show = SmallButton(bar, "Pokaż na mapie", () =>
                {
                    if (list.SelectedItems.Count == 0) return;
                    ShowObjectOnMap((string)list.SelectedItems[0].Tag);
                    dialog.DialogResult = DialogResult.OK;
                }, 150);
                var select = SmallButton(bar, "Zaznacz bez centrowania", () =>
                {
                    if (list.SelectedItems.Count == 0) return;
                    selectedGroupId = null;
                    selectedObjects.Clear();
                    selectedObjects.Add((string)list.SelectedItems[0].Tag);
                    selected = -1;
                    rightPanelOpen = true;
                    UpdateUI();
                    dialog.DialogResult = DialogResult.OK;
                }, 190);
                var cancel = SmallButton(bar, "Zamknij", () => dialog.Close(), 90);
                show.Enabled = select.Enabled = false;
                dialog.CancelButton = cancel;
                list.SelectedIndexChanged += (s, e) => show.Enabled = select.Enabled = list.SelectedItems.Count > 0;
                dialog.Controls.Add(list);
                dialog.Controls.Add(bar);
                RunModal(() => dialog.ShowDialog(this));
            }
        }
    }
}
