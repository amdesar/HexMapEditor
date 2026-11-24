using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0111Tests
    {
        const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static T Get<T>(object target,string name)
        {
            var t=target.GetType();
            var field=t.GetField(name,Members);
            return (T)(field!=null?field.GetValue(target):t.GetProperty(name,Members).GetValue(target,null));
        }
        static void Set(object target,string name,object value){target.GetType().GetField(name,Members).SetValue(target,value);}
        static object Call(object target,string name,params object[] args)
        {return target.GetType().GetMethods(Members).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);}
        static object Active(MainWindow w){return Get<object>(w,"activeDocument");}
        static IList Cards(MainWindow w){return Get<IList>(w,"documents");}
        static MapData Map(MainWindow w){return Get<MapData>(w,"map");}
        static void Open(MainWindow w,string path)
        {
            var args=new object[]{path,null,false};
            Check((bool)Call(w,"TryLoadProject",args),"Open: "+args[1]);
        }
        static void Move(MainWindow w,object card)
        {Check((bool)Call(w,"ActivateDocument",card)&&ReferenceEquals(Active(w),card),"Switch to wrong card");}
        static void Edit(MainWindow w,string name)
        {
            Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");Call(w,"RenameSelectedHex",name);
        }
        static string[] Fixtures()
        {
            var a=PresetCatalog.Default();a.Id="0111-shared";a.Name="Wspólna paleta 0111";
            var c=a.Copy();c.Id="0111-other";c.Name="Druga paleta 0111";
            PresetCatalog.Write(a,LibraryPaths.Presets,false);
            PresetCatalog.Write(c,LibraryPaths.Presets,false);
            var paths=new[]{"A — Długa nazwa projektu pokazuje początek, wielokropek i gwiazdkę","B — Następna mapa","C — Trzecia mapa","D — Czwarta mapa","E — Piąta mapa","F — Szósta mapa"}
                .Select(name=>Path.Combine(Root,name+".hexmap")).ToArray();
            for(int i=0;i<paths.Length;i++)
            {
                var map=MainWindow.CreateProjectData(8+i,5,"PointyTop","Próba 0111",i<2?a:c);
                var t=map.Presets[0].Terrains[0];
                MapLogic.ApplyTerrain(map.Cells[0],t,t.Variants[0]);
                var asset=map.Presets[0].Icons[0].Asset;
                map.IconAssets.Add(asset);
                map.Objects.Add(new MapObject{Name="Daleki obiekt",TemplateId="fit",AssetId=asset.Id,Size=30,X=1000,Y=180,Side="Ally"});
                MainWindow.WriteProjectData(map,paths[i],false);
            }
            return paths;
        }
        static MainWindow Window(string path)
        {
            var w=new MainWindow(MainWindow.ReadProjectData(path),path);
            w.Show();
            return w;
        }
        static void TabsFit(string[] paths)
        {
            using(var w=Window(paths[0]))
            {
                var a=Active(w);
                w.Width=w.MinimumSize.Width;
                var band=Get<Panel>(w,"tabBar");
                var viewport=Get<Panel>(w,"tabViewport");
                var back=Get<Button>(w,"tabsBack");
                var next=Get<Button>(w,"tabsForward");
                var status=Get<Label>(w,"statusLabel");
                Check(viewport.Width==207&&next.Left==viewport.Right+2&&!back.Enabled&&!next.Enabled,"Single card arrows");
                Check(status.Width>=TextRenderer.MeasureText(status.Text,status.Font).Width+status.Padding.Horizontal,"Full status does not fit");
                Edit(w,"Zmieniony heks");
                var tile=(Panel)Get<IDictionary>(w,"tabControls")[a];
                var title=(TabPartButton)tile.Controls[0];
                Check(title.Text.StartsWith("A — Długa")&&title.Dirty&&!title.Text.Contains("*")&&tile.BackColor==ChoiceStyle.ActiveBack,"Independent star or active background");
                Check(tile.Controls.Cast<Control>().All(x=>x.BackColor==tile.BackColor),"Card backgrounds differ");
                var actions=tile.Controls.OfType<Button>().Skip(1).ToArray();
                Check(actions.Length==2&&actions.All(b=>b.TabStop)&&actions[0].TabIndex<actions[1].TabIndex,"Tab order of actions");
                foreach(string path in paths.Skip(1))Open(w,path);
                Check(back.Enabled&&!next.Enabled,"Last page arrows");
                Move(w,a);
                Check(!back.Enabled&&next.Enabled&&next.BackColor==ChoiceStyle.ActiveBack,"First page arrows");
                Check(viewport.Width>=414&&viewport.Width%207==0&&next.Left==viewport.Right+2,"Minimum, cut-off tab or detached forward arrow");
                next.PerformClick();
                Check(back.Enabled&&next.Enabled&&back.BackColor==ChoiceStyle.ActiveBack,"Middle page arrows");
                Check(viewport.Controls.Cast<Control>().Where(x=>x.Visible).All(x=>viewport.ClientRectangle.Contains(x.Bounds)),"Partially visible tab");
                Check(band.Height==Get<ToolStrip>(w,"quickStrip").Height,"Bottom bar height");
                Move(w,Cards(w)[1]);
                Check(tile.BackColor==ChoiceStyle.NeutralBack&&title.Dirty,"Inactive dirty tile");
                Move(w,a);
                var targets=new Dictionary<string,float>();
                foreach(string fit in new[]{"FitMap","FitEverything"})
                {
                    var values=new List<Tuple<float,PointF>>();
                    for(int state=0;state<3;state++)
                    {
                        Set(w,"rightPanelOpen",state!=0);Set(w,"rightCollapsed",state==2);
                        Call(w,"UpdateUI");Call(w,fit);
                        Check(Get<bool>(w,"rightPanelOpen")== (state!=0)&&Get<bool>(w,"rightCollapsed")== (state==2),"Fit changed overlay state");
                        values.Add(Tuple.Create(Get<float>(w,"scale"),Get<PointF>(w,"pan")));
                    }
                    Check(values.All(v=>v.Equals(values[0])),fit+" differs under right overlay");
                    targets.Add(fit,values[0].Item1);
                    Console.WriteLine(fit+" all 3 states: "+values[0]);
                }
                Check(targets["FitEverything"]<targets["FitMap"],"Fit everything lost distant object");
                Console.WriteLine("Minimum="+w.Width+", viewport="+viewport.Width+", status="+status.Width);
            }
        }
        static void Autosave(string[] paths)
        {
            using(var w=new MainWindow(MainWindow.ReadProjectData(paths[0]),paths[0]))
            using(var timer=new Timer{Interval=35})
            {
                long minute=60000,now=0;
                var schedule=Get<AutoSaveSchedule>(w,"autoSchedule");
                schedule.Clock=()=>now;schedule.Reset();
                Exception failure=null;
                object a=null,b=null,c=null,memory=null;
                var messages=new List<string>();
                var watch=Stopwatch.StartNew();
                bool staleCycle=false;
                w.Shown+=(s,e)=>{
                    try
                    {
                        a=Active(w);Edit(w,"Konflikt A");
                        now=minute;Open(w,paths[1]);b=Active(w);Edit(w,"Ręczny B");
                        Check((bool)Call(w,"SaveProject"),"Manual B failed");
                        Edit(w,"Auto B");
                        now=3*minute;Open(w,paths[2]);c=Active(w);Edit(w,"Auto C");
                        Call(w,"StartNewProject",4,4,"PointyTop","Bez pliku",PresetCatalog.Default());memory=Active(w);
                        File.AppendAllText(paths[0]," ");
                        Get<Label>(w,"statusLabel").TextChanged+=(ss,ee)=>{
                            string line=Get<Label>(w,"statusLabel").Text;
                            if(line.StartsWith("Autozapis:"))messages.Add(line);
                        };
                        Check(schedule.Due==5*minute,"Open/manual save moved shared deadline");
                        now=5*minute-1;Call(w,"PollAutoSave");
                        Check(!Get<bool>(w,"autoSaving"),"Autosave before deadline");
                        now++;Call(w,"PollAutoSave");
                        Check(schedule.Due==10*minute&&Get<bool>(w,"autoSaving"),"Missing first cycle");
                        timer.Start();
                    }
                    catch(Exception ex){failure=ex;w.Dispose();}
                };
                timer.Tick+=(s,e)=>{
                    try
                    {
                        Check(watch.ElapsedMilliseconds<14000,"Autosave timeout");
                        if(messages.Count==0)return;
                        if(staleCycle)
                        {
                            if(messages.Count<2)return;
                            Check(messages[1]=="Autozapis: zapisano 0, problemy: 1."&&Get<bool>(c,"Unsaved")&&
                                MainWindow.ReadProjectData(paths[2]).Cells[0].Name=="Auto C","Stale snapshot was published or went clean");
                            timer.Stop();w.Dispose();return;
                        }
                        Check(messages.SequenceEqual(new[]{"Autozapis: zapisano 2, problemy: 1."}),"Multiple or inaccurate summaries");
                        Check(w.AutoSaveCount==2&&w.AutoSaveError!=null&&w.AutoSaveError.Contains("Długa nazwa"),"Conflict was not reported");
                        Check(Get<bool>(a,"Unsaved")&&!Get<bool>(b,"Unsaved")&&!Get<bool>(c,"Unsaved"),"Dirty state assigned to wrong doc");
                        Check(Get<string>(memory,"Path")==null&&Get<bool>(memory,"Unsaved")&&ReferenceEquals(Active(w),memory),"Memory-only map or selection changed");
                        Check(MainWindow.ReadProjectData(paths[1]).Cells[0].Name=="Auto B"&&MainWindow.ReadProjectData(paths[2]).Cells[0].Name=="Auto C","Inactive or late-opened map not written");
                        Check(String.IsNullOrEmpty(MainWindow.ReadProjectData(paths[0]).Cells[0].Name),"Overwrite despite conflict");
                        w.ConfirmUnsavedForTest=()=>DialogResult.No;Call(w,"CloseDocument",a);
                        Check(schedule.Due==10*minute&&Cards(w).Count==3,"Closing oldest moved shared deadline");
                        now=10*minute;Call(w,"PollAutoSave");
                        Check(messages.Count==1&&schedule.Due==15*minute,"Empty cycle reported or next deadline wrong");
                        Move(w,c);Edit(w,"Migawka C");Move(w,memory);
                        w.AutoPreparedForTest=()=>{
                            Move(w,c);
                            // Simulate a revision update at the authorization seam.
                            // Normal input is fenced while this document is being written.
                            var cell=Map(w).Cells[0];cell.Name="Nowsza zmiana C";Call(w,"MarkCellDirty",cell);
                        };
                        staleCycle=true;now=15*minute;Call(w,"PollAutoSave");
                        return;
                    }
                    catch(Exception ex){failure=ex;}
                    timer.Stop();w.Dispose();
                };
                Application.Run(w);
                if(failure!=null)throw failure;
            }
        }
        static void PaletteChange(MainWindow w,string name)
        {
            var p=(TerrainPreset)Call(w,"ActivePreset");
            var before=p.Terrains[0];var after=before.Copy();after.Name=name;
            Call(w,"UpdateTerrain",before,after,false);
        }
        static void Closing(string[] paths)
        {
            foreach(var first in new[]{DialogResult.No,DialogResult.Yes})
            using(var w=Window(paths[0]))
            {
                var a=Active(w);Edit(w,"Zmiana A "+first);
                Open(w,paths[1]);var b=Active(w);Edit(w,"Zmiana B");
                Open(w,paths[2]);var c=Active(w);
                w.ConfirmUnsavedForTest=()=>{
                    Check(ReferenceEquals(Active(w),a)&&Get<Panel>(w,"right").Visible&&w.Text.Contains("Długa nazwa"),"Wrong map behind tab prompt");
                    return DialogResult.Cancel;
                };
                Call(w,"CloseDocument",a);
                Check(Cards(w).Count==3&&ReferenceEquals(Active(w),a),"Cancel tab restored prior selection");
                int prompts=0;
                w.ConfirmUnsavedForTest=()=>{
                    Check(ReferenceEquals(Active(w),prompts==0?a:b),"Wrong map behind exit prompt");
                    return prompts++==0?first:DialogResult.Cancel;
                };
                w.Close();
                Check(!w.IsDisposed&&Cards(w).Count==2&&!Cards(w).Contains(a)&&Cards(w).Contains(c)&&ReferenceEquals(Active(w),b)&&Get<bool>(b,"Unsaved"),"Progressive close did not keep B and C only");
                Check((MainWindow.ReadProjectData(paths[0]).Cells[0].Name=="Zmiana A Yes")== (first==DialogResult.Yes),"Discard/save A wrong on disk");
            }
            using(var w=Window(paths[0]))
            {
                PaletteChange(w,"Wspólny draft");
                var icons=(QuickSnapshot)Call(w,"CaptureQuick");
                Call(w,"EditVisibleSet",false,new Action(()=>Call(w,"RemoveQuickEntry",icons.Icons[0],false,false)));
                var a=Active(w);Open(w,paths[1]);var b=Active(w);
                Edit(w,"Zmieniona B");
                w.ConfirmPresetForTest=p=>{throw new InvalidOperationException("Premature shared preset prompt");};
                Call(w,"CloseDocument",a);
                Check(Cards(w).Count==1&&ReferenceEquals(Active(w),b),"Closing clean inactive map switched or failed");
                Call(w,"StartNewProject",4,4,"PointyTop","Bez ścieżki",PresetCatalog.Default());var memory=Active(w);
                ProjectCreation.ShowSaveDialog=(d,o)=>{d.FileName=paths[1];return DialogResult.OK;};
                try
                {
                    w.ConfirmUnsavedForTest=()=>DialogResult.Yes;
                    Call(w,"CloseDocument",memory);
                    Check(Cards(w).Count==2&&ReferenceEquals(Active(w),memory),"Failed SaveAs closed unsaved tab");
                }
                finally{ProjectCreation.ShowSaveDialog=(d,o)=>d.ShowDialog(o);}
                Open(w,paths[0]);Edit(w,"Zmieniona A");
                Open(w,paths[2]);PaletteChange(w,"Osobny draft");Edit(w,"Zmieniona C");
                w.ConfirmUnsavedForTest=()=>DialogResult.No;
                var seen=new List<string>();
                w.ConfirmPresetForTest=p=>{
                    seen.Add(p.Name);
                    var preview=Get<TerrainPreset>(w,"presetReview");
                    Check(Cards(w).Count==0&&!Get<Panel>(w,"canvas").Visible&&Get<TableLayoutPanel>(w,"leftSections").Visible&&
                        ReferenceEquals(Get<ComboBox>(w,"terrainPicker").SelectedItem,preview)&&preview.Name==p.Name,
                        "Preset review reopened a map or failed to show the draft in the normal panel");
                    return p.Id=="0111-shared"?DialogResult.Yes:DialogResult.Cancel;
                };
                bool partial=false;
                using(var dialogTimer=new Timer{Interval=25})
                {
                    dialogTimer.Tick+=(s,e)=>{
                        var dialog=Application.OpenForms.OfType<SavePresetDialog>().SingleOrDefault();
                        if(dialog==null)return;
                        dialogTimer.Stop();
                        Get<SwitchBox>(dialog,"terrains").Checked=false;
                        Get<SwitchBox>(dialog,"objects").Checked=false;
                        partial=dialog.Sections==PresetSections.Icons;
                        ((Button)dialog.AcceptButton).PerformClick();
                    };
                    dialogTimer.Start();
                    w.Close();
                }
                Check(!w.IsDisposed&&Cards(w).Count==0&&seen.SequenceEqual(new[]{"Wspólna paleta 0111","Druga paleta 0111"}),"Shared draft repeated or cancelled exit resurrected docs");
                var saved=PresetCatalog.ReadFile(PresetCatalog.FindFile(LibraryPaths.Presets,"0111-shared"));
                Check(partial&&saved.Icons.Count==icons.Icons.Length-1&&saved.Terrains[0].Name!="Wspólny draft","Detached partial save changed wrong section");
                Check(!Get<Timer>(w,"autoTimer").Enabled,"Empty editor retains ticking schedule");
                Open(w,paths[0]);
                Check(Cards(w).Count==1&&Get<Panel>(w,"canvas").Visible&&Get<Timer>(w,"autoTimer").Enabled&&Get<TerrainPreset>(w,"presetReview")==null,"Opening map after preset cancel");
                var draft=(TerrainPreset)Call(w,"ActivePreset");
                Check(draft.Terrains[0].Name=="Wspólny draft"&&(bool)Call(w,"WorkingPresetDirty",draft),"Omitted draft section lost after cancelled close");
            }
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                LibraryPaths.EnsureDirectories(Root);
                string name=args.Single();var paths=Fixtures();
                switch(name)
                {
                    case "tabs-fit":TabsFit(paths);break;
                    case "autosave":Autosave(paths);break;
                    case "close":Closing(paths);break;
                    case "fixture":break;
                    default:throw new ArgumentException("Unknown Case");
                }
                Console.WriteLine(name+" PASS");return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
        }
    }
}
