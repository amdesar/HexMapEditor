using System;
using System.IO;
using System.Windows.Documents;
namespace HexMapEditor.Tests
{
    internal static class UiFixtures
    {
        static MapData Map(int columns, int rows) { var m = new MapData { Columns = columns, Rows = rows }; for (int i = 0; i < columns * rows; i++) m.Cells.Add(new CellData()); return m; }
        static void Save(string name, MapData map) { MapLogic.AtomicWrite(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name + ".hexmap"), MapLogic.Serializer().Serialize(map), false); }
        [STAThread] static int Main()
        {
            var map = Map(15, 10); var document = NoteCodec.FromPlain("Zażółć gęślą jaźń.\nDrugi wiersz opisu."); var table = new Table(); for (int c = 0; c < 3; c++) table.Columns.Add(new TableColumn()); var rows = new TableRowGroup(); table.RowGroups.Add(rows);
            for (int r = 0; r < 2; r++) { var row = new TableRow(); for (int c = 0; c < 3; c++) { var cell = NoteCodec.NewCell(); ((Paragraph)cell.Blocks.FirstBlock).Inlines.Add(new Run("R" + (r + 1) + " K" + (c + 1))); row.Cells.Add(cell); } rows.Rows.Add(row); } document.Blocks.Add(table);
            map.Cells[0].Name = "Próba opisu i tabeli"; map.Cells[0].NotesFormat = "richDocument1"; map.Cells[0].RichDescription = NoteCodec.Write(document); map.Cells[0].Description = NoteCodec.Plain(map.Cells[0].RichDescription);
            MapLogic.ApplyTerrain(map.Cells[1], PresetCatalog.Default().Terrains[1]); MapLogic.ApplyTerrain(map.Cells[2], PresetCatalog.Default().Terrains[1]); map.Cells[2].Name = "Teren i tekst";
            MapLogic.ApplyTerrain(map.Cells[3], new TerrainDefinition { Name = "Teren neutralny", Color = "#777F91" }); Save("Panel i grupy 0.2.2", map);
            map = Map(1, 99); map.LabelMode = "InCells"; Save("Wiersze 49 i 50", map); map = Map(200, 200); map.LabelMode = "InCells"; Save("Duże współrzędne 200x200", map);
            var preset = new TerrainPreset { Id = "ui-portable-022", Name = "Przenośny test 0.2.2" }; preset.Terrains.Add(PresetCatalog.Neutral()); preset.Terrains.Add(PresetCatalog.Default().Terrains[1]);
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Przenośny.terrainset.json"), MapLogic.Serializer().Serialize(preset)); Console.WriteLine("UI fixtures created beside the test generator, not in the user Library."); return 0;
        }
    }
}
