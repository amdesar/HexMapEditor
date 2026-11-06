using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        bool TryChangeLibraryIcon(LibraryIcon before, LibraryIcon next, bool updateMap, string directory, out string error)
        {
            error = null; EndCanvasGesture(true);
            try
            {
                directory = Path.GetFullPath(directory);
                if (before == null && next == null) return true;
                if (before != null && (!libraryIcons.Any(i => i.Id == before.Id) || !String.Equals(Path.GetDirectoryName(before.FilePath),directory,StringComparison.OrdinalIgnoreCase))) throw new IOException("Pozycja biblioteki zmieniła się. Wybierz ją ponownie.");
                if (before == null && libraryIcons.Count >= IconAssets.MaxAssets) throw new IOException("Limit 128 ikon w bibliotece.");
                if (next != null)
                {
                    next = next.Copy(); next.Name = (next.Name ?? "").Trim(); IconLibrary.Validate(next);
                    if (before != null && before.Id != next.Id) throw new IOException("Edycja nie może zmieniać tożsamości ikony.");
                    if (before == null && libraryIcons.Any(i => i.Id == next.Id)) throw new IOException("Powtórzone ID ikony.");
                    Directory.CreateDirectory(directory); next.FilePath = IconLibrary.Destination(directory,next.Name,before == null ? null : before.FilePath);
                    next.DiskBytes = new UTF8Encoding(false).GetBytes(json.Serialize(next));
                }
                var action = new EditAction { Label = before == null ? "Dodanie ikony" : next == null ? "Usunięcie ikony" : "Edycja ikony" };
                if (updateMap && before != null && next != null)
                {
                    foreach (var cell in map.Cells.Where(c => c.IconId != null && (c.IconEntryId ?? c.IconId) == before.Id))
                    {
                        if (cell.IconId == next.Asset.Id && cell.IconName == next.Name && cell.IconEntryId == next.Id) continue;
                        action.Layers.Add(new LayerChange { CellId=cell.Id, Icons=true, BeforeIcon=cell.IconId, BeforeEntry=cell.IconEntryId, BeforeName=cell.IconName, AfterIcon=next.Asset.Id, AfterEntry=next.Id, AfterName=next.Name });
                    }
                    if (action.Layers.Count > 0 && !map.IconAssets.Any(a => a.Id == next.Asset.Id))
                    {
                        if (map.IconAssets.Count >= IconAssets.MaxAssets || map.IconAssets.Sum(a=>(long)a.Png.Length)+next.Asset.Png.Length > 44L*1024*1024) throw new IOException("Limit zasobów dokumentu. Zapisz i ponownie otwórz mapę, aby zwolnić nieużywane zasoby Undo.");
                        action.RequiredAssets.Add(next.Asset);
                    }
                }
                bool fileChange = before == null || next == null || !String.Equals(before.FilePath,next.FilePath,StringComparison.Ordinal) || !before.DiskBytes.SequenceEqual(next.DiskBytes);
                if (!fileChange && action.Layers.Count == 0) return true;
                if (fileChange) action.LibraryChange = new IconLibraryChange { Before=before == null ? null : before.Copy(), After=next, BeforeBytes=before == null ? null : before.DiskBytes, AfterBytes=next == null ? null : next.DiskBytes, BeforeSelected=activeIcon, BeforeMaterial=iconMaterial, AfterSelected=next == null ? null : next.Id, AfterMaterial=next != null, Index=before == null ? libraryIcons.Count : libraryIcons.FindIndex(i=>i.Id==before.Id) };
                ApplyAction(action,true); PushAction(action); MarkDirty(); return true;
            }
            catch(Exception ex) { error=ex.Message; return false; }
        }
        void ApplyIconLibrary(IconLibraryChange change, bool after)
        {
            change.Write(after);
            var entry = after ? change.After : change.Before; var removed = after ? change.Before : change.After;
            if (removed != null) libraryIcons.RemoveAll(i => i.Id == removed.Id);
            if (entry != null) libraryIcons.Insert(Math.Min(change.Index,libraryIcons.Count),entry.Copy());
            activeIcon = after ? change.AfterSelected : change.BeforeSelected; iconMaterial = after ? change.AfterMaterial : change.BeforeMaterial;
            if (!libraryIcons.Any(i=>i.Id==activeIcon)) { activeIcon=null; iconMaterial=false; }
            RefreshIconPreview();
        }
    }
}
