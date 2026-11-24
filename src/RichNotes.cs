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
using System.Windows.Media.Imaging;
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
        public string Png { get; set; }
        public double DisplayWidth { get; set; }
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
        public const int MaxCharacters = 24 * 1024 * 1024;
        internal const long MaxMapCharacters = 64L * 1024 * 1024;
        internal static long LengthOf(string rich, string plain)
        {
            return (long)(rich ?? "").Length + (plain ?? "").Length;
        }
        internal static void CheckMapBudget(long characters)
        {
            if(characters>MaxMapCharacters)throw new InvalidDataException("Opisy tej mapy przekraczają wspólny limit 64 Mi znaków.");
        }
        public static RichDocument Read(string json)
        {
            if (json == null || json.Length > MaxCharacters) throw new InvalidDataException("Opis przekracza limit 24 Mi znaków modelu dokumentu.");
            var doc = MapLogic.Serializer().Deserialize<RichDocument>(json);
            if (doc == null || doc.SchemaVersion != 1 || doc.Blocks == null) throw new InvalidDataException("Nieobsługiwany model opisu.");
            int count = 0, images=0; foreach (var node in doc.Blocks) Validate(node, "block", 0, ref count,ref images); return doc;
        }
        static void Validate(RichNode node, string context, int depth, ref int count,ref int images)
        {
            if (node == null || node.Children == null || depth > 32 || ++count > 20000 || Double.IsNaN(node.Size) || Double.IsInfinity(node.Size) || node.Size < 1 || node.Size > 400) throw new InvalidDataException("Nieprawidłowy lub zbyt złożony opis.");
            if(context=="inline"&&node.Kind=="image")
            {
                if(++images>NoteImages.MaxPerNote||node.Children.Count!=0)throw new InvalidDataException("Limit 12 obrazów w opisie.");
                NoteImages.Validate(node);
                return;
            }
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
            foreach (var n in node.Children) Validate(n, child, depth + 1, ref count,ref images);
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
                var image=NoteImages.BlockImage(block);
                if(image!=null)
                {
                    var saved=NoteImages.Read(image);
                    if(saved!=null)result.Add(new RichNode { Kind="paragraph", Children=new List<RichNode>{saved} });
                    continue;
                }
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
                var container=inline as InlineUIContainer;
                var image=container==null?null:container.Child as Image;
                if(image!=null){var node=NoteImages.Read(image);if(node!=null)output.Add(node);continue;}
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
            var model = Read(json); var doc = NewFlow(); AddBlocks(doc.Blocks,model.Blocks); return doc;
        }
        static void AddBlocks(BlockCollection blocks,IEnumerable<RichNode> nodes)
        {
            foreach(var node in nodes)
            {
                if(node.Kind!="paragraph"||!node.Children.Any(child=>child.Kind=="image"))
                {blocks.Add(CreateBlock(node));continue;}
                // Map12 stores pictures in paragraphs. Give each picture its own
                // layout block without losing the runs on either side.
                var text=new RichNode { Kind="paragraph",Alignment=node.Alignment };
                foreach(var child in node.Children)
                {
                    if(child.Kind!="image"){text.Children.Add(child);continue;}
                    if(text.Children.Count!=0){blocks.Add(CreateBlock(text));text.Children.Clear();}
                    blocks.Add(NoteImages.CreateBlock(child));
                }
                if(text.Children.Count!=0)blocks.Add(CreateBlock(text));
            }
            if(blocks.Count==0)blocks.Add(new Paragraph());
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
                foreach (var item in n.Children) { var li = new ListItem(); AddBlocks(li.Blocks,item.Children); list.ListItems.Add(li); } return list;
            }
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 8) }; var group = new TableRowGroup(); table.RowGroups.Add(group);
            int columns = n.Children.Max(r => r.Children.Count); for (int c = 0; c < columns; c++) table.Columns.Add(new TableColumn());
            foreach (var row in n.Children)
            {
                var tr = new TableRow(); foreach (var cell in row.Children) { var tc = NewCell(); tc.Blocks.Clear(); AddBlocks(tc.Blocks,cell.Children); tr.Cells.Add(tc); } group.Rows.Add(tr);
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
        internal readonly MenuItem PlainPasteItem = new MenuItem { Header = "Wklej jako zwykły tekst", InputGestureText = "Ctrl+Shift+V" };
        bool loading;
        string lastValid;
        public Action<string, string, string> Changed;
        public Action<bool> HistoryRequested;
        public Func<bool,bool> HistoryAvailable;
        public Action<bool> SaveRequested;
        public Action BreakHistoryGroup;
        public Action<string> Error;
        public Func<int[]> TableSizeRequested;
        public Func<string> ImagePathRequested;
        readonly Forms.Panel numberBar=new Forms.Panel{Dock=Forms.DockStyle.Top,Height=35,Padding=new Forms.Padding(3,2,3,2)};
        readonly Dictionary<Button,Func<bool>> toolStates=new Dictionary<Button,Func<bool>>();
        Button undoTool,redoTool;
        internal void AddExpandButton(Forms.Button button)
        {
            button.Dock=Forms.DockStyle.Right;
            numberBar.Controls.Add(button);
        }
        public RichNoteEditor()
        {
            host.Dock = Forms.DockStyle.Fill; Controls.Add(host);
            numberBar.Controls.Add(new Forms.Label{Text="Rozmiar czcionki",AutoSize=true,Location=new System.Drawing.Point(6,9)});
            var size=new ScrubNumber{Minimum=1,Maximum=400,Value=12,Width=62,AccessibleName="Rozmiar czcionki opisu"};
            size.Location=new System.Drawing.Point(111,4);
            numberBar.Controls.Add(size);Controls.Add(numberBar);
            size.EditFinished+=(s,e)=>{if(!e.Cancelled&&e.Changed)FormatText(()=>Box.Selection.ApplyPropertyValue(TextElement.FontSizeProperty,(double)size.Value));};
            Box.SelectionChanged+=(s,e)=>{
                object current=Box.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                if(!size.IsEditing&&current is double)size.Value=(decimal)(double)current;
            };
            var dock = new DockPanel { LastChildFill = true, Background = new SolidColorBrush(Color.FromRgb(36, 40, 50)) }; host.Child = dock;
            var tools = new WrapPanel { Margin = new Thickness(3) }; DockPanel.SetDock(tools, System.Windows.Controls.Dock.Top); dock.Children.Add(tools);
            undoTool=Button(tools, "↶", "Cofnij opis", () => { if (HistoryRequested != null) HistoryRequested(false); });
            redoTool=Button(tools, "↷", "Ponów opis", () => { if (HistoryRequested != null) HistoryRequested(true); });
            Track(Button(tools,"B","Pogrubienie",()=>Execute(EditingCommands.ToggleBold)),TextElement.FontWeightProperty,FontWeights.Bold);
            Track(Button(tools,"I","Kursywa",()=>Execute(EditingCommands.ToggleItalic)),TextElement.FontStyleProperty,FontStyles.Italic);
            TrackDecoration(Button(tools,"U","Podkreślenie",()=>ToggleDecoration(TextDecorationLocation.Underline)),TextDecorationLocation.Underline);
            TrackDecoration(Button(tools,"S","Przekreślenie",()=>ToggleDecoration(TextDecorationLocation.Strikethrough)),TextDecorationLocation.Strikethrough);
            Track(Button(tools,"⇤","Wyrównaj do lewej",()=>Execute(EditingCommands.AlignLeft)),Paragraph.TextAlignmentProperty,TextAlignment.Left);
            Track(Button(tools,"↔","Wyśrodkuj",()=>Execute(EditingCommands.AlignCenter)),Paragraph.TextAlignmentProperty,TextAlignment.Center);
            Track(Button(tools,"⇥","Wyrównaj do prawej",()=>Execute(EditingCommands.AlignRight)),Paragraph.TextAlignmentProperty,TextAlignment.Right);
            Track(Button(tools,"≡","Wyjustuj akapity",()=>Execute(EditingCommands.AlignJustify)),Paragraph.TextAlignmentProperty,TextAlignment.Justify);
            toolStates.Add(Button(tools,"•≡","Lista punktowana",()=>Execute(EditingCommands.ToggleBullets)),()=>IsList(false));
            toolStates.Add(Button(tools,"1≡","Lista numerowana",()=>Execute(EditingCommands.ToggleNumbering)),()=>IsList(true));
            Box.SelectionChanged+=(s,e)=>{if(!loading)UpdateTools();};
            Button(tools, "▣", "Wklej tekst i obrazy", () => Paste(false));
            Button(tools, "▣T", "Wklej jako zwykły tekst", () => Paste(true));
            Button(tools, "▦", "Wstaw edytowalną tabelę", () => { int[] dimensions = TableSizeRequested == null ? new[] { 2, 2 } : TableSizeRequested(); if (dimensions != null) InsertTable(dimensions[0], dimensions[1]); });
            Button(tools, "⊞↓", "Dodaj wiersz poniżej bieżącego", () => ChangeTable("addRow"));
            Button(tools, "⊟↓", "Usuń bieżący wiersz", () => ChangeTable("removeRow"));
            Button(tools, "⊞→", "Dodaj kolumnę po bieżącej", () => ChangeTable("addColumn"));
            Button(tools, "⊟→", "Usuń bieżącą kolumnę", () => ChangeTable("removeColumn"));
            Button(tools, "▦×", "Usuń całą bieżącą tabelę", () => ChangeTable("remove"));
            Button(tools,"▧","Wstaw obraz z pliku",InsertImageFile);
            Button(tools,"▧−","Zmniejsz zaznaczony obraz",()=>ResizeImage(.8));
            Button(tools,"▧+","Powiększ zaznaczony obraz",()=>ResizeImage(1.25));
            Box.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,new MouseButtonEventHandler(SelectImage));
            Box.AcceptsTab = true; Box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; Box.IsUndoEnabled = false; Box.Background = dock.Background; Box.Foreground = Brushes.WhiteSmoke; Box.BorderBrush = Brushes.Gray; Box.Document = NoteCodec.FromPlain("");
            lastValid = NoteCodec.Write(Box.Document);
            Box.TextChanged += (s, e) => { if (!loading) Notify("typing"); };
            // Include the editor toolbar (e.g. after clicking its Undo button).
            dock.PreviewKeyDown += (s, e) =>
            {
                if (new[] { Key.Left, Key.Right, Key.Up, Key.Down, Key.Home, Key.End, Key.PageUp, Key.PageDown }.Contains(e.Key) && BreakHistoryGroup != null) BreakHistoryGroup();
                if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == 0) return;
                if (HandlePlainPasteKey(e.Key, Keyboard.Modifiers)) { e.Handled = true; return; }
                if (e.Key == Key.S) { e.Handled = true; if (SaveRequested != null) SaveRequested((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0); return; }
                if (HandleHistoryKey(e.Key, Keyboard.Modifiers)) { e.Handled = true; return; }
                RoutedUICommand command = e.Key == Key.B ? EditingCommands.ToggleBold : e.Key == Key.I ? EditingCommands.ToggleItalic : e.Key == Key.U ? EditingCommands.ToggleUnderline : e.Key == Key.E ? EditingCommands.AlignCenter : e.Key == Key.L ? EditingCommands.AlignLeft : e.Key == Key.R ? EditingCommands.AlignRight : e.Key == Key.J ? EditingCommands.AlignJustify : null;
                if (command != null) { e.Handled = true; Execute(command); }
            };
            Box.PreviewMouseDown += (s, e) => { if (BreakHistoryGroup != null) BreakHistoryGroup(); };
            Box.LostKeyboardFocus += (s, e) => { if (BreakHistoryGroup != null) BreakHistoryGroup(); };
            Box.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => { Paste(false); e.Handled = true; }, (s, e) => { e.CanExecute = true; e.Handled = true; }));
            dock.Children.Add(Box); AccessibleName = "Formatowany opis heksu";
            Box.SizeChanged += (s, e) => FitTables();
            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Header = "Wytnij", Command = ApplicationCommands.Cut, CommandTarget = Box });
            menu.Items.Add(new MenuItem { Header = "Kopiuj", Command = ApplicationCommands.Copy, CommandTarget = Box });
            menu.Items.Add(new MenuItem { Header = "Wklej", Command = ApplicationCommands.Paste, CommandTarget = Box });
            menu.Items.Add(new Separator()); menu.Items.Add(PlainPasteItem); PlainPasteItem.Click += (s, e) => Paste(true);
            menu.Opened += (s, e) => { try { PlainPasteItem.IsEnabled = HasPlainClipboardText(); } catch { PlainPasteItem.IsEnabled = false; } }; Box.ContextMenu = menu;
            UpdateTools();
        }
        void Track(Button button,DependencyProperty property,object value)
        {toolStates.Add(button,()=>Equals(Box.Selection.GetPropertyValue(property),value));}
        void TrackDecoration(Button button,TextDecorationLocation location)
        {
            toolStates.Add(button,()=>{
                var decorations=Box.Selection.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
                return decorations!=null&&decorations.Any(item=>item.Location==location);
            });
        }
        bool IsList(bool numbered)
        {
            var item=Box.Selection.Start.Paragraph==null?null:Box.Selection.Start.Paragraph.Parent as ListItem;
            var list=item==null?null:item.Parent as List;
            if(list==null)return false;
            bool bullet=list.MarkerStyle==TextMarkerStyle.Disc||list.MarkerStyle==TextMarkerStyle.Circle||list.MarkerStyle==TextMarkerStyle.Square;
            return numbered!=bullet;
        }
        static readonly Brush ToolBackground=new SolidColorBrush(Color.FromRgb(49,54,67));
        static readonly Brush ToolAccent=new SolidColorBrush(Color.FromRgb(42,70,105));
        static readonly ControlTemplate ToolTemplate=CreateToolTemplate();
        static ControlTemplate CreateToolTemplate()
        {
            // Use a local template so the Windows disabled theme cannot replace
            // the dark background and light icon with a white empty square.
            var border=new FrameworkElementFactory(typeof(Border),"Frame");
            border.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty,new Thickness(1));
            var content=new FrameworkElementFactory(typeof(ContentPresenter),"Glyph");
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);
            content.SetValue(FrameworkElement.MarginProperty,new TemplateBindingExtension(Control.PaddingProperty));
            border.AppendChild(content);
            var template=new ControlTemplate(typeof(Button)){VisualTree=border};
            var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};
            hover.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.LightSteelBlue,"Frame"));
            template.Triggers.Add(hover);
            var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};
            focus.Setters.Add(new Setter(Border.BorderBrushProperty,Brushes.WhiteSmoke,"Frame"));
            template.Triggers.Add(focus);
            var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.45,"Frame"));
            template.Triggers.Add(disabled);
            return template;
        }
        internal void UpdateTools()
        {
            if(undoTool==null||redoTool==null)return;
            undoTool.IsEnabled=HistoryAvailable!=null&&HistoryAvailable(false);
            redoTool.IsEnabled=HistoryAvailable!=null&&HistoryAvailable(true);
            foreach(var pair in toolStates)pair.Key.Background=pair.Value()?ToolAccent:ToolBackground;
        }
        internal bool HandleHistoryKey(Key key, System.Windows.Input.ModifierKeys modifiers)
        {
            const System.Windows.Input.ModifierKeys control = System.Windows.Input.ModifierKeys.Control;
            if ((key != Key.Z && key != Key.Y) || (modifiers & control) == 0) return false;
            if (modifiers == control && HistoryRequested != null) HistoryRequested(key == Key.Y);
            return true;
        }

        internal void FitTables() { FitTablesIn(Box.Document.Blocks); }
        static void FitTablesIn(BlockCollection blocks)
        {
            foreach (Block block in blocks)
            {
                var table = block as Table;
                if (table != null) { foreach (var column in table.Columns) if (!column.Width.IsAuto) column.Width = GridLength.Auto; foreach (var group in table.RowGroups) foreach (var row in group.Rows) foreach (var cell in row.Cells) FitTablesIn(cell.Blocks); }
                var list = block as List; if (list != null) foreach (var item in list.ListItems) FitTablesIn(item.Blocks);
                var section = block as Section; if (section != null) FitTablesIn(section.Blocks);
            }
        }
        sealed class NoteToolButton : Button
        {
            protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
            {
                bool canFocus=Focusable;
                Focusable=false;
                try { base.OnMouseLeftButtonDown(e); }
                finally { Focusable=canFocus; }
            }
        }
        static Button Button(WrapPanel parent, string text, string tip, Action action)
        {
            var b = new NoteToolButton { Content = text, ToolTip = tip, Margin = new Thickness(2), Padding = new Thickness(5, 2, 5, 2),
                MinHeight=32,MinWidth=33,FontSize=20,Background=ToolBackground,Foreground=Brushes.WhiteSmoke,BorderBrush=Brushes.SlateGray,Template=ToolTemplate };
            System.Windows.Automation.AutomationProperties.SetName(b, tip); b.Click += (s, e) => action(); parent.Children.Add(b);return b;
        }
        void Notify(string kind)
        {
            try { string json = NoteCodec.Write(Box.Document); if (Changed != null) Changed(json, NoteCodec.Plain(json), kind); lastValid = json; }
            catch (Exception ex) { loading = true; try { Box.Document = NoteCodec.FromJson(lastValid); } finally { loading = false; } if (Error != null) Error(ex.Message); }
            UpdateTools();
        }
        public void SetContent(string json, string plain)
        {
            loading = true; try { Box.Document = String.IsNullOrEmpty(json) ? NoteCodec.FromPlain(plain) : NoteCodec.FromJson(json); lastValid = NoteCodec.Write(Box.Document); } finally { loading = false; }
            UpdateTools();
        }
        public string ContentJson { get { return NoteCodec.Write(Box.Document); } }
        public void Mutate(string kind, Action action)
        {
            string before = lastValid; loading = true;
            try { Box.BeginChange(); try { action(); } finally { Box.EndChange(); } }
            catch (Exception ex) { Box.Document = NoteCodec.FromJson(before); if (Error != null) Error(ex.Message); }
            finally { loading = false; } Notify(kind);
        }
        void Execute(RoutedUICommand command)
        {
            if(command==EditingCommands.ToggleBold||command==EditingCommands.ToggleItalic||command==EditingCommands.ToggleUnderline)
                FormatText(()=>command.Execute(null,Box));
            else Mutate("format",()=>command.Execute(null,Box));
        }
        void FormatText(Action change)
        {
            Mutate("format",()=>{
                var caret=Box.CaretPosition;
                var start=Box.Selection.IsEmpty?WordEdge(caret,LogicalDirection.Backward):caret;
                var end=Box.Selection.IsEmpty?WordEdge(caret,LogicalDirection.Forward):caret;
                bool insideWord=start.CompareTo(caret)<0&&end.CompareTo(caret)>0;
                if(!insideWord){change();return;}
                // Anchor inside the word so a Run split during formatting does
                // not move the restored caret to an adjacent space.
                var restore=caret.GetPositionAtOffset(0,LogicalDirection.Forward);
                Box.Selection.Select(start,end);
                try { change(); }
                finally { Box.CaretPosition=restore; }
            });
        }
        static TextPointer WordEdge(TextPointer caret,LogicalDirection direction)
        {
            var edge=caret;
            var position=caret;
            int step=direction==LogicalDirection.Forward?1:-1;
            while(position!=null)
            {
                if(position.GetPointerContext(direction)==TextPointerContext.Text)
                {
                    var text=position.GetTextInRun(direction);
                    int count=0;
                    while(count<text.Length&&WordCharacter(text[step>0?count:text.Length-1-count]))count++;
                    if(count>0)edge=position.GetPositionAtOffset(step*count,direction);
                    if(count<text.Length)return edge;
                    position=edge;
                }
                else
                {
                    var inline=position.GetAdjacentElement(direction);
                    if(!(inline is Run)&&!(inline is Span))break;
                    position=position.GetNextContextPosition(direction);
                }
            }
            return edge;
        }
        static bool WordCharacter(char value)
        {
            var category=Char.GetUnicodeCategory(value);
            return Char.IsLetterOrDigit(value)||category==System.Globalization.UnicodeCategory.NonSpacingMark||
                category==System.Globalization.UnicodeCategory.SpacingCombiningMark||category==System.Globalization.UnicodeCategory.ConnectorPunctuation;
        }
        void ToggleDecoration(TextDecorationLocation location)
        {
            FormatText(() =>
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
                bool hasText = HasPlainClipboardText();
                string text = Clipboard.ContainsText(TextDataFormat.UnicodeText) ? Clipboard.GetText(TextDataFormat.UnicodeText) : hasText ? Clipboard.GetText(TextDataFormat.Text) : "";
                if(plain){if(hasText)Mutate("paste",()=>Box.Selection.Text=text);return;}
                var data=Clipboard.GetDataObject();
                if(data==null)return;
                PasteData(data,text);
            }
            catch (Exception ex) { if (Error != null) Error("Nie można wkleić formatowania: " + ex.Message + ". Użyj Wklej jako zwykły tekst."); }
        }
        internal static bool HasPlainClipboardText() { return Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text); }
        internal bool HandlePlainPasteKey(Key key, System.Windows.Input.ModifierKeys modifiers)
        {
            if (key != Key.V || modifiers != (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift) || !Box.IsKeyboardFocusWithin) return false;
            Paste(true); return true;
        }
        internal void PasteData(IDataObject data,string fallback)
        {
            int count=0;
            FlowDocument document=null;
            if(data.GetDataPresent(DataFormats.Rtf,false))
            {
                try { document=ReadRtf(data.GetData(DataFormats.Rtf,false) as string,out count); }
                catch(InvalidDataException) { }
                catch(ArgumentException) { }
                catch(System.Windows.Markup.XamlParseException) { }
            }
            if(count==0&&data.GetDataPresent(DataFormats.Html,false))
            {
                var html=NoteHtmlClipboard.Parse(data.GetData(DataFormats.Html,false) as string);
                if(html!=null&&(document==null||Images(html.Blocks).Any()))document=html;
            }
            if(document!=null&&(Images(document.Blocks).Any()||!String.IsNullOrWhiteSpace(new TextRange(document.ContentStart,document.ContentEnd).Text)))
            {
                PasteDocument(document);return;
            }
            if(!String.IsNullOrWhiteSpace(fallback)){Mutate("paste",()=>Box.Selection.Text=fallback);return;}
            var bytes=ClipboardRaster.ReadEncoded(data,NoteImages.MaxSourceBytes);
            if(bytes!=null)
            {
                try { InsertImage(NoteImages.FromBytes(bytes));return; }
                catch(ImageBudgetException) { throw; }
                catch(ArgumentException) { }
                catch(InvalidDataException) { }
            }
            var bitmap=ClipboardRaster.ReadBitmap(data);
            if(bitmap!=null){InsertImage(NoteImages.FromBitmap(bitmap));return;}
            if(Error!=null)Error("Schowek nie zawiera odczytywalnego tekstu ani obrazu.");
        }

        public void PasteRtf(string rtf,string fallback)
        {
            try { int count;PasteDocument(ReadRtf(rtf,out count)); }
            catch(Exception ex)
            {
                if(String.IsNullOrEmpty(fallback)){if(Error!=null)Error("Nie można odczytać RTF: "+ex.Message);return;}
                Mutate("paste",()=>Box.Selection.Text=fallback);
            }
        }

        static byte[] ReadPictureBytes(string group)
        {
            bool raster=false;
            var pixels=new StringBuilder();
            for(int i=1;i<group.Length-1;)
            {
                char c=group[i++];
                if(Char.IsWhiteSpace(c))continue;
                if(c=='\\'&&pixels.Length==0)
                {
                    int start=i;
                    while(i<group.Length&&Char.IsLetter(group[i]))i++;
                    string word=group.Substring(start,i-start).ToLowerInvariant();
                    if(word=="pngblip"||word=="jpegblip")raster=true;
                    if(i<group.Length&&group[i]=='-')i++;
                    while(i<group.Length&&Char.IsDigit(group[i]))i++;
                    continue;
                }
                if(c=='{'&&pixels.Length==0)
                {
                    // Word emits nested picture properties (picprop/sp/sn/sv) and
                    // a blipuid before the encoded raster. They are not pixels.
                    int depth=1;
                    while(i<group.Length&&depth!=0)
                    {
                        char tag=group[i++];
                        if(tag=='\\'&&i<group.Length&&"{}\\".IndexOf(group[i])>=0)i++;
                        else if(tag=='{')depth++;
                        else if(tag=='}')depth--;
                    }
                    if(depth!=0)return null;
                    continue;
                }
                if(!raster||!Uri.IsHexDigit(c))return null;
                if(pixels.Length>=2*NoteImages.MaxSourceBytes)throw new ImageBudgetException("Zakodowane źródło obrazu może mieć najwyżej 8 MiB.");
                pixels.Append(c);
            }
            if(!raster||pixels.Length<8||pixels.Length%2!=0)return null;
            string hex=pixels.ToString();
            var bytes=new byte[hex.Length/2];
            for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)(Uri.FromHex(hex[i*2])*16+Uri.FromHex(hex[i*2+1]));
            return bytes;
        }

        static FlowDocument ReadRtf(string rtf,out int count)
        {
            string prefix="HME"+Guid.NewGuid().ToString("N");
            var pictures=new Dictionary<string,RichNode>();
            long pictureCharacters=0;
            string filtered=RtfClipboard.Filter(rtf,group=>{
                var bytes=ReadPictureBytes(group);
                if(bytes==null)return "";
                if(pictures.Count>=NoteImages.MaxPerNote)throw new ImageBudgetException("Limit 12 obrazów w opisie.");
                var marker=prefix+pictures.Count.ToString("D2")+"IMAGE";
                try
                {
                    var node=NoteImages.FromBytes(bytes);
                    pictureCharacters+=node.Png.Length;
                    if(pictureCharacters>NoteCodec.MaxCharacters)throw new ImageBudgetException("Obrazy przekraczają limit 24 Mi znaków opisu.");
                    pictures.Add(marker,node);
                }
                catch(ImageBudgetException){throw;}
                catch(IOException){return "";} catch(ArgumentException){return "";} catch(System.Runtime.InteropServices.ExternalException){return "";}
                return "{"+marker+"}";
            });
            var document=new FlowDocument();
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(filtered)))
                new TextRange(document.ContentStart,document.ContentEnd).Load(stream,DataFormats.Rtf);
            var model=NoteCodec.Read(NoteCodec.Write(document));
            foreach(var block in model.Blocks)ReplacePictureMarkers(block,pictures);
            count=pictures.Count;
            return NoteCodec.FromJson(MapLogic.Serializer().Serialize(model));
        }

        static void ReplacePictureMarkers(RichNode node,Dictionary<string,RichNode> pictures)
        {
            if(node.Kind=="paragraph")
            {
                var result=new List<RichNode>();
                foreach(var child in node.Children)
                {
                    if(child.Kind!="run"||String.IsNullOrEmpty(child.Text)){result.Add(child);continue;}
                    string rest=child.Text;
                    foreach(var marker in pictures.Keys.OrderBy(key=>{int at=rest.IndexOf(key,StringComparison.Ordinal);return at<0?Int32.MaxValue:at;}).ToArray())
                    {
                        int at=rest.IndexOf(marker,StringComparison.Ordinal);
                        if(at<0)continue;
                        if(at>0)result.Add(CopyRun(child,rest.Substring(0,at)));
                        result.Add(pictures[marker]);rest=rest.Substring(at+marker.Length);
                    }
                    if(rest.Length>0)result.Add(CopyRun(child,rest));
                }
                node.Children=result;
            }
            else foreach(var child in node.Children)ReplacePictureMarkers(child,pictures);
        }

        static RichNode CopyRun(RichNode source,string text)
        {
            return new RichNode{Kind="run",Text=text,Size=source.Size,Bold=source.Bold,Italic=source.Italic,Underline=source.Underline,Strike=source.Strike};
        }
        static IEnumerable<Image> Images(BlockCollection blocks)
        {
            foreach(var block in blocks)
            {
                var blockImage=NoteImages.BlockImage(block);
                if(blockImage!=null)yield return blockImage;
                var p=block as Paragraph;
                if(p!=null)foreach(var item in p.Inlines.OfType<InlineUIContainer>())if(item.Child is Image)yield return (Image)item.Child;
                var table=block as Table;
                if(table!=null)foreach(var group in table.RowGroups)foreach(var row in group.Rows)foreach(var cell in row.Cells)foreach(var image in Images(cell.Blocks))yield return image;
                var list=block as List;
                if(list!=null)foreach(var item in list.ListItems)foreach(var image in Images(item.Blocks))yield return image;
                var section=block as Section;
                if(section!=null)foreach(var image in Images(section.Blocks))yield return image;
            }
        }
        void PasteDocument(FlowDocument document)
        {
            // The package comes from the validated in-memory document, never directly from the clipboard.
            Mutate("paste",()=>{
                using(var stream=new MemoryStream())
                {
                    new TextRange(document.ContentStart,document.ContentEnd).Save(stream,DataFormats.XamlPackage,true);
                    stream.Position=0;Box.Selection.Load(stream,DataFormats.XamlPackage);
                }
                FitTables();
            });
        }
        void InsertImageFile()
        {
            try
            {
                var path=ImagePathRequested==null?null:ImagePathRequested();
                if(path==null)return;
                if(new FileInfo(path).Length>NoteImages.MaxSourceBytes)throw new ImageBudgetException("Zakodowane źródło obrazu może mieć najwyżej 8 MiB.");
                InsertImage(NoteImages.FromBytes(File.ReadAllBytes(path)));
            }
            catch(Exception ex){if(Error!=null)Error(ex.Message);}
        }
        internal void InsertImage(RichNode node)
        {
            var document=NoteCodec.FromPlain("");
            document.Blocks.Clear();document.Blocks.Add(NoteImages.CreateBlock(node));
            PasteDocument(document);
        }
        static TextElement ImageContainer(Image image)
        {
            return LogicalTreeHelper.GetParent(image) as TextElement;
        }
        void SelectImage(object sender,MouseButtonEventArgs e)
        {
            var image=e.OriginalSource as Image;
            if(image==null)return;
            var container=ImageContainer(image);
            if(container==null)return;
            Box.Focus();
            Box.Selection.Select(container.ElementStart,container.ElementEnd);
            e.Handled=true;
        }
        void ResizeImage(double factor)
        {
            if(Box.Selection.IsEmpty)return;
            var start=Box.Selection.Start;
            var end=Box.Selection.End;
            var selected=Images(Box.Document.Blocks).FirstOrDefault(image=>{
                var container=ImageContainer(image);
                // WPF moves the endpoints inside a BlockUIContainer when the
                // selected range consists of that block.
                return container!=null&&start.CompareTo(container.ContentStart)<=0&&end.CompareTo(container.ContentEnd)>=0;
            });
            if(selected!=null)Mutate("format",()=>selected.MaxWidth=Math.Max(24,Math.Min(900,selected.MaxWidth*factor)));
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
