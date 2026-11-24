using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0122Tests
    {
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static T Field<T>(MainWindow window,string name){return (T)typeof(MainWindow).GetField(name,Instance).GetValue(window);}
        static object Call(MainWindow window,string name,params object[] args){return typeof(MainWindow).GetMethods(Instance).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(window,args);}
        static MapData GroupMap()
        {
            var preset=PresetCatalog.Default().Copy();
            preset.Id="patch-0122-fixture";preset.Name="Paleta 0122";
            var map=MainWindow.CreateProjectData(15,10,"PointyTop","Grupa i mgła",preset);
            var asset=preset.Objects.First().Asset;
            if(!map.IconAssets.Any(a=>a.Id==asset.Id))map.IconAssets.Add(asset);
            foreach(int col in new[]{4,6})
            {
                var position=BrushGeometry.Center(map,col,4);
                map.Objects.Add(new MapObject{Name=col==4?"Członek neutralny":"Członek sojusznik",TemplateId="0122",
                    AssetId=asset.Id,Size=30,X=position.X,Y=position.Y,Side=col==4?"Neutral":"Ally"});
            }
            var group=new ObjectGroup{Name="Sprawdzenie mgły",StateId="Ally",RevealsFog=true,Members=map.Objects.Select(o=>o.Id).ToList()};
            GroupRules.Set(group,map.Objects[0].Id,GroupProperty.Fog,true);
            map.Groups.Add(group);
            return map;
        }
        static void Seed(string program)
        {
            var storage=new AppStorage(program);
            Directory.CreateDirectory(storage.DataRoot);
            var fixtures=Directory.CreateDirectory(Path.Combine(program,"Data","Hexmaps")).FullName;
            var assets=Directory.CreateDirectory(Path.Combine(storage.LibraryRoot,"Terrains","Presets")).FullName;
            var map=GroupMap();PresetCatalog.Write(map.Presets.First(p=>p.Id==map.ActivePresetId),assets,false);
            var paths=Enumerable.Range(1,4).Select(i=>{
                var path=Path.Combine(fixtures,"Próba "+i+".hexmap");
                MainWindow.WriteProjectData(MainWindow.CreateProjectData(5,4,"FlatTop","Próba "+i,PresetCatalog.Default()),path,false);
                return path;
            }).ToList();
            var groupPath=Path.Combine(fixtures,"Grupa i mgła.hexmap");
            MainWindow.WriteProjectData(map,groupPath,false);
            paths.Insert(0,groupPath);
            paths.AddRange(MapLogic.Serializer().Deserialize<string[]>(File.ReadAllText(storage.RecentProjectsPath)));
            File.WriteAllText(storage.RecentProjectsPath,MapLogic.Serializer().Serialize(paths));
        }
        static void Controls()
        {
            var map=GroupMap();
            string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Stan po odczycie.hexmap");
            MainWindow.WriteProjectData(map,path,false);
            map=MainWindow.ReadProjectData(path);
            using(var w=new MainWindow(map,path))
            {
                w.Show();
                w.Size=w.MinimumSize;
                var sections=Field<TableLayoutPanel>(w,"leftSections");
                Check(sections.GetControlFromPosition(0,5).Visible&&sections.GetControlFromPosition(0,5).AccessibleName=="Zakładki","Missing tabs heading");
                var cursor=Field<Label>(w,"coordinateLabel");
                var status=Field<Label>(w,"statusLabel");
                var tabs=Field<Panel>(w,"tabViewport");
                Rectangle fixedStatus=status.Bounds,fixedTabs=tabs.Bounds;
                map.Columns=map.Rows=200; // Exercise the label limit without drawing a 40,000-cell fixture.
                foreach(int cell in new[]{0,39999,-1})
                {
                    Call(w,"UpdateMapStatus",cell);
                    Check(status.Bounds==fixedStatus&&tabs.Bounds==fixedTabs,"Status move shifted other controls");
                    Check(TextRenderer.MeasureText(cursor.Text,cursor.Font).Width<=cursor.Width-cursor.Padding.Horizontal,"Long coordinate does not fit");
                }
                map.Columns=15;map.Rows=10;
                Call(w,"SelectGroup",map.Groups[0]);
                var grid=Field<MemberMatrix>(w,"groupMatrix");
                var fog=(SwitchBox)grid.GetControlFromPosition(3,1);
                var sight=(SwitchBox)grid.GetControlFromPosition(4,1);
                var state=(SwitchBox)grid.GetControlFromPosition(1,1);
                Check(!fog.Enabled&&fog.Checked&&sight.Enabled&&!GroupRules.Effective(map,map.Objects[0]).RevealsFog,"Incorrect saved neutral override");
                state.Checked=true;
                Check(fog.Enabled&&fog.Checked&&GroupRules.Effective(map,map.Objects[0]).RevealsFog,"Allied effective state did not unlock saved fog");
                fog.Checked=false;
                state.Checked=false;
                Check(!fog.Enabled&&!fog.Checked&&sight.Enabled,"Unchecking State did not retain fog preference");
                state.Checked=true;
                Check(fog.Enabled&&!fog.Checked,"Taking allied state silently set fog override");
                state.Checked=false;
                grid.ColumnClicked(GroupProperty.Fog);
                Check(!fog.Checked && (GroupRules.Used(map.Groups[0],map.Objects[1].Id)&GroupProperty.Fog)!=0,"Column changed ineligible member");
            }
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                switch(args[0])
                {
                    case "controls": Controls();break;
                    case "seed": Seed(args[1]);break;
                    default: throw new ArgumentException("Wybierz jawny przypadek próby.");
                }
                Console.WriteLine(args[0]+" PASS");return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
        }
    }
}
