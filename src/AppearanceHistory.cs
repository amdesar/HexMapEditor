using System.Collections.Generic;
using System.Linq;

namespace HexMapEditor
{
    internal sealed class AppearanceHistory
    {
        internal const int Limit = 100;
        readonly List<List<FillVariant>> states = new List<List<FillVariant>>();
        int cursor;
        internal AppearanceHistory(IEnumerable<FillVariant> initial) { states.Add(Snapshot(initial)); }
        internal static List<FillVariant> Snapshot(IEnumerable<FillVariant> fills)
        {
            return fills.Select(fill => { var copy = fill.Copy(); if (copy.Graphic != null) copy.Graphic = copy.Graphic.Copy(); return copy; }).ToList();
        }
        internal List<FillVariant> Current { get { return Snapshot(states[cursor]); } }
        internal bool CanUndo { get { return cursor > 0; } }
        internal bool CanRedo { get { return cursor < states.Count - 1; } }
        internal bool Commit(List<FillVariant> next)
        {
            FillRules.ValidatePool(next);
            if (FillRules.Signature(states[cursor]) == FillRules.Signature(next)) return false;
            states.RemoveRange(cursor + 1, states.Count - cursor - 1);
            states.Add(Snapshot(next)); cursor++;
            if (states.Count > Limit + 1) { states.RemoveAt(0); cursor--; }
            return true;
        }
        internal bool Move(bool redo)
        {
            if (redo ? !CanRedo : !CanUndo) return false;
            cursor += redo ? 1 : -1; return true;
        }
    }
}
