using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal static class GraphicSchema
    {
        static Dictionary<string, object> Fields(object value, params string[] fields)
        {
            var row = value as Dictionary<string, object>;
            if (row == null || fields.Any(f => !row.ContainsKey(f))) throw new InvalidDataException("Niepełne dane źródła, kadru lub wariantu.");
            return row;
        }
        internal static void Definition(object value)
        {
            var definition = Fields(value, "Id", "SourceId", "Name", "Pointy", "Flat", "Linked");
            if (!(definition["Linked"] is bool)) throw new InvalidDataException("Brak jawnego połączenia kadrów.");
            foreach (string key in new[] { "Pointy", "Flat" }) Fields(definition[key], "X", "Y", "Zoom", "Angle");
        }
        static void Fill(object value) { Fields(value, "Id", "Color", "GraphicId"); }
        static IEnumerable<object> Array(object value)
        {
            var array = value as object[];
            if (array == null) throw new InvalidDataException("Brak wymaganej kolekcji.");
            return array;
        }
        internal static void Tables(Dictionary<string, object> raw)
        {
            Fields(raw, "TerrainAssets", "GraphicSources");
            foreach (var graphic in Array(raw["TerrainAssets"])) Definition(graphic);
            foreach (var source in Array(raw["GraphicSources"])) Fields(source, "Id", "Data", "Width", "Height");
        }
        internal static void Preset(object value)
        {
            var preset = Fields(value, "PresetFormatVersion", "Id", "Name", "Terrains");
            foreach (var terrain in Array(preset["Terrains"]))
            {
                var definition = Fields(terrain, "TerrainId", "VariantId", "Revision", "Name", "Color", "FillKind", "TextureId", "Variants");
                foreach (var fill in Array(definition["Variants"])) Fill(fill);
            }
        }
        internal static void Map(Dictionary<string, object> raw)
        {
            Tables(raw); Fields(raw, "Presets", "Cells", "FogPalette", "Background", "Ink", "DarkPalette");
            foreach(var fill in Array(raw["DarkPalette"]))Fill(fill);
            foreach(var stroke in Array(raw["Ink"])) {var row=Fields(stroke,"Id","Color","Width","Points","Surface");foreach(var point in Array(row["Points"]))Fields(point,"X","Y");}
            foreach (var preset in Array(raw["Presets"])) Preset(preset);
            foreach (var fill in Array(raw["FogPalette"])) Fill(fill);
            foreach (var cell in Array(raw["Cells"]))
            {
                var row = Fields(cell, "FillVariantId", "TextureId", "FogFill", "DarkFill", "Darkness"); Fill(row["FogFill"]); Fill(row["DarkFill"]); if(!(row["Darkness"] is bool))throw new InvalidDataException("Brak bazowego ograniczenia widoczności.");
            }
            if (raw["Background"] != null) Fields(raw["Background"], "SourceId", "Left", "Top", "Width", "Height", "Opacity", "Saturation", "Blur");
        }
    }
}
