using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class StateRemovalDialog : EditorDialog
    {
        readonly ComboBox replacement = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly CheckBox deleteFile = new CheckBox { Text = "Usuń również z biblioteki", AutoSize = true };
        public Func<bool> CanAccept;
        public string ReplacementId { get { return ((ObjectStateDefinition)replacement.SelectedItem).Id; } }
        public bool DeleteFile { get { return deleteFile.Checked; } }

        public StateRemovalDialog(ObjectStateDefinition state, List<ObjectStateDefinition> choices,
            IEnumerable<string> uses, bool canDeleteFile) : base("Usuń stan z mapy", 325)
        {
            ClientSize = new System.Drawing.Size(540, 325);
            var lines = uses.ToArray();
            Row(0, "Stan", new Label { Text = state.Name + (lines.Length == 0 ? " (nieużywany)" : " (używany)"), AutoEllipsis = true }, true);
            Row(1, "Referencje", new ListBox { IntegralHeight = false, HorizontalScrollbar = true, DataSource = lines }, true);
            Layout.RowStyles[1].Height = 125;
            replacement.Items.AddRange(choices.Cast<object>().ToArray());
            replacement.SelectedItem = choices.Single(item => item.Id == "Neutral");
            Row(2, "Zamiennik", replacement, true);
            deleteFile.Enabled = canDeleteFile;
            if (!canDeleteFile) deleteFile.Text += " (brak pliku)";
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Layout.Controls.Add(deleteFile, 0, 3);
            Layout.SetColumnSpan(deleteFile, 3);
            var note = new Label { Text = "Plik biblioteki można przywrócić w historii sesji. Zatwierdzenie zastąpi wskazane referencje.", Dock = DockStyle.Fill };
            Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            Layout.Controls.Add(note, 0, 4);
            Layout.SetColumnSpan(note, 3);
            Buttons(5, () => { if (CanAccept == null || CanAccept()) DialogResult = DialogResult.OK; });
            Ok.Text = "Usuń stan";
        }
    }

    public sealed partial class MainWindow
    {
        bool StateUsed(string id)
        {
            return map.Objects.Any(obj => obj.Side == id) || map.Groups.Any(group => group.StateId == id);
        }

        void RemoveDocumentStateFromMenu(string id, StateFile entry)
        {
            if (StateLibrary.IsBuiltin(id)) return;
            var states = AvailableStates();
            var state = states.FirstOrDefault(item => item.Id == id);
            if (state == null) return;
            var uses = new List<string>();
            foreach (var obj in map.Objects.Where(item => item.Side == id))
            {
                var group = GroupRules.For(map, obj.Id);
                string name = !previewWithoutFog && ObjectFogLevel(obj) != 0 ? "???" : obj.Name;
                uses.Add("Baza: " + name + (group != null && group.StateId != null ? " (pod nakładką grupy)" : ""));
            }
            uses.AddRange(map.Groups.Where(group => group.StateId == id).Select(group => "Nakładka grupy: " + group.Name));
            using (var dialog = new StateRemovalDialog(state, states.Where(item => item.Id != id).ToList(),
                uses, entry != null && File.Exists(entry.Path)))
            {
                dialog.CanAccept = () => uses.Count == 0 || GuardLayer(MapLayer.Objects, dialog);
                if (RunModal(() => dialog.ShowDialog(this)) == DialogResult.OK)
                    RemoveDocumentState(id, dialog.ReplacementId, dialog.DeleteFile);
            }
        }

        bool RemoveDocumentState(string id, string replacementId, bool deleteFile)
        {
            if (StateLibrary.IsBuiltin(id) || id == replacementId) return false;
            var states = AvailableStates();
            if (!states.Any(item => item.Id == id)) return false;
            var replacement = states.Single(item => item.Id == replacementId);
            bool used = StateUsed(id);
            var action = new EditAction
            {
                Label = "Usuń stan z mapy",
                BeforeStates = map.ObjectStates.Select(item => item.Copy()).ToList(),
                AfterStates = map.ObjectStates.Where(item => item.Id != id).Select(item => item.Copy()).ToList(),
                BeforeExcludedStates = new List<string>(map.ExcludedStateIds),
                AfterExcludedStates = map.ExcludedStateIds.Concat(new[] { id }).ToList()
            };
            if (used && !action.AfterStates.Any(item => item.Id == replacementId))
                action.AfterStates.Add(replacement.Copy());

            foreach (var obj in map.Objects.Where(item => item.Side == id))
            {
                var next = obj.Copy();
                next.Side = replacementId;
                if (!replacement.CanReveal) next.RevealsFog = false;
                action.Objects.Add(new ObjectChange { Before = obj.Copy(), After = next, Index = map.Objects.IndexOf(obj) });
            }
            if (map.Groups.Any(group => group.StateId == id))
            {
                action.BeforeGroups = map.Groups.Select(group => group.Copy()).ToList();
                action.AfterGroups = map.Groups.Select(group =>
                {
                    var next = group.Copy();
                    if (next.StateId == id) next.StateId = replacementId;
                    return next;
                }).ToList();
            }
            if (deleteFile)
            {
                int index = libraryStates.FindIndex(item => item.Definition.Id == id);
                if (index < 0 || !File.Exists(libraryStates[index].Path))
                    throw new IOException("Plik stanu zniknął. Ponów próbę bez usuwania pliku.");
                action.StateFileChange = new StateFileChange { Before = libraryStates[index], Index = index };
            }
            return CommitObjectTransaction(action, null, used);
        }
    }
}
