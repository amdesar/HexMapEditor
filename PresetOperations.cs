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
        string pickerSignature, paletteSignature;
        Button deletePresetButton;
        TerrainPreset ActivePreset() { return map.Presets.First(p => p.Id == map.ActivePresetId); }
        List<TerrainPreset> AvailablePresets() { return map.Presets.Concat(libraryPresets.Where(p => !map.Presets.Any(m => m.Id == p.Id))).ToList(); }
        void RefreshPresetPicker()
        {
            bool previous = updating; updating = true;
            try
            {
                var available = AvailablePresets(); string key = json.Serialize(available.Select(p => new { p.Id, p.Name }).ToArray());
                bool referencesChanged = terrainPicker.Items.Count != available.Count || available.Where((p, i) => i >= terrainPicker.Items.Count || !Object.ReferenceEquals(terrainPicker.Items[i], p)).Any();
                if (pickerSignature != key || referencesChanged)
                {
                    terrainPicker.BeginUpdate(); try { terrainPicker.Items.Clear(); foreach (var p in available) terrainPicker.Items.Add(p); } finally { terrainPicker.EndUpdate(); }
                    pickerSignature = key;
                }
                terrainPicker.SelectedItem = ActivePreset();
                var palette = ActivePreset(); brushIndex = Math.Max(0, Math.Min(brushIndex, palette.Terrains.Count - 1));
                string signature = palette.Id + ":" + json.Serialize(palette.Terrains);
                if (signature != paletteSignature)
                {
                    var body = paletteTiles.Parent; if (body != null) body.SuspendLayout(); left.SuspendLayout(); paletteTiles.SuspendLayout();
                    try
                    {
                        var existing = paletteTiles.Controls.Cast<TerrainTile>().ToDictionary(t => t.TerrainId);
                        var ids = new HashSet<string>(palette.Terrains.Select(t => t.TerrainId));
                        foreach (var tile in existing.Values.Where(t => !ids.Contains(t.TerrainId)).ToArray()) { paletteTiles.Controls.Remove(tile); tile.Dispose(); }
                        for (int i = 0; i < palette.Terrains.Count; i++)
                        {
                            var terrain = palette.Terrains[i]; TerrainTile tile;
                            if (!existing.TryGetValue(terrain.TerrainId, out tile)) { tile = new TerrainTile(SelectTerrain, EditTerrainById, RemoveTerrainById); tile.TerrainId = terrain.TerrainId; paletteTiles.Controls.Add(tile); }
                            tile.UpdateTerrain(terrain, i == brushIndex); paletteTiles.Controls.SetChildIndex(tile, i);
                        }
                        paletteSignature = signature;
                    }
                    finally { paletteTiles.ResumeLayout(true); if (body != null) body.ResumeLayout(true); left.ResumeLayout(true); }
                }
                UpdateTerrainSelection(); UpdatePresetStatus(); UpdateMaterialIndicator();
            }
            finally { updating = previous; }
        }
        void UpdateTerrainSelection()
        {
                var p = ActivePreset(); foreach (TerrainTile tile in paletteTiles.Controls) { int at = p.Terrains.FindIndex(t => t.TerrainId == tile.TerrainId); if (at >= 0) tile.UpdateTerrain(p.Terrains[at], !iconMaterial && at == brushIndex); }
        }
        void SelectTerrain(string id) { EndCanvasGesture(true); int at = ActivePreset().Terrains.FindIndex(t => t.TerrainId == id); if (at >= 0) { brushIndex = at; iconMaterial = false; RefreshIconSelection(); UpdateMaterialIndicator(); } }
        void SelectPreset(string id)
        {
            if (id == map.ActivePresetId) return; var chosen = AvailablePresets().First(p => p.Id == id);
            ChangePalette("Wybór presetu", () => { if (!map.Presets.Any(p => p.Id == id)) map.Presets.Add(chosen.Copy()); map.ActivePresetId = id; brushIndex = 0; });
        }
        void UpdatePresetStatus()
        {
            var p = ActivePreset(); var stored = libraryPresets.FirstOrDefault(t => t.Id == p.Id);
            bool differs = p.Id == PresetCatalog.DefaultId ? json.Serialize(p) != json.Serialize(PresetCatalog.Default()) : stored == null || json.Serialize(p) != json.Serialize(stored);
            string status = p.Id == PresetCatalog.DefaultId ? (differs ? "Kopia robocza wzorca Domyślny *" : "Domyślny: chroniony wzorzec") : stored == null ? "Preset niezapisany w bibliotece" : differs ? "Paleta różni się od zapisanego presetu *" : "Paleta zgodna z zapisanym presetem";
            presetStateLabel.Text = status; presetStateLabel.ForeColor = differs ? Color.FromArgb(246, 207, 124) : ForeColor; tips.SetToolTip(presetStateLabel, status + "\nNazwa robocza: " + p.Name); presetStateLabel.AccessibleName = status;
            if (deletePresetButton != null) { deletePresetButton.Enabled = p.Id != PresetCatalog.DefaultId && stored != null; tips.SetToolTip(deletePresetButton, p.Id == PresetCatalog.DefaultId ? "Chronionego wzorca Domyślny nie można usunąć." : stored == null ? "Ten roboczy preset nie ma pliku w bibliotece." : "Usuń własny preset z biblioteki; dane mapy pozostaną."); }
        }
        void LoadLibraryPresets(bool initial)
        {
            if (!EnsureLibrary()) return;
            if (!initial) { string notice; if (!TryRefreshPresets(LibraryPaths.Presets, out notice) || !String.IsNullOrEmpty(notice)) ShowEditorMessage(notice, "Odświeżenie presetów", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try { string warning; libraryPresets = PresetCatalog.Load(LibraryPaths.Presets, out warning); RefreshPresetPicker(); if (!String.IsNullOrEmpty(warning)) ShowEditorMessage(warning, "Niektóre presety zostały pominięte", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (Exception ex) { ShowEditorMessage("Nie można odczytać biblioteki presetów. Dane projektu pozostają dostępne.\n" + ex.Message, "Błąd biblioteki", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        bool TryRefreshPresets(string directory, out string notice)
        {
            notice = null;
            try
            {
                var previous = libraryPresets.FirstOrDefault(p => p.Id == map.ActivePresetId);
                string warning; var loaded = PresetCatalog.Load(directory, out warning);
                var next = loaded.FirstOrDefault(p => p.Id == map.ActivePresetId);
                var messages = new List<string>();
                if (!String.IsNullOrEmpty(warning)) messages.Add(warning);
                if (previous != null && previous.Id != PresetCatalog.DefaultId)
                {
                    if (next == null) messages.Add("Plik aktywnego presetu zniknął lub został pominięty. Zachowano roboczą paletę projektu; nie jest ona zapisana w odczytanej bibliotece.");
                    else if (json.Serialize(previous) != json.Serialize(next)) messages.Add("Plik aktywnego presetu zmienił się poza programem. Zachowano roboczą paletę projektu. Zapis nadpisze plik dopiero po świadomym wyborze.");
                }
                libraryPresets = loaded; RefreshPresetPicker(); notice = String.Join(Environment.NewLine, messages); return true;
            }
            catch (Exception ex) { notice = "Nie można odświeżyć presetów. Zachowano poprzednią listę i robocze dane projektu.\n" + ex.Message; return false; }
        }
        void CreatePreset()
        {
            using (var d = new NameDialog("Nowy preset", "Nowy preset")) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK)
            { string error; if (!TryCreatePreset(LibraryPaths.Presets, d.Value, out error)) ShowEditorMessage("Nie można utworzyć presetu.\n" + error, "Błąd presetu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        bool TryCreatePreset(string directory, string name, out string error)
        {
            error = null;
            try
            {
                if (map.Presets.Count >= 100) throw new InvalidDataException("Limit 100 roboczych presetów w projekcie.");
                if (map.Presets.Any(p => PresetCatalog.SameName(p.Name, name))) throw new InvalidDataException("Nazwa jest już używana w projekcie.");
                var preset = new TerrainPreset { Name = (name ?? "").Trim() }; preset.Terrains.Add(PresetCatalog.Neutral()); PresetCatalog.Write(preset, directory, false);
                libraryPresets.Add(preset.Copy()); ChangePalette("Nowy zapisany preset", () => { map.Presets.Add(preset); map.ActivePresetId = preset.Id; brushIndex = 0; }); return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        void RemovePreset()
        {
            var p = ActivePreset(); var saved = libraryPresets.FirstOrDefault(t => t.Id == p.Id);
            if (p.Id == PresetCatalog.DefaultId || saved == null) return;
            bool changed = json.Serialize(p) != json.Serialize(saved);
            string message = "Usunąć preset „" + saved.Name + "” z biblioteki?\n\nDane już zapisane na mapie pozostaną bez zmian.\nHistoria Undo mapy nie cofnie usunięcia pliku." + (changed ? "\n\nRobocza paleta ma niezapisane zmiany. Preset zostanie usunięty z listy projektu; Undo może przywrócić go wyłącznie jako niezapisany preset roboczy." : "") + "\n\nPlik zostanie przekazany do Kosza systemowego. Program nie stosuje cichego trwałego usuwania.";
            if (ShowEditorMessage(message, "Usuń własny preset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            string error = null; if (!RunModal(() => TryDeletePreset(LibraryPaths.Presets, (path) => RecycleFile.Send(path, Handle), out error))) ShowEditorMessage("Nie można usunąć presetu.\n" + error, "Błąd presetu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        bool TryDeletePreset(string directory, Action<string> recycle, out string error)
        {
            error = null; string path = null; var current = ActivePreset();
            try
            {
                if (current.Id == PresetCatalog.DefaultId) throw new InvalidDataException("Nie można usunąć chronionego wzorca.");
                path = PresetCatalog.FindFile(directory, current.Id); recycle(path);
                if (File.Exists(path)) throw new IOException("Plik nadal istnieje; lista nie została zmieniona.");
                libraryPresets.RemoveAll(p => p.Id == current.Id);
                ChangePalette("Usunięcie roboczego presetu z projektu", () => {
                    map.Presets.RemoveAll(p => p.Id == current.Id);
                    if (!map.Presets.Any(p => p.Id == PresetCatalog.DefaultId)) map.Presets.Add(PresetCatalog.Default());
                    map.ActivePresetId = PresetCatalog.DefaultId; brushIndex = 0;
                }); return true;
            }
            catch (Exception ex)
            {
                // If the shell completed removal before reporting a later error,
                // keep the working palette, but never mark the missing file as saved.
                if (path != null && !File.Exists(path)) { libraryPresets.RemoveAll(p => p.Id == current.Id); RefreshPresetPicker(); }
                error = ex.Message; return false;
            }
        }
        void RenamePreset()
        {
            var p = ActivePreset(); using (var d = new NameDialog(p.Id == PresetCatalog.DefaultId ? "Nazwa roboczej kopii Domyślnego" : "Nazwa roboczego presetu", p.Name)) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK) ChangePalette("Nazwa roboczego presetu", () => p.Name = d.Value);
        }
        void SavePreset()
        {
            var p = ActivePreset(); bool saved = p.Id != PresetCatalog.DefaultId && libraryPresets.Any(t => t.Id == p.Id);
            using (var d = new SavePresetDialog(p, saved))
            {
                if (RunModal(() => d.ShowDialog(this)) != DialogResult.OK) return;
                string error; if (!TrySaveWorkingPreset(LibraryPaths.Presets, d.AsNew, d.PresetName, out error)) ShowEditorMessage("Nie można zapisać presetu.\n" + error, "Błąd presetu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        bool TrySaveWorkingPreset(string directory, bool asNew, string name, out string error)
        {
            error = null;
            try
            {
                var current = ActivePreset(); var candidate = current.Copy();
                bool firstSave = current.Id != PresetCatalog.DefaultId && !libraryPresets.Any(p => p.Id == current.Id);
                if (asNew)
                {
                    if (!firstSave && map.Presets.Count >= 100) throw new InvalidDataException("Limit 100 roboczych presetów w projekcie.");
                    if (!firstSave) candidate.Id = Guid.NewGuid().ToString("N"); candidate.Name = (name ?? "").Trim();
                    if (map.Presets.Any(p => p.Id != current.Id && PresetCatalog.SameName(p.Name, candidate.Name))) throw new InvalidDataException("Nazwa jest już używana przez inny preset projektu.");
                }
                PresetCatalog.Write(candidate, directory, !asNew);
                libraryPresets.RemoveAll(p => p.Id == candidate.Id); libraryPresets.Add(candidate.Copy());
                if (asNew) ChangePalette(firstSave ? "Pierwszy zapis presetu" : "Nowa zapisana kopia presetu", () => { if (firstSave) map.Presets[map.Presets.FindIndex(p => p.Id == current.Id)] = candidate; else map.Presets.Add(candidate); map.ActivePresetId = candidate.Id; });
                RefreshPresetPicker(); return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        void EditTerrainById(string id) { EditTerrainDialog(false, id); }
        void EditTerrain(bool add) { EditTerrainDialog(add, null); }
        void EditTerrainDialog(bool add, string id)
        {
            var p = ActivePreset(); var old = add ? new TerrainDefinition() : p.Terrains.FirstOrDefault(t => t.TerrainId == id);
            if (old == null) return;
            using (var d = new TerrainDialog(old, !add)) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK)
            {
                var next = old.Copy(); next.Name = d.TerrainName; next.Color = d.TerrainColor;
                if (add) AddTerrain(next);
                else if (next.Name != old.Name || !String.Equals(next.Color, old.Color, StringComparison.OrdinalIgnoreCase)) { next.VariantId = Guid.NewGuid().ToString("N"); next.Revision++; UpdateTerrain(old, next, d.ApplyToMap); }
            }
        }
        void AddTerrain(TerrainDefinition terrain)
        { ChangePalette("Dodanie terenu", () => { var p = ActivePreset(); p.Terrains.Add(terrain.Copy()); brushIndex = p.Terrains.Count - 1; }); }
        void UpdateTerrain(TerrainDefinition old, TerrainDefinition next, bool apply)
        {
            var p = ActivePreset(); int at = p.Terrains.FindIndex(t => t.TerrainId == old.TerrainId && t.VariantId == old.VariantId); if (at < 0) throw new InvalidOperationException("Brak wersji terenu.");
            if (old.Name == next.Name && String.Equals(old.Color, next.Color, StringComparison.OrdinalIgnoreCase)) return;
            ChangePalette("Edycja terenu" + (apply ? " i mapy" : ""), () => p.Terrains[at] = next.Copy(), action =>
            {
                if (!apply) return;
                foreach (var cell in map.Cells.Where(c => c.VariantId == old.VariantId)) { var before = new TerrainState(cell); MapLogic.ApplyTerrain(cell, next); action.Changes.Add(new CellChange { CellId = cell.Id, Before = before, After = new TerrainState(cell) }); }
            });
        }
        void RemoveTerrainById(string id)
        {
            var t = ActivePreset().Terrains.FirstOrDefault(v => v.TerrainId == id); if (t == null) return;
            if (ShowEditorMessage("Usunąć teren „" + t.Name + "” z roboczej palety? Istniejące heksy zachowają swoje dane.", "Usuń teren", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes) DeleteTerrain(id);
        }
        void DeleteTerrain(string id)
        {
            var p = ActivePreset(); int at = p.Terrains.FindIndex(t => t.TerrainId == id); if (at < 0) return;
            string selectedId = brushIndex < p.Terrains.Count ? p.Terrains[brushIndex].TerrainId : null;
            ChangePalette("Usunięcie terenu", () => { p.Terrains.RemoveAt(at); int selected = p.Terrains.FindIndex(t => t.TerrainId == selectedId); brushIndex = selected >= 0 ? selected : Math.Max(0, Math.Min(at, p.Terrains.Count - 1)); });
        }
    }
}
