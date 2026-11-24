using System;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal sealed class PresetFileChange
    {
        public TerrainPreset Before, After;
        public int Index;
        public bool IsEmpty { get { return Before != null && After != null && Before.FilePath == After.FilePath && Before.DiskBytes.SequenceEqual(After.DiskBytes); } }
        public void Write(bool after)
        {
            var from = after ? Before : After; var to = after ? After : Before;
            if ((from != null && from.Id == PresetCatalog.DefaultId) || (to != null && to.Id == PresetCatalog.DefaultId)) throw new IOException("Chronionego Domyślnego nie można zmieniać.");
            string source = from == null ? null : from.FilePath, target = to == null ? null : to.FilePath;
            string directory = Path.GetDirectoryName(source ?? target);
            foreach (string path in Directory.GetFiles(directory,"*.json"))
            {
                if (String.Equals(path,source,StringComparison.OrdinalIgnoreCase)) continue;
                var other = PresetCatalog.ReadIdentity(path);
                if ((from != null && other.Id == from.Id) || (to != null && (other.Id == to.Id || PresetCatalog.SameName(other.Name,to.Name)))) throw new IOException("Konflikt ID lub nazwy presetu w bibliotece. Nie zmieniono plików ani historii.\n"+path);
            }
            LibraryFiles.Transition(source,from == null ? null : from.DiskBytes,target,to == null ? null : to.DiskBytes);
        }
    }
    public sealed partial class MainWindow
    {
        bool CommitPresetFile(PresetFileChange file, PaletteState after, string label, PresetSections savedSections = 0)
        {
            EndCanvasGesture(true); var before = PaletteState.Capture(map,brushIndex,workingPresets);
            bool paletteChanges = before.Signature != after.Signature;
            bool resetIcons = savedSections.HasFlag(PresetSections.Icons) && CurrentQuick.DraftIcons != null;
            bool resetObjects = savedSections.HasFlag(PresetSections.Objects) && CurrentQuick.DraftObjects != null;
            if (file.IsEmpty && !paletteChanges && !resetIcons && !resetObjects) return false;
            file.Index = file.Before == null ? libraryPresets.Count : libraryPresets.FindIndex(p=>p.Id==file.Before.Id);
            var action = new EditAction { Label=label, PresetChange=file.IsEmpty ? null : file, BeforePalette=paletteChanges ? before : null, AfterPalette=paletteChanges ? after : null };
            if (resetIcons || resetObjects)
            {
                action.QuickPresetId = map.ActivePresetId;
                action.QuickSections = (resetIcons ? PresetSections.Icons : 0) | (resetObjects ? PresetSections.Objects : 0);
                action.BeforeQuick = CaptureQuick();
                action.AfterQuick = CaptureQuick();
                if (resetIcons) action.AfterQuick.DraftIcons = null;
                if (resetObjects) action.AfterQuick.DraftObjects = null;
            }
            ApplyAction(action,true); PushAction(action); MarkDirty(); return true;
        }
        void ApplyPresetLibrary(PresetFileChange change, bool after)
        {
            change.Write(after);
            var removed = after ? change.Before : change.After; var added = after ? change.After : change.Before;
            if (removed != null) libraryPresets.RemoveAll(p=>p.Id==removed.Id);
            if (added != null) libraryPresets.Insert(Math.Max(0,Math.Min(change.Index,libraryPresets.Count)),added.Copy());
            TrackPresetFile(removed,added);
        }
    }
}
