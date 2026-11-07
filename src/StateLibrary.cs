using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class StateFile
    {
        public ObjectStateDefinition Definition;
        public string Path;
        public byte[] Bytes;
    }

    internal sealed class StateFileChange
    {
        public StateFile Before, After;
        public int Index;

        public void Write(bool forward)
        {
            var source = forward ? Before : After;
            var target = forward ? After : Before;
            string sourcePath = source == null ? null : source.Path;
            string targetPath = target == null ? null : target.Path;
            string id = (target ?? source).Definition.Id;
            string directory = System.IO.Path.GetDirectoryName(sourcePath ?? targetPath);
            foreach (var file in Directory.GetFiles(directory, "state-*.json"))
            {
                if (String.Equals(file, sourcePath, StringComparison.OrdinalIgnoreCase)) continue;
                if (StateLibrary.Read(file).Definition.Id == id)
                    throw new IOException("W bibliotece istnieje już plik tego stanu: " + file);
            }
            LibraryFiles.Transition(sourcePath, source == null ? null : source.Bytes,
                targetPath, target == null ? null : target.Bytes);
        }
    }

    internal static class StateLibrary
    {
        public static bool IsBuiltin(string id)
        {
            return id == "Ally" || id == "Neutral" || id == "Enemy";
        }

        public static void Validate(ObjectStateDefinition state)
        {
            if (state == null || String.IsNullOrWhiteSpace(state.Id) || state.Id.Length > 200 ||
                String.IsNullOrWhiteSpace(state.Name) || state.Name.Length > 200 ||
                !MapLogic.ValidColor(state.Color))
                throw new InvalidDataException("Nieprawidłowa definicja stanu.");
        }

        public static StateFile Read(string path)
        {
            if (new FileInfo(path).Length > 16384) throw new InvalidDataException("Za duży plik stanu.");
            byte[] bytes = File.ReadAllBytes(path);
            var json = MapLogic.Serializer();
            var raw = json.DeserializeObject(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF')) as Dictionary<string, object>;
            if (raw == null || !new[]{"Id", "Name", "Color"}.All(key => raw.ContainsKey(key) && raw[key] is string) ||
                !raw.ContainsKey("CanReveal") || !(raw["CanReveal"] is bool))
                throw new InvalidDataException("Brak jawnej definicji stanu.");
            var definition = json.ConvertToType<ObjectStateDefinition>(raw);
            Validate(definition);
            return new StateFile { Definition = definition, Path = path, Bytes = bytes };
        }

        public static List<StateFile> Defaults()
        {
            return ObjectStateDefinition.Defaults().Select(s => new StateFile { Definition = s }).ToList();
        }

        public static List<StateFile> Load(string directory, out string warning)
        {
            Directory.CreateDirectory(directory);
            var result = Defaults();
            var errors = new List<string>();
            foreach (var path in Directory.GetFiles(directory, "state-*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var entry = Read(path);
                    int index = result.FindIndex(item => item.Definition.Id == entry.Definition.Id);
                    if (index >= 0)
                    {
                        if (result[index].Path != null) throw new InvalidDataException("Powtórzone ID stanu.");
                        result[index] = entry;
                    }
                    else
                    {
                        if (result.Count >= 100) throw new InvalidDataException("Limit 100 stanów biblioteki.");
                        result.Add(entry);
                    }
                }
                catch (Exception ex) { errors.Add(System.IO.Path.GetFileName(path) + ": " + ex.Message); }
            }
            warning = String.Join("\n", errors);
            return result;
        }

        public static List<ObjectStateDefinition> ForSave(MapData map)
        {
            var used = new HashSet<string>(map.Objects.Select(o => o.Side));
            used.UnionWith(map.Groups.Select(g => g.StateId));
            return map.ObjectStates.Where(s => IsBuiltin(s.Id) || used.Contains(s.Id)).ToList();
        }
    }

    public sealed partial class MainWindow
    {
        List<StateFile> libraryStates = StateLibrary.Defaults();
        string StatesDirectory { get { return Path.Combine(ObjectsDirectory, "States"); } }

        List<ObjectStateDefinition> NewStateDefinitions()
        {
            return libraryStates.Select(s => s.Definition.Copy()).ToList();
        }

        List<ObjectStateDefinition> AvailableStates()
        {
            var ids = new HashSet<string>(map.ObjectStates.Select(s => s.Id));
            return map.ObjectStates.Concat(libraryStates.Where(s => !ids.Contains(s.Definition.Id))
                .Select(s => s.Definition)).ToList();
        }

        void LoadStateLibrary()
        {
            try
            {
                string warning;
                libraryStates = StateLibrary.Load(StatesDirectory, out warning);
                if (currentPath == null && !dirty && undo.Count == 0)
                {
                    map.ObjectStates = NewStateDefinitions();
                    CaptureSavedState();
                }
                UpdateUI();
                if (warning.Length > 0)
                    ShowEditorMessage(warning, "Pominięte stany", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ShowEditorMessage(ex.Message, "Biblioteka stanów", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        bool ChangeStateFile(ObjectStateDefinition next, string id, bool updateMap, string directory)
        {
            EndCanvasGesture(true);
            int index = libraryStates.FindIndex(s => s.Definition.Id == id);
            var before = index < 0 ? null : libraryStates[index];
            if (next == null && (before == null || StateLibrary.IsBuiltin(id)))
                throw new InvalidOperationException("Tego stanu nie można usunąć.");
            if (next != null && next.Id != id) throw new InvalidOperationException("ID stanu musi pozostać stałe.");
            if (before == null && libraryStates.Count >= 100) throw new InvalidOperationException("Limit 100 stanów biblioteki.");

            var action = new EditAction { Label = next == null ? "Usuń stan z biblioteki" : "Zapisz stan w bibliotece" };
            StateFile after = null;
            if (next != null)
            {
                StateLibrary.Validate(next);
                directory = Path.GetFullPath(directory);
                Directory.CreateDirectory(directory);
                string path = PresetFiles.Destination(directory, "state-" + PresetFiles.Stem(next.Name), before == null ? null : before.Path);
                if (!Path.GetFileName(path).StartsWith("state-", StringComparison.Ordinal))
                    throw new IOException("Za długa ścieżka stanu.");
                after = new StateFile { Definition = next.Copy(), Path = path, Bytes = new UTF8Encoding(false).GetBytes(json.Serialize(next)) };
            }
            if (before == null || after == null || before.Path != after.Path || !before.Bytes.SequenceEqual(after.Bytes))
                action.StateFileChange = new StateFileChange { Before = before, After = after, Index = index < 0 ? libraryStates.Count : index };

            bool reconcile = false;
            if (updateMap && next != null)
            {
                var old = map.ObjectStates.FirstOrDefault(s => s.Id == id);
                if (old == null || json.Serialize(old) != json.Serialize(next))
                {
                    IncludeState(action, next, true);
                    reconcile = old != null && old.CanReveal != next.CanReveal;
                }
            }
            if (action.StateFileChange == null && action.AfterStates == null) return false;
            return CommitObjectTransaction(action, null, reconcile);
        }

        void ApplyStateFile(StateFileChange change, bool forward)
        {
            change.Write(forward);
            var from = forward ? change.Before : change.After;
            var to = forward ? change.After : change.Before;
            if (from != null) libraryStates.RemoveAll(s => s.Definition.Id == from.Definition.Id);
            if (to != null) libraryStates.Insert(Math.Min(change.Index, libraryStates.Count), to);
        }

        void IncludeState(EditAction action, ObjectStateDefinition state, bool replace)
        {
            int index = map.ObjectStates.FindIndex(s => s.Id == state.Id);
            if (index >= 0 && !replace) return;
            action.BeforeStates = map.ObjectStates.Select(s => s.Copy()).ToList();
            action.AfterStates = action.BeforeStates.ToList();
            if (index < 0) action.AfterStates.Add(state.Copy());
            else action.AfterStates[index] = state.Copy();
        }

        void IncludeLibraryState(EditAction action, string id)
        {
            if (id == null || map.ObjectStates.Any(s => s.Id == id)) return;
            var entry = libraryStates.FirstOrDefault(s => s.Definition.Id == id);
            if (entry != null) IncludeState(action, entry.Definition, false);
        }

        void ChangeStateFromMenu(string id, bool remove)
        {
            var library = libraryStates.FirstOrDefault(s => s.Definition.Id == id);
            var document = map.ObjectStates.FirstOrDefault(s => s.Id == id);
            try
            {
                if (remove)
                {
                    if (library == null || StateLibrary.IsBuiltin(id)) return;
                    string text = "Usunąć z biblioteki stan „" + library.Definition.Name + "”? Mapa zachowa swoją definicję. " +
                        "Plik zostanie usunięty bez Kosza. Cofnięcie jest dostępne w historii tej sesji (do 300 wpisów).";
                    if (ShowEditorMessage(text, "Usuń stan z biblioteki", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                        ChangeStateFile(null, id, false, StatesDirectory);
                    return;
                }
                if (library == null && document != null)
                {
                    ChangeStateFile(document, id, false, StatesDirectory);
                    return;
                }
                using (var dialog = new ObjectStateDialog(library == null ? null : library.Definition))
                {
                    if (RunModal(() => dialog.ShowDialog(this)) != DialogResult.OK) return;
                    if (id == null) AddObjectState(dialog.StateName, dialog.StateColor, dialog.Allows);
                    else
                    {
                        var next = new ObjectStateDefinition { Id = id, Name = dialog.StateName,
                            Color = dialog.StateColor, CanReveal = dialog.Allows };
                        ChangeStateFile(next, id, dialog.UpdateMap, StatesDirectory);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowEditorMessage(ex.Message, "Biblioteka stanów", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
