using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        bool previewWithoutFog;
        CheckBox fogPreviewButton;
        void SetFogPreview(bool value){EndCanvasGesture(true);previewWithoutFog=value;if(fogPreviewButton!=null){fogPreviewButton.Checked=value;fogPreviewButton.BackColor=value?Color.FromArgb(103,78,26):Color.Transparent;}editorCellId=null;UpdateUI();}
        void AskFogVisibility(){ResolvePreviewFogRequest(ShowEditorMessage("Ręczna edycja mgły wymaga normalnej widoczności. Wyłączyć podgląd bez mgły?\nPo zgodzie ponów gest na płótnie.","Podgląd bez mgły",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes);}
        void ResolvePreviewFogRequest(bool accept){if(accept)SetFogPreview(false);}
        // 0 visible, 1 partial, 2 hidden. Rectangular geometry only, no per-pixel scan.
        int ObjectFogLevel(MapObject obj)
        {
            double fraction = FogVisibility.VisibleFraction(map, ObjectBounds(obj));
            return fraction == 1 ? 0 : fraction == 0 ? 2 : 1;
        }
        static void ValidateGroupFields(Dictionary<string,object> raw, int format)
        {
            string[][] required={new[]{"ObjectStates","Id","Name","Color","CanReveal"},new[]{"Groups","Id","Name","Members","StateId","Locked","RevealsFog","RevealRadius","SizeMode","SizeValue"}};
            foreach(var fields in required){var items=raw.ContainsKey(fields[0])?raw[fields[0]] as object[]:null;if(items==null||items.Any(o=>!(o is Dictionary<string,object>)||fields.Skip(1).Any(k=>!((Dictionary<string,object>)o).ContainsKey(k))))throw new InvalidDataException("Brak jawnych danych stanów lub grup.");}
            if (format == 6 && ((object[])raw["Groups"]).Cast<Dictionary<string, object>>().Any(g =>
                !g.ContainsKey("FrameEnabled") || !(g["FrameEnabled"] is bool) ||
                !g.ContainsKey("FrameColor") || !(g["FrameColor"] is string)))
                throw new InvalidDataException("Brak jawnych danych ramki grupy.");
        }
    }
}
