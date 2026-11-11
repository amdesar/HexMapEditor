using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace HexMapEditor
{
    public sealed class TerrainDefinition
    {
        public string TerrainId { get; set; }
        public string VariantId { get; set; }
        public int Revision { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string FillKind { get; set; }
        public string TextureId { get; set; }
        internal GraphicDefinition Texture;
        List<FillVariant> variants;
        public List<FillVariant> Variants
        {
            get { return variants ?? (variants = new List<FillVariant> { new FillVariant { Color = Color, GraphicId = TextureId, Graphic = Texture } }); }
            set { variants = value; }
        }
        public TerrainDefinition() { TerrainId = Guid.NewGuid().ToString("N"); VariantId = Guid.NewGuid().ToString("N"); Revision = 1; Name = "Nowy teren"; Color = "#777F91"; FillKind = "color"; }
        public TerrainDefinition Copy() { var copy = (TerrainDefinition)MemberwiseClone(); copy.Variants = FillRules.Copy(Variants); return copy; }
    }

    public sealed class TerrainPreset
    {
        public int PresetFormatVersion { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public List<TerrainDefinition> Terrains { get; set; }
        internal string FilePath;
        internal byte[] DiskBytes;
        internal long LibraryOrder;
        public TerrainPreset() { PresetFormatVersion = 3; Id = Guid.NewGuid().ToString("N"); Name = "Nowy preset"; Terrains = new List<TerrainDefinition>(); }
        public TerrainPreset Copy() { return new TerrainPreset { PresetFormatVersion = PresetFormatVersion, Id = Id, Name = Name, Terrains = Terrains.Select(t => t.Copy()).ToList(), FilePath = FilePath, DiskBytes = DiskBytes, LibraryOrder = LibraryOrder }; }
    }

    internal static class PresetCatalog
    {
        public const string DefaultId = "default";
        public const string NeutralId = "default-neutral";
        public const string NeutralVariant = "default-neutral-v1";
        public static TerrainPreset Default()
        {
            string[] names = { "Teren neutralny", "Woda", "Równina", "Góry", "Pustynia", "Czerń / Pustka" };
            string[] colors = { "#777F91", "#287DB3", "#72A65B", "#8D8174", "#D1A94E", "#252735" };
            string[] ids = { NeutralId, "default-water", "default-plain", "default-mountains", "default-desert", "default-void" };
            var result = new TerrainPreset { Id = DefaultId, Name = "Domyślny" };
            for (int i = 0; i < names.Length; i++) result.Terrains.Add(new TerrainDefinition { TerrainId = ids[i], VariantId = ids[i] + "-v1", Name = names[i], Color = colors[i], Variants = new List<FillVariant> { new FillVariant { Id = ids[i] + "-fill", Color = colors[i] } } });
            return result;
        }
        public static TerrainDefinition Neutral() { return Default().Terrains[0]; }
        public static void Validate(TerrainPreset preset)
        {
            if (preset == null || preset.PresetFormatVersion != 3 || String.IsNullOrWhiteSpace(preset.Id) || String.IsNullOrWhiteSpace(preset.Name) || preset.Terrains == null || preset.Terrains.Count > 1000) throw new InvalidDataException("Nieprawidłowy preset terenów. Obsługiwany format: 3.");
            var ids = new HashSet<string>(); var variants = new HashSet<string>();
            foreach (var t in preset.Terrains)
                if (t == null || String.IsNullOrWhiteSpace(t.TerrainId) || String.IsNullOrWhiteSpace(t.VariantId) || t.Revision < 1 || String.IsNullOrWhiteSpace(t.Name) || !MapLogic.ValidColor(t.Color) || !TerrainTextures.ValidFill(t) || !ids.Add(t.TerrainId) || !variants.Add(t.VariantId)) throw new InvalidDataException("Nieprawidłowy lub powtórzony teren w presecie.");
            if (preset.Terrains.Any(t => t.TerrainId == UnpaintedTerrain.Id || t.VariantId == UnpaintedTerrain.Version)) throw new InvalidDataException("Stan niepomalowany należy do mapy, nie do presetu.");
            foreach (var terrain in preset.Terrains) FillRules.ValidatePool(terrain.Variants);
        }
        public static List<TerrainPreset> Load(string directory, out string warning)
        {
            var result = new List<TerrainPreset> { Default() }; var errors = new List<string>();
            Directory.CreateDirectory(directory);
            foreach (var file in Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var preset = ReadFile(file);
                    if (preset.Id == DefaultId) { errors.Add(Path.GetFileName(file) + ": plik nie zastępuje chronionego wzorca Domyślny; dane pliku pozostawiono bez zmian."); continue; }
                    int at = result.FindIndex(p => p.Id == preset.Id); if (at >= 0) { errors.Add(Path.GetFileName(file) + ": powtórzony identyfikator presetu."); continue; }
                    if (result.Any(p => SameName(p.Name, preset.Name))) { errors.Add(Path.GetFileName(file) + ": powtórzona nazwa presetu."); continue; } result.Add(preset);
                }
                catch (Exception ex) { errors.Add(Path.GetFileName(file) + ": " + ex.Message); }
            }
            warning = String.Join("\n", errors); return result.Take(1).Concat(LibrarySequence.Sort(result.Skip(1), p => p.LibraryOrder, p => p.Id)).ToList();
        }
        public static TerrainPreset ReadFile(string file)
        {
            if (new FileInfo(file).Length > TerrainTextures.MaxPresetBytes) throw new InvalidDataException("Preset przekracza 160 MiB.");
            byte[] bytes = File.ReadAllBytes(file); string text; using (var stream = new MemoryStream(bytes)) using (var reader = new StreamReader(stream, Encoding.UTF8, true)) text = reader.ReadToEnd();
            var json = MapLogic.Serializer(); var raw = json.DeserializeObject(text) as Dictionary<string, object>;
            string[] fields = { "PresetFormatVersion", "Id", "Name", "Terrains" };
            if (raw == null || fields.Any(k => !raw.ContainsKey(k)) || raw.Keys.Any(k => k != "LibraryOrder" && k != "TerrainAssets" && k != "GraphicSources" && !fields.Contains(k))) throw new InvalidDataException("Brak wymaganych pól lub nieobsługiwane dodatkowe pola presetu; plik pozostawiono bez zmian.");
            var terrains = raw["Terrains"] as object[]; string[] terrainFields = { "TerrainId", "VariantId", "Revision", "Name", "Color", "FillKind" };
            if (terrains == null || terrains.Any(t => !(t is Dictionary<string, object>) || !((Dictionary<string, object>)t).ContainsKey("Variants") || terrainFields.Any(k => !((Dictionary<string, object>)t).ContainsKey(k)) || ((Dictionary<string, object>)t).Keys.Any(k => k != "TextureId" && k != "Variants" && !terrainFields.Contains(k)))) throw new InvalidDataException("Niepełna lub nieobsługiwana definicja terenu; plik pozostawiono bez zmian.");
            var preset = json.ConvertToType<TerrainPreset>(raw);
            GraphicSchema.Tables(raw); GraphicSchema.Preset(raw);
            if (preset.PresetFormatVersion != 3 || !raw.ContainsKey("TerrainAssets") || !raw.ContainsKey("GraphicSources")) throw new InvalidDataException("Nieprawidłowy format tabeli grafik presetu. Wymagany format 3.");
            TerrainTextures.Attach(preset, TerrainTextures.ReadTable(json.ConvertToType<List<GraphicDefinition>>(raw["TerrainAssets"]), json.ConvertToType<List<GraphicSource>>(raw["GraphicSources"])));
            Validate(preset); preset.LibraryOrder = LibrarySequence.Read(raw); preset.FilePath = Path.GetFullPath(file); preset.DiskBytes = bytes; return preset;
        }
        public static string FindFile(string directory, string id)
        {
            if (id == DefaultId) throw new InvalidDataException("Chroniony wzorzec nie ma pliku użytkownika.");
            var files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly).Where(f => ReadFile(f).Id == id).ToArray();
            if (files.Length != 1) throw new InvalidDataException("Nie można jednoznacznie wskazać pliku presetu. Uruchom program ponownie i sprawdź komunikaty biblioteki.");
            return files[0];
        }
        public static void Save(TerrainPreset preset, string directory)
        {
            Write(preset, directory, false);
        }
        public static void Write(TerrainPreset preset, string directory, bool overwrite)
        {
            var plan = PrepareWrite(preset,directory,overwrite,null);
            LibraryFiles.Transition(plan.Before == null ? null : plan.Before.FilePath, plan.Before == null ? null : plan.Before.DiskBytes,plan.After.FilePath,plan.After.DiskBytes);
        }
        internal sealed class CatalogIdentity
        {
            internal string Id, Name;
            internal long Order;
        }
        // Collision inventory is not an import. Reserve even a skipped file's identity,
        // without requiring its unrelated terrain payload to use the current format.
        internal static CatalogIdentity ReadIdentity(string path)
        {
            if (new FileInfo(path).Length > TerrainTextures.MaxPresetBytes) throw new InvalidDataException("Nie można sprawdzić tożsamości zbyt dużego pliku presetu: " + path);
            var raw = MapLogic.Serializer().DeserializeObject(File.ReadAllText(path).TrimStart('\uFEFF')) as Dictionary<string, object>;
            object id, name;
            if (raw == null || !raw.TryGetValue("Id", out id) || !(id is string) || String.IsNullOrWhiteSpace((string)id) ||
                !raw.TryGetValue("Name", out name) || !(name is string) || String.IsNullOrWhiteSpace((string)name))
                throw new InvalidDataException("Nie można bezpiecznie sprawdzić ID i nazwy pliku presetu. Nie zmieniono danych: " + path);
            return new CatalogIdentity { Id = (string)id, Name = (string)name, Order = LibrarySequence.Read(raw) };
        }
        internal static PresetFileChange PrepareWrite(TerrainPreset preset, string directory, bool overwrite, TerrainPreset expected)
        {
            Validate(preset);
            if (preset.Id == DefaultId) throw new InvalidDataException("Domyślny jest chronionym wzorcem. Zapisz roboczą paletę jako nowy preset.");
            Directory.CreateDirectory(directory);
            var records = new List<KeyValuePair<string, CatalogIdentity>>();
            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                var identity = ReadIdentity(file); records.Add(new KeyValuePair<string, CatalogIdentity>(file, identity));
            }
            if (SameName(preset.Name, Default().Name) || records.Any(r => r.Value.Id != preset.Id && SameName(r.Value.Name, preset.Name))) throw new InvalidDataException("Nazwa presetu jest już używana. Wybierz inną nazwę; żaden plik nie został nadpisany.");
            var targets = records.Where(r => r.Value.Id == preset.Id).ToArray(); string path;
            if (overwrite) { if (targets.Length != 1) throw new InvalidDataException("Nie można jednoznacznie wskazać zapisanego presetu. Zapisz jako nowy preset."); path = targets[0].Key; }
            else { if (targets.Length != 0) throw new InvalidDataException("Identyfikator presetu jest już zapisany; wymagany jest jawny wybór nadpisania."); path = null; }
            // A file actually being overwritten still needs complete current validation.
            var before = path == null ? null : ReadFile(path);
            long order = before == null ? LibrarySequence.Next(records.Select(r => r.Value.Order)) : before.LibraryOrder;
            var assets = TerrainTextures.Collect(preset.Terrains.SelectMany(t => t.Variants).Select(f => f.Graphic));
            var serialized = MapLogic.Serializer().Serialize(new { PresetFormatVersion = 3, preset.Id, preset.Name, preset.Terrains, TerrainAssets = assets, GraphicSources = GraphicSources.Collect(assets.Select(a => a.Source)), LibraryOrder = order });
            if (Encoding.UTF8.GetByteCount(serialized) > TerrainTextures.MaxPresetBytes) throw new InvalidDataException("Preset przekracza limit zapisu/odczytu 160 MiB.");
            string destination = PresetFiles.Destination(directory, preset.Name, path);
            if (expected != null && (before == null || !String.Equals(expected.FilePath,before.FilePath,StringComparison.OrdinalIgnoreCase) || !expected.DiskBytes.SequenceEqual(before.DiskBytes))) throw new IOException("Plik presetu zmienił się poza programem. Zachowano plik, projekt i historię. Odśwież presety i sprawdź dane przed ponownym zapisem.");
            var after = preset.Copy(); after.LibraryOrder = order; after.FilePath = destination; after.DiskBytes = new UTF8Encoding(false).GetBytes(serialized);
            return new PresetFileChange { Before = before, After = after };
        }
        public static bool SameName(string a, string b) { return String.Equals((a ?? "").Trim().Normalize(), (b ?? "").Trim().Normalize(), StringComparison.OrdinalIgnoreCase); }
        public static TerrainDefinition Legacy(string name, string color)
        {
            var known = Default().Terrains.FirstOrDefault(t => t.Name == name && String.Equals(t.Color, color, StringComparison.OrdinalIgnoreCase));
            if (known != null) return known;
            string key = "legacy-" + MapLogic.Hash(name + "\n" + color.ToUpperInvariant());
            return new TerrainDefinition { TerrainId = key, VariantId = key + "-v1", Name = name, Color = color };
        }
    }

    internal sealed class ResizeState
    {
        public int Columns, Rows, RowParity, ColumnParity;
        public List<CellData> Cells;
        internal int ShiftColumns, ShiftRows;
        public static ResizeState Capture(MapData map) { return new ResizeState { Columns = map.Columns, Rows = map.Rows, RowParity = map.RowParity, ColumnParity = map.ColumnParity, Cells = new List<CellData>(map.Cells) }; }
        public void Apply(MapData map) { map.Columns = Columns; map.Rows = Rows; map.RowParity = RowParity; map.ColumnParity = ColumnParity; map.Cells = new List<CellData>(Cells); }
    }

    internal static class MapLogic
    {
        static readonly TerrainState neutralState = new TerrainState(new CellData());
        internal static bool IsUnpainted(CellData cell) { return neutralState.Matches(cell); }
        public static JavaScriptSerializer Serializer() { return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 128 }; }
        public static string Hash(string value) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "").ToLowerInvariant(); }
        public static bool ValidColor(string value)
        {
            return value != null && value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
        }
        public static void ApplyTerrain(CellData cell, TerrainDefinition terrain)
        { ApplyTerrain(cell, terrain, terrain.Variants[0]); }
        public static void ApplyTerrain(CellData cell, TerrainDefinition terrain, FillVariant appearance)
        {
            cell.Terrain = terrain.Name; cell.Color = terrain.Color; cell.TerrainId = terrain.TerrainId; cell.VariantId = terrain.VariantId; cell.TerrainRevision = terrain.Revision;
            cell.Color = appearance.Color; cell.TextureId = appearance.GraphicId; cell.Texture = appearance.Graphic; cell.FillVariantId = appearance.Id;
        }
        public static MapData Migrate(MapData map)
        {
            if (map == null) throw new InvalidDataException("Brak danych mapy.");
            if (map.FormatVersion != 1 && map.FormatVersion != 2) throw new InvalidDataException("Nieobsługiwana wersja formatu: " + map.FormatVersion);
            if (map.Cells == null) throw new InvalidDataException("Brak pól mapy.");
            if (map.FormatVersion == 1)
            {
                foreach (var cell in map.Cells)
                {
                    if (cell == null || !ValidColor(cell.Color) || String.IsNullOrWhiteSpace(cell.Terrain)) throw new InvalidDataException("Uszkodzone dane terenu.");
                    if (!String.IsNullOrEmpty(cell.NotesFormat) && cell.NotesFormat != "plainText") throw new InvalidDataException("Nieobsługiwany format starego opisu; plik pozostaje bez zmian.");
                    cell.Id = Guid.NewGuid().ToString("N"); var legacy = PresetCatalog.Legacy(cell.Terrain, cell.Color);
                    cell.TerrainId = legacy.TerrainId; cell.VariantId = legacy.VariantId; cell.TerrainRevision = legacy.Revision;
                    cell.RichDescription = null; cell.NotesFormat = "plainText";
                }
                map.RowParity = map.ColumnParity = 0; map.Presets = new List<TerrainPreset> { PresetCatalog.Default() }; map.ActivePresetId = PresetCatalog.DefaultId; map.FormatVersion = 2;
            }
            if (String.IsNullOrEmpty(map.LabelMode)) map.LabelMode = "Axes";
            return map;
        }
        public static IEnumerable<int> Neighbors(MapData map, int index)
        {
            int c = index % map.Columns, r = index / map.Columns;
            var points = new List<Point>();
            if (map.Orientation == "PointyTop")
            {
                int shift = ((r + map.RowParity) & 1) == 1 ? 1 : -1;
                points.Add(new Point(c - 1, r)); points.Add(new Point(c + 1, r));
                points.Add(new Point(c, r - 1)); points.Add(new Point(c + shift, r - 1)); points.Add(new Point(c, r + 1)); points.Add(new Point(c + shift, r + 1));
            }
            else
            {
                int shift = ((c + map.ColumnParity) & 1) == 1 ? 1 : -1;
                points.Add(new Point(c, r - 1)); points.Add(new Point(c, r + 1));
                points.Add(new Point(c - 1, r)); points.Add(new Point(c - 1, r + shift)); points.Add(new Point(c + 1, r)); points.Add(new Point(c + 1, r + shift));
            }
            foreach (var p in points) if (p.X >= 0 && p.Y >= 0 && p.X < map.Columns && p.Y < map.Rows) yield return p.Y * map.Columns + p.X;
        }
        public static List<int> Flood(MapData map, int start, bool byColor = false)
        { return Flood(map, start, map.Cells.Select(c => new RegionState(c)).ToArray(), byColor); }
        public static List<int> Flood(MapData map, int start, RegionState[] original, bool fog, bool dark = false)
        {
            var result = new List<int>(); if (start < 0 || start >= map.Cells.Count) return result;
            var seed = original[start]; var visited = new bool[map.Cells.Count]; var pending = new Queue<int>(); pending.Enqueue(start); visited[start] = true;
            while (pending.Count != 0)
            {
                int current = pending.Dequeue(); result.Add(current);
                foreach (int next in Neighbors(map, current)) if (!visited[next] && original[next].TerrainId == seed.TerrainId && original[next].Variant == seed.Variant && (!fog || original[next].Fog == seed.Fog) && (!dark || original[next].Darkness == seed.Darkness)) { visited[next] = true; pending.Enqueue(next); }
            }
            return result;
        }
        public static bool HasUserData(CellData cell)
        {
            return ContentKind(cell) != CellContentKind.None;
        }
        public static CellContentKind ContentKind(CellData cell)
        {
            var kind = CellContentKind.None;
            if (!neutralState.Matches(cell)) kind |= CellContentKind.Terrain;
            if (cell.IconId != null) kind |= CellContentKind.Icon;
            if (cell.Fog) kind |= CellContentKind.Fog; if(cell.Darkness)kind|=CellContentKind.Darkness;
            if (!String.IsNullOrEmpty(cell.Name) || !String.IsNullOrEmpty(cell.Description) || !String.IsNullOrEmpty(cell.RichDescription)) kind |= CellContentKind.Text;
            return kind;
        }
        public static ResizeState Resize(MapData map, string side, int delta, out int removed, out int withData)
        {
            if (!new[] { "top", "bottom", "left", "right" }.Contains(side) || delta == 0) throw new InvalidDataException("Wybierz stronę i dodatnią liczbę pól.");
            var plan = ResizeAll(map, side == "top" ? delta : 0, side == "bottom" ? delta : 0, side == "left" ? delta : 0, side == "right" ? delta : 0);
            removed = plan.RemovedIndices.Count; withData = plan.DataIndices.Count; return plan.State;
        }
        public static Size ResizeDimensions(int columns, int rows, int top, int bottom, int left, int right)
        {
            if (new[] { top, bottom, left, right }.Any(n => n < -199 || n > 199)) throw new InvalidDataException("Zmiana jednej strony musi mieścić się w zakresie −199…199.");
            if (Math.Max(0, -left) + Math.Max(0, -right) >= columns || Math.Max(0, -top) + Math.Max(0, -bottom) >= rows) throw new InvalidDataException("Nie można usunąć całej starej szerokości lub wysokości, także przy dodawaniu pól po drugiej stronie.");
            int resultColumns = columns + left + right, resultRows = rows + top + bottom;
            if (resultColumns < 1 || resultColumns > 200 || resultRows < 1 || resultRows > 200) throw new InvalidDataException("Wynikowy rozmiar musi mieścić się w zakresie 1–200 w każdej osi.");
            return new Size(resultColumns, resultRows);
        }
        public static ResizePlan ResizeAll(MapData map, int top, int bottom, int left, int right)
        {
            var dimensions = ResizeDimensions(map.Columns, map.Rows, top, bottom, left, right);
            var plan = new ResizePlan(); if (top == 0 && bottom == 0 && left == 0 && right == 0) return plan;
            int cols = dimensions.Width, rows = dimensions.Height;
            var state = new ResizeState { Columns = cols, Rows = rows, RowParity = (map.RowParity - top) & 1, ColumnParity = (map.ColumnParity - left) & 1, Cells = new List<CellData>(), ShiftColumns=left, ShiftRows=top };
            var kept = new HashSet<string>();
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
            {
                int oldC = c - left, oldR = r - top;
                var cell = oldC >= 0 && oldC < map.Columns && oldR >= 0 && oldR < map.Rows ? map.Cells[oldR * map.Columns + oldC] : new CellData();
                state.Cells.Add(cell); kept.Add(cell.Id);
            }
            for (int i = 0; i < map.Cells.Count; i++) if (!kept.Contains(map.Cells[i].Id)) { plan.RemovedIndices.Add(i); if (HasUserData(map.Cells[i])) plan.DataIndices.Add(i); }
            plan.State = state; return plan;
        }
        public static string Coordinate(int index, int columns)
        {
            int n = index % columns + 1; string letters = ""; while (n > 0) { n--; letters = (char)('A' + n % 26) + letters; n /= 26; } return letters + (index / columns + 1);
        }
        public static void AtomicWrite(string path, string text, bool overwrite)
        {
            string temp = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), ".hexmap-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(text);
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (overwrite && File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }

    internal sealed class SavedMapState
    {
        readonly Dictionary<string, string> saved = new Dictionary<string, string>();
        readonly HashSet<string> changed = new HashSet<string>();
        string header;
        bool headerChanged;
        static string CellKey(CellData cell) { return MapLogic.Hash(MapLogic.Serializer().Serialize(cell)); }
        static string Header(MapData map)
        {
            return MapLogic.Serializer().Serialize(new { map.Name, map.Columns, map.Rows, map.Orientation, map.LabelMode, map.RowParity, map.ColumnParity, map.ActivePresetId, map.Presets, map.Objects, map.Groups, ObjectStates = StateLibrary.ForSave(map), map.ExcludedStateIds, map.FogPalette, map.DarkPalette, map.Ink, map.Background });
        }
        public void Capture(MapData map) { saved.Clear(); foreach (var cell in map.Cells) saved.Add(cell.Id, CellKey(cell)); header = Header(map); changed.Clear(); headerChanged = false; }
        public bool CheckCell(CellData cell) { string value; if (saved.TryGetValue(cell.Id, out value) && value == CellKey(cell)) changed.Remove(cell.Id); else changed.Add(cell.Id); return Dirty; }
        public bool CheckHeader(MapData map){headerChanged=header!=Header(map);return Dirty;}
        public bool CheckAll(MapData map)
        {
            changed.Clear(); var ids = new HashSet<string>(); foreach (var cell in map.Cells) { ids.Add(cell.Id); CheckCell(cell); }
            foreach (string id in saved.Keys) if (!ids.Contains(id)) changed.Add(id);
            headerChanged = header != Header(map); return Dirty;
        }
        public bool Dirty { get { return headerChanged || changed.Count != 0; } }
    }

    internal sealed class TextHistory
    {
        public readonly List<string> States = new List<string>();
        public int Cursor;
        DateTime lastEdit;
        DateTime groupStarted;
        string lastKind;
        public TextHistory(string initial) { States.Add(initial ?? ""); }
        public void BreakGroup() { lastKind = null; }
        public void Record(string value, string kind)
        {
            value = value ?? ""; if (States[Cursor] == value) return;
            if (Cursor < States.Count - 1) States.RemoveRange(Cursor + 1, States.Count - Cursor - 1);
            bool group = kind == "typing" && lastKind == kind && Cursor > 0 && DateTime.UtcNow - lastEdit < TimeSpan.FromMilliseconds(750) && DateTime.UtcNow - groupStarted < TimeSpan.FromSeconds(5);
            if (group) States[Cursor] = value; else { States.Add(value); Cursor++; }
            if (!group) groupStarted = DateTime.UtcNow;
            while (States.Count > 101) { States.RemoveAt(0); Cursor--; }
            lastEdit = DateTime.UtcNow; lastKind = kind;
        }
        public string Undo() { BreakGroup(); if (Cursor > 0) Cursor--; return States[Cursor]; }
        public string Redo() { BreakGroup(); if (Cursor + 1 < States.Count) Cursor++; return States[Cursor]; }
        // Current content belongs to the document; budget counts additional undo/redo snapshots.
        public long Bytes { get { return States.Where((s, i) => i != Cursor).Sum(s => (long)s.Length * 2); } }
        public bool DropOldest() { if (Cursor <= 0 || States.Count <= 1) return false; States.RemoveAt(0); Cursor--; return true; }
    }

    internal sealed class TextHistoryStore
    {
        readonly Dictionary<string, TextHistory> entries = new Dictionary<string, TextHistory>();
        public const long Budget = 32L * 1024 * 1024;
        public TextHistory For(string cellId, string field, string initial)
        {
            string key = cellId + ":" + field; TextHistory result;
            if (!entries.TryGetValue(key, out result)) { result = new TextHistory(initial); entries.Add(key, result); } return result;
        }
        public void BreakGroups() { foreach (var history in entries.Values) history.BreakGroup(); }
        public void Clear() { entries.Clear(); }
        public void Trim()
        {
            long total = entries.Values.Sum(h => h.Bytes);
            while (total > Budget)
            {
                var oldest = entries.Values.FirstOrDefault(h => h.Cursor > 0 && h.States.Count > 1);
                if (oldest == null) break; long before = oldest.Bytes; oldest.DropOldest(); total -= before - oldest.Bytes;
            }
            // Redo snapshots also count; discard distant redo first if needed.
            if (total > Budget) foreach (var h in entries.Values) { if (h.Cursor + 1 < h.States.Count) h.States.RemoveRange(h.Cursor + 1, h.States.Count - h.Cursor - 1); }
        }
    }
}
