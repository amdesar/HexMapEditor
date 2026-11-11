using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        void AttachFogAppearanceMenu()
        {
            foreach (var tool in new[] { EditorTool.FogBrush, EditorTool.FogBucket })
            {
                var button = toolButtons[tool];
                var menu = new ContextMenuStrip();
                menu.Items.Add("Wygląd mgły…", null, (s, e) => EditFogAppearance());
                button.ContextMenuStrip = menu;
                button.Disposed += (s, e) => menu.Dispose();
                tips.SetToolTip(button, button.AccessibleName + ". Prawy przycisk: Wygląd mgły.");
            }
        }
        void EditFogAppearance()
        {
            using (var dialog = new AppearancePoolDialog("Wygląd mgły", null, map.FogPalette, true,
                map.Orientation == "FlatTop", "Zastosuj wygląd do obecnie zasłoniętych heksów"))
            {
                dialog.ChooseGraphic = ChooseTerrainGraphic;
                dialog.EditCrop = ShowTerrainCrop;
                dialog.CanAccept = () =>
                {
                    try { CheckTextureBudget(dialog.Fills.Select(f => f.Graphic)); }
                    catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Budżet grafik"); return false; }
                    return !dialog.ApplyToMap || GuardLayer(MapLayer.Fog, dialog);
                };
                if (RunModal(() => dialog.ShowDialog(this)) != DialogResult.OK) return;
                ChangeFogAppearance(dialog.Fills, dialog.ApplyToMap);
            }
        }
        bool ChangeFogAppearance(List<FillVariant> fills, bool apply)
        {
            FillRules.ValidatePool(fills); CheckTextureBudget(fills.Select(f => f.Graphic)); EndCanvasGesture(true);
            var action = new EditAction { Label = "Wygląd mgły" };
            if (FillRules.Signature(map.FogPalette) != FillRules.Signature(fills))
            {
                action.BeforeFogPalette = FillRules.Copy(map.FogPalette); action.AfterFogPalette = FillRules.Copy(fills);
            }
            if (apply)
            {
                foreach (var cell in map.Cells.Where(c => c.Fog))
                {
                    var next = NextFill(fills);
                    if (FillRules.Signature(new[] { next }) == FillRules.Signature(new[] { cell.FogFill })) continue;
                    action.Layers.Add(new LayerChange { CellId = cell.Id, FogAppearance = true,
                        BeforeFill = cell.FogFill.Copy(), AfterFill = next });
                }
            }
            if (action.BeforeFogPalette == null && action.Layers.Count == 0) return false;
            ApplyAction(action, true); PushAction(action); MarkDirty(); return true;
        }
    }
}
