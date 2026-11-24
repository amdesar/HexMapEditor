using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0110Tests
    {
        const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        static object Get(object target,string name)
        {
            var type=target.GetType();var field=type.GetField(name,Members);
            return field!=null?field.GetValue(target):type.GetProperty(name,Members).GetValue(target,null);
        }
        static T Get<T>(object target,string name){return (T)Get(target,name);}
        static void Set(object target,string name,object value){target.GetType().GetField(name,Members).SetValue(target,value);}
        static object Call(object target,string name,params object[] args)
        {return target.GetType().GetMethods(Members).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);}
        static object Active(MainWindow w){return Get(w,"activeDocument");}
        static IList Cards(MainWindow w){return Get<IList>(w,"documents");}
        static MapData Map(MainWindow w){return Get<MapData>(w,"map");}
        static bool Open(MainWindow w,string path)
        {
            var args=new object[]{path,null,false};
            bool ok=(bool)Call(w,"TryLoadProject",args);
            if(!ok)Console.WriteLine(args[1]);return ok;
        }
        static void Edit(MainWindow w,string name)
        {
            Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");
            Call(w,"RenameSelectedHex",name);
        }
        static void Move(MainWindow w,object card)
        {Check((bool)Call(w,"ActivateDocument",card)&&ReferenceEquals(Active(w),card),"Switch card");}
        static string[] Fixtures()
        {
            var a=PresetCatalog.Default();a.Id="shared-A";a.Name="Paleta o tej samej nazwie";
            var c=a.Copy();c.Id="other-C";c.Terrains[0].Name="Osobna paleta";
            PresetCatalog.Write(a,LibraryPaths.Presets,false);
            string extra=Path.Combine(Root,"Osobna biblioteka");PresetCatalog.Write(c,extra,false);
            File.Copy(Directory.GetFiles(extra,"*.json").Single(),Path.Combine(LibraryPaths.Presets,"Druga paleta.json"));
            var paths=new[]{Path.Combine(Root,"Mapa A.hexmap"),Path.Combine(Root,"Mapa B.hexmap"),
                Path.Combine(Directory.CreateDirectory(Path.Combine(Root,"Drugi folder")).FullName,"Mapa A.hexmap")};
            for(int i=0;i<paths.Length;i++)
            {
                var map=MainWindow.CreateProjectData(7+i,5,"PointyTop",new[]{"Mapa A","Mapa B","Mapa C"}[i],i==2?c:a);
                var definition=map.Presets[0].Terrains[0];
                MapLogic.ApplyTerrain(map.Cells[0],definition,definition.Variants[0]);
                if(i==0)
                {
                    var asset=map.Presets[0].Icons[0].Asset;map.IconAssets.Add(asset);
                    for(int col=0;col<2;col++)
                    {
                        var center=BrushGeometry.Center(map,col,2);
                        map.Objects.Add(new MapObject{Name="Obiekt "+col,TemplateId="probe",AssetId=asset.Id,
                            Side="Ally",Size=24,X=center.X,Y=center.Y});
                    }
                    map.Groups.Add(new ObjectGroup{Name="Grupa A",Members=map.Objects.Select(o=>o.Id).ToList()});
                }
                MainWindow.WriteProjectData(map,paths[i],false);RecentProjects.Shared.Touch(paths[i]);
            }
            return paths;
        }
        static void Documents(string[] paths)
        {
            using(var w=new MainWindow(MainWindow.ReadProjectData(paths[0]),paths[0]))
            {
                w.Show();var a=Active(w);w.ConfirmUnsavedForTest=()=>DialogResult.Cancel;
                Edit(w,"Zmiana A");
                var tool=EditorTool.Hand;
                Call(w,"SelectTool",tool);
                Call(w,"SelectTerrain",((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].TerrainId);
                Set(w,"rightCollapsed",true);Set(w,"scale",.75f);Set(w,"pan",new PointF(140,160));
                var heading=Get<DetailNameHeader>(w,"hexDetailName");
                Call(w,"UpdateUI");Call(heading,"Begin");
                heading.Controls.OfType<TextBox>().Single().Text="Draft niezapisany";
                Check(Open(w,paths[1]),"New B");
                var b=Active(w);Check(Cards(w).Count==2,"New card count");
                Check(Get<EditorTool>(w,"currentTool")==tool&&Get<bool>(w,"noMaterial"),"Shared tool or stale material");
                Check(Get<int>(w,"selected")==-1&&!Get<bool>(w,"rightPanelOpen"),"B inherited selection");
                Edit(w,"Zmiana B");
                Check((bool)Call(w,"TrySaveTo",paths[1],true,null),"Save B");
                Check(Get<bool>(a,"Unsaved")&&!Get<bool>(b,"Unsaved"),"Save B changed dirty of A");
                Check(MainWindow.ReadProjectData(paths[1]).Cells[0].Name=="Zmiana B"&&String.IsNullOrEmpty(MainWindow.ReadProjectData(paths[0]).Cells[0].Name),"Wrong file saved");
                Check(Open(w,paths[0].ToUpperInvariant())&&Cards(w).Count==2&&ReferenceEquals(Active(w),a),"Open case-insensitive duplicate");
                Check(Map(w).Cells[0].Name=="Zmiana A"&&Get<bool>(w,"rightCollapsed")&&Get<int>(w,"selected")==0&&
                    Get<float>(w,"scale")==.75f&&Get<PointF>(w,"pan")==new PointF(140,160),"A view or cancelled draft");
                Check((bool)Call(w,"TryHistory",false,null),"Undo A");
                Check(String.IsNullOrEmpty(Map(w).Cells[0].Name)&&Get<MapData>(b,"Map").Cells[0].Name=="Zmiana B","Shared Undo");
                Check((bool)Call(w,"TryHistory",true,null),"Redo A");
                var group=Map(w).Groups[0];Call(w,"SelectGroup",group);
                Move(w,b);Move(w,a);
                Check(Get<string>(w,"selectedGroupId")==group.Id&&Get<bool>(w,"rightPanelOpen"),"Group not restored");
                string item=group.Members[0];Call(w,"SelectPanelMember",item);
                Move(w,b);Move(w,a);
                Check(Get<string>(w,"selectedGroupId")==null&&Get<System.Collections.Generic.HashSet<string>>(w,"selectedObjects").SetEquals(new[]{item}),"Instance not restored");
                Check(!(bool)Call(w,"TrySaveTo",paths[1],true,null),"Overwrote another open file");
                Check(!(bool)Call(w,"SaveAsTo",paths[1],new Func<string,DialogResult>(p=>{throw new InvalidOperationException("Unexpected overwrite prompt");})),"Save as overwrote another open file");

                var first=(TerrainPreset)Call(w,"ActivePreset");
                var original=first.Terrains[0];var changed=original.Copy();changed.Name="Wspólny draft";
                Call(w,"UpdateTerrain",original,changed,false);
                Move(w,b);
                Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name=="Wspólny draft"&&
                    Map(w).Cells[0].Terrain!="Wspólny draft"&&!Get<bool>(b,"Unsaved"),"Shared palette leaked to saved cells or failed to share");
                Check((bool)Call(w,"TryRefreshPresets",LibraryPaths.Presets,null)&&
                    ((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name=="Wspólny draft","Refresh discarded draft");
                var quick=Get<QuickAccess>(Get(w,"CurrentQuick"),"Icons");
                string removed=quick.Visible[0];
                var independentVisible=quick.Visible;
                Move(w,a);
                Check(Get<QuickAccess>(Get(w,"CurrentQuick"),"Icons").Visible.SequenceEqual(independentVisible),"Wrong initial quick set");
                Call(w,"EditVisibleSet",false,new Action(()=>Call(w,"RemoveQuickEntry",removed,false,false)));
                Move(w,b);
                Check(!Get<QuickAccess>(Get(w,"CurrentQuick"),"Icons").Visible.Contains(removed)&&!Get<bool>(b,"Unsaved"),"Explicit quick change did not follow shared draft");
                Check(Open(w,paths[2]),"Same file name in different folder");var c=Active(w);
                Check(((TerrainPreset)Call(w,"ActivePreset")).Terrains[0].Name=="Osobna paleta"&&
                    Get<QuickAccess>(Get(w,"CurrentQuick"),"Icons").Visible.Contains(removed),"Distinct preset files were merged");
                Check(!Open(w,Path.Combine(Root,"Nie istnieje.hexmap"))&&ReferenceEquals(Active(w),c)&&Cards(w).Count==3,"Bad open changed view");
                Call(w,"CloseDocument",a);
                Check(Cards(w).Count==3&&ReferenceEquals(Active(w),c),"Cancel close failed");
                w.ConfirmUnsavedForTest=()=>DialogResult.No;
                Call(w,"CloseDocument",b);
                Check(Cards(w).Count==2&&ReferenceEquals(Active(w),c),"Closing an inactive card changed selection");
                Edit(w,"Zmiana C");int review=0;
                w.ConfirmUnsavedForTest=()=>++review==1?DialogResult.Yes:DialogResult.Cancel;
                w.Close();
                Check(!w.IsDisposed&&Cards(w).Count==2&&MainWindow.ReadProjectData(paths[0]).Cells[0].Name=="Zmiana A"&&Get<bool>(c,"Unsaved"),"Cancel exit rolled back A or lost C");

                int min=w.MinimumSize.Width;w.Width=min;
                int height=Get<Panel>(w,"canvas").Height;
                Move(w,a);Call(w,"FitMap");
                Check(Get<Panel>(w,"tabViewport").Width>=414&&!Get<Button>(w,"tabsBack").Enabled,"Minimum or initial tab viewport");
                for(int i=0;i<5;i++)Call(w,"StartNewProject",4,4,"PointyTop","Karta "+(i+1),PresetCatalog.Default());
                Check(Get<Button>(w,"tabsBack").Enabled&&!Get<Button>(w,"tabsForward").Enabled,"Reveal or arrow state");
                Call(w,"FitMap");Check(Get<Panel>(w,"canvas").Height==height,"Tab count changed canvas height");
                Move(w,a);Call(w,"FitMap");Check(Get<Button>(w,"tabsForward").Enabled,"Forward not available at left side");
                Console.WriteLine("Minimum {0}, viewport {1} px, bottom {2} px, canvas height {3} px",min,
                    Get<Panel>(w,"tabViewport").Width,Get<Panel>(w,"tabBar").Height,height);
            }
        }
        static void Autosave(string[] paths)
        {
            using(var w=new MainWindow(MainWindow.ReadProjectData(paths[0]),paths[0]))
            using(var timer=new Timer{Interval=80})
            {
                Exception failed=null;object a=null,b=null,unnamed=null;var elapsed=Stopwatch.StartNew();
                w.Shown+=(s,e)=>{
                    try
                    {
                        a=Active(w);Edit(w,"A w konflikcie");Open(w,paths[1]);b=Active(w);Edit(w,"B autozapis");
                        Call(w,"StartNewProject",4,4,"PointyTop","Bez ścieżki",PresetCatalog.Default());unnamed=Active(w);
                        Edit(w,"Bez pliku");Move(w,a);
                        File.AppendAllText(paths[0]," ");
                        foreach(var doc in Cards(w))Get<AutoSaveSchedule>(doc,"Schedule").Due=0;
                        w.AutoPreparedForTest=()=>Move(w,unnamed);
                        w.ActiveControl=null;Call(w,"PollAutoSave");timer.Start();
                    }
                    catch(Exception ex){failed=ex;w.Dispose();}
                };
                timer.Tick+=(s,e)=>{
                    try
                    {
                        if(w.AutoSaveCount==0||w.AutoSaveError==null){Check(elapsed.ElapsedMilliseconds<9000,"Cycle did not complete");return;}
                        Check(w.AutoSaveCount==1&&Get<bool>(a,"Unsaved")&&!Get<bool>(b,"Unsaved"),"Autosave dirty or continuation");
                        Check(Get<string>(unnamed,"Path")==null&&Get<bool>(unnamed,"Unsaved")&&ReferenceEquals(Active(w),unnamed),"Pathless was saved or active tab changed");
                        Check(MainWindow.ReadProjectData(paths[1]).Cells[0].Name=="B autozapis"&&
                            MainWindow.ReadProjectData(paths[1]).LastSavedAppVersion==MainWindow.Version,"Background file contents");
                        Check(String.IsNullOrEmpty(MainWindow.ReadProjectData(paths[0]).Cells[0].Name),"Conflicting file overwritten");
                    }
                    catch(Exception ex){failed=ex;}
                    timer.Stop();w.Dispose();
                };
                Application.Run(w);
                if(failed!=null)throw failed;
            }
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                LibraryPaths.EnsureDirectories(Root);
                string name=args.Single();var paths=Fixtures();
                switch(name){case "documents":Documents(paths);break;case "autosave":Autosave(paths);break;case "fixture":break;default:throw new ArgumentException("Unknown Case");}
                Console.WriteLine(name+" PASS");return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
