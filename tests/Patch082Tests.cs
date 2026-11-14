using System;using System.Drawing;using System.IO;using System.Reflection;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace HexMapEditor.Tests
{
 internal static class Patch082Tests
 {
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static object Get(object o,string name){return o.GetType().GetField(name,Private).GetValue(o);}
  static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,Private).Invoke(o,args);}
  static int Hit(Control w,Point client){var p=w.PointToScreen(client);return (int)SendMessage(w.Handle,0x84,IntPtr.Zero,(IntPtr)((p.Y<<16)|(p.X&65535)));}
  static void Cycle(MainWindow w,Rectangle before,int cycles)
  {
   var client=w.ClientSize;
   Console.WriteLine("BEFORE outer="+before+" client="+client);
   for(int i=0;i<cycles;i++){
    SendMessage(w.Handle,0x112,(IntPtr)0xf030,IntPtr.Zero);Console.WriteLine("MAX outer="+w.Bounds+" client="+w.ClientSize+" restore="+w.RestoreBounds);
    Check(w.WindowState==FormWindowState.Maximized,"SC_MAXIMIZE ignored");
    SendMessage(w.Handle,0x112,(IntPtr)0xf120,IntPtr.Zero);Console.WriteLine("RESTORE "+i+" outer="+w.Bounds+" client="+w.ClientSize);
    Check(w.Bounds==before&&w.ClientSize==client,"Native restore changed geometry");
   }
  }
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
  [STAThread]static int Main(string[] args)
  {
   try{
    if(args.Length!=1||(args[0]!="geometry"&&args[0]!="caption"))throw new Exception("Wymagany jeden Case: geometry / caption");
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    var data=MainWindow.CreateProjectData(15,10,"PointyTop","Próba ramki 0.8.2",PresetCatalog.Default());
    MainWindow.WriteProjectData(data,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Próba ramki.hexmap"),false);
    using(var w=new MainWindow(data,null)){
     w.StartPosition=FormStartPosition.Manual;w.Location=new Point(100,100);w.CaptionTrace=Console.WriteLine;w.Show();
     if(args[0]=="geometry"){
      foreach(var size in new[]{new Size(1440,940),new Size(1220,780)}){w.Bounds=new Rectangle(90,70,size.Width,size.Height);Cycle(w,w.Bounds,3);}
      var before=w.Bounds;SendMessage(w.Handle,0x112,(IntPtr)0xf020,IntPtr.Zero);Check(w.WindowState==FormWindowState.Minimized,"Minimize");SendMessage(w.Handle,0x112,(IntPtr)0xf120,IntPtr.Zero);Check(w.Bounds==before,"Minimize restore");
      // The same WM_NCLBUTTONDBLCLK path as caption double-click; no property shortcut.
      var p=w.PointToScreen(new Point((int)Get(w,"captionMenuEnd")+20,16));var lp=(IntPtr)((p.Y<<16)|(p.X&65535));
      SendMessage(w.Handle,0xa3,(IntPtr)2,lp);Check(w.WindowState==FormWindowState.Maximized,"Caption double click maximize");SendMessage(w.Handle,0xa3,(IntPtr)2,lp);Check(w.Bounds==before,"Caption double click restore");
      w.Scale(new SizeF(1.25f,1.25f));Cycle(w,w.Bounds,2);Console.WriteLine("125% control-scaling simulation only; not a physical DPI monitor test");
     }else{
      var menu=(MenuStrip)Get(w,"mainMenu");var quick=(ToolStrip)Get(w,"quickStrip");Check(menu.Items.Count==6,"Menu count");
      foreach(var size in new[]{w.MinimumSize,new Size(1440,940)}){
       w.Size=size;w.Text="HexMapEditor — "+new string('W',150)+" *";var name=(Rectangle)Get(w,"captionNameBounds");int menuEnd=(int)Get(w,"captionMenuEnd"),buttonLeft=(int)Get(w,"captionButtonsLeft");
       Check(name.IsEmpty||(name.Left>menuEnd&&name.Right<buttonLeft),"Caption overlap");Check(quick.Top>=menu.Bottom&&quick.Left==0,"Two rows / left quick bar");
       Check(Hit(w,new Point(menuEnd+10,16))==2,"Free area not HTCAPTION");
       Check(Hit(menu,new Point(20,menu.Height/2))==1,"Menu intercepted by caption");
       Check(Hit(w,new Point(10,-1))==12,"Top resize");Check(Hit(w,new Point(-2,w.Height/2))==10,"Left resize");
       Check(Hit(w,new Point(w.ClientSize.Width+2,w.ClientSize.Height+2))==17,"Bottom right resize");
       Console.WriteLine("Native max hit normal="+Hit(w,new Point(w.ClientSize.Width-70,14)));
       SendMessage(w.Handle,0x112,(IntPtr)0xf030,IntPtr.Zero);
       Console.WriteLine("Native max hit maximized="+Hit(w,new Point(w.ClientSize.Width-70,14)));
       Check(Hit(w,new Point(w.ClientSize.Width-70,14))==9,"Native max button hit test");
       SendMessage(w.Handle,0x112,(IntPtr)0xf120,IntPtr.Zero);
       Console.WriteLine("LAYOUT "+size+" menu="+menu.Bounds+" name="+name+" native buttons start="+buttonLeft+" quick="+quick.Bounds);
      }
      w.Text="HexMapEditor — Próba";var centered=(Rectangle)Get(w,"captionNameBounds");Check(Math.Abs(centered.Left+centered.Width/2-w.ClientSize.Width/2)<=1,"Whole-window center");
      var file=(ToolStripMenuItem)menu.Items[0];file.ShowDropDown();Check(file.DropDown.Visible,"Real menu");file.HideDropDown();Check(!file.DropDown.Visible,"Menu close");
      var visible=(ToolStripMenuItem)menu.Items[3];visible.ShowDropDown();visible.HideDropDown();
      Call(w,"AdoptNewProject",data,null);int asked=0;w.ConfirmUnsavedForTest=()=>{asked++;return DialogResult.Cancel;};
      SendMessage(w.Handle,0x112,(IntPtr)0xf060,IntPtr.Zero);Check(!w.IsDisposed&&asked==1,"System close bypasses unsaved guard");
      Check(w.Text.EndsWith(" *"),"System title lost dirty");
      Console.WriteLine("caption PASS: layout, native hit tests, menus, system close guard; no drag or visual claim");
     }
    }
    Console.WriteLine(args[0]+" PASS");return 0;
   }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
  }
 }
}
