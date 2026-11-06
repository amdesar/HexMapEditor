using System;
using System.Collections.Generic;
using System.Linq;

namespace HexMapEditor
{
    [Flags] internal enum CellContentKind { None = 0, Terrain = 1, Text = 2 }
    internal sealed class RemovalGroup
    {
        public CellContentKind Kind;
        public string[] Coordinates;
        public string Title { get { return Kind == CellContentKind.Terrain ? "Tylko zmieniony teren" : Kind == CellContentKind.Text ? "Tylko tekst" : "Teren i tekst"; } }
        public static List<RemovalGroup> For(MapData before, IEnumerable<int> indices)
        {
            return indices.Distinct().Select(i => new { Index = i, Kind = MapLogic.ContentKind(before.Cells[i]) }).Where(p => p.Kind != CellContentKind.None)
                .GroupBy(p => p.Kind).OrderBy(g => (int)g.Key).Select(g => new RemovalGroup { Kind = g.Key, Coordinates = g.Select(p => MapLogic.Coordinate(p.Index, before.Columns)).ToArray() }).ToList();
        }
    }
}
