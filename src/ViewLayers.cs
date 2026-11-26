using System;
using System.Drawing;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal enum MapLayer { Terrain, Icons, Objects, Fog, Background, Darkness, Ink }

    public sealed partial class MainWindow
    {
        bool hideTerrain, hideIcons, hideObjects, hideDarkness, hideInk;
        ToolStripMenuItem[] layerItems, quickLayerItems;
        ToolStripDropDownButton quickLayers;
        static readonly string[] LayerNames = { "Tereny", "Ikony", "Obiekty", "Mgła", "Tło", "Ograniczenie widoczności", "Rysik" };

        bool LayerVisible(MapLayer layer)
        {
            switch (layer)
            {
                case MapLayer.Darkness: return !hideDarkness;
                case MapLayer.Ink: return !hideInk;
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
                case MapLayer.Darkness: hideDarkness=!visible; editorCellId=null; break;
                case MapLayer.Ink: hideInk=!visible;if(hideInk)ClearPointerContext(); break;
                case MapLayer.Terrain: hideTerrain = !visible; break;
                case MapLayer.Icons: hideIcons = !visible; break;
                case MapLayer.Objects: hideObjects = !visible;InvalidateLight();editorCellId=null; break;
                case MapLayer.Background: hideBackground = !visible; break;
                default: previewWithoutFog = !visible; editorCellId = null; break;
            }
            UpdateLayerChecks();
            UpdateUI();
        }

        void UpdateLayerChecks()
        {
            for (int i = 0; i < LayerNames.Length; i++)
            {
                bool visible=LayerVisible((MapLayer)i);
                if(layerItems!=null)layerItems[i].Checked=visible;
                if(quickLayerItems!=null)quickLayerItems[i].Checked=visible;
            }
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
            if(currentTool==EditorTool.Stylus||currentTool==EditorTool.Eraser)return GuardLayer(MapLayer.Ink);
            if(currentTool==EditorTool.DarkBrush||currentTool==EditorTool.DarkBucket)return GuardLayer(MapLayer.Darkness);
            if (currentTool == EditorTool.FogBrush || currentTool == EditorTool.FogBucket) return GuardLayer(MapLayer.Fog);
            if (currentTool == EditorTool.Brush && objectMaterial) return GuardLayer(MapLayer.Objects);
            if ((currentTool == EditorTool.Brush || currentTool == EditorTool.Bucket) && !noMaterial && !objectMaterial)
                return GuardLayer(iconMaterial ? MapLayer.Icons : MapLayer.Terrain);
            return true;
        }

        ToolStripMenuItem[] FillLayerMenu(ToolStripDropDown menu)
        {
            var items=new ToolStripMenuItem[LayerNames.Length];
            foreach (int i in new[] { (int)MapLayer.Background, (int)MapLayer.Terrain, (int)MapLayer.Icons, (int)MapLayer.Objects, (int)MapLayer.Darkness, (int)MapLayer.Fog, (int)MapLayer.Ink })
            {
                var layer = (MapLayer)i;
                var item = new ToolStripMenuItem(LayerNames[i]);
                item.Click += (sender, args) => { if(!BlockInkCommit())SetLayerVisible(layer,!LayerVisible(layer)); };
                items[i] = item;
                menu.Items.Add(item);
            }
            menu.Opening += (sender, args) => UpdateLayerChecks();
            menu.Closing += (sender, args) =>
            {
                if (args.CloseReason == ToolStripDropDownCloseReason.ItemClicked) args.Cancel = true;
            };
            return items;
        }
        void AddQuickLayers()
        {
            var menu=new ToolStripDropDownMenu { BackColor=shellToolbar.BackColor,ForeColor=ForeColor,Renderer=new FitMenuRenderer() };
            quickLayerItems=FillLayerMenu(menu);MenuFence(menu);
            quickLayers=new ToolStripDropDownButton("Warstwy widoku",EditorIcons.Small("layers"))
            {
                DisplayStyle=ToolStripItemDisplayStyle.Image,AccessibleName="Warstwy widoku",
                ToolTipText="Warstwy widoku — pokaż lub ukryj warstwy (Ctrl+Shift+L)",Overflow=ToolStripItemOverflow.AsNeeded,
                DropDown=menu
            };
            quickStrip.Items.Add(quickLayers);
        }

        bool HandleLayerShortcut(Keys keyData)
        {
            if(keyData!=(Keys.Control|Keys.Shift|Keys.L)||quickLayers==null||!quickLayers.Enabled)return false;
            if(quickLayers.DropDown.Visible)quickLayers.HideDropDown();
            else
            {
                if(quickLayers.Placement==ToolStripItemPlacement.Overflow)quickStrip.OverflowButton.ShowDropDown();
                quickLayers.ShowDropDown();
                quickLayerItems[(int)MapLayer.Background].Select();
            }
            return true;
        }
    }
}
