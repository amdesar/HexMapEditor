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
                UpdateTerrainSelection(); UpdatePresetStatus();
            }
            finally { updating = previous; }
        }
        void UpdateTerrainSelection()
        {
            var p = ActivePreset(); foreach (TerrainTile tile in paletteTiles.Controls) { int at = p.Terrains.FindIndex(t => t.TerrainId == tile.TerrainId); if (at >= 0) tile.UpdateTerrain(p.Terrains[at], at == brushIndex); }
        }
        void SelectTerrain(string id) { EndCanvasGesture(true); int at = ActivePreset().Terrains.FindIndex(t => t.TerrainId == id); if (at >= 0) { brushIndex = at; UpdateTerrainSelection(); } }
        void SelectPreset(string id)
        {
            if (id == map.ActivePresetId) return; var chosen = AvailablePresets().First(p => p.Id == id);
            ChangePalette("Wybór zestawu", () => { if (!map.Presets.Any(p => p.Id == id)) map.Presets.Add(chosen.Copy()); map.ActivePresetId = id; brushIndex = 0; });
        }
        void UpdatePresetStatus()
        {
            var p = ActivePreset(); var stored = libraryPresets.FirstOrDefault(t => t.Id == p.Id);
            bool differs = p.Id == PresetCatalog.DefaultId ? json.Serialize(p) != json.Serialize(PresetCatalog.Default()) : stored == null || json.Serialize(p) != json.Serialize(stored);
            string status = p.Id == PresetCatalog.DefaultId ? (differs ? "Kopia robocza wzorca Domyślny *" : "Domyślny: chroniony wzorzec") : stored == null ? "Zestaw niezapisany w bibliotece" : differs ? "Paleta różni się od zapisanego presetu *" : "Paleta zgodna z zapisanym presetem";
            presetStateLabel.Text = status; presetStateLabel.ForeColor = differs ? Color.FromArgb(246, 207, 124) : ForeColor; tips.SetToolTip(presetStateLabel, status + "\nNazwa robocza: " + p.Name); presetStateLabel.AccessibleName = status;
        }
        void LoadLibraryPresets(bool initial)
        {
            if (!EnsureLibrary()) return; string warning; libraryPresets = PresetCatalog.Load(Path.Combine(LibraryPaths.Root, "Terrains"), out warning);
            RefreshPresetPicker(); if (!String.IsNullOrEmpty(warning)) ShowEditorMessage(warning, "Niektóre zestawy zostały pominięte", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        void CreatePreset()
        {
            using (var d = new NameDialog("Nowy zestaw", "Nowy zestaw")) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK)
            { var p = new TerrainPreset { Name = d.Value }; p.Terrains.Add(PresetCatalog.Neutral()); ChangePalette("Nowy zestaw", () => { map.Presets.Add(p); map.ActivePresetId = p.Id; brushIndex = 0; }); }
        }
        void RenamePreset()
        {
            var p = ActivePreset(); using (var d = new NameDialog(p.Id == PresetCatalog.DefaultId ? "Nazwa roboczej kopii Domyślnego" : "Nazwa roboczego zestawu", p.Name)) if (RunModal(() => d.ShowDialog(this)) == DialogResult.OK) ChangePalette("Nazwa roboczego zestawu", () => p.Name = d.Value);
        }
        void SavePreset()
        {
            var p = ActivePreset(); bool saved = p.Id != PresetCatalog.DefaultId && libraryPresets.Any(t => t.Id == p.Id);
            using (var d = new SavePresetDialog(p, saved))
            {
                if (RunModal(() => d.ShowDialog(this)) != DialogResult.OK) return;
                string error; if (!TrySaveWorkingPreset(Path.Combine(LibraryPaths.Root, "Terrains"), d.AsNew, d.PresetName, out error)) ShowEditorMessage("Nie można zapisać zestawu.\n" + error, "Błąd zestawu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        bool TrySaveWorkingPreset(string directory, bool asNew, string name, out string error)
        {
            error = null;
            try
            {
                var current = ActivePreset(); var candidate = current.Copy();
                if (asNew)
                {
                    if (map.Presets.Count >= 100) throw new InvalidDataException("Limit 100 roboczych zestawów w projekcie.");
                    candidate.Id = Guid.NewGuid().ToString("N"); candidate.Name = (name ?? "").Trim();
                    if (map.Presets.Any(p => p.Id != current.Id && PresetCatalog.SameName(p.Name, candidate.Name))) throw new InvalidDataException("Nazwa jest już używana przez inny zestaw projektu.");
                }
                PresetCatalog.Write(candidate, directory, !asNew);
                libraryPresets.RemoveAll(p => p.Id == candidate.Id); libraryPresets.Add(candidate.Copy());
                if (asNew) ChangePalette("Nowa zapisana kopia zestawu", () => { map.Presets.Add(candidate); map.ActivePresetId = candidate.Id; });
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
