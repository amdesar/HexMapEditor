using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal enum MapLayer { Terrain, Icons, Objects, Fog, Background }

    public sealed partial class MainWindow
    {
        bool hideTerrain, hideIcons, hideObjects;
        ToolStripMenuItem[] layerItems;
        static readonly string[] LayerNames = { "Tereny", "Ikony", "Obiekty", "Mgła", "Tło" };

        bool LayerVisible(MapLayer layer)
        {
            switch (layer)
            {
                case MapLayer.Terrain: return !hideTerrain;
                case MapLayer.Icons: return !hideIcons;
                case MapLayer.Objects: return !hideObjects;
                case MapLayer.Background: return !hideBackground;
                default: return !previewWithoutFog;
            }
        }

        void SetLayerVisible(MapLayer layer, bool visible)
        {
            EndCanvasGesture(true);
            switch (layer)
            {
                case MapLayer.Terrain: hideTerrain = !visible; break;
                case MapLayer.Icons: hideIcons = !visible; break;
                case MapLayer.Objects: hideObjects = !visible; break;
                case MapLayer.Background: hideBackground = !visible; break;
                default: previewWithoutFog = !visible; editorCellId = null; break;
            }
            UpdateLayerChecks();
            UpdateUI();
        }

        void UpdateLayerChecks()
        {
            if (layerItems == null) return;
            for (int i = 0; i < layerItems.Length; i++) layerItems[i].Checked = LayerVisible((MapLayer)i);
        }

        bool GuardLayer(MapLayer layer, IWin32Window owner = null)
        {
            if (LayerVisible(layer)) return true;
            var answer = RunModal(() => MessageBox.Show(owner ?? this,
                "Warstwa „" + LayerNames[(int)layer] + "” jest ukryta. Pokazać ją? Po pokazaniu ponów operację.",
                "Ukryta warstwa", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2));
            RestoreLayerAfterPrompt(layer, answer == DialogResult.Yes);
            return false;
        }

        void RestoreLayerAfterPrompt(MapLayer layer, bool accept)
        {
            if (accept) SetLayerVisible(layer, true);
        }

        bool GuardCanvasLayer()
        {
            if (currentTool == EditorTool.FogBrush || currentTool == EditorTool.FogBucket) return GuardLayer(MapLayer.Fog);
            if (currentTool == EditorTool.Brush && objectMaterial) return GuardLayer(MapLayer.Objects);
            if ((currentTool == EditorTool.Brush || currentTool == EditorTool.Bucket) && !noMaterial && !objectMaterial)
                return GuardLayer(iconMaterial ? MapLayer.Icons : MapLayer.Terrain);
            return true;
        }

        void BuildViewMenu(Control parent)
        {
            var menu = new ContextMenuStrip { BackColor = Color.FromArgb(34, 38, 48), ForeColor = Color.WhiteSmoke,
                Renderer = new ToolStripProfessionalRenderer(new FitMenuColors()) };
            layerItems = new ToolStripMenuItem[LayerNames.Length];
            for (int i = 0; i < layerItems.Length; i++)
            {
                var layer = (MapLayer)i;
                var item = new ToolStripMenuItem(LayerNames[i]);
                item.Click += (sender, args) => SetLayerVisible(layer, !LayerVisible(layer));
                layerItems[i] = item;
                menu.Items.Add(item);
            }
            menu.Opening += (sender, args) => { EndCanvasGesture(true); UpdateLayerChecks(); };
            menu.Closing += (sender, args) =>
            {
                if (args.CloseReason == ToolStripDropDownCloseReason.ItemClicked) args.Cancel = true;
            };
            menu.Closed += (sender, args) =>
            {
                waitingForMouseRelease = Control.MouseButtons != MouseButtons.None;
                requireFreshCanvasDown = true;
            };

            var strip = new ToolStrip { AutoSize = false, Size = new Size(72, 32), Margin = new Padding(1),
                Padding = Padding.Empty, GripStyle = ToolStripGripStyle.Hidden, CanOverflow = false,
                BackColor = Color.FromArgb(49, 54, 67), Renderer = new FitButtonRenderer() };
            var button = new ToolStripDropDownButton("Widok", EditorIcons.Small("labels"))
            {
                AutoSize = false, Size = strip.Size, Margin = Padding.Empty, ShowDropDownArrow = false,
                ForeColor = ForeColor, AccessibleName = "Widok — warstwy", DropDown = menu
            };
            strip.Items.Add(button);
            parent.Controls.Add(strip);
        }
    }
}
