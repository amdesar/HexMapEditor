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
        bool PresetReadOnly { get { return map.ActivePresetId == PresetCatalog.DefaultId; } }
        List<TerrainPreset> AvailablePresets()
        {
            return libraryPresets.Select(p => map.Presets.FirstOrDefault(m => m.Id == p.Id) ?? p)
                .Concat(map.Presets.Where(p => !libraryPresets.Any(item => item.Id == p.Id))).ToList();
        }
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
                            tile.UpdateTerrain(terrain, i == brushIndex, map.Orientation == "FlatTop",lowQuality); paletteTiles.Controls.SetChildIndex(tile, i);
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
            var p = ActivePreset(); foreach (TerrainTile tile in paletteTiles.Controls) { int at = p.Terrains.FindIndex(t => t.TerrainId == tile.TerrainId); if (at >= 0) tile.UpdateTerrain(p.Terrains[at], !iconMaterial && !objectMaterial && !noMaterial && at == brushIndex, map.Orientation == "FlatTop",lowQuality); tile.Delete.Enabled = !PresetReadOnly; }
        }
        void SelectTerrain(string id) { EndCanvasGesture(true); int at = ActivePreset().Terrains.FindIndex(t => t.TerrainId == id); if (at >= 0) { brushIndex = at; iconMaterial = false; objectMaterial=false;noMaterial=false; MaterialChosen(); RefreshIconSelection(); UpdateMaterialIndicator(); } }
        void MaterialChosen() { if (!updating && (currentTool == EditorTool.FogBrush || currentTool == EditorTool.FogBucket)) SelectTool(EditorTool.Brush); }
        void SelectPreset(string id)
        {
            if (id == map.ActivePresetId) return; var chosen = AvailablePresets().First(p => p.Id == id);
            try
            {
                CheckTextureBudget(chosen.Terrains.SelectMany(t => t.Variants).Select(f => f.Graphic));
                ChangePalette("Wybór presetu", () => { if (!map.Presets.Any(p => p.Id == id)) map.Presets.Add(chosen.Copy()); map.ActivePresetId = id; brushIndex = 0; });
            }
            catch (InvalidDataException ex)
            {
                RefreshPresetPicker();
                ShowEditorMessage(ex.Message, "Wybór presetu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        string PresetContent(TerrainPreset preset) { return json.Serialize(new { preset.Name, preset.Terrains }); }
        void UpdatePresetStatus()
        {
            var p = ActivePreset(); var stored = libraryPresets.FirstOrDefault(t => t.Id == p.Id);
            if (renamePresetButton != null) renamePresetButton.Enabled = !PresetReadOnly;
            if (addTerrainButton != null) addTerrainButton.Enabled = !PresetReadOnly;
            bool differs = p.Id == PresetCatalog.DefaultId ? PresetContent(p) != PresetContent(PresetCatalog.Default()) : stored == null || PresetContent(p) != PresetContent(stored);
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
                var preset = new TerrainPreset { Name = (name ?? "").Trim() }; preset.Terrains.Add(PresetCatalog.Neutral());
                var file = PresetCatalog.PrepareWrite(preset,directory,false,null); var after = PaletteState.Capture(map,brushIndex);
                after.Presets.Add(preset); after.ActivePresetId=preset.Id; after.BrushIndex=0; after.SelectedTerrainId=preset.Terrains[0].TerrainId;
                CommitPresetFile(file,after,"Nowy zapisany preset"); return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        void RemovePreset()
        {
            var p = ActivePreset(); var saved = libraryPresets.FirstOrDefault(t => t.Id == p.Id);
            if (p.Id == PresetCatalog.DefaultId || saved == null) return;
            bool changed = json.Serialize(p) != json.Serialize(saved);
            string message = "Usunąć preset „" + saved.Name + "” z biblioteki?\n\nDane heksów pozostaną bez zmian. Plik nie trafia do Kosza; Undo przywróci plik i paletę z kopii w pamięci." + (changed ? "\nRobocza paleta ma niezapisane zmiany; Undo odtworzy również ten stan." : "") + "\n\nCofanie jest możliwe tylko, dopóki wpis pozostaje w historii (limit 300). Nowa/Otwórz, wypadnięcie wpisu lub zakończenie programu usuwa tę możliwość.";
            if (ShowEditorMessage(message,"Usuń własny preset",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            string error; if (!TryDeletePreset(LibraryPaths.Presets,out error)) ShowEditorMessage("Nie można usunąć presetu.\n"+error,"Błąd presetu",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        bool TryDeletePreset(string directory, out string error)
        {
            error=null;
            try
            {
                EndCanvasGesture(true); var current=ActivePreset();
                if (PresetReadOnly) throw new InvalidDataException("Nie można usunąć chronionego wzorca.");
                var stored=libraryPresets.SingleOrDefault(p=>p.Id==current.Id);
                if (stored==null || stored.FilePath==null || !String.Equals(Path.GetDirectoryName(stored.FilePath),Path.GetFullPath(directory),StringComparison.OrdinalIgnoreCase)) throw new IOException("Brak zapisanego pliku aktywnego presetu.");
                var after=PaletteState.Capture(map,brushIndex); after.Presets.RemoveAll(p=>p.Id==current.Id);
                if (!after.Presets.Any(p=>p.Id==PresetCatalog.DefaultId)) after.Presets.Add(PresetCatalog.Default());
                after.ActivePresetId=PresetCatalog.DefaultId;after.BrushIndex=0;after.SelectedTerrainId=PresetCatalog.NeutralId;
                CommitPresetFile(new PresetFileChange {Before=stored.Copy()},after,"Usunięcie presetu");return true;
            }
            catch(Exception ex){error=ex.Message;return false;}
        }        void RenamePreset()
        {
            if (PresetReadOnly) return;
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
                var stored=libraryPresets.FirstOrDefault(p=>p.Id==candidate.Id);
                if (!asNew && stored==null) throw new IOException("Preset nie ma zapisanego pliku. Zapisz jako nowy.");
                var file=PresetCatalog.PrepareWrite(candidate,directory,!asNew,asNew ? null : stored);
                var after=PaletteState.Capture(map,brushIndex);
                if (asNew)
                {
                    if(firstSave) after.Presets[after.Presets.FindIndex(p=>p.Id==current.Id)]=candidate;
                    else after.Presets.Add(candidate);
                    after.ActivePresetId=candidate.Id;
                }
                CommitPresetFile(file,after,asNew ? "Zapis kopii presetu" : "Zapis presetu");RefreshPresetPicker();return true;
            }            catch (Exception ex) { error = ex.Message; return false; }
        }
        void EditTerrainById(string id) { EditTerrainDialog(false, id); }
        void EditTerrain(bool add) { EditTerrainDialog(add, null); }
        void EditTerrainDialog(bool add, string id)
        { try { EditTerrainDialogCore(add, id); } finally { ArmCanvasInput(); } }
        void EditTerrainDialogCore(bool add, string id)
        {
            if (PresetReadOnly)
            {
                if (!add) ShowEditorMessage("Nie można edytować chronionego presetu Domyślny.\nUtwórz nowy preset, zapisz własną kopię lub wybierz inny preset.",
                    "Chroniony preset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var p = ActivePreset(); var old = add ? new TerrainDefinition() : p.Terrains.FirstOrDefault(t => t.TerrainId == id);
            if (old == null) return;
            using (var d = new AppearancePoolDialog(add ? "Nowy teren" : "Edycja terenu", old.Name, old.Variants, !add, map.Orientation == "FlatTop", "Zastosuj do bieżącej mapy"))
            {
                d.ChooseGraphic = ChooseTerrainGraphic;
                d.EditCrop = ShowTerrainCrop;
                d.CanAccept = () =>
                {
                    try { CheckTextureBudget(d.Fills.Select(f => f.Graphic)); }
                    catch (InvalidDataException ex) { MessageBox.Show(d, ex.Message, "Limit grafik", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
                    return !d.ApplyToMap || !map.Cells.Any(c => c.VariantId == old.VariantId) || GuardLayer(MapLayer.Terrain, d);
                };
                var answer = RunModal(() => d.ShowDialog(this)); RememberModalClick(d);
                if (answer != DialogResult.OK) return;
                var next = old.Copy(); next.Name = d.TerrainName; FillRules.SetPool(next, d.Fills);
                if (add) AddTerrain(next);
                else if (!SameTerrainAppearance(old, next)) { VersionTerrain(old, next); UpdateTerrain(old, next, d.ApplyToMap, d.PreserveLayout); }
            }
        }
        static bool SameTerrainAppearance(TerrainDefinition old, TerrainDefinition next)
        { return old.Name == next.Name && FillRules.Signature(old.Variants) == FillRules.Signature(next.Variants); }
        static void VersionTerrain(TerrainDefinition old, TerrainDefinition next)
        {
            if (FillRules.SurfaceSignature(old.Variants) == FillRules.SurfaceSignature(next.Variants)) { next.VariantId = old.VariantId; next.Revision = old.Revision; }
            else { next.VariantId = Guid.NewGuid().ToString("N"); next.Revision = old.Revision + 1; }
        }
        void AddTerrain(TerrainDefinition terrain)
        { if (PresetReadOnly) return; ChangePalette("Dodanie terenu", () => { var p = ActivePreset(); p.Terrains.Add(terrain.Copy()); brushIndex = p.Terrains.Count - 1; }); }
        void UpdateTerrain(TerrainDefinition old, TerrainDefinition next, bool apply)
        { UpdateTerrain(old, next, apply, true); }
        void UpdateTerrain(TerrainDefinition old, TerrainDefinition next, bool apply, bool preserve)
        {
            if (PresetReadOnly) return;
            var p = ActivePreset(); int at = p.Terrains.FindIndex(t => t.TerrainId == old.TerrainId && t.VariantId == old.VariantId); if (at < 0) throw new InvalidOperationException("Brak wersji terenu.");
            if (SameTerrainAppearance(old, next)) return;
            ChangePalette("Edycja terenu" + (apply ? " i mapy" : ""), () => p.Terrains[at] = next.Copy(), action =>
            {
                if (!apply) return;
                foreach (var cell in map.Cells.Where(c => c.TerrainId == old.TerrainId && c.VariantId == old.VariantId))
                {
                    var fill = preserve ? next.Variants.FirstOrDefault(f => f.Id == cell.FillVariantId) : NextFill(next.Variants);
                    if (fill == null) continue; // Removed positions keep the historical snapshot and resources.
                    var before = new TerrainState(cell); MapLogic.ApplyTerrain(cell, next, fill);
                    if (!before.Matches(cell)) action.Changes.Add(new CellChange { CellId = cell.Id, Before = before, After = new TerrainState(cell) });
                }
            });
        }
        void RemoveTerrainById(string id)
        {
            if (PresetReadOnly) return;
            var t = ActivePreset().Terrains.FirstOrDefault(v => v.TerrainId == id); if (t == null) return;
            if (ShowEditorMessage("Usunąć teren „" + t.Name + "” z roboczej palety? Istniejące heksy zachowają swoje dane.", "Usuń teren", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes) DeleteTerrain(id);
        }
        void DeleteTerrain(string id)
        {
            if (PresetReadOnly) return;
            var p = ActivePreset(); int at = p.Terrains.FindIndex(t => t.TerrainId == id); if (at < 0) return;
            string selectedId = brushIndex < p.Terrains.Count ? p.Terrains[brushIndex].TerrainId : null;
            ChangePalette("Usunięcie terenu", () => { p.Terrains.RemoveAt(at); int selected = p.Terrains.FindIndex(t => t.TerrainId == selectedId); brushIndex = selected >= 0 ? selected : Math.Max(0, Math.Min(at, p.Terrains.Count - 1)); });
        }
    }
}
