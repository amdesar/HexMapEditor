using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.IO;using System.Linq;using System.Reflection;using System.Windows.Forms;
namespace HexMapEditor.Tests
{
 internal static class Patch071Tests
 {
  const BindingFlags P=BindingFlags.Instance|BindingFlags.NonPublic;
  static object Get(object o,string n){return o.GetType().GetField(n,P).GetValue(o);}
  static void Set(object o,string n,object v){o.GetType().GetField(n,P).SetValue(o,v);}
  static object Call(object o,string n,params object[] a){return o.GetType().GetMethods(P).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);}
  static string Json(object o){return MapLogic.Serializer().Serialize(o);}
  static void Check(bool b,string s){if(!b)throw new Exception(s);}
  static IEnumerable<Control> All(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in All(child))yield return nested;}}
  static List<InkStroke> Drawing(){return Enumerable.Range(0,16).Select(i=>new InkStroke{Width=35,Color="#FFD04A",Points=Enumerable.Range(0,180).Select(n=>new InkPoint(new PointF(30+n*5,30+i*24+(float)Math.Sin(n*.2)*12))).ToList()}).ToList();}
  static void New(MainWindow w){Call(w,"StartNewProject",15,10,"PointyTop","Own071",PresetCatalog.Default());}
  static void BaselineLifecycle()
  {
   for(int run=0;run<3;run++)
   {
    var ctx=new StartupContext(null);var start=ctx.MainForm;var clock=Stopwatch.StartNew();var timer=new Timer{Interval=25};Exception failure=null;int phase=0;bool reached=false;int resize=run;
    start.FormClosed+=(s,e)=>Console.WriteLine("baseline startClosed editorMain="+(ctx.MainForm is MainWindow));ctx.ThreadExit+=(s,e)=>Console.WriteLine("baseline threadExit reached="+reached);
    timer.Tick+=(s,e)=>{try{if(clock.ElapsedMilliseconds>15000)throw new Exception("Lifecycle timeout");if(phase==0&&clock.ElapsedMilliseconds>100){if(resize==1)start.WindowState=FormWindowState.Maximized;if(resize==2)start.ClientSize=new Size(800,600);phase=1;start.BeginInvoke((Action)(()=>Call(start,"ChooseNew")));return;}
     var dialog=Application.OpenForms.Cast<Form>().OfType<NewMapDialog>().FirstOrDefault();if(phase==1&&dialog!=null){phase=2;All(dialog).OfType<Button>().Single(b=>b.Text=="Utwórz").PerformClick();return;}
     var editor=Application.OpenForms.Cast<Form>().OfType<MainWindow>().FirstOrDefault();if(phase==2&&editor!=null&&editor.Visible){reached=true;Console.WriteLine("baseline run="+run+" resizeMode="+resize+" editorVisible="+editor.Visible+" owner="+(editor.Owner==null?"none":editor.Owner.GetType().Name));phase=3;timer.Stop();editor.Close();}
    }catch(Exception ex){failure=ex;timer.Stop();ctx.ExitThread();}};
    timer.Start();Application.Run(ctx);timer.Dispose();foreach(var f in Application.OpenForms.Cast<Form>().ToArray())f.Dispose();ctx.Dispose();if(failure!=null)throw failure;Check(reached,"Unexpected loop exit before editor");
   }
   Console.WriteLine("baseline lifecycle3 PASS; sporadic user crash not reproduced; no diagnosis from resize");
  }
  static void BaselineInk()
  {
   for(int run=0;run<2;run++)
   {
    var w=new MainWindow();Exception failure=null;var timer=new Timer{Interval=15};var clock=Stopwatch.StartNew();int enabled=0,layouts=0,paints=0,ticks=0,phase=0;double maxGap=0,last=0;
    w.Shown+=(s,e)=>w.BeginInvoke((Action)(()=>{try{New(w);var map=(MapData)Get(w,"map");map.Ink=Drawing();Call(w,"CaptureSavedState");var canvas=(Control)Get(w,"canvas");foreach(string field in new[]{"canvas","shellToolbar","left","right"}){var c=(Control)Get(w,field);c.EnabledChanged+=(a,b)=>enabled++;c.Layout+=(a,b)=>layouts++;}canvas.Paint+=(a,b)=>paints++;var gesture=new InkEraseGesture(42);if(run==0)gesture.Add(new PointF(2000,2000),new PointF(2000,2000));else{var a=new PointF(60,35);for(int n=0;n<100;n++){var b=new PointF(60+n*7,35+n*3.7f+(float)Math.Sin(n*.12)*30);gesture.Add(a,b);a=b;}}
      Set(w,"inkBefore",new List<InkStroke>(map.Ink));Set(w,"eraseGesture",gesture);clock.Restart();Call(w,"FinishInk",true);Console.WriteLine("baseline ink run="+run+" enqueue="+clock.Elapsed.TotalMilliseconds.ToString("F2")+" pending="+(Get(w,"pendingInk")!=null));last=clock.Elapsed.TotalMilliseconds;phase=1;timer.Start();}catch(Exception ex){failure=ex;w.Dispose();}}));
    timer.Tick+=(s,e)=>{try{double now=clock.Elapsed.TotalMilliseconds;maxGap=Math.Max(maxGap,now-last);last=now;ticks++;if(now>15000)throw new Exception("Ink timeout");if(phase==1&&Get(w,"pendingInk")==null){Console.WriteLine("baseline ink run="+run+" EnabledChanges="+enabled+" layouts="+layouts+" paints="+paints+" ticks="+ticks+" maxGap="+maxGap.ToString("F2")+" "+w.LastInkCommitTiming);timer.Stop();Call(w,"CaptureSavedState");w.Close();}}catch(Exception ex){failure=ex;timer.Stop();w.Dispose();}};
    Application.Run(w);timer.Dispose();if(failure!=null)throw failure;
   }
  }
  static void ZoomMeasure(string label)
  {
   using(var w=new MainWindow())using(var image=new Bitmap(1000,640))using(var g=Graphics.FromImage(image))
   {
    w.Show();New(w);var map=(MapData)Get(w,"map");map.Ink=Drawing();foreach(bool gradient in new[]{false,true}){foreach(var stroke in map.Ink){stroke.Gradient=gradient?GradientRules.Rainbow():null;stroke.CycleWorld=gradient?600:0;double d=0;for(int i=0;i<stroke.Points.Count;i++){if(i>0){var a=stroke.Points[i-1];var b=stroke.Points[i];d+=Math.Sqrt((b.X-a.X)*(double)(b.X-a.X)+(b.Y-a.Y)*(double)(b.Y-a.Y));}stroke.Points[i].Distance=gradient?d:0;}}
      InkLayerRenderer.Clear();Action<float,bool> draw=(scale,interactive)=>{g.ResetTransform();g.Clear(Color.Teal);g.TranslateTransform(30,30);g.ScaleTransform(scale,scale);InkLayerRenderer.Draw(g,map.Ink,null,new Rectangle(0,0,1000,640),interactive,false);};draw(1,false);var watch=Stopwatch.StartNew();for(int i=0;i<12;i++){draw(i%2==0?.89f:1f,true);for(int n=0;n<4;n++)draw(i%2==0?.89f:1f,true);}Console.WriteLine(label+" alternating gradient="+gradient+"60previewPaints="+watch.Elapsed.TotalMilliseconds.ToString("F2")+"ms");draw(.89f,false);watch.Restart();for(int n=0;n<10;n++)draw(.89f,false);Console.WriteLine(label+" exact warm10="+watch.Elapsed.TotalMilliseconds.ToString("F2")+"ms");}
    Call(w,"CaptureSavedState");
   }
  }
  static void Lifecycle(string root)
  {
   for(int run=0;run<4;run++)
   {
    int variant=run,phase=0;bool reached=false,rolledBack=false;Exception failure=null;var clock=Stopwatch.StartNew();var timer=new Timer{Interval=20};var ctx=new StartupContext(null);var start=ctx.MainForm;
    ProjectCreation.ShowSaveDialog=(dialog,owner)=>{dialog.FileName=Path.Combine(root,"New071-"+variant+".hexmap");return DialogResult.OK;};
    ctx.Failure=ex=>{Console.WriteLine("controlled failure variant="+variant+" type="+ex.GetType().Name);rolledBack=true;};
    if(run==1)ctx.EditorFactory=(data,path)=>{var e=new MainWindow(data,path);e.Load+=(a,b)=>{throw new InvalidOperationException("synthetic Load");};return e;};
    if(run==2)ctx.EditorFactory=(data,path)=>{var e=new MainWindow(data,path);e.Load+=(a,b)=>e.Close();return e;};
    timer.Tick+=(s,e)=>{try{if(clock.ElapsedMilliseconds>15000)throw new Exception("Lifecycle timeout");if(phase==0&&clock.ElapsedMilliseconds>120){Check(start.FormBorderStyle==FormBorderStyle.FixedSingle&&!start.MaximizeBox,"Start is resizable");phase=1;start.BeginInvoke((Action)(()=>Call(start,"ChooseNew")));return;}var d=Application.OpenForms.Cast<Form>().OfType<NewMapDialog>().FirstOrDefault();if(phase==1&&d!=null){phase=2;All(d).OfType<Button>().Single(b=>b.Text=="Utwórz").PerformClick();return;}
     if(phase==2&&rolledBack){Check(ReferenceEquals(ctx.MainForm,start)&&!start.IsDisposed&&start.Visible,"Rollback lost start/loop");timer.Stop();start.Close();return;}
     var editor=ctx.MainForm as MainWindow;if(phase==2&&editor!=null&&editor.Visible){reached=true;Check(start.IsDisposed,"Start not closed after ready");Check(File.Exists((string)Get(editor,"currentPath"))&&!(bool)Get(editor,"dirty"),"New project has no file / dirty");Check(StartupTrace.Entries.Any(line=>line.Contains("editor.ready")),"No ready diagnostic");timer.Stop();editor.Close();}
    }catch(Exception ex){failure=ex;timer.Stop();ctx.ExitThread();}};
    timer.Start();Application.Run(ctx);timer.Dispose();foreach(var f in Application.OpenForms.Cast<Form>().ToArray())f.Dispose();ctx.Dispose();if(failure!=null)throw failure;Check(variant==1||variant==2?rolledBack&&!reached:reached&&!rolledBack,"Unexpected lifecycle outcome");Console.WriteLine("lifecycle variant="+variant+" ready="+reached+" rollback="+rolledBack);
   }
   Console.WriteLine("lifecycle PASS: real New dialog, shared initial file,ready-before-close, injected Load/early Close rollback, bounded four runs");
  }
  static void NewFile(string root)
  {
   using(var w=new MainWindow()){w.Show();New(w);var old=(MapData)Get(w,"map");string prior=Json(old);var data=MainWindow.CreateProjectData(8,6,"PointyTop","Nowy ą",PresetCatalog.Default());
    ProjectCreation.ShowSaveDialog=(d,o)=>DialogResult.Cancel;Check(ProjectCreation.ChooseFile(w,data)==null&&Json(old)==prior,"Cancel changed map");
    var created=ProjectCreation.Save(data,Path.Combine(root,"Nowy ą"),p=>false);Check(created.Path.EndsWith(".hexmap")&&MainWindow.ReadProjectData(created.Path).Cells.Count==48,"Initial shared writer/reader");
    string bytes=File.ReadAllText(created.Path);Check(ProjectCreation.Save(data,created.Path,p=>false)==null&&File.ReadAllText(created.Path)==bytes,"Overwrite without consent");
    bool bad=false;try{ProjectCreation.Save(data,Path.Combine(root,"missing","bad.hexmap"),p=>true);}catch(IOException){bad=true;}Check(bad&&Json(old)==prior,"Failed write replaced previous map");
    Call(w,"AdoptNewProject",created.Data,created.Path);var args=new object[]{created.Path,true,null};Check((bool)Call(w,"TrySaveTo",args)&&!(bool)Get(w,"dirty"),"CtrlS path save");Call(w,"CaptureSavedState");
   }
   Console.WriteLine("new-file PASS: cancel, atomic file+path/read, overwrite rejection, failed save preserves old map, subsequent save; map11 unchanged");
  }
  static void Ink(string root)
  {
   var w=new MainWindow();Exception failure=null;var timer=new Timer{Interval=15};int phase=0,enabled=0,paints=0,ticks=0,baseUpdates=0,history=0;double maxGap=0,last=0;var watch=Stopwatch.StartNew();string before=null,after=null;List<InkStroke> original=null;MapData expected=null;bool handoff=false;
   Action begin=()=>{New(w);expected=(MapData)Get(w,"map");original=Drawing();if(phase==1){var broad=new InkStroke{Width=180,Points=new List<InkPoint>{new InkPoint(new PointF(80,200)),new InkPoint(new PointF(420,200))}};original=InkGeometry.Erase(new List<InkStroke>{broad},new PointF(220,200),new PointF(220,200),100);}expected.Ink=original;Call(w,"CaptureSavedState");before=Json(original);history=((System.Collections.ICollection)Get(w,"undo")).Count;baseUpdates=w.UiRefreshCount;enabled=paints=ticks=0;last=0;maxGap=0;handoff=false;
    var gesture=new InkEraseGesture(phase==1?30:42,original);var a=phase==0?new PointF(2000,2000):phase==1?new PointF(220,200):new PointF(60,35);if(phase<=2)gesture.Add(a,a);else for(int n=0;n<100;n++){var b=new PointF(60+n*7,35+n*3.7f+(float)Math.Sin(n*.12)*30);gesture.Add(a,b);a=b;}
    Set(w,"inkBefore",original);Set(w,"eraseGesture",gesture);w.RunInkWork=(work,token)=>{if(phase==5)throw new IOException("synthetic worker error");return work.Execute(token);};watch.Restart();Call(w,"FinishInk",true);Console.WriteLine("ink phase="+phase+" enqueue="+watch.Elapsed.TotalMilliseconds.ToString("F2")+" pending="+(Get(w,"pendingInk")!=null));last=watch.Elapsed.TotalMilliseconds;
    if(phase<=1){Check(Get(w,"pendingInk")==null,"No-op started job");Check(Json(expected.Ink)==before&&w.UiRefreshCount==baseUpdates&&!(bool)Get(w,"dirty"),"No-op modified model/UI");}
    else{Check(Get(w,"pendingInk")!=null,"No pending hit");var save=new object[]{Path.Combine(root,"must-not-save.hexmap"),false,null};Check(!(bool)Call(w,"TrySaveTo",save),"Save not blocked");if(phase==4)Call(w,"CancelInkCommit");if(phase==6){Set(w,"map",MainWindow.CreateProjectData(15,10,"PointyTop","Other",PresetCatalog.Default()));Set(w,"documentRevision",(long)Get(w,"documentRevision")+1);}}
   };
   w.ObserveInkHandoff=()=>{if(phase==2||phase==3){Check(Get(w,"pendingInk")!=null&&Get(w,"inkHandoff")!=null,"Preview released before commit");Check(((MapData)Get(w,"map")).Ink.Any(stroke=>stroke.Surface!=null),"Preview release before data");handoff=true;}};
   w.Shown+=(s,e)=>w.BeginInvoke((Action)(()=>{try{foreach(string field in new[]{"canvas","shellToolbar","left","right"})((Control)Get(w,field)).EnabledChanged+=(a,b)=>enabled++;((Control)Get(w,"canvas")).Paint+=(a,b)=>paints++;begin();timer.Start();}catch(Exception ex){failure=ex;w.Dispose();}}));
   timer.Tick+=(s,e)=>{try{double now=watch.Elapsed.TotalMilliseconds;maxGap=Math.Max(maxGap,now-last);last=now;ticks++;if(now>20000)throw new Exception("Ink timeout");if(Get(w,"pendingInk")!=null)return;
    var map=(MapData)Get(w,"map");Check(enabled==0,"Disabled entire UI branch");Check(w.UiRefreshCount==baseUpdates,"Broad UpdateUI on commit/no-op");
    if(phase==2||phase==3){Check(handoff,"Handoff not observed");after=Json(map.Ink);Check(before!=after&&((System.Collections.ICollection)Get(w,"undo")).Count==history+1,"Atomic history");Check(map.Ink.Any(stroke=>stroke.Surface!=null),"No exact surface");var undo=new object[]{false,null};Check((bool)Call(w,"TryHistory",undo)&&Json(map.Ink)==before,"Undo mismatch");var redo=new object[]{true,null};Check((bool)Call(w,"TryHistory",redo)&&Json(map.Ink)==after,"Redo mismatch");if(phase==3){var save=new object[]{Path.Combine(root,"Ink071.hexmap"),false,null};Check((bool)Call(w,"TrySaveTo",save),"Roundtrip save");Check(Json(MainWindow.ReadProjectData((string)save[0]).Ink)==after,"Roundtrip surface");}}
    else if(phase<=1||phase==4||phase==5)Check(Json(map.Ink)==before&&((System.Collections.ICollection)Get(w,"undo")).Count==history,"Noop/cancel/error mutation");else Check(!ReferenceEquals(map,expected)&&map.Name=="Other"&&map.Ink.Count==0,"Stale result applied");
    Console.WriteLine("ink phase="+phase+" enabled="+enabled+" paints="+paints+" ticks="+ticks+" maxGap="+maxGap.ToString("F2")+" paintLast="+((double)Get(w,"LastInkPaintMilliseconds")).ToString("F2")+"ms timing="+w.LastInkCommitTiming);phase++;if(phase<7){begin();return;}timer.Stop();Call(w,"CaptureSavedState");w.Close();
   }catch(Exception ex){failure=ex;timer.Stop();w.Dispose();}};
   Application.Run(w);timer.Dispose();if(failure!=null)throw failure;Console.WriteLine("ink PASS: exterior/hole no-op, small/long hit,0branchEnabled/0broadUI, structural preview-to-result, cancel/error/stale,oneUndoRedo/map11; no frame-by-frame visual claim");
  }
  static void Zoom()
  {
   InkLayerRenderer.ExactBuilds=InkLayerRenderer.PreviewBuilds=InkLayerRenderer.PreviewHits=InkLayerRenderer.PanUpdates=0;ZoomMeasure("current");Console.WriteLine("exact="+InkLayerRenderer.ExactBuilds+" previewBuilds="+InkLayerRenderer.PreviewBuilds+" previewHits="+InkLayerRenderer.PreviewHits);Check(InkLayerRenderer.PreviewHits>20,"No transient frame reuse");
   var w=new MainWindow();var timer=new Timer{Interval=25};var clock=Stopwatch.StartNew();Exception failure=null;long builds=0;int phase=0;
   w.Shown+=(s,e)=>w.BeginInvoke((Action)(()=>{try{New(w);var map=(MapData)Get(w,"map");map.Ink=Drawing();Set(w,"scale",1.137f);Set(w,"pan",new PointF(30.2f,32.7f));int updates=w.UiRefreshCount;for(int n=0;n<12;n++){Call(w,"CanvasMouseWheel",Get(w,"canvas"),new MouseEventArgs(MouseButtons.None,0,500,300,n%2==0?120:-120));((Control)Get(w,"canvas")).Refresh();}Check((float)Get(w,"scale")==1.137f&&(PointF)Get(w,"pan")==new PointF(30.2f,32.7f),"Camera drift on reverse wheel");Check(w.UiRefreshCount==updates,"Full UI layout on wheel");clock.Restart();timer.Start();}catch(Exception ex){failure=ex;w.Dispose();}}));
   timer.Tick+=(s,e)=>{try{if(phase==0&&clock.ElapsedMilliseconds>300){Check(!(bool)Get(w,"cameraInteraction")&&!((Timer)Get(w,"cameraIdle")).Enabled&&w.cameraFinalRequests==1,"Stuck camera interaction/timer/final requests");builds=InkLayerRenderer.ExactBuilds;phase=1;}else if(phase==1&&clock.ElapsedMilliseconds>500){Check(InkLayerRenderer.ExactBuilds==builds,"Repeated exact renders while idle");timer.Stop();Call(w,"CaptureSavedState");w.Close();}}catch(Exception ex){failure=ex;timer.Stop();w.Dispose();}};
   Application.Run(w);timer.Dispose();if(failure!=null)throw failure;Console.WriteLine("zoom PASS:12reverse wheels,no float drift/0fullUI,onefinalrequest,timer stopped,idle no exact builds; low unchanged");
  }
  static void InkWait()
  {
   var w=new MainWindow();var timer=new Timer{Interval=20};var clock=Stopwatch.StartNew();Exception failure=null;bool visible=false,checkedFast=false;int baseShows=0;Rectangle bounds=Rectangle.Empty;
   w.Shown+=(s,e)=>w.BeginInvoke((Action)(()=>{try{New(w);var map=(MapData)Get(w,"map");map.Ink=Drawing();Call(w,"CaptureSavedState");var canvas=(Control)Get(w,"canvas");bounds=canvas.Bounds;var gesture=new InkEraseGesture(42,map.Ink);gesture.Add(new PointF(60,35),new PointF(600,300));Set(w,"inkBefore",map.Ink);Set(w,"eraseGesture",gesture);w.RunInkWork=(work,token)=>{token.WaitHandle.WaitOne();token.ThrowIfCancellationRequested();return work.Execute(token);};baseShows=w.inkWaitShows;Call(w,"FinishInk",true);clock.Restart();timer.Start();}catch(Exception ex){failure=ex;w.Dispose();}}));
   timer.Tick+=(s,e)=>{try{if(clock.ElapsedMilliseconds>5000)throw new Exception("Wait UI timeout");if(!checkedFast&&clock.ElapsedMilliseconds<150){Check(w.inkWaitShows==baseShows,"Wait UI flashed immediately");checkedFast=true;}if(!visible&&w.inkWaitShows>baseShows){visible=true;Check(((Control)Get(w,"canvas")).Bounds==bounds&&((Control)Get(w,"canvas")).Enabled&&((Control)Get(w,"left")).Enabled,"Layout/Enabled changed during wait");var undo=(Control)Get(w,"undoButton");var message=Message.Create(undo.Handle,0x201,IntPtr.Zero,IntPtr.Zero);Check(w.PreFilterMessage(ref message),"Command not filtered");Call(w,"CancelInkCommit");}if(visible&&Get(w,"pendingInk")==null){timer.Stop();Check(!(bool)Get(w,"dirty"),"Cancel dirtied map");Call(w,"CaptureSavedState");w.Close();}}catch(Exception ex){failure=ex;timer.Stop();w.Dispose();}};
   Application.Run(w);timer.Dispose();if(failure!=null)throw failure;Check(visible&&checkedFast,"Wait phases not exercised");Console.WriteLine("ink-wait PASS: canceled180ms deferred visibility,no branch disable/layout,logical command filter,cancel original; synchronization test not performance metric");
  }
  static void Argument(string root)
  {
   var data=MainWindow.CreateProjectData(9,7,"PointyTop","Argument071",PresetCatalog.Default());string path=Path.Combine(root,"Argument071.hexmap");MainWindow.WriteProjectData(data,path,false);var ctx=new StartupContext(path);var timer=new Timer{Interval=20};Exception failure=null;var clock=Stopwatch.StartNew();bool ready=false;
   Check(ctx.MainForm==null||ctx.MainForm is MainWindow,"Argument schedules automatic start Show");
   timer.Tick+=(s,e)=>{try{if(clock.ElapsedMilliseconds>10000)throw new Exception("Argument timeout");var editor=ctx.MainForm as MainWindow;if(editor==null)return;Check(editor.Visible&&(string)Get(editor,"currentPath")==path,"Wrong argument editor");Check(!StartupTrace.Entries.Any(line=>line.Contains("start.visible")),"Argument displayed start");ready=true;timer.Stop();editor.Close();}catch(Exception ex){failure=ex;timer.Stop();ctx.ExitThread();}};
   timer.Start();Application.Run(ctx);timer.Dispose();ctx.Dispose();if(failure!=null)throw failure;Check(ready,"No argument editor");Console.WriteLine("argument PASS: actual ApplicationContext loop,valid map, no temporary start Visible; no installer/association campaign");
  }
  static void Fixture(string root)
  {
   var data=MainWindow.CreateProjectData(15,10,"PointyTop","Próba gumki 0.7.1",PresetCatalog.Default());data.Ink=Drawing();
   var project=ProjectCreation.Save(data,Path.Combine(root,"Próba gumki 0.7.1.hexmap"),path=>false);Check(project!=null,"Fixture save");Console.WriteLine("fixture ONLY "+project.Path);
  }
  [STAThread]static int Main(string[] args){try{if(args.Length!=1)throw new Exception("One Case required");Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);switch(args[0]){case "baseline":BaselineLifecycle();BaselineInk();ZoomMeasure("baseline");break;case "lifecycle":Lifecycle(AppDomain.CurrentDomain.BaseDirectory);break;case "new-file":NewFile(AppDomain.CurrentDomain.BaseDirectory);break;case "ink":Ink(AppDomain.CurrentDomain.BaseDirectory);break;case "ink-wait":InkWait();break;case "zoom":Zoom();break;case "argument":Argument(AppDomain.CurrentDomain.BaseDirectory);break;case "fixture":Fixture(AppDomain.CurrentDomain.BaseDirectory);break;default:throw new Exception("Not implemented: "+args[0]);}return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
 }
}
