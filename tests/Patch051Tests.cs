using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch051Tests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Get(MainWindow window, string field) { return typeof(MainWindow).GetField(field, Private).GetValue(window); }
        static void Set(MainWindow window, string field, object value) { typeof(MainWindow).GetField(field, Private).SetValue(window, value); }
        static object Call(MainWindow window, string method, params object[] args)
        { return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(window, args); }
        static MapData Map(MainWindow window) { return (MapData)Get(window, "map"); }
        static void History(MainWindow window, bool redo)
        {
            var args = new object[] { redo, null };
            Check((bool)Call(window, "TryHistory", args), "Historia " + args[1]);
        }
        sealed class Choices : Random
        {
            int index;
            readonly int[] choices;
            internal Choices(params int[] choices) { this.choices = choices; }
            public override int Next(int maxValue) { return choices[index++ % choices.Length] % maxValue; }
        }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        internal static GraphicDefinition Graphic(string root)
        {
            string path = Path.Combine(root, "Pasy.png");
            using (var bitmap = new Bitmap(600, 300))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.MediumSeaGreen);
                    g.FillRectangle(Brushes.Red, 0, 0, 130, 300);
                    g.FillRectangle(Brushes.Blue, 470, 0, 130, 300);
                    g.FillRectangle(Brushes.Gold, 0, 140, 600, 20);
                }
                bitmap.Save(path, ImageFormat.Png);
            }
            var source = GraphicSources.Import(path);
            Check(Convert.FromBase64String(source.Data).SequenceEqual(File.ReadAllBytes(path)), "Import utracił oryginalne bajty.");
            var graphic = new GraphicDefinition { Source = source, SourceId = source.Id, Name = "Pasy" };
            graphic.Seal(); return graphic;
        }
        static void Crop(string root)
        {
            var graphic = Graphic(root);
            graphic.Linked = false;
            CropMath.Rotate(graphic, false, 90);
            graphic.Linked = true;
            Check(graphic.Pointy.Angle == 90 && graphic.Flat.Angle == 0, "Połączenie resetuje kadry.");
            CropMath.Rotate(graphic, false, 90);
            Check(graphic.Pointy.Angle == 180 && graphic.Flat.Angle == 90, "Względny obrót po połączeniu.");
            graphic.Linked = false;
            CropMath.Zoom(graphic, false, 2);
            graphic.Linked = true;
            double pointy = graphic.Pointy.Zoom, flat = graphic.Flat.Zoom;
            CropMath.Zoom(graphic, true, 1.2);
            Check(Math.Abs(graphic.Pointy.Zoom / pointy - 1.2) < 1e-10 && Math.Abs(graphic.Flat.Zoom / flat - 1.2) < 1e-10, "Względny zoom.");
            CropMath.Pan(graphic, false, .03, .02);
            Check(Math.Abs(graphic.Pointy.X - graphic.Flat.X) < 1e-10 && Math.Abs(graphic.Pointy.Y - graphic.Flat.Y) < 1e-10, "Wspólna delta obrazu.");
            CropMath.Rotate(graphic, false, 200);
            Check(graphic.Pointy.Angle == 20 && graphic.Flat.Angle == 290, "Przejście przez zero.");
            graphic.Seal();
            var saved = MapLogic.Serializer().Serialize(graphic);
            var reloaded = MapLogic.Serializer().Deserialize<GraphicDefinition>(saved);
            reloaded.Source = graphic.Source;
            Check(TerrainTextures.Same(graphic, reloaded), "Zapis dwóch kadrów.");
            var other = graphic.Copy();
            other.Pointy.Angle = 0; other.Flat.Angle = 0; other.Pointy.X = .75; other.Pointy.Y = .5; other.Pointy.Zoom = 1;
            other.Flat = other.Pointy.Copy(); CropMath.Clamp(other.Pointy, other.Source); CropMath.Clamp(other.Flat, other.Source); other.Seal();
            using (var result = TerrainTextures.Decode(other))
                Check(result.GetPixel(200, 70).B > 150, "Ponowny kadr nie widzi pełnego źródła.");
            var list = TerrainTextures.Collect(new[] { graphic, other });
            Check(GraphicSources.Collect(list.Select(a => a.Source)).Count == 1, "Deduplikacja pełnego źródła.");
            for (int angle = 0; angle < 360; angle += 45)
            {
                var rotated = graphic.Copy();
                rotated.Pointy.Angle = angle; rotated.Pointy.Zoom = 1;
                CropMath.Clamp(rotated.Pointy, rotated.Source); rotated.Seal();
                using (var bitmap = TerrainTextures.Decode(rotated))
                    Check(bitmap.GetPixel(2, 2).A == 255 && bitmap.GetPixel(253, 253).A == 255, "Puste narożniki po obrocie.");
            }
        }
        static void Pool(string root)
        {
            using (var window = new MainWindow())
            {
                var graphic = Graphic(root);
                var terrain = new TerrainDefinition { Name = "Łąka" };
                FillRules.SetPool(terrain, new List<FillVariant> {
                    new FillVariant { Id = "light", Color = "#00CC00" }, new FillVariant { Id = "dark", Color = "#006600" },
                    new FillVariant { Id = "image", Color = "#003300", GraphicId = graphic.Id, Graphic = graphic } });
                var preset = new TerrainPreset { Name = "Test" }; preset.Terrains.Add(terrain);
                Call(window, "StartNewProject", 4, 2, "PointyTop", "Test", preset);
                Set(window, "appearanceRandom", new Choices(0, 0, 1, 2));
                var map = Map(window);
                map.Cells[0].Name = "Zachowaj"; map.Cells[0].Description = "Tekst";
                Call(window, "BeginMaterialStroke", 0, EditorTool.Brush);
                Call(window, "PaintMaterial", 0); Call(window, "PaintMaterial", 0);
                Call(window, "PaintMaterial", 1); Call(window, "PaintMaterial", 2); Call(window, "PaintMaterial", 3);
                Call(window, "FinishStroke");
                Check(map.Cells.Take(4).Select(c => c.FillVariantId).SequenceEqual(new[] { "light", "light", "dark", "image" }), "Wybory i pojedyncze losowanie odwiedzonego pola.");
                string recorded = MapLogic.Serializer().Serialize(map.Cells);
                History(window, false); Check(map.Cells[0].Texture == null && map.Cells[0].Name == "Zachowaj", "Undo zachowuje tekst.");
                History(window, true); Check(recorded == MapLogic.Serializer().Serialize(map.Cells), "Redo losuje ponownie.");
                var frozen = map.Cells.Select(c => new RegionState(c)).ToArray();
                Check(MapLogic.Flood(map, 0, frozen, false).Count == 4, "Warianty rozbijają region.");
                map.Cells[0].Fog = map.Cells[1].Fog = true;
                frozen = map.Cells.Select(c => new RegionState(c)).ToArray();
                Check(MapLogic.Flood(map, 0, frozen, true).Count == 2, "Mgła nie respektuje rodzaju i początkowego Fog.");
                map.Orientation = "FlatTop"; map.Cells[0].Fog = false; map.Cells[1].Fog = false;
                var output = (MapData)Call(window, "MapForSave");
                Check(output.TerrainAssets.Count == 1 && output.GraphicSources.Count == 1, "Grafika puli nie jest osadzona.");
            }
        }
        static void Library(string root)
        {
            var graphic = Graphic(root); string directory = Path.Combine(root, "library");
            Directory.CreateDirectory(directory);
            using (var window = new MainWindow())
            {
                var entries = new List<TerrainGraphic>(); Set(window, "terrainGraphics", entries);
                var first = TerrainGraphicLibrary.Prepare(directory, null, "Obraz", graphic, entries);
                Call(window, "ChangeTerrainGraphic", null, first);
                Func<TerrainGraphic, GraphicDefinition, DialogResult> use = (a, b) => DialogResult.Yes;
                Func<TerrainGraphic, GraphicDefinition, DialogResult> cancel = (a, b) => DialogResult.Cancel;
                Func<TerrainGraphic, GraphicDefinition, DialogResult> copy = (a, b) => DialogResult.No;
                Check(Object.ReferenceEquals(Call(window, "CommitGraphicImport", graphic, use, directory), first), "Użyj istniejącej.");
                Check(Call(window, "CommitGraphicImport", graphic, cancel, directory) == null && entries.Count == 1, "Anulowanie duplikatu.");
                var other = graphic.Copy(); other.Linked = false; CropMath.Rotate(other, true, 90); other.Seal();
                Check(MainWindow.FindGraphicDuplicate(entries, other) == null, "Inny kadr uznano za duplikat.");
                var second = (TerrainGraphic)Call(window, "CommitGraphicImport", graphic.Copy(), copy, directory);
                Check(first.Id != second.Id && first.Path != second.Path && Directory.GetFiles(Path.Combine(directory, "Sources")).Length == 1, "Niezależne ID i jedno źródło.");
                var renamed = TerrainGraphicLibrary.Prepare(directory, second, "Nowa nazwa", other, entries);
                Call(window, "ChangeTerrainGraphic", second, renamed);
                Check(!File.Exists(second.Path) && File.Exists(renamed.Path), "Zmiana nazwy pliku.");
                History(window, false);
                Check(File.ReadAllBytes(second.Path).SequenceEqual(second.Bytes), "Undo bajtów nazwy i kadrów.");
                History(window, true);
                Call(window, "ChangeTerrainGraphic", renamed, null);
                File.WriteAllText(renamed.Path, "obce dane");
                var args = new object[] { false, null };
                Check(!(bool)Call(window, "TryHistory", args) && File.ReadAllText(renamed.Path) == "obce dane", "Konflikt historii źródła.");
                File.Delete(renamed.Path); History(window, false);
                Check(File.ReadAllBytes(renamed.Path).SequenceEqual(renamed.Bytes), "Ponowienie po konflikcie.");
                string warning; var reloaded = TerrainGraphicLibrary.Load(directory, out warning);
                Check(warning == "" && reloaded.Select(e => e.Id).SequenceEqual(new[] { first.Id, second.Id }), "Kolejność po odczycie.");
            }
        }
        static void Fog(string root)
        {
            string file = Path.Combine(root, "Alpha.png");
            using (var bitmap = new Bitmap(32, 32)) bitmap.Save(file, ImageFormat.Png);
            var source = GraphicSources.Import(file);
            var graphic = new GraphicDefinition { Source = source, SourceId = source.Id, Name = "Przezroczyste" }; graphic.Seal();
            using (var window = new MainWindow())
            {
                Call(window, "StartNewProject", 4, 3, "PointyTop", "Mgła", PresetCatalog.Default());
                var map = Map(window);
                map.FogPalette = new List<FillVariant> { new FillVariant { Id = "alpha", Color = "#123456", GraphicId = graphic.Id, Graphic = graphic } };
                var icon = IconAssets.Starters()[0]; map.IconAssets.Add(icon);
                var center = BrushGeometry.Center(map, 1, 0);
                map.Objects.Add(new MapObject { AssetId = icon.Id, TemplateId = "test", X = center.X, Y = center.Y, Side = "Ally", RevealsFog = true, RevealRadius = 0 });
                Call(window, "CaptureSavedState");
                string before = MapLogic.Serializer().Serialize(map.Cells);
                Set(window, "appearanceRandom", new Choices(0));
                Set(window, "currentTool", EditorTool.FogBucket);
                Call(window, "FillRegion", 0);
                Check(map.Cells[0].Fog && !map.Cells[1].Fog && map.Cells[0].FogFill.GraphicId == graphic.Id, "Mgła i aktywny odkrywca.");
                string after = MapLogic.Serializer().Serialize(map.Cells);
                History(window, false); Check(MapLogic.Serializer().Serialize(map.Cells) == before, "Undo wyglądu mgły.");
                History(window, true); Check(MapLogic.Serializer().Serialize(map.Cells) == after, "Redo wyglądu mgły.");
                Set(window, "pan", new PointF(90, 90)); Set(window, "scale", 1f); map.LabelMode = "None";
                using (var bitmap = new Bitmap(180, 180))
                using (var g = Graphics.FromImage(bitmap))
                using (var args = new PaintEventArgs(g, new Rectangle(0, 0, 180, 180)))
                {
                    Call(window, "PaintMap", null, args);
                    // 0.5.4 intentionally preserves graphic alpha; logical Fog still masks details/list.
                    Check(bitmap.GetPixel(90, 90).ToArgb() == ColorTranslator.FromHtml(map.Cells[0].Color).ToArgb() && map.Cells[0].Fog, "Render alpha zmienia logiczny Fog lub dodaje nieprzezroczystą bazę.");
                }
                var newPool = new List<FillVariant> { new FillVariant { Id = "pink", Color = "#CC3388" } };
                Call(window, "ChangeFogAppearance", newPool, false);
                Check(map.Cells[0].FogFill.GraphicId == graphic.Id, "Zmiana puli zmieniła starą mgłę.");
                Call(window, "ChangeFogAppearance", newPool, true, false); // Explicit reroll; 0.5.3 defaults to preserving IDs.
                Check(map.Cells[0].FogFill.Color == "#CC3388" && !map.Cells[1].Fog, "Aktualizacja tylko wyglądu.");
                History(window, false); Check(map.Cells[0].FogFill.GraphicId == graphic.Id, "Undo aktualizacji wyglądu.");
            }
        }
        static void Background(string root)
        {
            var graphic = Graphic(root);
            using (var window = new MainWindow())
            {
                var settings = new MapBackground { SourceId = graphic.SourceId, Source = graphic.Source, Left = .1, Top = .1, Width = .8, Height = .8, Saturation = 0, Blur = 3, Opacity = 60 };
                Call(window, "ChangeBackground", settings);
                using (var bitmap = new Bitmap(300, 200))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Black); Call(window, "DrawBackground", g);
                    var cache = Get(window, "backgroundBitmap"); var color = bitmap.GetPixel(100, 100);
                    Check(color.R == color.G && color.G == color.B, "Nasycenie zero nie jest szarością.");
                    Set(window, "pan", new PointF(500, 330)); Set(window, "scale", 4f);
                    g.Clear(Color.Black); Call(window, "DrawBackground", g);
                    Check(Object.ReferenceEquals(cache, Get(window, "backgroundBitmap")) && bitmap.GetPixel(100, 100) == color, "Tło przesuwa się lub przelicza na pan/zoom.");
                }
                History(window, false); Check(Map(window).Background == null, "Undo tła.");
                History(window, true); Check(Map(window).Background.SourceId == graphic.SourceId, "Redo tła.");
                Call(window, "CaptureSavedState"); Call(window, "SetLayerVisible", MapLayer.Background, false);
                Check(!(bool)Get(window, "dirty") && Map(window).Background != null, "Flaga Tło zmieniła dokument.");
            }
        }
        static void Portable(string root)
        {
            var graphic = Graphic(root); var other = graphic.Copy(); CropMath.Rotate(other, true, 90); other.Seal();
            var terrain = new TerrainDefinition { Name = "Pula" };
            FillRules.SetPool(terrain, new List<FillVariant> { new FillVariant { Graphic = graphic, GraphicId = graphic.Id },
                new FillVariant { Graphic = other, GraphicId = other.Id, Color = "#445566" } });
            var preset = new TerrainPreset { Name = "Pełne źródło" }; preset.Terrains.Add(terrain);
            string file = Path.Combine(root, "Portable.hexmap"), presets = Path.Combine(root, "presets");
            using (var window = new MainWindow())
            {
                Call(window, "StartNewProject", 3, 2, "FlatTop", "Test", preset);
                MapLogic.ApplyTerrain(Map(window).Cells[0], terrain, terrain.Variants[1]);
                Map(window).FogPalette = new List<FillVariant> { terrain.Variants[0].Copy() };
                Map(window).Cells[0].Fog = true; Map(window).Cells[0].FogFill = terrain.Variants[1].Copy();
                Map(window).Background = new MapBackground { Source = graphic.Source, SourceId = graphic.SourceId };
                var args = new object[] { file, false, null };
                Check((bool)Call(window, "TrySaveTo", args), "Zapis mapy " + args[2]);
                var output = (MapData)Call(window, "MapForSave");
                Check(output.GraphicSources.Count == 1 && output.TerrainAssets.Count == 2, "Źródło powielone między paletą, heksami, mgłą i tłem.");
            }
            PresetCatalog.Save(preset, presets);
            File.Delete(Path.Combine(root, "Pasy.png"));
            var loadedPreset = PresetCatalog.ReadFile(Directory.GetFiles(presets).Single());
            Check(GraphicSources.Same(loadedPreset.Terrains[0].Variants[0].Graphic.Source, graphic.Source), "Preset utracił pełne źródło.");
            using (var window = new MainWindow())
            {
                var args = new object[] { file, null, false };
                Check((bool)Call(window, "TryLoadProject", args), "Odczyt mapy " + args[1]);
                var map = Map(window);
                Check(map.Cells[0].FogFill.Graphic.Flat.Angle == 90 && map.Cells[0].Texture.Flat.Angle == 90 &&
                    GraphicSources.Same(map.Background.Source, graphic.Source), "Utrata kadrów lub tła po odczycie.");
                var broken = Path.Combine(root, "Broken.hexmap");
                File.WriteAllText(broken, File.ReadAllText(file).Replace(graphic.Source.Id, "bad-source"));
                args = new object[] { broken, null, false };
                Check(!(bool)Call(window, "TryLoadProject", args) && Object.ReferenceEquals(map, Map(window)), "Uszkodzony plik podmienił dokument.");
            }
        }
        static void Controls(string root)
        {
            var graphic = Graphic(root); graphic.Linked = false; CropMath.Rotate(graphic, true, 90); graphic.Seal();
            using (var preview = new TerrainPreview(true) { Size = new Size(200, 200) })
            using (var bitmap = new Bitmap(200, 200))
            {
                preview.ShowFill(Color.White, graphic); preview.DrawToBitmap(bitmap, preview.ClientRectangle);
                Check(bitmap.GetPixel(100, 45).R > 200 && bitmap.GetPixel(100, 45).G > 150, "Podgląd Flat nie używa kadru Flat.");
                Check(bitmap.GetPixel(45, 100).R < 150, "Podgląd Flat używa poziomego pasa Pointy.");
            }
            var background = new MapBackground { Source = graphic.Source, SourceId = graphic.SourceId };
            using (var surface = new BackgroundCropSurface { Size = new Size(450, 320) })
            {
                surface.CreateControl(); surface.SetContent(background);
                typeof(BackgroundCropSurface).GetMethod("OnMouseDown", Private).Invoke(surface, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 6, 51, 0) });
                typeof(BackgroundCropSurface).GetMethod("OnMouseMove", Private).Invoke(surface, new object[] { new MouseEventArgs(MouseButtons.Left, 0, 65, 85, 0) });
                Check(background.Left > .1 && background.Top > .1, "Próba nie przesunęła narożnika.");
                BackgroundRenderer.Validate(background);
                using (var bitmap = BackgroundRenderer.Render(background, new Size(290, 320))) Check(bitmap.Width > 0, "Kadr nie renderuje.");
            }
        }
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 1) throw new ArgumentException("Wymagany dokładnie jeden Case.");
                if (args[0] == "crop") Crop(AppDomain.CurrentDomain.BaseDirectory);
                else if (args[0] == "pool") Pool(AppDomain.CurrentDomain.BaseDirectory);
                else if (args[0] == "library") Library(AppDomain.CurrentDomain.BaseDirectory);
                else if (args[0] == "fog") Fog(AppDomain.CurrentDomain.BaseDirectory);
                else if (args[0] == "background") Background(AppDomain.CurrentDomain.BaseDirectory);
                else if (args[0] == "portable") Portable(AppDomain.CurrentDomain.BaseDirectory);
                else if (args[0] == "controls") Controls(AppDomain.CurrentDomain.BaseDirectory);
                else throw new ArgumentException("Przypadek nie jest jeszcze wdrożony.");
                Console.WriteLine("PASS " + args[0]); return 0;
            }
            catch (Exception ex) { Console.WriteLine(ex); return 1; }
        }
    }
}
