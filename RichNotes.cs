using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Integration;
using Forms = System.Windows.Forms;

namespace HexMapEditor
{
    public sealed class RichNode
    {
        public string Kind { get; set; }
        public string Text { get; set; }
        public double Size { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strike { get; set; }
        public string Alignment { get; set; }
        public string Marker { get; set; }
        public int Start { get; set; }
        public List<RichNode> Children { get; set; }
        public RichNode() { Children = new List<RichNode>(); Size = 12; Start = 1; }
    }
    public sealed class RichDocument
    {
        public int SchemaVersion { get; set; }
        public List<RichNode> Blocks { get; set; }
        public RichDocument() { SchemaVersion = 1; Blocks = new List<RichNode>(); }
    }

    internal static class NoteCodec
    {
        public const int MaxCharacters = 2 * 1024 * 1024;
        public static RichDocument Read(string json)
        {
            if (json == null || json.Length > MaxCharacters) throw new InvalidDataException("Opis przekracza limit 2 Mi znaków modelu dokumentu.");
            var doc = MapLogic.Serializer().Deserialize<RichDocument>(json);
            if (doc == null || doc.SchemaVersion != 1 || doc.Blocks == null) throw new InvalidDataException("Nieobsługiwany model opisu.");
            int count = 0; foreach (var node in doc.Blocks) Validate(node, "block", 0, ref count); return doc;
        }
        static void Validate(RichNode node, string context, int depth, ref int count)
        {
            if (node == null || node.Children == null || depth > 32 || ++count > 20000 || Double.IsNaN(node.Size) || Double.IsInfinity(node.Size) || node.Size < 1 || node.Size > 400) throw new InvalidDataException("Nieprawidłowy lub zbyt złożony opis.");
            string child;
            if (context == "inline" && (node.Kind == "run" || node.Kind == "break")) { if (node.Children.Count != 0) throw new InvalidDataException("Nieprawidłowy tekst."); return; }
            if (context == "block" && node.Kind == "paragraph") child = "inline";
            else if (context == "block" && node.Kind == "list") { if (node.Marker != "decimal" && node.Marker != "disc") throw new InvalidDataException("Nieznany rodzaj listy."); child = "item"; }
            else if (context == "block" && node.Kind == "table") { if (node.Children.Count < 1 || node.Children.Count > 100) throw new InvalidDataException("Tabela musi mieć 1–100 wierszy."); child = "row"; }
            else if (context == "item" && node.Kind == "item") child = "block";
            else if (context == "row" && node.Kind == "row") { if (node.Children.Count < 1 || node.Children.Count > 100) throw new InvalidDataException("Tabela musi mieć 1–100 kolumn."); child = "cell"; }
            else if (context == "cell" && node.Kind == "cell") child = "block";
            else throw new InvalidDataException("Niedozwolony element opisu: " + node.Kind);
            if (node.Alignment != null && !new[] { "Left", "Center", "Right", "Justify" }.Contains(node.Alignment)) throw new InvalidDataException("Nieprawidłowe wyrównanie.");
            foreach (var n in node.Children) Validate(n, child, depth + 1, ref count);
        }
        public static string Write(FlowDocument doc)
        {
            var model = new RichDocument(); model.Blocks = ReadBlocks(doc.Blocks); string json = MapLogic.Serializer().Serialize(model); Read(json); return json;
        }
        static List<RichNode> ReadBlocks(BlockCollection blocks)
        {
            var result = new List<RichNode>();
            foreach (Block block in blocks)
            {
                var paragraph = block as Paragraph;
                if (paragraph != null)
                {
                    var n = new RichNode { Kind = "paragraph", Alignment = paragraph.TextAlignment.ToString() }; ReadInlines(paragraph.Inlines, n.Children); result.Add(n); continue;
                }
                var list = block as List;
                if (list != null)
                {
                    var n = new RichNode { Kind = "list", Marker = list.MarkerStyle == TextMarkerStyle.Disc || list.MarkerStyle == TextMarkerStyle.Circle || list.MarkerStyle == TextMarkerStyle.Square ? "disc" : "decimal", Start = Math.Max(1, list.StartIndex) };
                    foreach (var item in list.ListItems) n.Children.Add(new RichNode { Kind = "item", Children = ReadBlocks(item.Blocks) }); result.Add(n); continue;
                }
                var table = block as Table;
                if (table != null)
                {
                    var n = new RichNode { Kind = "table" };
                    foreach (var group in table.RowGroups) foreach (var row in group.Rows)
                    {
                        var r = new RichNode { Kind = "row" };
                        foreach (var cell in row.Cells)
                        {
                            r.Children.Add(new RichNode { Kind = "cell", Children = ReadBlocks(cell.Blocks) });
                            for (int c = 1; c < cell.ColumnSpan; c++) r.Children.Add(new RichNode { Kind = "cell" });
                        }
                        n.Children.Add(r);
                    }
                    if (n.Children.Count > 0)
                    {
                        int columns = n.Children.Max(r => r.Children.Count);
                        foreach (var row in n.Children) while (row.Children.Count < columns) row.Children.Add(new RichNode { Kind = "cell" });
                        result.Add(n);
                    }
                    continue;
                }
                var section = block as Section;
                if (section != null) { result.AddRange(ReadBlocks(section.Blocks)); continue; }
                string text = new TextRange(block.ContentStart, block.ContentEnd).Text;
                if (!String.IsNullOrEmpty(text)) result.Add(new RichNode { Kind = "paragraph", Children = new List<RichNode> { new RichNode { Kind = "run", Text = text } } });
            }
            return result;
        }
        static void ReadInlines(InlineCollection inlines, List<RichNode> output)
        {
            foreach (Inline inline in inlines)
            {
                var run = inline as Run;
                if (run != null)
                {
                    output.Add(new RichNode { Kind = "run", Text = run.Text, Size = Math.Max(1, Math.Min(400, run.FontSize)), Bold = run.FontWeight.ToOpenTypeWeight() >= 600, Italic = run.FontStyle == FontStyles.Italic || run.FontStyle == FontStyles.Oblique, Underline = run.TextDecorations.Any(d => d.Location == TextDecorationLocation.Underline), Strike = run.TextDecorations.Any(d => d.Location == TextDecorationLocation.Strikethrough) }); continue;
                }
                if (inline is LineBreak) { output.Add(new RichNode { Kind = "break" }); continue; }
                var span = inline as Span;
                if (span != null) { ReadInlines(span.Inlines, output); continue; }
                string text = new TextRange(inline.ContentStart, inline.ContentEnd).Text;
                if (!String.IsNullOrEmpty(text)) output.Add(new RichNode { Kind = "run", Text = text });
            }
        }
        public static FlowDocument FromPlain(string text)
        {
            var doc = NewFlow();
            foreach (string line in (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) doc.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0, 0, 0, 6) });
            return doc;
        }
        public static FlowDocument FromJson(string json)
        {
            var model = Read(json); var doc = NewFlow(); foreach (var node in model.Blocks) doc.Blocks.Add(CreateBlock(node)); if (doc.Blocks.Count == 0) doc.Blocks.Add(new Paragraph()); return doc;
        }
        static FlowDocument NewFlow() { return new FlowDocument { FontFamily = new FontFamily("Segoe UI"), FontSize = 12, PagePadding = new Thickness(10), Foreground = Brushes.WhiteSmoke, Background = new SolidColorBrush(Color.FromRgb(36, 40, 50)) }; }
        static Block CreateBlock(RichNode n)
        {
            if (n.Kind == "paragraph")
            {
                var p = new Paragraph { Margin = new Thickness(0, 0, 0, 6) }; TextAlignment alignment;
                if (Enum.TryParse<TextAlignment>(n.Alignment, out alignment)) p.TextAlignment = alignment;
                foreach (var item in n.Children)
                {
                    if (item.Kind == "break") { p.Inlines.Add(new LineBreak()); continue; }
                    var r = new Run(item.Text ?? "") { FontSize = item.Size, FontWeight = item.Bold ? FontWeights.Bold : FontWeights.Normal, FontStyle = item.Italic ? FontStyles.Italic : FontStyles.Normal };
                    var decorations = new TextDecorationCollection(); if (item.Underline) decorations.Add(TextDecorations.Underline[0]); if (item.Strike) decorations.Add(TextDecorations.Strikethrough[0]); r.TextDecorations = decorations; p.Inlines.Add(r);
                }
                return p;
            }
            if (n.Kind == "list")
            {
                var list = new List { MarkerStyle = n.Marker == "decimal" ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc, StartIndex = Math.Max(1, n.Start), Margin = new Thickness(18, 0, 0, 6) };
                foreach (var item in n.Children) { var li = new ListItem(); foreach (var child in item.Children) li.Blocks.Add(CreateBlock(child)); if (li.Blocks.Count == 0) li.Blocks.Add(new Paragraph()); list.ListItems.Add(li); } return list;
            }
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 8) }; var group = new TableRowGroup(); table.RowGroups.Add(group);
            int columns = n.Children.Max(r => r.Children.Count); for (int c = 0; c < columns; c++) table.Columns.Add(new TableColumn());
            foreach (var row in n.Children)
            {
                var tr = new TableRow(); foreach (var cell in row.Children) { var tc = NewCell(); tc.Blocks.Clear(); foreach (var child in cell.Children) tc.Blocks.Add(CreateBlock(child)); if (tc.Blocks.Count == 0) tc.Blocks.Add(new Paragraph()); tr.Cells.Add(tc); } group.Rows.Add(tr);
            }
            return table;
        }
        public static TableCell NewCell() { return new TableCell(new Paragraph()) { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(.5), Padding = new Thickness(5) }; }
        public static string Plain(string json)
        {
            return String.Join(Environment.NewLine, Read(json).Blocks.Select(PlainNode));
        }
        static string PlainNode(RichNode n)
        {
            if (n.Kind == "run") return n.Text ?? ""; if (n.Kind == "break") return Environment.NewLine;
            string separator = n.Kind == "paragraph" ? "" : n.Kind == "row" ? "\t" : Environment.NewLine;
            return String.Join(separator, n.Children.Select(PlainNode));
        }
    }

    internal sealed class RichNoteEditor : Forms.UserControl
    {
        readonly ElementHost host = new ElementHost();
        internal readonly RichTextBox Box = new RichTextBox();
        bool loading;
        string lastValid;
        public Action<string, string, string> Changed;
        public Action<bool> HistoryRequested;
        public Action<bool> SaveRequested;
        public Action BreakHistoryGroup;
        public Action<string> Error;
        public Func<int[]> TableSizeRequested;
        public RichNoteEditor()
        {
            host.Dock = Forms.DockStyle.Fill; Controls.Add(host);
            var dock = new DockPanel { LastChildFill = true, Background = new SolidColorBrush(Color.FromRgb(36, 40, 50)) }; host.Child = dock;
            var tools = new WrapPanel { Margin = new Thickness(3) }; DockPanel.SetDock(tools, System.Windows.Controls.Dock.Top); dock.Children.Add(tools);
            Button(tools, "↶", "Cofnij opis", () => { if (HistoryRequested != null) HistoryRequested(false); });
            Button(tools, "↷", "Ponów opis", () => { if (HistoryRequested != null) HistoryRequested(true); });
            var size = new ComboBox { Width = 55, Margin = new Thickness(2), ToolTip = "Rozmiar czcionki" }; foreach (int n in new[] { 8, 10, 12, 14, 16, 18, 24, 32, 48, 72 }) size.Items.Add(n); size.SelectedItem = 12; tools.Children.Add(size);
            size.SelectionChanged += (s, e) => { if (!loading && size.SelectedItem != null) Mutate("format", () => Box.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, Convert.ToDouble(size.SelectedItem))); };
            Button(tools, "B", "Pogrubienie", () => Execute(EditingCommands.ToggleBold));
            Button(tools, "I", "Kursywa", () => Execute(EditingCommands.ToggleItalic));
            Button(tools, "U", "Podkreślenie", () => ToggleDecoration(TextDecorationLocation.Underline));
            Button(tools, "S", "Przekreślenie", () => ToggleDecoration(TextDecorationLocation.Strikethrough));
            Button(tools, "Do lewej", "Wyrównaj do lewej", () => Execute(EditingCommands.AlignLeft));
            Button(tools, "Środek", "Wyśrodkuj", () => Execute(EditingCommands.AlignCenter));
            Button(tools, "Do prawej", "Wyrównaj do prawej", () => Execute(EditingCommands.AlignRight));
            Button(tools, "Wyjustuj", "Wyjustuj akapity", () => Execute(EditingCommands.AlignJustify));
            Button(tools, "Lista •", "Lista punktowana", () => Execute(EditingCommands.ToggleBullets));
            Button(tools, "Lista 1.", "Lista numerowana", () => Execute(EditingCommands.ToggleNumbering));
            Button(tools, "Wklej", "Wklej obsługiwane formatowanie RTF", () => Paste(false));
            Button(tools, "Sam tekst", "Wklej jako zwykły tekst", () => Paste(true));
            Button(tools, "Tabela", "Wstaw edytowalną tabelę", () => { int[] dimensions = TableSizeRequested == null ? new[] { 2, 2 } : TableSizeRequested(); if (dimensions != null) InsertTable(dimensions[0], dimensions[1]); });
            Button(tools, "+wiersz", "Dodaj wiersz poniżej bieżącego", () => ChangeTable("addRow"));
            Button(tools, "−wiersz", "Usuń bieżący wiersz", () => ChangeTable("removeRow"));
            Button(tools, "+kol.", "Dodaj kolumnę po bieżącej", () => ChangeTable("addColumn"));
            Button(tools, "−kol.", "Usuń bieżącą kolumnę", () => ChangeTable("removeColumn"));
            Button(tools, "Usuń tabelę", "Usuń całą bieżącą tabelę", () => ChangeTable("remove"));
            Box.AcceptsTab = true; Box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; Box.IsUndoEnabled = false; Box.Background = dock.Background; Box.Foreground = Brushes.WhiteSmoke; Box.BorderBrush = Brushes.Gray; Box.Document = NoteCodec.FromPlain("");
            lastValid = NoteCodec.Write(Box.Document);
            Box.TextChanged += (s, e) => { if (!loading) Notify("typing"); };
            // Include the editor toolbar (e.g. after clicking its Undo button).
            dock.PreviewKeyDown += (s, e) =>
            {
                if (new[] { Key.Left, Key.Right, Key.Up, Key.Down, Key.Home, Key.End, Key.PageUp, Key.PageDown }.Contains(e.Key) && BreakHistoryGroup != null) BreakHistoryGroup();
                if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == 0) return;
                if (e.Key == Key.S) { e.Handled = true; if (SaveRequested != null) SaveRequested((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0); return; }
                if (e.Key == Key.Z || e.Key == Key.Y) { e.Handled = true; if (HistoryRequested != null) HistoryRequested(e.Key == Key.Y); return; }
                RoutedUICommand command = e.Key == Key.B ? EditingCommands.ToggleBold : e.Key == Key.I ? EditingCommands.ToggleItalic : e.Key == Key.U ? EditingCommands.ToggleUnderline : e.Key == Key.E ? EditingCommands.AlignCenter : e.Key == Key.L ? EditingCommands.AlignLeft : e.Key == Key.R ? EditingCommands.AlignRight : e.Key == Key.J ? EditingCommands.AlignJustify : null;
                if (command != null) { e.Handled = true; Execute(command); }
            };
            Box.PreviewMouseDown += (s, e) => { if (BreakHistoryGroup != null) BreakHistoryGroup(); };
            Box.LostKeyboardFocus += (s, e) => { if (BreakHistoryGroup != null) BreakHistoryGroup(); };
            Box.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => { Paste(false); e.Handled = true; }, (s, e) => { e.CanExecute = true; e.Handled = true; }));
            dock.Children.Add(Box); AccessibleName = "Formatowany opis heksu";
        }
        static void Button(WrapPanel parent, string text, string tip, Action action)
        {
            var b = new Button { Content = text, ToolTip = tip, Margin = new Thickness(2), Padding = new Thickness(5, 2, 5, 2), MinHeight = 25 };
            System.Windows.Automation.AutomationProperties.SetName(b, tip); b.Click += (s, e) => action(); parent.Children.Add(b);
        }
        void Notify(string kind)
        {
            try { string json = NoteCodec.Write(Box.Document); if (Changed != null) Changed(json, NoteCodec.Plain(json), kind); lastValid = json; }
            catch (Exception ex) { loading = true; try { Box.Document = NoteCodec.FromJson(lastValid); } finally { loading = false; } if (Error != null) Error(ex.Message); }
        }
        public void SetContent(string json, string plain)
        {
            loading = true; try { Box.Document = String.IsNullOrEmpty(json) ? NoteCodec.FromPlain(plain) : NoteCodec.FromJson(json); lastValid = NoteCodec.Write(Box.Document); } finally { loading = false; }
        }
        public string ContentJson { get { return NoteCodec.Write(Box.Document); } }
        public void Mutate(string kind, Action action)
        {
            string before = ContentJson; loading = true;
            try { Box.Focus(); Box.BeginChange(); try { action(); } finally { Box.EndChange(); } NoteCodec.Write(Box.Document); }
            catch (Exception ex) { Box.Document = NoteCodec.FromJson(before); if (Error != null) Error(ex.Message); }
            finally { loading = false; } Notify(kind);
        }
        void Execute(RoutedUICommand command) { Mutate("format", () => command.Execute(null, Box)); }
        void ToggleDecoration(TextDecorationLocation location)
        {
            Mutate("format", () =>
            {
                var current = Box.Selection.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection; var result = current == null ? new TextDecorationCollection() : current.Clone();
                bool exists = result.Any(d => d.Location == location); foreach (var d in result.Where(d => d.Location == location).ToList()) result.Remove(d);
                if (!exists) result.Add(location == TextDecorationLocation.Underline ? TextDecorations.Underline[0] : TextDecorations.Strikethrough[0]); Box.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, result);
            });
        }
        public void Paste(bool plain)
        {
            try
            {
                string text = Clipboard.ContainsText() ? Clipboard.GetText() : "";
                if (!plain && Clipboard.ContainsData(DataFormats.Rtf))
                {
                    string rtf = Clipboard.GetData(DataFormats.Rtf) as string;
                    PasteRtf(rtf, text); return;
                }
                Mutate("paste", () => Box.Selection.Text = text);
            }
            catch (Exception ex) { if (Error != null) Error("Nie można wkleić formatowania: " + ex.Message + ". Użyj Wklej jako zwykły tekst."); }
        }
        public void PasteRtf(string rtf, string fallback)
        {
            // Reject active/embedded destinations BEFORE the framework RTF importer sees them.
            // For such payloads the clipboard's Unicode text is the loss-minimizing safe fallback.
            try
            {
                var imported = new FlowDocument();
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(RtfClipboard.Filter(rtf)))) new TextRange(imported.ContentStart, imported.ContentEnd).Load(stream, DataFormats.Rtf);
                var clean = NoteCodec.FromJson(NoteCodec.Write(imported));
                Mutate("paste", () => { using (var stream = new MemoryStream()) { new TextRange(clean.ContentStart, clean.ContentEnd).Save(stream, DataFormats.Rtf); stream.Position = 0; Box.Selection.Load(stream, DataFormats.Rtf); } });
            }
            catch
            {
                if (String.IsNullOrEmpty(fallback)) { if (Error != null) Error("Nie można bezpiecznie odczytać RTF, a schowek nie zawiera zwykłego tekstu. Opis pozostał bez zmian."); return; }
                Mutate("paste", () => Box.Selection.Text = fallback);
            }
        }
        public void InsertTable(int rows, int columns)
        {
            if (rows < 1 || columns < 1 || rows > 100 || columns > 100) throw new InvalidDataException("Tabela: zakres 1–100 wierszy i kolumn.");
            Mutate("table", () =>
            {
                var table = new Table { CellSpacing = 0 }; for (int c = 0; c < columns; c++) table.Columns.Add(new TableColumn()); var group = new TableRowGroup(); table.RowGroups.Add(group);
                for (int r = 0; r < rows; r++) { var row = new TableRow(); for (int c = 0; c < columns; c++) row.Cells.Add(NoteCodec.NewCell()); group.Rows.Add(row); }
                Paragraph paragraph = Box.CaretPosition.Paragraph;
                if (paragraph == null || !(paragraph.Parent is FlowDocument)) Box.Document.Blocks.Add(table); else Box.Document.Blocks.InsertAfter(paragraph, table);
                Box.Document.Blocks.InsertAfter(table, new Paragraph()); Box.CaretPosition = group.Rows[0].Cells[0].ContentStart.GetInsertionPosition(LogicalDirection.Forward);
            });
        }
        internal TableCell CurrentCell()
        {
            for (object node = Box.CaretPosition.Parent; node != null; )
            {
                var cell = node as TableCell; if (cell != null) return cell;
                var content = node as FrameworkContentElement; node = content == null ? null : content.Parent;
            }
            return null;
        }
        public void ChangeTable(string operation)
        {
            var cell = CurrentCell(); if (cell == null) { if (Error != null) Error("Ustaw kursor w komórce tabeli."); return; }
            var row = (TableRow)cell.Parent; var group = (TableRowGroup)row.Parent; var table = (Table)group.Parent;
            Mutate("table", () =>
            {
                int column = row.Cells.IndexOf(cell), at = group.Rows.IndexOf(row);
                if (operation == "remove" || (operation == "removeRow" && group.Rows.Count == 1) || (operation == "removeColumn" && row.Cells.Count == 1))
                {
                    BlockCollection blocks;
                    if (table.Parent is FlowDocument) blocks = ((FlowDocument)table.Parent).Blocks;
                    else if (table.Parent is TableCell) blocks = ((TableCell)table.Parent).Blocks;
                    else if (table.Parent is ListItem) blocks = ((ListItem)table.Parent).Blocks;
                    else blocks = ((Section)table.Parent).Blocks;
                    blocks.Remove(table); if (blocks.Count == 0) blocks.Add(new Paragraph()); Box.CaretPosition = blocks.FirstBlock.ContentStart.GetInsertionPosition(LogicalDirection.Forward); return;
                }
                if (operation == "addRow") { if (group.Rows.Count >= 100) throw new InvalidDataException("Limit 100 wierszy."); var created = new TableRow(); for (int c = 0; c < row.Cells.Count; c++) created.Cells.Add(NoteCodec.NewCell()); group.Rows.Insert(at + 1, created); Box.CaretPosition = created.Cells[0].ContentStart.GetInsertionPosition(LogicalDirection.Forward); }
                else if (operation == "removeRow") { group.Rows.Remove(row); Box.CaretPosition = group.Rows[Math.Min(at, group.Rows.Count - 1)].Cells[0].ContentStart.GetInsertionPosition(LogicalDirection.Forward); }
                else if (operation == "addColumn") { if (row.Cells.Count >= 100) throw new InvalidDataException("Limit 100 kolumn."); foreach (var g in table.RowGroups) foreach (var r in g.Rows) r.Cells.Insert(Math.Min(column + 1, r.Cells.Count), NoteCodec.NewCell()); table.Columns.Add(new TableColumn()); }
                else if (operation == "removeColumn") { foreach (var g in table.RowGroups) foreach (var r in g.Rows) if (r.Cells.Count > column) r.Cells.RemoveAt(column); if (table.Columns.Count > column) table.Columns.RemoveAt(column); Box.CaretPosition = row.Cells[Math.Min(column, row.Cells.Count - 1)].ContentStart.GetInsertionPosition(LogicalDirection.Forward); }
            });
        }
    }
}
