using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class GraphicDuplicateDialog : DarkForm
    {
        internal GraphicDuplicateDialog(TerrainGraphic existing, GraphicDefinition proposed)
        {
            Text = "Identyczna grafika i kadry";
            ClientSize = new Size(570, 295);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(34, 38, 48); ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI", 9);
            var oldPreview = new TerrainPreview(false) { Bounds = new Rectangle(15, 50, 260, 160) };
            var newPreview = new TerrainPreview(false) { Bounds = new Rectangle(295, 50, 260, 160) };
            oldPreview.ShowFill(Color.SlateGray, existing.Asset);
            newPreview.ShowFill(Color.SlateGray, proposed);
            var use = new EditorButton { Text = "Użyj istniejącej", DialogResult = DialogResult.Yes, Bounds = new Rectangle(15, 245, 162, 32) };
            var import = new EditorButton { Text = "Importuj jako nową", DialogResult = DialogResult.No, Bounds = new Rectangle(189, 245, 181, 32) };
            var cancel = new EditorButton { Text = "Anuluj", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(382, 245, 173, 32) };
            Controls.AddRange(new Control[] { oldPreview, newPreview, use, import, cancel,
                new Label { Text = "Istniejąca: " + existing.Name, AutoEllipsis = true, Bounds = new Rectangle(15, 15, 260, 35) },
                new Label { Text = "Proponowana: " + proposed.Name, AutoEllipsis = true, Bounds = new Rectangle(295, 15, 260, 35) } });
            AcceptButton = use; CancelButton = cancel;
        }
    }
}
