using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0130Tests
    {
        static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool actual, string message)
        { if (!actual) throw new InvalidOperationException(message); }
        static byte[] Picture(Color left, bool jpeg)
        {
            using (var stream = new MemoryStream())
            using (var image = new Bitmap(380,240))
            using (var graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(left))
            {
                graphics.Clear(Color.SlateGray); graphics.FillEllipse(brush, 10,15,230,217);
                graphics.FillRectangle(Brushes.Gold, 244,7,105,35);
                image.Save(stream, jpeg ? ImageFormat.Jpeg : ImageFormat.Png);
                return stream.ToArray();
            }
        }
        static string Add(string root, ImportCategory category, string name, byte[] image)
        {
            string path = Path.Combine(root,"Import",category.ToString(),name);
            File.WriteAllBytes(path,image);
            return path;
        }
        static GraphicDefinition Terrain(GraphicSource source, string name)
        {
            var terrain = new GraphicDefinition { Name = name, Source = source, Linked = false };
            terrain.Seal(); return terrain;
        }
        static void Library()
        {
            string library = Path.Combine(Root,"Separate Library");
            string icons = Directory.CreateDirectory(Path.Combine(library,"Icons")).FullName;
            string objects = Directory.CreateDirectory(Path.Combine(library,"Objects")).FullName;
            string warning;
            var starters = IconLibrary.Load(icons, out warning);
            var models = ObjectLibrary.Load(objects,out warning);
            Check(starters.Count == 20 && models.Count == 12, "Factory catalog");
            Check(starters.All(i=>i.Asset.LibraryCrop==null) && models.All(i=>i.Asset.LibraryCrop==null), "Factory PNG got a crop");
            Check(starters.All(i=>i.SourceFile.EndsWith("-original.png") && Path.GetDirectoryName(i.Asset.LibrarySource.LibraryFile)==icons)
                && models.All(i=>i.SourceFile.EndsWith("-original.png") && Path.GetDirectoryName(i.Asset.LibrarySource.LibraryFile)==objects)
                && !Directory.Exists(Path.Combine(library,"Sources")), "Factory sources are not local named PNGs");
            var starterFiles = Directory.GetFiles(icons,"*.png").Concat(Directory.GetFiles(objects,"*.png")).ToArray();
            var first = starters[0];
            var renamed = first.Copy(); renamed.Name = "Nazwa użytkownika";
            renamed.DiskBytes = IconLibrary.Metadata(renamed,first);
            new IconLibraryChange { Before = first, BeforeBytes = first.DiskBytes, After = renamed, AfterBytes = renamed.DiskBytes }.Write(true);
            var refreshed = IconLibrary.Load(icons,out warning);
            Check(refreshed.Count == 20 && refreshed.Single(i=>i.Id==first.Id).Name==renamed.Name &&
                refreshed.Single(i=>i.Id==first.Id).Asset.Id==first.Asset.Id && renamed.SourceFile==first.SourceFile &&
                starterFiles.SequenceEqual(Directory.GetFiles(icons,"*.png").Concat(Directory.GetFiles(objects,"*.png"))), "Restart replaced a starter");
            var original = Picture(Color.Orange, true);
            string input = Path.Combine(Root,"Source.jpg"); File.WriteAllBytes(input,original);
            IconAsset asset;
            using (var crop = new SquareImportDialog(input))
            {
                var nameBox=crop.Controls.OfType<DarkTextBox>().Single();
                Check(nameBox.Text=="Source","Default name is not the input filename");
                crop.Show(); nameBox.Text="Próba Łąki"; crop.Draft.Zoom(1.2); ((Button)crop.AcceptButton).PerformClick(); asset = crop.Result;
            }
            Check(asset != null && asset.Name=="Próba Łąki" && asset.LibrarySource.Id == GraphicSources.Id(original), "Crop forgot name or original file");
            var entry = AddIcon(icons, asset);
            var otherCrop = asset.LibraryCrop.Copy(); otherCrop.Angle = 29;
            var changed = SquareLibraryAsset.Render(asset.LibrarySource, "Drugi", otherCrop);
            var model = new ObjectTemplate { Name = "Source", Asset = changed, FilePath = ObjectLibrary.Destination(objects,"Source",null) };
            model.DiskBytes = ObjectLibrary.Metadata(model);
            new ObjectLibraryChange { After = model }.Write(true);
            var readIcon = IconLibrary.Read(entry.FilePath);
            var readObject = ObjectLibrary.Read(model.FilePath);
            Check(readIcon.Asset.Png == asset.Png && readObject.Asset.Png == changed.Png && readObject.Asset.Png != readIcon.Asset.Png,
                "Restart changed pixels or merged crops");
            Check(readObject.Asset.LibraryCrop.Angle == 29 && readIcon.Asset.LibraryCrop.Angle == 0 &&
                readIcon.SourceFile=="Próba Łąki-original.jpg" && readObject.SourceFile=="Source-original.jpg" &&
                readIcon.Asset.LibrarySource.LibraryFile != readObject.Asset.LibrarySource.LibraryFile &&
                File.ReadAllBytes(readObject.Asset.LibrarySource.LibraryFile).SequenceEqual(original) &&
                File.ReadAllBytes(readIcon.Asset.LibrarySource.LibraryFile).SequenceEqual(original) &&
                File.ReadAllBytes(input).SequenceEqual(original), "Original bytes or independent settings lost");
            Check(!File.ReadAllText(model.FilePath).Contains("\"Png\"") && !File.ReadAllText(entry.FilePath).Contains("\"Data\""),
                "Catalog embeds image data");
            var second = AddIcon(icons,changed);
            Check(second.Id!=entry.Id && second.SourceFile==entry.SourceFile && IconLibrary.Read(second.FilePath).Asset.Png==changed.Png,
                "Second crop changed the original or reused entry identity");
            var deletion = new IconLibraryChange { Before=readIcon, BeforeBytes=readIcon.DiskBytes };
            deletion.Write(true);
            Check(!File.Exists(entry.FilePath) && IconLibrary.Read(second.FilePath).Asset.Png==changed.Png, "Removing one crop destroyed another");
            deletion.Write(false);
            Check(IconLibrary.Read(entry.FilePath).SourceFile==second.SourceFile,"Undo did not restore the original reference");
            var clipboard = new System.Windows.DataObject();
            using(var stream = new MemoryStream(Picture(Color.Red,false)))
            {
                clipboard.SetData("PNG", stream);
                var bytes=ClipboardRaster.Read(clipboard,SquareLibraryAsset.InputBytes,2048,2048L*2048);
                Check(bytes!=null,"Clipboard PNG unreadable");
                var source=GraphicSources.Import(bytes);
                using(var dialog=new SquareImportDialog(source,"Ze schowka"))
                {
                    dialog.Show(); ((Button)dialog.AcceptButton).PerformClick();
                    Check(dialog.Result != null && dialog.Result.LibrarySource.Id==source.Id,"Clipboard crop lost its graphic source");
                    var clipboardEntry = AddIcon(icons,dialog.Result);
                    Check(clipboardEntry.SourceFile=="Ze schowka-original.png" && File.ReadAllBytes(Path.Combine(icons,clipboardEntry.SourceFile)).SequenceEqual(bytes),
                        "Clipboard original changed bytes or name");
                }
            }
            var nextSource = GraphicSources.Import(Picture(Color.Pink,false));
            var nextAsset = SquareLibraryAsset.Render(nextSource, "PRÓBA ŁĄKI", new SquareCropState(new Size(nextSource.Width,nextSource.Height)).Transform);
            Check(AddIcon(icons,nextAsset).SourceFile=="PRÓBA ŁĄKI-original-2.png","Case-insensitive collision or format not handled");
            var reserved=LibraryImageStore.Store(objects,"CON.Nazwa: zdjęcia",nextSource);
            Check(reserved=="_CON.Nazwa_ zdjęcia-original.png","Reserved and invalid Windows characters");
            var longSource=GraphicSources.Import(Picture(Color.Navy,false));
            var longFile=LibraryImageStore.Store(objects,new String('ą',200),longSource);
            Check(longFile.Length<200 && longFile.EndsWith("-original.png"),"Original name did not fit filesystem budget");
            var legacy = new LibraryIcon { Id = "old", Name="Stara", Asset = asset };
            string old = Path.Combine(icons,"icon-old.json");
            string raw = MapLogic.Serializer().Serialize(legacy);
            File.WriteAllText(old,raw);
            Check(IconLibrary.Read(old).Asset.Png == asset.Png && File.ReadAllText(old)==raw, "Old record was rewritten");
            string tampered=Path.Combine(icons,"icon-invalid.json");
            string json=File.ReadAllText(entry.FilePath);
            foreach (string badPath in new[] { @"..\Próba Łąki-original.jpg", @"C:\Próba Łąki-original.jpg", "Resources/Próba Łąki-original.jpg" })
            {
                File.WriteAllText(tampered,json.Replace(MapLogic.Serializer().Serialize(entry.SourceFile), MapLogic.Serializer().Serialize(badPath)));
                try { IconLibrary.Read(tampered); throw new InvalidOperationException("Accepted invalid reference: "+badPath); }
                catch(InvalidDataException) { }
            }
            string originalFile = Path.Combine(icons,entry.SourceFile);
            File.WriteAllBytes(originalFile,Convert.FromBase64String(nextSource.Data));
            try { IconLibrary.Read(entry.FilePath); throw new InvalidOperationException("Accepted replaced bytes"); }
            catch(InvalidDataException) { }
            File.WriteAllBytes(originalFile,original);
            Legacy(library, original);
            string moved=Path.Combine(Root,"Relocated Library");
            Directory.Move(library,moved);
            Check(IconLibrary.Read(Path.Combine(moved,"Icons","Metadata",Path.GetFileName(entry.FilePath))).Asset.Png==asset.Png &&
                TerrainGraphicLibrary.Load(Path.Combine(moved,"Terrains","Graphics"),out warning).Count==3 && warning=="",
                "Moving the entire Library broke relative references");
        }
        static LibraryIcon AddIcon(string directory, IconAsset asset)
        {
            var entry = new LibraryIcon { Name=asset.Name, Asset=asset, FilePath=IconLibrary.Destination(directory,asset.Name,null) };
            entry.DiskBytes=IconLibrary.Metadata(entry);
            new IconLibraryChange { After=entry, AfterBytes=entry.DiskBytes }.Write(true);
            return entry;
        }
        static void Legacy(string library, byte[] original)
        {
            var source=GraphicSources.Import(original);
            string central=Directory.CreateDirectory(Path.Combine(library,"Sources")).FullName;
            string legacyFile=Path.Combine(central,source.Id+".jpg");
            File.WriteAllBytes(legacyFile,original);
            var crop=new SquareCropState(new Size(source.Width,source.Height)).Transform;
            var json=MapLogic.Serializer();
            string icons=Path.Combine(library,"Icons"), objects=Path.Combine(library,"Objects");
            string iconFile=Path.Combine(icons,"icon-z-0131.json"), objectFile=Path.Combine(objects,"object-z-0131.json");
            File.WriteAllText(iconFile,json.Serialize(new { IconFormatVersion=3, Id="legacy-icon", Name="Ikona 0131", SourceId=source.Id, AssetName="Ikona", Crop=crop }));
            File.WriteAllText(objectFile,json.Serialize(new { ObjectFormatVersion=2, Id="legacy-object", Name="Obiekt 0131", SourceId=source.Id, AssetName="Obiekt", Crop=crop }));
            var icon=IconLibrary.Read(iconFile);
            var edited=icon.Copy(); edited.Name="Zmieniona stara";
            edited.DiskBytes=IconLibrary.Metadata(edited,icon);
            new IconLibraryChange { Before=icon, BeforeBytes=icon.DiskBytes, After=edited, AfterBytes=edited.DiskBytes }.Write(true);
            Check(IconLibrary.Read(iconFile).Asset.LibrarySource.LibraryFile==legacyFile &&
                ObjectLibrary.Read(objectFile).Asset.LibrarySource.LibraryFile==legacyFile,"Legacy square source was rewritten");
            string terrains=Directory.CreateDirectory(Path.Combine(library,"Terrains","Graphics")).FullName;
            string backgrounds=Directory.CreateDirectory(Path.Combine(library,"Backgrounds")).FullName;
            var graphic=Terrain(source,"Grafika 0131");
            var old=new TerrainGraphic { Id="old-terrain",Name="Grafika 0131",Asset=graphic };
            string oldPath=Path.Combine(terrains,"terrain-old.json");
            File.WriteAllText(oldPath,json.Serialize(old));
            var background=new BackgroundEntry { BackgroundFormatVersion=1, Id="legacy-background",Name="Tło 0131",
                Settings=new MapBackground { Source=source,SourceId=source.Id } };
            string backgroundPath=Path.Combine(backgrounds,"background-old.json");
            File.WriteAllText(backgroundPath,json.Serialize(background));
            var from = TerrainGraphicLibrary.Read(oldPath);
            var adjusted=from.Asset.Copy(); CropMath.Rotate(adjusted,false,25); adjusted.Seal();
            var after=TerrainGraphicLibrary.Prepare(terrains,from,from.Name,adjusted,new[]{from});
            new TerrainGraphicChange { Before=from,After=after }.Write(true);
            var read=TerrainGraphicLibrary.Read(after.Path);
            var another=TerrainGraphicLibrary.Prepare(terrains,null,"Drugi kadr",adjusted,new[]{read});
            new TerrainGraphicChange { After=another }.Write(true);
            Check(read.Asset.Pointy.Angle==25 && read.Asset.Source.LibraryFile==legacyFile &&
                BackgroundLibrary.Read(backgroundPath,new Dictionary<string,GraphicSource>()).Settings.Source.LibraryFile==legacyFile &&
                another.SourceFile=="Drugi kadr-original.jpg" && File.ReadAllBytes(legacyFile).SequenceEqual(original),
                "Legacy recrop changed original or new entry kept external reference");
            var older=GraphicSources.Import(Picture(Color.DarkGreen,false));
            var olderFolder=Directory.CreateDirectory(Path.Combine(terrains,"Sources")).FullName;
            File.WriteAllText(Path.Combine(olderFolder,older.Id+".json"),json.Serialize(older));
            File.WriteAllText(Path.Combine(terrains,"terrain-older.json"),
                json.Serialize(new TerrainGraphic { Id="older-terrain", Name="Starsza",Asset=Terrain(older,"Starsza") }));
            Check(TerrainGraphicLibrary.Read(Path.Combine(terrains,"terrain-older.json")).Asset.Source.Data==older.Data,"Legacy JSON source unreadable");
        }
        static void Queue()
        {
            string library=Path.Combine(Root,"External Library");
            var import=new FolderImport(library); import.EnsureFolders();
            string presets=Directory.CreateDirectory(Path.Combine(library,"Terrains","Presets")).FullName;
            var preset=PresetCatalog.Default(); preset.Id="queue-fixture"; preset.Name="Niezmieniana paleta";
            string presetPath=Path.Combine(presets,"queue-preset.json");
            File.WriteAllText(presetPath,MapLogic.Serializer().Serialize(preset));
            string mapPath=Path.Combine(Root,"queue-map.hexmap");
            MainWindow.WriteProjectData(MainWindow.CreateProjectData(4,3,"PointyTop","Mapa kolejki",preset),mapPath,false);
            byte[] savedMap=File.ReadAllBytes(mapPath);
            byte[] savedPreset=File.ReadAllBytes(presetPath);
            var image=Picture(Color.MediumPurple,false);
            var newer=Picture(Color.Green,false);
            string terrain=Add(library,ImportCategory.Terrains,"a.png",image);
            string icon=Add(library,ImportCategory.Icons,"a.png",image);
            string duplicate=Add(library,ImportCategory.Icons,"z.png",image);
            string background=Add(library,ImportCategory.Backgrounds,"a.png",image);
            string obj=Add(library,ImportCategory.Objects,"a.png",image);
            Add(library,ImportCategory.Objects,"bad.png",new byte[]{137,80,3,4});
            Check(import.Scan().Count==4,"Category dedup or broken input abort");
            string renamed=Path.Combine(Path.GetDirectoryName(icon),"b.png"); File.Move(icon,renamed);
            var entry=import.Scan().Single(x=>x.Category==ImportCategory.Icons);
            Check(entry.Path==renamed && entry.SourceId==GraphicSources.Id(image),"Rename created a new identity");
            import.Skip(entry); Check(import.Scan().Count==3 && File.Exists(renamed),"Skip did not suppress same bytes");
            File.WriteAllBytes(renamed,newer);
            entry=import.Scan().Single(x=>x.Category==ImportCategory.Icons);
            Check(entry.SourceId==GraphicSources.Id(newer),"Changed contents did not appear");
            using(var cancel=new SquareImportDialog(import.Read(entry),"Próba anulowania"))
            {
                cancel.Show(); ((Button)cancel.CancelButton).PerformClick(); Check(cancel.Result==null && File.Exists(renamed),"Cancel changed input");
            }
            var locked=import.Scan().Single(x=>x.Category==ImportCategory.Objects);
            var draft=new ImportDraft(locked.Category,import.Read(locked),"Obiekt",true);
            // Make metadata target unavailable, then change the actual input after successful editing.
            string blocked=Path.Combine(library,"Objects");File.WriteAllText(blocked,"conflict");
            try { draft.Save(import,locked); throw new InvalidOperationException("Catalog failure was accepted"); }
            catch(IOException) { Check(File.Exists(obj),"Catalog failure removed input"); }
            File.Delete(blocked);
            File.WriteAllBytes(obj,newer);
            string warning=draft.Save(import,locked);
            Check(warning!=null && File.ReadAllBytes(obj).SequenceEqual(newer),"Changed input was removed");
            var pending=import.Scan();
            Check(pending.Count==4,"Unexpected batch candidates");
            var batch=FolderImportDialog.ImportDefaults(import,pending);
            Check(batch.Result.Count==4 && batch.Notes.Count==0,"Batch or source removal failed: "+String.Join(" / ",batch.Notes));
            Check(File.ReadAllBytes(presetPath).SequenceEqual(savedPreset) && File.ReadAllBytes(mapPath).SequenceEqual(savedMap) &&
                Directory.GetFiles(presets).Length==1, "Queue changed a map or preset");
            Check(!File.Exists(terrain)&&!File.Exists(background)&&!File.Exists(renamed)&&!File.Exists(obj)&&File.Exists(duplicate),"Input cleanup touched skipped file");
            string left=Add(library,ImportCategory.Objects,"again.png",image);
            var restart=new FolderImport(library);
            Check(restart.Scan().All(c=>c.Path!=left),"Committed metadata did not survive a rescan");
            foreach (var sample in new[] {
                new { Category=Path.Combine("Terrains","Graphics"), Name="a-original.png", Data=image },
                new { Category="Backgrounds", Name="a-original.png", Data=image },
                new { Category="Icons", Name="b-original.png", Data=newer },
                new { Category="Objects", Name="Obiekt-original.png", Data=image } })
                Check(File.ReadAllBytes(Path.Combine(library,sample.Category,sample.Name)).SequenceEqual(sample.Data), "Incorrect queue original: "+sample.Category);
            Check(!Directory.Exists(Path.Combine(library,"Sources")) && restart.Scan().All(c=>
                c.Path.StartsWith(Path.Combine(library,"Import")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)),
                "Category file entered queue or created a central store");
            string removal=Add(library,ImportCategory.Icons,"Recycled.png",Picture(Color.Blue,false));
            RecycleFile.Send(removal,IntPtr.Zero);
            Check(!File.Exists(removal)&&File.Exists(duplicate),"Recycle touched other queue files");
        }
        static string Pixels(MapData data)
        {
            using(var first=TerrainTextures.Decode(data.Cells[0].Texture))
            using(var flat=TerrainTextures.Decode(data.Cells[1].Texture,true))
            using(var back=BackgroundRenderer.Render(data.Background,new Size(180,108)))
            {
                var asset=data.IconAssets.Single(a=>a.Id==data.Cells[0].IconId);
                return MapLogic.Hash(IconAssets.FromBitmap(first,"first").Png + IconAssets.FromBitmap(flat,"flat").Png +
                    IconAssets.FromBitmap(back,"background").Png + asset.Png);
            }
        }
        static void Map()
        {
            string separate = Path.Combine(Root,"Separate map sources");
            string terrains = Directory.CreateDirectory(Path.Combine(separate,"Terrains","Graphics")).FullName;
            var initial = Terrain(GraphicSources.Import(Picture(Color.Brown,true)),"Dwa kadry");
            var stored = TerrainGraphicLibrary.Prepare(terrains,null,initial.Name,initial,new TerrainGraphic[0]);
            new TerrainGraphicChange { After=stored }.Write(true);
            var source = TerrainGraphicLibrary.Read(stored.Path).Asset.Source;
            string sourceData = source.Data;
            var one=Terrain(source,"Dwa kadry"); var two=one.Copy();
            CropMath.Zoom(two,true,1.8); CropMath.Rotate(two,true,25); two.Seal();
            var fills=new List<FillVariant> { new FillVariant { Graphic=one, GraphicId=one.Id }, new FillVariant { Graphic=two, GraphicId=two.Id } };
            var preset=PresetCatalog.Default();preset.Id="test-0130";preset.Name="Paleta 0130";
            FillRules.SetPool(preset.Terrains[0],fills);
            var map=MainWindow.CreateProjectData(60,40,"PointyTop","Próba samodzielnej mapy",preset);
            for(int i=0;i<map.Cells.Count;i++) MapLogic.ApplyTerrain(map.Cells[i],preset.Terrains[0],fills[i%2]);
            map.Background=new MapBackground { Source=source,SourceId=source.Id,Opacity=65,Saturation=120,Blur=2,Left=.15,Width=.8 };
            var squareSource=GraphicSources.Import(Picture(Color.LightSkyBlue,false));
            var asset=SquareLibraryAsset.Render(squareSource,"Wspólny obraz",new SquareCropState(new Size(squareSource.Width,squareSource.Height)).Transform);
            map.IconAssets.Add(asset);
            map.Cells[0].IconId=asset.Id;map.Cells[0].IconEntryId="entry-icon";map.Cells[0].IconName=asset.Name;
            map.Objects.Add(new MapObject { TemplateId="entry-object",AssetId=asset.Id,Name="Wspólny obraz",X=35,Y=35,Size=44 });
            string signature=Pixels(map);
            string path=Path.Combine(Root,"portable-fixture.hexmap");
            MainWindow.WriteProjectData(map,path,false);
            Directory.Move(separate,Path.Combine(Root,"Disconnected map sources"));
            string json=File.ReadAllText(path);
            Check(!json.Contains(squareSource.Id),"Map contains a new square original.");
            var read=MainWindow.ReadProjectData(path);
            Check(read.GraphicSources.Count==1 && read.Objects[0].Size==44 && read.Background.Opacity==65 && Pixels(read)==signature,
                "Portable payload changed");
            var edited=read.Cells[1].Texture.Copy();
            CropMath.Rotate(edited,true,12);edited.Seal();
            MapLogic.ApplyTerrain(read.Cells[1],read.Presets[0].Terrains[0],new FillVariant { Graphic=edited,GraphicId=edited.Id });
            string editedPath=Path.Combine(Root,"portable-edited.hexmap");
            MainWindow.WriteProjectData(read,editedPath,false);
            var reloaded=MainWindow.ReadProjectData(editedPath);
            Check(reloaded.Cells[1].Texture.Flat.Angle==edited.Flat.Angle && Pixels(reloaded)!=signature &&
                reloaded.Background.Source.Data==sourceData && !Directory.Exists(Path.Combine(Root,"Library")),
                "Recrop or save without Library failed");
        }
        static void Metadata()
        {
            string library=Path.Combine(Root,"Moved graphics catalog");
            var import=new FolderImport(library); import.EnsureFolders();
            var bytes=Picture(Color.IndianRed,false);
            foreach(var category in FolderImport.Categories) Add(library,category,"Wspólna.png",bytes);
            var pending=import.Scan();
            var batch=FolderImportDialog.ImportDefaults(import,pending);
            Check(batch.Result.Count==4 && batch.Notes.Count==0,"Fresh queue could not save all metadata");
            var terrain=Path.Combine(library,"Terrains","Graphics");
            var icons=Path.Combine(library,"Icons");
            var objects=Path.Combine(library,"Objects");
            var backgrounds=Path.Combine(library,"Backgrounds");
            var categories=new[]{terrain,icons,objects,backgrounds};
            var flat=new Dictionary<string,byte[]>();
            foreach(var catalog in categories)
            {
                string original=Path.Combine(catalog,"Wspólna-original.png");
                Check(File.ReadAllBytes(original).SequenceEqual(bytes),"Original is not directly in "+catalog);
                string metadata=Path.Combine(catalog,"Metadata");
                Check(!Directory.GetFiles(metadata,"*.png").Any()&&!Directory.GetFiles(catalog,"*.json").Any(),"New layout put files in the wrong folder");
                var path=Directory.GetFiles(metadata,"*Wspólna*.json").Single();
                var legacy=Path.Combine(catalog,Path.GetFileName(path));
                File.Move(path,legacy); flat.Add(legacy,File.ReadAllBytes(legacy));
                File.WriteAllText(Path.Combine(catalog,"nieznany.json"),"{\"Id\":\"not-graphic\",\"Name\":\"Obcy wpis\"}");
            }
            // Open the flat layout through a new Library location to simulate a first load.
            string reopened=Path.Combine(Root,"Restored 0132");
            Directory.Move(library,reopened);
            string oldTerrain=Path.Combine(reopened,"Terrains","Graphics");
            string oldIcons=Path.Combine(reopened,"Icons");
            string oldObjects=Path.Combine(reopened,"Objects");
            string oldBackgrounds=Path.Combine(reopened,"Backgrounds");
            string oldTerrainPath=Directory.GetFiles(oldTerrain,"terrain-*.json").Single();
            var serde=MapLogic.Serializer();
            var competing=serde.DeserializeObject(File.ReadAllText(oldTerrainPath)) as Dictionary<string,object>;
            competing["Id"]="competing-terrain";
            competing["Name"]="Inny poprawny wpis";
            string collision=Path.Combine(oldTerrain,"Metadata",Path.GetFileName(oldTerrainPath));
            File.WriteAllText(collision,serde.Serialize(competing));
            string warning;
            var terrains=TerrainGraphicLibrary.Load(oldTerrain,out warning);
            Check(terrains.Count==2 && warning.Contains("pozostawiono dotychczasowy plik") && File.Exists(oldTerrainPath),
                "Collision hid an old entry: "+warning);
            var loadedIcons=IconLibrary.Load(oldIcons,out warning);
            var loadedObjects=ObjectLibrary.Load(oldObjects,out warning);
            var loadedBackgrounds=BackgroundLibrary.Load(oldBackgrounds,out warning);
            foreach(var pair in flat)
            {
                string relative=pair.Key.Substring(library.Length+1);
                string legacy=Path.Combine(reopened,relative);
                string catalog=Path.GetDirectoryName(legacy);
                string moved=Path.Combine(catalog,"Metadata",Path.GetFileName(legacy));
                Check(File.Exists(Path.Combine(catalog,"nieznany.json"))&&!File.Exists(Path.Combine(catalog,"Metadata","nieznany.json")),"Moved unknown JSON");
                if(legacy==oldTerrainPath)
                    Check(File.ReadAllBytes(legacy).SequenceEqual(pair.Value) && TerrainGraphicLibrary.Read(moved).Id=="competing-terrain","Collision overwrote data");
                else
                    Check(!File.Exists(legacy) && File.ReadAllBytes(moved).SequenceEqual(pair.Value),"Move rewrote "+relative);
            }
            Check(loadedBackgrounds.Single().Settings.Source.LibraryFile==Path.Combine(oldBackgrounds,"Wspólna-original.png")&&
                loadedObjects.Single(o=>o.Name=="Wspólna").Asset.LibrarySource.LibraryFile==Path.Combine(oldObjects,"Wspólna-original.png"),
                "Relocated references no longer resolve against their category");
            var first=loadedIcons.Single(i=>i.Name=="Wspólna");
            var change=new IconLibraryChange { Before=first,BeforeBytes=first.DiskBytes };
            change.Write(true); Check(!File.Exists(first.FilePath),"Delete missed relocated metadata");
            change.Write(false); Check(IconLibrary.Read(first.FilePath).Id==first.Id,"Undo did not restore migrated entry");
            var renamed=first.Copy(); renamed.Name="Po uporządkowaniu"; renamed.FilePath=IconLibrary.Destination(oldIcons,renamed.Name,first.FilePath);
            renamed.DiskBytes=IconLibrary.Metadata(renamed,first);
            var edit=new IconLibraryChange { Before=first,BeforeBytes=first.DiskBytes,After=renamed,AfterBytes=renamed.DiskBytes };
            edit.Write(true); Check(IconLibrary.Read(renamed.FilePath).SourceFile==first.SourceFile,"Edit changed the original");
            edit.Write(false);
            string blocked=Directory.CreateDirectory(Path.Combine(Root,"Unwritable layout","Backgrounds")).FullName;
            var before=loadedBackgrounds[0];
            File.Copy(before.Settings.Source.LibraryFile,Path.Combine(blocked,before.SourceFile));
            string flatBackground=Path.Combine(blocked,Path.GetFileName(before.Path));
            File.Copy(before.Path,flatBackground);
            File.WriteAllText(Path.Combine(blocked,"Metadata"),"not a directory");
            Check(BackgroundLibrary.Load(blocked,out warning).Single().Path==flatBackground && warning.Contains("Metadata"),
                "Failed metadata creation blocked legacy entry");
            try
            {
                BackgroundLibrary.Prepare(blocked,null,"Odmowa zapisu",before.Settings,new BackgroundEntry[0]);
                throw new InvalidOperationException("Write was allowed in an unavailable metadata folder");
            }
            catch(IOException) { Check(File.Exists(flatBackground),"Failed write removed old entry"); }
            Check(TerrainGraphicLibrary.Load(oldTerrain,out warning).Count==2 && IconLibrary.Load(oldIcons,out warning).Count==loadedIcons.Count,
                "Second load duplicated metadata");
            string again=Add(reopened,ImportCategory.Icons,"Ponownie.png",bytes);
            Check(new FolderImport(reopened).Scan().All(i=>i.Path!=again),"Queue ignored relocated metadata");
        }
        [STAThread]
        internal static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                if (args.Length != 1) throw new ArgumentException("Pass one explicit case.");
                switch(args[0]) {
                    case "library": Library(); break;
                    case "queue": Queue(); break;
                    case "map": Map(); break;
                    case "metadata": Metadata(); break;
                    default: throw new ArgumentException("Unknown case.");
                }
                Console.WriteLine(args[0]+" PASS"); return 0;
            }
            catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
