using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class FolderImportDialog : DarkForm
    {
        readonly FolderImport import;
        readonly HashSet<string> reviewed = new HashSet<string>();
        readonly ListView files = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false,
            HideSelection = false, BackColor = Color.FromArgb(29,33,42), ForeColor = Color.WhiteSmoke,
            OwnerDraw = true, ShowItemToolTips = true };
        readonly Label status = new Label { Dock = DockStyle.Top, Height = 30, Text = "Sprawdzanie folderów…" };
        readonly Button add = new EditorButton { Text = "Dodaj", AutoSize = true };
        readonly Button skip = new EditorButton { Text = "Pomiń na teraz", AutoSize = true };
        readonly Button delete = new EditorButton { Text = "Usuń plik…", AutoSize = true };
        readonly Button addRest = new EditorButton { Text = "Dodaj pozostałe z domyślnym kadrem", AutoSize = true };
        readonly Button skipRest = new EditorButton { Text = "Pomiń pozostałe", AutoSize = true };
        readonly Button close = new EditorButton { Text = "Zamknij", AutoSize = true, DialogResult = DialogResult.Cancel };
        bool busy, sizingColumns;
        internal bool Saved { get; private set; }

        internal FolderImportDialog(FolderImport import)
        {
            this.import = import;
            Text = "Kolejka importu grafik"; ClientSize = new Size(748,390); StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(590,300); Padding = new Padding(12);
            files.Columns.Add("Kategoria"); files.Columns.Add("Plik");
            files.Resize += (s,e) => QueueColumnSize();
            files.FontChanged += (s,e) => QueueColumnSize();
            files.DrawItem += (s,e) => { }; // Details view draws each cell in DrawSubItem.
            files.DrawColumnHeader += (s,e) => PaintCell(e.Graphics,e.Bounds,e.Header.Text,Color.FromArgb(49,54,67));
            files.DrawSubItem += (s,e) => PaintCell(e.Graphics,e.Bounds,e.SubItem.Text,
                e.Item.Selected ? Color.FromArgb(63,82,118) : files.BackColor);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = true, Padding = new Padding(0,5,0,0) };
            buttons.Controls.AddRange(new Control[] { add, skip, delete, addRest, skipRest, close });
            Controls.Add(files); Controls.Add(status); Controls.Add(buttons); CancelButton = close;
            files.SelectedIndexChanged += (s,e) => UpdateActions();
            files.DoubleClick += (s,e) => AddSelected();
            add.Click += (s,e) => AddSelected();
            skip.Click += (s,e) => { var item = Selected; if (item != null) { import.Skip(item); Remove(item); } };
            delete.Click += (s,e) => DeleteSelected();
            addRest.Click += (s,e) => AddRemaining();
            skipRest.Click += (s,e) => { foreach (var item in Items()) import.Skip(item); Close(); };
            FormClosing += (s,e) => { if (busy) e.Cancel = true; };
            Shown += async (s,e) => {
                SetBusy(true);
                try
                {
                    foreach (var item in await Task.Run(() => import.Scan()))
                        files.Items.Add(new ListViewItem(new[] { CategoryName(item.Category), item.Name }) { Tag = item, ToolTipText = item.Name });
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Nie można odczytać kolejki", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                finally { SetBusy(false); }
                if (files.Items.Count > 0) files.Items[0].Selected = true;
                QueueColumnSize();
                SetCount();
            };
            UpdateActions();
        }
        void QueueColumnSize()
        {
            if(sizingColumns||!IsHandleCreated||IsDisposed)return;
            sizingColumns=true;
            // Let the native list settle its client rectangle and scrollbar before sizing.
            BeginInvoke((Action)(() => {
                sizingColumns=false;
                int available=files.ClientSize.Width-2;
                if(available<2)return;
                int natural=FolderImport.Categories.Select(CategoryName).Concat(new[] { files.Columns[0].Text })
                    .Max(text => TextRenderer.MeasureText(text,files.Font).Width)+16;
                int category=Math.Min(natural,Math.Max(1,available/3));
                files.Columns[0].Width=category;
                files.Columns[1].Width=available-category;
            }));
        }
        void PaintCell(Graphics graphics,Rectangle bounds,string text,Color background)
        {
            using (var brush = new SolidBrush(background)) graphics.FillRectangle(brush,bounds);
            if(bounds.Width <= 12) return;
            TextRenderer.DrawText(graphics,text,files.Font,Rectangle.Inflate(bounds,-6,0),files.ForeColor,
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        static string CategoryName(ImportCategory category)
        {
            switch (category) {
                case ImportCategory.Backgrounds: return "Tła";
                case ImportCategory.Terrains: return "Grafiki terenów";
                case ImportCategory.Icons: return "Ikony";
                default: return "Obiekty";
            }
        }
        ImportCandidate Selected { get { return files.SelectedItems.Count == 1 ? (ImportCandidate)files.SelectedItems[0].Tag : null; } }
        ImportCandidate[] Items() { return files.Items.Cast<ListViewItem>().Select(row => (ImportCandidate)row.Tag).ToArray(); }
        void SetBusy(bool value) { busy = value; files.Enabled = close.Enabled = !value; UpdateActions(); }
        void SetCount() { status.Text = files.Items.Count == 0 ? "Kolejka jest pusta." :
            "Grafiki do rozpatrzenia: " + files.Items.Count + ". Po udanym zapisie wejściowy plik znika z kolejki."; }
        void UpdateActions()
        {
            add.Enabled = skip.Enabled = delete.Enabled = !busy && Selected != null;
            skipRest.Enabled = !busy && files.Items.Count > 0;
            addRest.Enabled = !busy && Items().Any(item => !reviewed.Contains(item.Key));
        }
        void Remove(ImportCandidate item)
        {
            var row = files.Items.Cast<ListViewItem>().FirstOrDefault(r => ReferenceEquals(r.Tag, item));
            if (row != null)
            {
                int index = row.Index;
                files.Items.Remove(row);
                if (files.Items.Count > 0 && files.SelectedItems.Count == 0) files.Items[Math.Min(index, files.Items.Count-1)].Selected = true;
            }
            SetCount(); UpdateActions();
        }
        void AddSelected()
        {
            var item = Selected;
            if (busy || item == null) return;
            reviewed.Add(item.Key);
            try
            {
                var source = import.Read(item);
                string name = ImportName(item);
                var draft = new ImportDraft(item.Category, source, name, false);
                switch (item.Category)
                {
                    case ImportCategory.Terrains:
                        using (var dialog = new TerrainCropDialog(draft.Terrain, false, true))
                        { if (dialog.ShowDialog(this) != DialogResult.OK) return; draft.Terrain = dialog.Result; }
                        break;
                    case ImportCategory.Backgrounds:
                        using (var dialog = new BackgroundDialog(draft.Background, new Size(640,480), true))
                        { if (dialog.ShowDialog(this) != DialogResult.OK) return; draft.Background = dialog.Result; }
                        break;
                    default:
                        using (var dialog = new SquareImportDialog(source, name))
                        { if (dialog.ShowDialog(this) != DialogResult.OK) return; draft.Square = dialog.Result; }
                        break;
                }
                string note = draft.Save(import, item);
                Saved = true; Remove(item);
                if (note != null) MessageBox.Show(this, note, "Import grafiki", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Nie można dodać grafiki", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { UpdateActions(); }
        }
        void DeleteSelected()
        {
            var item = Selected;
            if (item == null || MessageBox.Show(this, "Przenieść ten plik z dysku do Kosza?\n\n" + item.Path +
                "\n\nJeśli Kosz jest niedostępny, trwałe usunięcie wymaga dodatkowego potwierdzenia.", "Usuń plik z dysku",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try { import.Read(item); RecycleFile.Send(item.Path, Handle); Remove(item); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Nie można usunąć pliku", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        async void AddRemaining()
        {
            var items = Items().Where(item => !reviewed.Contains(item.Key)).ToArray();
            if (items.Length == 0 || MessageBox.Show(this,
                "Dodać nieprzejrzane grafiki?\n\nTła: cała grafika, bez efektów. Tereny: środkowy kadr heksagonalny dla obu orientacji. Ikony i obiekty: cały obraz dopasowany do przezroczystego kwadratu.\n\nPo udanym zapisie pliki wejściowe zostaną usunięte z folderu Import.",
                "Potwierdź kadry domyślne", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            SetBusy(true);
            try
            {
                var result = await Task.Run(() => ImportDefaults(import, items));
                Saved = Saved || result.Result.Count != 0;
                foreach (var item in result.Result) Remove(item);
                var summary = new List<string> { "Zakończone pozycje: " + result.Result.Count, "Pozostawione po błędzie zapisu: " + (items.Length - result.Result.Count) };
                summary.AddRange(result.Notes);
                using (var dialog = new DarkForm { Text = "Wynik importu", ClientSize = new Size(620,320), StartPosition = FormStartPosition.CenterParent })
                {
                    var ok = new EditorButton { Text = "OK", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };
                    dialog.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical,
                        Text = String.Join(Environment.NewLine, summary) });
                    dialog.Controls.Add(ok); dialog.AcceptButton = dialog.CancelButton = ok; dialog.ShowDialog(this);
                }
            }
            finally { SetBusy(false); }
        }
        internal static ImportBatchResult ImportDefaults(FolderImport import, IEnumerable<ImportCandidate> items)
        {
            var result = new ImportBatchResult();
            foreach (var item in items)
            {
                try
                {
                    var source = import.Read(item);
                    string note = new ImportDraft(item.Category, source, ImportName(item), true).Save(import, item);
                    result.Result.Add(item);
                    if (note != null) result.Notes.Add(item.Category + "/" + item.Name + " — " + note);
                }
                catch (Exception ex) { result.Notes.Add(item.Category + "/" + item.Name + " — " + ex.Message); }
            }
            return result;
        }
        static string ImportName(ImportCandidate item)
        {
            string name = Path.GetFileNameWithoutExtension(item.Name).Trim();
            return name.Length == 0 ? "Grafika" : name.Substring(0, Math.Min(200, name.Length));
        }
    }

    internal sealed class ImportBatchResult
    {
        internal readonly List<ImportCandidate> Result = new List<ImportCandidate>();
        internal readonly List<string> Notes = new List<string>();
    }

    internal sealed class ImportDraft
    {
        readonly GraphicSource source;
        readonly string name;
        internal GraphicDefinition Terrain;
        internal IconAsset Square;
        internal MapBackground Background;
        internal ImportDraft(ImportCategory category, GraphicSource source, string name, bool defaults)
        {
            this.source = source; this.name = name;
            if (category == ImportCategory.Terrains)
            {
                Terrain = new GraphicDefinition { Name = name, Source = source, Linked = !defaults };
                Terrain.Seal();
            }
            else if (category == ImportCategory.Backgrounds) Background = new MapBackground { SourceId = source.Id, Source = source };
            else if (defaults)
            {
                var crop = new SquareCropState(new Size(source.Width,source.Height));
                Square = SquareLibraryAsset.Render(source, name, crop.Transform);
            }
        }
        internal string Save(FolderImport import, ImportCandidate item)
        { return import.Commit(item, source, name, Terrain, Square, Background); }
    }
}
