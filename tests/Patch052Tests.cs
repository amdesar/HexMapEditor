using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
namespace HexMapEditor.Tests
{
    internal static class Patch052Tests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Get(MainWindow w, string key) { return typeof(MainWindow).GetField(key, Private).GetValue(w); }
        static object Call(MainWindow w, string method, params object[] args) { return typeof(MainWindow).GetMethods(Private).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(w, args); }
        static MapData Map(MainWindow w) { return (MapData)Get(w, "map"); }
        static TerrainPreset Active(MainWindow w) { return Map(w).Presets.Single(p => p.Id == Map(w).ActivePresetId); }
        static void Check(bool value, string text) { if (!value) throw new Exception(text); }
        static void Create(MainWindow w, string dir, string name) { var args = new object[] { dir, name, null }; Check((bool)Call(w,"TryCreatePreset", args), "Create: " + args[2]); }
        static void Save(MainWindow w, string dir, bool copy, string name) { var args = new object[] { dir, copy, name, null }; Check((bool)Call(w,"TrySaveWorkingPreset", args), "Save: " + args[3]); }
        static string Old(string directory)
        {
            Directory.CreateDirectory(directory); string path = Path.Combine(directory,"Pominiety.json");
            File.WriteAllText(path,"{\"PresetFormatVersion\":2,\"Id\":\"old-052\",\"Name\":\"Pominiety\",\"Terrains\":[{\"TerrainId\":\"old\",\"VariantId\":\"old-v1\",\"Revision\":1,\"Name\":\"Stary\",\"Color\":\"#123456\",\"FillKind\":\"color\"}],\"LibraryOrder\":1}");
            return path;
        }
        static void Regression(string root)
        {
            string dir=Path.Combine(root,"presets"), old=Old(dir); byte[] original=File.ReadAllBytes(old);
            using(var w=new MainWindow())
            {
                Create(w,dir,"Nowy"); var created=Active(w).Copy();
                Check(created.PresetFormatVersion==3 && created.Terrains.Count==1,"Nowy format/neutralny.");
                var reloaded=PresetCatalog.ReadFile(Directory.GetFiles(dir).Single(p=>p!=old)); PresetCatalog.Validate(reloaded);
                Call(w,"SelectPreset",PresetCatalog.DefaultId); var baseline=MapLogic.Serializer().Serialize(Active(w));
                Save(w,dir,true,"Wlasna kopia"); var copy=Active(w);
                Check(copy.Id!=PresetCatalog.DefaultId && !(bool)typeof(MainWindow).GetProperty("PresetReadOnly",Private).GetValue(w,null),"Kopia nieedytowalna.");
                Check(copy.Terrains.Count==6 && copy.Terrains.Any(t=>t.TerrainId=="default-void"),"Pustka/neutralny zniknely.");
                copy.Terrains[0].Variants[0].Color="#112233";
                Check(baseline==MapLogic.Serializer().Serialize(Map(w).Presets.Single(p=>p.Id==PresetCatalog.DefaultId)),"Klon zmienia Domyslny.");
                string warning; var all=PresetCatalog.Load(dir,out warning);
                Check(all.Count==3 && warning.Contains("Pominiety.json"),"Pominiety plik uznany za poprawny lub brak odczytu nowych.");
                Check(original.SequenceEqual(File.ReadAllBytes(old)),"Stary plik zmieniony.");
            }
        }
        static void History(MainWindow w, bool redo, bool success)
        {
            var args = new object[] { redo, null };
            Check((bool)Call(w,"TryHistory",args)==success,"Historia: "+args[1]);
        }
        static string FileFor(string dir,string id) { return Directory.GetFiles(dir,"*.json").Single(f=>PresetCatalog.ReadFile(f).Id==id); }
        static GraphicDefinition Graphic(string root)
        {
            string path=Path.Combine(root,"Wlasny obraz.png");
            using(var image=new Bitmap(160,120)) { using(var g=Graphics.FromImage(image)){g.Clear(Color.SeaGreen);g.FillRectangle(Brushes.Gold,0,40,160,15);}image.Save(path,ImageFormat.Png); }
            var source=GraphicSources.Import(path);var graphic=new GraphicDefinition { Source=source,SourceId=source.Id,Name="Wlasny obraz" };
            graphic.Linked=false;CropMath.Rotate(graphic,false,90);CropMath.Zoom(graphic,true,1.4);graphic.Seal();return graphic;
        }
        static void Lifecycle(string root)
        {
            string dir=Path.Combine(root,"presets"), id, copyId, terrainId;
            var graphic=Graphic(root);
            using(var w=new MainWindow())
            {
                Create(w,dir,"Wlasny");id=Active(w).Id;
                var added=new TerrainDefinition { Name="Teren do edycji" };Call(w,"AddTerrain",added);terrainId=added.TerrainId;
                var before=Active(w).Terrains.Single(t=>t.TerrainId==terrainId);var next=before.Copy();
                next.Name="Laka po edycji";next.VariantId=Guid.NewGuid().ToString("N");next.Revision++;
                FillRules.SetPool(next,new List<FillVariant>{new FillVariant { Color="#123456" },new FillVariant { Color="#778899",Graphic=graphic,GraphicId=graphic.Id }});
                Call(w,"UpdateTerrain",before,next,false);Save(w,dir,false,null);
                var loaded=PresetCatalog.ReadFile(FileFor(dir,id));var terrain=loaded.Terrains.Single(t=>t.TerrainId==terrainId);
                Check(terrain.Name==next.Name && terrain.Color=="#123456" && terrain.Variants.Count==2,"Utrata nazwy/koloru/puli.");
                Check(TerrainTextures.Same(terrain.Variants[1].Graphic,graphic) && GraphicSources.Same(terrain.Variants[1].Graphic.Source,graphic.Source),"Zrodlo/kadry/referencje.");
                Save(w,dir,true,"Kopia mieszana");copyId=Active(w).Id;
                Check(copyId!=id,"Kopia uzywa ID oryginalu.");
                var copyTerrain=Active(w).Terrains.Single(t=>t.TerrainId==terrainId);copyTerrain.Variants[0].Color="#ABCDEF";
                Check(Map(w).Presets.Single(p=>p.Id==id).Terrains.Single(t=>t.TerrainId==terrainId).Variants[0].Color=="#123456","Kopia wspoldzieli mutowalna pule.");
                var cropDraft=copyTerrain.Variants[1].Graphic.Copy();CropMath.Rotate(cropDraft,true,40);cropDraft.Seal();
                Check(Map(w).Presets.Single(p=>p.Id==id).Terrains.Single(t=>t.TerrainId==terrainId).Variants[1].Graphic.Flat.Angle==0,"Edycja kadru kopii zmienia oryginal.");
                FillRules.SetPool(copyTerrain,copyTerrain.Variants);Save(w,dir,false,null);
                Call(w,"SelectPreset",id);Call(w,"SelectPreset",copyId);Check(Active(w).Id==copyId,"Powrot wyboru.");
                string oldFile=FileFor(dir,copyId);Active(w).Name="Przemianowana kopia";Save(w,dir,false,null);
                Check(!File.Exists(oldFile) && Path.GetFileName(FileFor(dir,copyId))=="Przemianowana kopia.json","Rename tworzy duplikat.");
                Call(w,"DeleteTerrain",terrainId);Save(w,dir,false,null);
                Check(!PresetCatalog.ReadFile(FileFor(dir,copyId)).Terrains.Any(t=>t.TerrainId==terrainId),"Usuniecie terenu nie utrwalone.");
            }
            File.Delete(Path.Combine(root,"Wlasny obraz.png"));
            using(var w=new MainWindow())
            {
                string warning;var loaded=PresetCatalog.Load(dir,out warning);Check(warning.Length==0 && loaded.Count==3,"Restart katalogu.");
                typeof(MainWindow).GetField("libraryPresets",Private).SetValue(w,loaded);
                Call(w,"SelectPreset",id);Check(Active(w).Terrains.Single(t=>t.TerrainId==terrainId).Variants.Count==2,"Restart utracil mieszana pule.");
                Call(w,"SelectPreset",copyId);var args=new object[]{dir,null};Check((bool)Call(w,"TryDeletePreset",args),"Usuniecie presetu: "+args[1]);
                Check(!Directory.GetFiles(dir).Any(f=>PresetCatalog.ReadFile(f).Id==copyId),"Plik nieusuwany.");
                History(w,false,true);Check(File.Exists(FileFor(dir,copyId)),"Undo nie odtworzyl pliku.");History(w,true,true);
                Check(Map(w).ActivePresetId==PresetCatalog.DefaultId,"Brak wyboru Domyslnego po usunieciu.");
                args=new object[]{dir,null};Check(!(bool)Call(w,"TryDeletePreset",args),"Usunieto Domyslny.");
                args=new object[]{dir,false,null,null};Check(!(bool)Call(w,"TrySaveWorkingPreset",args),"Nadpisano Domyslny.");
            }
        }
        static void Safety(string root)
        {
            string dir=Path.Combine(root,"presets"), old=Old(dir);var oldBytes=File.ReadAllBytes(old);
            using(var w=new MainWindow())
            {
                string before=MapLogic.Serializer().Serialize(Map(w));var args=new object[]{dir,"Pominiety",null};
                Check(!(bool)Call(w,"TryCreatePreset",args) && before==MapLogic.Serializer().Serialize(Map(w)),"Kolizja nazwy zmienia projekt.");
                var candidate=PresetCatalog.Neutral();var invalid=new TerrainPreset {Name="Niepoprawny"};invalid.Terrains.Add(candidate);candidate.Variants.Clear();
                bool rejected=false;try{PresetCatalog.PrepareWrite(invalid,dir,false,null);}catch(InvalidDataException){rejected=true;}
                Check(rejected && before==MapLogic.Serializer().Serialize(Map(w)) && Directory.GetFiles(dir).Length==1,"Niepoprawny preset zmienia plik/projekt.");
                var foreign=new TerrainPreset {Name="Inna nazwa"};foreign.Terrains.Add(PresetCatalog.Neutral());PresetCatalog.Save(foreign,dir);
                string foreignPath=FileForExceptOld(dir,foreign.Id,old), occupied=Path.Combine(dir,"Nowy.json");File.Move(foreignPath,occupied);var bytes=File.ReadAllBytes(occupied);
                Create(w,dir,"Nowy");string activeId=Active(w).Id;string path=Directory.GetFiles(dir).Single(f=>f!=old && f!=occupied);
                Check(Path.GetFileName(path)=="Nowy (2).json" && bytes.SequenceEqual(File.ReadAllBytes(occupied)),"Nadpisano obcy plik.");
                // The skipped payload still reserves identity, including ID during history replay.
                string duplicate=Path.Combine(dir,"Kolizja ID.json");File.WriteAllText(duplicate,"{\"Id\":\""+activeId+"\",\"Name\":\"Obcy\",\"PresetFormatVersion\":2}");
                int undo=((Stack<EditAction>)Get(w,"undo")).Count;History(w,false,false);
                Check(((Stack<EditAction>)Get(w,"undo")).Count==undo && Active(w).Id==activeId && File.Exists(path),"Konflikt przesunal historie/projekt.");
                File.Delete(duplicate);History(w,false,true);Check(!File.Exists(path),"Undo utworzenia zostawil plik.");
                File.WriteAllText(path,"obce bajty");History(w,true,false);Check(File.ReadAllText(path)=="obce bajty","Redo nadpisalo konflikt.");
                File.Delete(path);History(w,true,true);Check(PresetCatalog.ReadFile(path).Id==activeId,"Nie mozna ponowic Redo.");
                // An unreadable identity cannot safely be ignored as a hidden ID/name collision.
                string broken=Path.Combine(dir,"Uszkodzony.json");File.WriteAllText(broken,"{}");before=MapLogic.Serializer().Serialize(Map(w));
                args=new object[]{dir,"Po uszkodzeniu",null};Check(!(bool)Call(w,"TryCreatePreset",args) && before==MapLogic.Serializer().Serialize(Map(w)),"Pominieto nieznana tozsamosc.");
                File.Delete(broken);Check(oldBytes.SequenceEqual(File.ReadAllBytes(old)),"Zmieniono pomijany stary plik.");
            }
        }
        static string FileForExceptOld(string dir,string id,string old) { return Directory.GetFiles(dir).Where(f=>f!=old).Single(f=>PresetCatalog.ReadFile(f).Id==id); }
        [STAThread] static int Main(string[] args)
        {
            try { if(args.Length!=1)throw new ArgumentException("Wymagany Case.");
                if(args[0]=="regression")Regression(AppDomain.CurrentDomain.BaseDirectory);
                else if(args[0]=="lifecycle")Lifecycle(AppDomain.CurrentDomain.BaseDirectory);
                else if(args[0]=="safety")Safety(AppDomain.CurrentDomain.BaseDirectory);
                else throw new ArgumentException("Brak przypadku.");
                Console.WriteLine("PASS "+args[0]); return 0;
            } catch(Exception ex) { Console.WriteLine(ex);return 1; }
        }
    }
}
