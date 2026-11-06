using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Wpf = System.Windows;
using Input = System.Windows.Input;

namespace HexMapEditor.Tests
{
    internal static class ConvenienceTests
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
        static string root; static int passed;
        static object Get(MainWindow w, string n) { return typeof(MainWindow).GetField(n,F).GetValue(w); }
        static object Call(MainWindow w,string n,params object[] args) { return typeof(MainWindow).GetMethods(F).Single(m=>m.Name==n && m.GetParameters().Length==args.Length).Invoke(w,args); }
        static void Check(bool value,string message) { if(!value)throw new Exception(message); }
        static void Test(string name,Action test) { test();passed++;Console.WriteLine("PASS "+name); }
        static TerrainPreset Preset(string name) { var p=new TerrainPreset {Name=name};p.Terrains.Add(PresetCatalog.Neutral());return p; }
        static string Dir(string name) { string p=Path.Combine(root,name);Directory.CreateDirectory(p);return p; }
        static int Count(MainWindow w,string n) { return ((ICollection)Get(w,n)).Count; }
        [STAThread] static int Main()
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");Directory.CreateDirectory(root);
            try
            {
                Test("readable Unicode filenames, safe Windows normalization and bounded paths",()=>{
                    string d=Dir("names");var p=Preset("Kosmos żółty 1");PresetCatalog.Write(p,d,false);Check(Path.GetFileName(PresetCatalog.FindFile(d,p.Id))=="Kosmos żółty 1.terrainset.json","Unreadable filename");
                    foreach(string name in new[]{"../CON", "AUX.txt", "NUL", "COM1", "LPT²", "...", "<>:\"/\\|?*", "kropka. ",new string('ą',220)}){
                        p=Preset(name);PresetCatalog.Write(p,d,false);string file=PresetCatalog.FindFile(d,p.Id),leaf=Path.GetFileName(file);
                        Check(Path.GetDirectoryName(file)==d && file.Length<=210 && leaf.EndsWith(PresetFiles.Extension) && !leaf.Contains("..\\"),"Unsafe/long filename");
                    }
                    Check(PresetFiles.Stem("CON.txt")=="_CON.txt" && PresetFiles.Stem("... ")=="Preset" && PresetFiles.Stem("kropka. ")=="kropka","Device/trailing/empty normalization");
                });
                Test("normalized collisions and case-insensitive occupancy use suffix without overwrite",()=>{
                    string d=Dir("collision");var a=Preset("Kosmos?");var b=Preset("kosmos*");PresetCatalog.Write(a,d,false);string old=PresetCatalog.FindFile(d,a.Id),bytes=File.ReadAllText(old);PresetCatalog.Write(b,d,false);
                    Check(Path.GetFileName(PresetCatalog.FindFile(d,b.Id))=="kosmos_ (2).terrainset.json" && File.ReadAllText(old)==bytes,"Collision overwrote first file");
                    PresetCatalog.Write(b,d,true);Check(Directory.GetFiles(d,"*.terrainset.json").Length==2,"Save duplicated file");var c=Preset("KOSMOS?");bool rejected=false;try{PresetCatalog.Write(c,d,false);}catch(InvalidDataException){rejected=true;}Check(rejected,"Duplicate logical name accepted");
                });
                Test("legacy filename read, explicit rename/save preserves ID and single source",()=>{
                    string d=Dir("rename");var p=Preset("Stary");string legacy=Path.Combine(d,MapLogic.Hash(p.Id)+PresetFiles.Extension);File.WriteAllText(legacy,MapLogic.Serializer().Serialize(p));string warning;
                    var read=PresetCatalog.Load(d,out warning);Check(read.Any(x=>x.Id==p.Id) && File.Exists(legacy),"Legacy read renamed file");
                    p.Name="Nowa nazwa";PresetCatalog.Write(p,d,true);string next=PresetCatalog.FindFile(d,p.Id);Check(!File.Exists(legacy) && Path.GetFileName(next)=="Nowa nazwa.terrainset.json" && PresetCatalog.ReadFile(next).Id==p.Id && Directory.GetFiles(d,"*.terrainset.json").Length==1,"Rename duplicated/changed ID");
                    p.Name="NOWA NAZWA";PresetCatalog.Write(p,d,true);Check(Path.GetFileName(PresetCatalog.FindFile(d,p.Id))=="NOWA NAZWA.terrainset.json","Case-only rename failed");
                });
                Test("failed rename/replacement keeps original bytes and does not leave active duplicates",()=>{
                    string d=Dir("failure");var p=Preset("Original");PresetCatalog.Write(p,d,false);string old=PresetCatalog.FindFile(d,p.Id),destination=Path.Combine(d,"Changed"+PresetFiles.Extension);byte[] original=File.ReadAllBytes(old);bool failed=false;
                    try{PresetFiles.Write(destination,old,"changed",(a,b)=>{throw new IOException("synthetic replace failure");});}catch(IOException){failed=true;}
                    Check(failed && File.ReadAllBytes(old).SequenceEqual(original) && !File.Exists(destination) && Directory.GetFiles(d).Length==1,"Rollback lost data or active duplicate");
                    File.WriteAllText(destination,"KEEP");failed=false;try{PresetFiles.Write(destination,old,"new");}catch(IOException){failed=true;}Check(failed && File.ReadAllText(destination)=="KEEP" && File.ReadAllBytes(old).SequenceEqual(original),"Occupied destination changed");
                });
                Test("refresh discovers portable file and preserves project, selection, dirty and history",()=>{
                    using(var w=new MainWindow()){
                        string d=Dir("refresh");object[] create={d,"Active",null};Check((bool)Call(w,"TryCreatePreset",create),"Create failed");Call(w,"AddTerrain",new TerrainDefinition {Name="Working"});Call(w,"Undo");Call(w,"Redo");
                        var map=(MapData)Get(w,"map");string before=MapLogic.Serializer().Serialize(map);int undo=Count(w,"undo"),redo=Count(w,"redo");bool dirty=(bool)Get(w,"dirty");var rows=((FlowLayoutPanel)Get(w,"paletteTiles")).Controls.Cast<Control>().ToArray();
                        var portable=Preset("Portable");PresetCatalog.Write(portable,d,false);object[] args={d,null};Check((bool)Call(w,"TryRefreshPresets",args),"Refresh failed");Call(w,"TryRefreshPresets",args);
                        Check(MapLogic.Serializer().Serialize(map)==before && Count(w,"undo")==undo && Count(w,"redo")==redo && (bool)Get(w,"dirty")==dirty && rows.SequenceEqual(((FlowLayoutPanel)Get(w,"paletteTiles")).Controls.Cast<Control>()),"Refresh changed project/history/palette");
                        var picker=(ComboBox)Get(w,"terrainPicker");Check(picker.Items.Cast<TerrainPreset>().Count(p=>p.Id==portable.Id)==1 && ((TerrainPreset)picker.SelectedItem).Id==map.ActivePresetId,"Repeated refresh duplicated/selected imported file");
                        string file=PresetCatalog.FindFile(d,map.ActivePresetId);var external=PresetCatalog.ReadFile(file);external.Terrains.Add(new TerrainDefinition {Name="External"});PresetCatalog.Write(external,d,true);Call(w,"TryRefreshPresets",args);Check(((string)args[1]).Contains("zmienił") && MapLogic.Serializer().Serialize(map)==before,"External change replaced working palette");
                        File.Delete(file);Call(w,"TryRefreshPresets",args);Check(((string)args[1]).Contains("zniknął") && MapLogic.Serializer().Serialize(map)==before && ((Label)Get(w,"presetStateLabel")).Text.Contains("niezapisany"),"Missing file discarded working data");
                        File.Copy(PresetCatalog.FindFile(d,portable.Id),Path.Combine(d,"duplicate.terrainset.json"));File.WriteAllText(Path.Combine(d,"bad.terrainset.json"),"bad");Call(w,"TryRefreshPresets",args);Check(((string)args[1]).Contains("identyfikator") && ((string)args[1]).Contains("bad.terrainset.json") && MapLogic.Serializer().Serialize(map)==before,"Errors hidden or project changed");
                    }
                });
                Test("compact palette fits scrolling width; 2x2 preset buttons and stable rows",()=>{
                    using(var w=new MainWindow()){
                        w.Size=w.MinimumSize;w.Show();Application.DoEvents();var panel=(FlowLayoutPanel)Get(w,"paletteTiles");var left=(Panel)Get(w,"left");var actions=(FlowLayoutPanel)Get(w,"presetActions");
                        Check(actions.Controls[0].Top==actions.Controls[1].Top && actions.Controls[2].Top==actions.Controls[3].Top && actions.Controls[2].Top>actions.Controls[0].Top,"Actions not 2x2");
                        for(int i=0;i<12;i++)Call(w,"AddTerrain",new TerrainDefinition {Name=new string('Ż',100)});Application.DoEvents();
                        Check(!left.HorizontalScroll.Visible && left.VerticalScroll.Visible && panel.Controls.Cast<TerrainTile>().All(t=>t.Height==32 && t.Delete.Right<=t.Width && t.Width==panel.Width),"Compact scrolling clipped rows");
                        var body=(Control)Get(w,"paletteBody");Check(body.Right<=left.ClientSize.Width && ((Control)Get(w,"presetPickerRow")).Width==panel.Width,"Content ignores scrollbar");
                        Call(w,"CaptureSavedState");w.Close();
                    }
                });
                Test("Ctrl Shift V only in description, one text history, no map history or empty deletion",()=>{
                    var previous=Wpf.Clipboard.GetDataObject();try{using(var w=new MainWindow()){
                        typeof(MainWindow).GetField("selected",F).SetValue(w,0);typeof(MainWindow).GetField("rightPanelOpen",F).SetValue(w,true);w.Show();Call(w,"UpdateUI");Application.DoEvents();var editor=(RichNoteEditor)Get(w,"cellDescription");editor.Box.Focus();Application.DoEvents();
                        var data=new Wpf.DataObject();data.SetData(Wpf.DataFormats.UnicodeText,"Zażółć\r\njaźń");data.SetData(Wpf.DataFormats.Rtf,@"{\rtf1\ansi\b FORMATTED}");Wpf.Clipboard.SetDataObject(data,true);string before=editor.ContentJson;
                        Check(editor.HandlePlainPasteKey(Input.Key.V,Input.ModifierKeys.Control|Input.ModifierKeys.Shift),"Shortcut not handled");string after=editor.ContentJson;Check(NoteCodec.Plain(after).Contains("Zażółć") && !after.Contains("FORMATTED") && Count(w,"undo")==0,"Shortcut wrong path/map history");
                        Call(w,"TextUndo","description",false);Check(editor.ContentJson==before,"Not one Undo");Call(w,"TextUndo","description",true);Check(editor.ContentJson==after,"Redo mismatch");
                        Check(!editor.HandlePlainPasteKey(Input.Key.V,Input.ModifierKeys.Control),"Ctrl V intercepted");Wpf.Clipboard.Clear();editor.Box.SelectAll();editor.HandlePlainPasteKey(Input.Key.V,Input.ModifierKeys.Control|Input.ModifierKeys.Shift);Check(editor.ContentJson==after,"Empty clipboard removed selection");
                        ((Control)Get(w,"cellName")).Focus();Application.DoEvents();Check(!editor.HandlePlainPasteKey(Input.Key.V,Input.ModifierKeys.Control|Input.ModifierKeys.Shift),"Shortcut ran outside description");Check(editor.PlainPasteItem.InputGestureText=="Ctrl+Shift+V","Menu shortcut absent");Call(w,"TextUndo","description",false);w.Close();
                    }}finally{if(previous!=null)Wpf.Clipboard.SetDataObject(previous,true);else Wpf.Clipboard.Clear();}
                });
                Console.WriteLine("Passed: "+passed+"; 0.2.3 targeted methods/control tests, not native UI.");return 0;
            }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{Directory.Delete(root,true);}
        }
    }
}
