using System;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class HistoryNameBox : DarkTextBox
    {
        public string EditKind = "typing";
        public Action HistoryUndo;
        protected override void WndProc(ref Message message)
        {
            // Include the native context menu, not just Ctrl+Z routed by the Form.
            if ((message.Msg == 0x304 || message.Msg == 0xC7) && HistoryUndo != null) { HistoryUndo(); return; }
            string previous = EditKind;
            if (message.Msg == 0x302) EditKind = "paste";
            if (message.Msg == 0x300 || message.Msg == 0x303) EditKind = "cut";
            try { base.WndProc(ref message); } finally { EditKind = previous; }
        }
    }
}
