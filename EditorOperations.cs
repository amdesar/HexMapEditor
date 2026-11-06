using System;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        void NameEdited()
        {
            if (updating || selected < 0) return; var cell = map.Cells[selected];
            var history = textHistory.For(cell.Id, "name", cell.Name); history.Record(cellName.Text, cellName.EditKind); textHistory.Trim(); cell.Name = cellName.Text; MarkCellDirty(cell);
        }
        void DescriptionEdited(string document, string plain, string kind)
        {
            if (updating || selected < 0) return; var cell = map.Cells[selected];
            if (cell.RichDescription == null && document == NoteCodec.Write(NoteCodec.FromPlain(cell.Description))) return;
            string initial = DescriptionHistoryState(cell);
            var history = textHistory.For(cell.Id, "description", initial); history.Record("rich:" + document, kind); textHistory.Trim();
            cell.RichDescription = document; cell.Description = plain; cell.NotesFormat = "richDocument1"; MarkCellDirty(cell);
        }
        void TextUndo(string field, bool repeat)
        {
            if (selected < 0) return; var cell = map.Cells[selected]; textHistory.BreakGroups();
            string initial = field == "name" ? cell.Name : DescriptionHistoryState(cell);
            var history = textHistory.For(cell.Id, field, initial); int before = history.Cursor;
            string value = repeat ? history.Redo() : history.Undo(); if (before == history.Cursor) return;
            updating = true;
            try
            {
                if (field == "name") { int caret = cellName.SelectionStart; cell.Name = value; cellName.Text = value; cellName.SelectionStart = Math.Min(caret, value.Length); }
                else { bool rich = value.StartsWith("rich:", StringComparison.Ordinal); cell.RichDescription = rich ? value.Substring(5) : null; cell.Description = rich ? NoteCodec.Plain(cell.RichDescription) : value.Substring(6); cell.NotesFormat = rich ? "richDocument1" : "plainText"; cellDescription.SetContent(cell.RichDescription, cell.Description); }
            }
            finally { updating = false; }
            MarkCellDirty(cell);
        }
        static string DescriptionHistoryState(CellData cell) { return cell.RichDescription == null ? "plain:" + (cell.Description ?? "") : "rich:" + cell.RichDescription; }
        void PushAction(EditAction action)
        {
            undo.Push(action); if (undo.Count > 300) { var keep = undo.Reverse().Skip(1).ToArray(); undo.Clear(); foreach (var item in keep) undo.Push(item); } redo.Clear();
        }
        void ApplyAction(EditAction action, bool after)
        {
            string selectedId = selected < 0 ? null : map.Cells[selected].Id;
            if (action.BeforeResize != null) (after ? action.AfterResize : action.BeforeResize).Apply(map);
            if (action.BeforePalette != null) { (after ? action.AfterPalette : action.BeforePalette).Apply(map, out brushIndex); RefreshPresetPicker(); }
            {
                var cells = map.Cells.ToDictionary(c => c.Id);
                foreach (var change in action.Changes) { CellData cell; if (cells.TryGetValue(change.CellId, out cell)) (after ? change.After : change.Before).ApplyTo(cell); }
            }
            selected = selectedId == null ? -1 : map.Cells.FindIndex(c => c.Id == selectedId); editorCellId = null;
        }
        bool ChangePalette(string label, Action mutate, Action<EditAction> changeMap = null)
        {
            EndCanvasGesture(true); var before = PaletteState.Capture(map, brushIndex); var action = new EditAction { Label = label, BeforePalette = before };
            mutate(); var after = PaletteState.Capture(map, brushIndex);
            if (before.Signature == after.Signature) return false;
            if (changeMap != null) changeMap(action); action.AfterPalette = after; PushAction(action); RefreshPresetPicker(); MarkDirty(); return true;
        }
        void FillRegion(int start)
        {
            strokeBefore.Clear(); foreach (int index in MapLogic.Flood(map, start)) PaintCell(index); FinishStroke();
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
            ApplyAction(action, true); PushAction(action); MarkDirty();
        }
    }
}
