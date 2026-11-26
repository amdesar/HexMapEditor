using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        void RenameSelectedHex(string name)
        {
            if(selected<0||CellMasked(selected)||BlockInkCommit())return;
            var cell=map.Cells[selected];if(cell.Name==name)return;
            var action=new EditAction{Label="Nazwa heksa",NamedCellId=cell.Id,BeforeCellName=cell.Name,AfterCellName=name};
            cell.Name=name;
            PushAction(action);MarkCellDirty(cell);UpdateUI();
        }
        void DescriptionEdited(string document, string plain, string kind)
        {
            if (updating || BlockInkCommit() || selected < 0 || CellMasked(selected)) return; var cell = map.Cells[selected];
            if (cell.RichDescription == null && document == NoteCodec.Write(NoteCodec.FromPlain(cell.Description))) return;
            CheckDescriptionBudget(cell,document,plain);
            string initial = DescriptionHistoryState(cell);
            var history = textHistory.For(cell.Id, "description", initial); history.Record("rich:" + document, kind); textHistory.Trim();
            cell.RichDescription = document; cell.Description = plain; cell.NotesFormat = "richDocument1"; MarkCellDirty(cell);
        }
        void CheckDescriptionBudget(CellData current,string rich,string plain)
        {
            NoteCodec.CheckMapBudget(map.Cells.Sum(cell=>ReferenceEquals(cell,current)?
                NoteCodec.LengthOf(rich,plain):NoteCodec.LengthOf(cell.RichDescription,cell.Description)));
        }
        void TextUndo(string field, bool repeat)
        {
            if(BlockInkCommit())return;
            if (selected < 0 || CellMasked(selected)) return; var cell = map.Cells[selected]; textHistory.BreakGroups();
            string initial = field == "name" ? cell.Name : DescriptionHistoryState(cell);
            var history = textHistory.For(cell.Id, field, initial); int before = history.Cursor;
            int target=before+(repeat?1:-1);
            if(target<0||target>=history.States.Count)return;
            string value=history.States[target];
            bool rich = value.StartsWith("rich:", StringComparison.Ordinal);
            string richText=rich?value.Substring(5):null;
            string plain=rich?NoteCodec.Plain(richText):value.Substring(6);
            try { CheckDescriptionBudget(cell,richText,plain); }
            catch (Exception ex) { ShowEditorMessage(ex.Message,"Opis heksu",MessageBoxButtons.OK,MessageBoxIcon.Warning);return; }
            if(repeat)history.Redo();else history.Undo();
            updating = true;
            try
            {
                cell.RichDescription = richText;
                cell.Description = plain;
                cell.NotesFormat = rich ? "richDocument1" : "plainText";
                cellDescription.SetContent(cell.RichDescription, cell.Description);
            }
            finally { updating = false; }
            MarkCellDirty(cell);
        }
        static string DescriptionHistoryState(CellData cell) { return cell.RichDescription == null ? "plain:" + (cell.Description ?? "") : "rich:" + cell.RichDescription; }
        void PushAction(EditAction action)
        {
            documentRevision++; undo.Push(action); if (undo.Count > 300) { var keep = undo.Reverse().Skip(1).ToArray(); undo.Clear(); foreach (var item in keep) undo.Push(item); } redo.Clear();
        }
        void ApplyAction(EditAction action, bool after)
        {
            var fromPalette=after?action.BeforePalette:action.AfterPalette;
            var toPalette=after?action.AfterPalette:action.BeforePalette;
            CheckSharedPaletteHistory(fromPalette,toPalette);
            CheckSharedQuickHistory(action.QuickPresetId,after?action.BeforeQuick:action.AfterQuick,action.QuickSections);
            // Nothing in memory or on either stack changes before the checked disk transition.
            if(action.BackgroundFileChange!=null)ApplyBackgroundFile(action.BackgroundFileChange,after);
            if (action.LibraryChange != null) ApplyIconLibrary(action.LibraryChange, after);
            if (action.PresetChange != null) ApplyPresetLibrary(action.PresetChange, after);
            if (action.ObjectLibraryChange != null) ApplyObjectLibrary(action.ObjectLibraryChange, after);
            if (action.StateFileChange != null) ApplyStateFile(action.StateFileChange, after);
            if (action.TerrainGraphicChange != null) ApplyTerrainGraphic(action.TerrainGraphicChange, after);
            if(action.BeforeInk!=null)map.Ink=new List<InkStroke>(after?action.AfterInk:action.BeforeInk);
            if(action.BeforeDarkPalette!=null)map.DarkPalette=FillRules.Copy(after?action.AfterDarkPalette:action.BeforeDarkPalette);
            if (action.BeforeFogPalette != null) map.FogPalette = FillRules.Copy(after ? action.AfterFogPalette : action.BeforeFogPalette);
            if (action.BackgroundChange)
            {
                var background = after ? action.AfterBackground : action.BeforeBackground;
                map.Background = background == null ? null : background.Copy(); ClearBackgroundCache();
            }
            foreach (var asset in action.RequiredAssets) if (!map.IconAssets.Any(a => a.Id == asset.Id)) map.IconAssets.Add(asset);
            string selectedId = selected < 0 ? null : map.Cells[selected].Id;
            if (action.BeforeResize != null) (after ? action.AfterResize : action.BeforeResize).Apply(map);
            if (action.BeforeOrientation != null) map.Orientation=after?action.AfterOrientation:action.BeforeOrientation;
            if(action.BeforeGroups!=null)map.Groups=(after?action.AfterGroups:action.BeforeGroups).Select(g=>g.Copy()).ToList();
            if(action.BeforeStates!=null)map.ObjectStates=(after?action.AfterStates:action.BeforeStates).Select(s=>s.Copy()).ToList();
            if (action.BeforeExcludedStates != null) map.ExcludedStateIds = new List<string>(after ? action.AfterExcludedStates : action.BeforeExcludedStates);
            foreach(var change in action.Objects)change.Apply(map,after);
            selectedObjects.RemoveWhere(id=>!map.Objects.Any(o=>o.Id==id));
            if (!map.Groups.Any(g => g.Id == selectedGroupId && selectedObjects.SetEquals(g.Members))) selectedGroupId = null;
            if(toPalette!=null)
            {
                activeDocument.Presets=toPalette.ApplyWorking(map,out brushIndex);
                PublishPaletteChanges(fromPalette,toPalette);ResolveWorkingPresets(activeDocument);
            }
            if(action.BeforeQuick!=null){ApplyQuick(action.QuickPresetId,after?action.AfterQuick:action.BeforeQuick,action.QuickSections);PublishQuickDraft(action.QuickPresetId,action.QuickSections);}
            if (action.Changes.Count != 0 || action.Layers.Count != 0)
            {
                var cells = map.Cells.ToDictionary(c => c.Id);
                foreach (var change in action.Changes) { CellData cell; if (cells.TryGetValue(change.CellId, out cell)) (after ? change.After : change.Before).ApplyTo(cell); }
                foreach (var change in action.Layers) { CellData cell; if (cells.TryGetValue(change.CellId, out cell)) change.Apply(cell, after); }
            }
            if(action.NamedCellId!=null)
            {
                var named=map.Cells.FirstOrDefault(c=>c.Id==action.NamedCellId);
                if(named!=null)named.Name=after?action.AfterCellName:action.BeforeCellName;
            }
            InvalidateLight();
            selected = selectedId == null ? -1 : map.Cells.FindIndex(c => c.Id == selectedId); editorCellId = null;
            if (action.BeforePalette != null || action.PresetChange != null) RefreshPresetPicker();
        }
        bool ChangePalette(string label, Action mutate, Action<EditAction> changeMap = null)
        {
            EndCanvasGesture(true); var before = PaletteState.Capture(map, brushIndex,workingPresets); var action = new EditAction { Label = label, BeforePalette = before };
            mutate(); var after = PaletteState.Capture(map, brushIndex,workingPresets);
            if (before.Signature == after.Signature) return false;
            if (changeMap != null) changeMap(action); action.AfterPalette = after; PublishPaletteChanges(before,after);PushAction(action); RefreshPresetPicker(); MarkDirty(); return true;
        }
        void FillRegion(int start)
        {
            BeginMaterialStroke(start, currentTool); VisitBucketRegion(start); FinishStroke(); strokeStarted = false;
        }
        void ResizeProject()
        {
            using (var d = new ResizeDialog(map.Columns, map.Rows))
            {
                if (RunModal(() => d.ShowDialog(this)) != DialogResult.OK) return;
                try
                {
                    var plan = MapLogic.ResizeAll(map, d.TopDelta, d.BottomDelta, d.LeftDelta, d.RightDelta); if (plan.IsEmpty) return;
                    if (plan.DataIndices.Count > 0) using (var warning = new ResizeWarningDialog(plan.RemovedIndices.Count, RemovalGroup.For(map, plan.DataIndices))) { if (RunModal(() => warning.ShowDialog(this)) != DialogResult.Yes) return; }
                    ApplyResize(plan.State);
                }
                catch (Exception ex) { ShowEditorMessage(ex.Message, "Nie można zmienić rozmiaru", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }
        void ApplyResize(ResizeState state)
        {
            if (state == null) return;
            EndCanvasGesture(true); textHistory.BreakGroups();
            var action = new EditAction { Label = "Zmiana rozmiaru", BeforeResize = ResizeState.Capture(map), AfterResize = state };
            var geometry=new MapData {Orientation=map.Orientation,RowParity=state.RowParity,ColumnParity=state.ColumnParity};action.Objects=TransformObjects(geometry,state.ShiftColumns,state.ShiftRows);
            ApplyAction(action, true); PushAction(action); MarkDirty();
        }
    }
}
