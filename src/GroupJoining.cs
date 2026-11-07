using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        readonly List<string> groupSelectionOrder = new List<string>();

        List<ObjectGroup> SelectedWholeGroups()
        {
            var complete = map.Groups.Where(g => g.Members.All(selectedObjects.Contains)).ToList();
            groupSelectionOrder.RemoveAll(id => !complete.Any(g => g.Id == id));
            // New groups selected by the same area gesture follow document order.
            foreach (var group in complete)
                if (!groupSelectionOrder.Contains(group.Id)) groupSelectionOrder.Add(group.Id);
            return groupSelectionOrder.Select(id => complete.Single(g => g.Id == id)).ToList();
        }

        bool JoinSelectedGroups(string retainedId, string freshName)
        {
            EndCanvasGesture(true);
            var groups = SelectedWholeGroups();
            var members = map.Objects.Where(o => selectedObjects.Contains(o.Id)).Select(o => o.Id).ToList();
            if (members.Count < 2 || map.Groups.Any(g => g.Members.Any(selectedObjects.Contains) && !groups.Contains(g)))
                return false;
            if (groups.Count == 0) return CreateObjectGroup(freshName);
            var retained = groups.FirstOrDefault(g => g.Id == retainedId);
            if (retainedId != null && retained == null) return false;
            if (groups.Count == 1 && groups[0].Members.Count == members.Count) return false;

            var joined = retained == null ? new ObjectGroup { Name = freshName.Trim() } : retained.Copy();
            joined.Members = members;
            var before = map.Groups.Select(g => g.Copy()).ToList();
            var after = map.Groups.Where(g => !groups.Contains(g)).Select(g => g.Copy()).ToList();
            int index = retained == null ? after.Count : map.Groups.TakeWhile(g => g.Id != retained.Id).Count(g => !groups.Contains(g));
            after.Insert(index, joined);
            var action = new EditAction { Label = groups.Count == 1 ? "Dodaj do grupy" : "Połącz grupy",
                BeforeGroups = before, AfterGroups = after };
            CommitObjectTransaction(action);
            SelectGroup(map.Groups.Single(g => g.Id == joined.Id));
            return true;
        }

        void JoinGroupsFromPanel()
        {
            EndCanvasGesture(true);
            var groups = SelectedWholeGroups();
            if (map.Groups.Any(g => g.Members.Any(selectedObjects.Contains) && !groups.Contains(g)))
            {
                ShowEditorMessage("Zaznacz całą grupę, aby dołączyć lub połączyć członków.",
                    "Grupowanie", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                if (groups.Count == 1) { JoinSelectedGroups(groups[0].Id, null); return; }
                string retained = null;
                if (groups.Count > 1)
                {
                    using (var dialog = new MergeGroupsDialog(groups))
                    {
                        if (RunModal(() => dialog.ShowDialog(this)) != DialogResult.OK) return;
                        retained = dialog.RetainedId;
                    }
                }
                if (retained != null) { JoinSelectedGroups(retained, null); return; }
                using (var name = new NameDialog("Nowa trwała grupa", "Grupa"))
                    if (RunModal(() => name.ShowDialog(this)) == DialogResult.OK)
                        JoinSelectedGroups(null, name.Value);
            }
            catch (Exception ex)
            {
                ShowEditorMessage(ex.Message, "Grupowanie", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    internal sealed class MergeGroupsDialog : EditorDialog
    {
        readonly ComboBox choice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly List<ObjectGroup> groups;
        public string RetainedId { get { return choice.SelectedIndex < groups.Count ? groups[choice.SelectedIndex].Id : null; } }

        public MergeGroupsDialog(List<ObjectGroup> groups) : base("Połącz grupy", 145)
        {
            this.groups = groups;
            foreach (var group in groups) choice.Items.Add(group.Name);
            choice.Items.Add("Świeże ustawienia");
            choice.SelectedIndex = 0;
            Row(0, "Ustawienia", choice, true);
            Buttons(1, () => DialogResult = DialogResult.OK);
        }
    }
}
