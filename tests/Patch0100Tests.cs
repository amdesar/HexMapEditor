using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0100Tests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool value,string text){if(!value)throw new Exception(text);}
        static object Call(MainWindow w,string name,params object[] args){return typeof(MainWindow).GetMethods(Private).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(w,args);}
        static T Field<T>(MainWindow w,string name){return (T)typeof(MainWindow).GetField(name,Private).GetValue(w);}
        static void Set(MainWindow w,string name,object value){typeof(MainWindow).GetField(name,Private).SetValue(w,value);}
        static byte[] BitmapPng(bool transparent)
        {
            using(var bitmap=new Bitmap(80,60))
            using(var stream=new MemoryStream())
            {
                using(var g=Graphics.FromImage(bitmap))
                {
                    if(transparent)g.FillRectangle(Brushes.Cyan,37,27,6,6);
                    else g.Clear(Color.Cyan);
                }
                bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }
        static MapData Map()
        {
            var map=MainWindow.CreateProjectData(8,7,"PointyTop","Próba 0.10.0",new TerrainPreset{Name="Próba panelu"});
            var asset=IconAssets.Starters()[0];map.IconAssets.Add(asset);
            var a=BrushGeometry.Center(map,2,2);var b=BrushGeometry.Center(map,4,2);
            map.Objects.Add(new MapObject{Name="Pierwszy",TemplateId="fixture",AssetId=asset.Id,Side="Ally",RevealsFog=true,Size=12,X=a.X,Y=a.Y});
            map.Objects.Add(new MapObject{Name="Drugi",TemplateId="fixture",AssetId=asset.Id,Size=100,X=b.X,Y=b.Y});
            return map;
        }
        static ObjectGroup AddGroup(MapData map)
        {
            var group=new ObjectGroup{Name="Grupa próbna",Members=map.Objects.Select(o=>o.Id).ToList(),SizeMode="Offset",SizeValue=-30};
            map.Groups.Add(group);return group;
        }
        static void Groups()
        {
            var map=Map();var group=AddGroup(map);var a=map.Objects[0];var b=map.Objects[1];
            Check(GroupRules.Used(group,a.Id)==0&&GroupRules.Effective(map,a).Size==12,"New member not individual");
            GroupRules.Set(group,a.Id,GroupProperty.Size|GroupProperty.Fog,true);
            Check(GroupRules.Effective(map,a).Size==8&&!GroupRules.Effective(map,a).RevealsFog&&a.RevealsFog,"Offset clamp/fog off");
            GroupRules.ToggleColumn(group,GroupProperty.Size);
            Check(GroupRules.Effective(map,b).Size==70&&GroupRules.Effective(map,a).Size==8,"Mixed column");
            GroupRules.ToggleColumn(group,GroupProperty.Size);
            Check(GroupRules.Effective(map,a).Size==12&&GroupRules.Effective(map,b).Size==100,"All on to off");
            GroupRules.Set(group,a.Id,GroupProperty.Size,true);GroupRules.Set(group,b.Id,GroupProperty.Size|GroupProperty.Lock,true);group.Locked=true;
            var file=Path.Combine(Root,"grupa.hexmap");MainWindow.WriteProjectData(map,file,false);
            var loaded=MainWindow.ReadProjectData(file);var g=loaded.Groups[0];var first=loaded.Objects[0];var second=loaded.Objects[1];
            Check(first.Size==12&&second.Size==100&&GroupRules.Effective(loaded,second).Size==70&&GroupRules.Effective(loaded,second).Locked,"Round-trip");
            using(var w=new MainWindow(loaded,file))
            {
                Check((bool)Call(w,"DetachObjects",g.Id,new Dictionary<string,GroupProperty>{{first.Id,GroupProperty.Size},{second.Id,GroupProperty.Lock}}),"Dissolve");
                Check(loaded.Groups.Count==0&&loaded.Objects[0].Size==8&&loaded.Objects[1].Size==100&&loaded.Objects[1].Locked,"Different per-member selections");
                Call(w,"Undo");Check(loaded.Groups.Count==1&&loaded.Objects[0].Size==12&&!loaded.Objects[1].Locked,"Undo dissolve");
                g=loaded.Groups.Single();
                Check((bool)Call(w,"DetachObjects",g.Id,new Dictionary<string,GroupProperty>{{first.Id,0}}),"Detach single member");
                Check(loaded.Groups.Single().Members.SequenceEqual(new[]{second.Id})&&loaded.Objects[0].Size==12,"Detach preserves base");
                Call(w,"Undo");
                g=loaded.Groups.Single();
                Call(w,"SelectGroup",g);
                Func<string,bool> decline=_=>false,approve=_=>true;
                Check(!(bool)Call(w,"DeleteObjectsAfterConfirmation",decline)&&loaded.Objects.Count==2&&loaded.Groups.Count==1,"Cancel deletion");
                Check((bool)Call(w,"DeleteObjectsAfterConfirmation",approve)&&loaded.Objects.Count==0&&loaded.Groups.Count==0,"Delete group occurrences");
                Call(w,"Undo");
                Check(loaded.Objects.Count==2&&loaded.Groups.Count==1&&GroupRules.Used(loaded.Groups[0],second.Id).HasFlag(GroupProperty.Lock),"Undo deletion");
                var added=loaded.Objects[1].Copy();
                added.Id=Guid.NewGuid().ToString("N");
                loaded.Objects.Add(added);
                var selection=Field<HashSet<string>>(w,"selectedObjects");
                selection.UnionWith(loaded.Objects.Select(o=>o.Id));
                Check((bool)Call(w,"JoinSelectedGroups",g.Id,null)&&GroupRules.Used(loaded.Groups.Single(),added.Id)==0,"Joined member defaults to individual values");
            }
            Console.WriteLine("groups PASS");
        }
        static void Panel()
        {
            var map=Map();var group=AddGroup(map);
            using(var w=new MainWindow(map,null))
            {
                w.Show();Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");
                var right=Field<Panel>(w,"right");var hex=Field<Control>(w,"hexDetailScroll");
                Check(right.Visible&&hex.Visible,"New hex panel");
                Check(!((ScrollableControl)hex).VerticalScroll.Visible,"Unnecessary hex scrollbar");
                var center=BrushGeometry.Center(map,0,0);
                var tiles=Field<FlowLayoutPanel>(w,"hexObjects");
                Check(!tiles.Visible,"Empty member section");
                for(int i=0;i<14;i++)
                {
                    var anchored=map.Objects[0].Copy();
                    anchored.Id=Guid.NewGuid().ToString("N");
                    anchored.X=center.X;anchored.Y=center.Y;anchored.Size=8;
                    map.Objects.Add(anchored);
                    if(i!=0&&i!=13)continue;
                    Set(w,"hexMemberKey",null);
                    Call(w,"UpdateUI");
                    Check(tiles.Visible&&tiles.Controls.Count==i+1,"Anchored member count");
                    if(i==0)Check(!((ScrollableControl)hex).VerticalScroll.Visible,"Single row should not scroll");
                    else Check(tiles.Controls.Cast<Control>().Select(tile=>tile.Top).Distinct().Count()>1,"Member tiles wrap");
                }
                ((Button)tiles.Controls[0]).PerformClick();
                Check(Field<HashSet<string>>(w,"selectedObjects").Contains(map.Objects[2].Id),"Open object from hex");
                map.Objects.RemoveRange(2,14);
                Call(w,"CloseDetailsPanel");Set(w,"selected",0);Set(w,"rightPanelOpen",true);Call(w,"UpdateUI");
                var header=Field<DetailNameHeader>(w,"hexDetailName");
                var draft=header.Controls.OfType<DraftNameBox>().Single();
                header.Controls.OfType<Button>().Single(button=>button.AccessibleName.EndsWith("edytuj")).PerformClick();
                draft.Text="Robocza nazwa";
                draft.Step(false);Check(draft.Text==""&&map.Cells[0].Name=="","Text undo is local");
                draft.Step(true);Check(draft.Text=="Robocza nazwa","Text redo");
                header.Controls.OfType<Button>().Single(button=>button.AccessibleName=="Anuluj edycję nazwy").PerformClick();
                Check(map.Cells[0].Name=="","Cancel name draft");
                using(var path=ObjectGeometry.Hex(map,center,28))
                using(var visible=new Bitmap(new MemoryStream(BitmapPng(false))))
                using(var clear=new Bitmap(new MemoryStream(BitmapPng(true))))
                {
                    var pts=path.PathPoints;
                    var opaque=HexObjectCoverage.AlphaSpans(visible);
                    Check(!HexObjectCoverage.Covers(pts,new RectangleF(-6,-6,12,12),visible.Size,opaque),"Below 20%");
                    Check(HexObjectCoverage.Covers(pts,new RectangleF(-16,-16,32,32),visible.Size,opaque),"Above 20%");
                    Check(!HexObjectCoverage.Covers(pts,path.GetBounds(),clear.Size,HexObjectCoverage.AlphaSpans(clear)),"Transparent padding");
                }
                Call(w,"RenameSelectedHex","Heks próbny");Check(map.Cells[0].Name=="Heks próbny","Name edit");Call(w,"Undo");Check(map.Cells[0].Name!="Heks próbny","Name undo");
                Call(w,"SetRightWidth",540);Field<Button>(w,"rightToggle").PerformClick();
                Check(right.Width==18&&!hex.Visible,"Collapse");
                Call(w,"SelectGroup",group);
                Check(right.Width==18,"Selection expanded collapsed panel");
                Call(w,"SetRightWidth",440);
                Check(right.Width==18,"Reset expanded collapsed panel");
                Field<Button>(w,"rightToggle").PerformClick();
                Check(right.Width==440&&Field<TableLayoutPanel>(w,"groupLayout").Visible,"Restore shows current group");
                Call(w,"SelectPanelMember",map.Objects[0].Id);
                Check(Field<TableLayoutPanel>(w,"instanceLayout").Visible,"Member selected from group");
                Call(w,"ScaleObjectSize",2.0);Check(map.Objects[0].Size==24,"×2");
                Call(w,"ScaleObjectSize",.5);Check(map.Objects[0].Size==12,"÷2");
                Call(w,"ScaleObjectSize",0.0);Check(map.Objects[0].Size==30,"reset");
                Call(w,"CloseDetailsPanel");Check(!right.Visible&&!Field<bool>(w,"rightCollapsed"),"Close details resets collapse");
            }
            Console.WriteLine("panel PASS");
        }
        static string Rtf(byte[] png)
        {
            return @"{\rtf1\ansi Start {\*\shppict {\pict\pngblip\picw80\pich60 "+
                BitConverter.ToString(png).Replace("-","")+@"}} Koniec}";
        }
        static RichNode[] Flatten(IEnumerable<RichNode> nodes){return nodes.SelectMany(n=>new[]{n}.Concat(Flatten(n.Children))).ToArray();}
        static void CheckMixed(string value)
        {
            var parts=Flatten(NoteCodec.Read(value).Blocks);
            int image=Array.FindIndex(parts,n=>n.Kind=="image");
            Check(image>=0&&parts.Take(image).Any(n=>(n.Text??"").Contains("Start"))&&parts.Skip(image+1).Any(n=>(n.Text??"").Contains("Koniec")),"Mixed order: "+NoteCodec.Plain(value));
        }
        static void Notes()
        {
            var png=BitmapPng(false);
            using(var editor=new RichNoteEditor())
            {
                editor.Error=s=>{throw new Exception(s);};
                editor.PasteRtf(Rtf(png),"Start Koniec");CheckMixed(editor.ContentJson);
                editor.SetContent(null,"");var data=new System.Windows.DataObject();
                data.SetData(System.Windows.DataFormats.Html,"<b>Start</b><img src='data:image/png;base64,"+Convert.ToBase64String(png)+"'><img src='https://invalid.example/p.png'><script>no</script>Koniec");
                editor.PasteData(data,"Start Koniec");CheckMixed(editor.ContentJson);
                Check(Flatten(NoteCodec.Read(editor.ContentJson).Blocks).Count(n=>n.Kind=="image")==1,"External image");
                editor.SetContent(null,"");var picture=new System.Windows.DataObject();picture.SetData("PNG",png);
                editor.PasteData(picture,"");
                Check(Flatten(NoteCodec.Read(editor.ContentJson).Blocks).Any(n=>n.Kind=="image"),"Image-only paste");
                editor.SetContent(null,"");editor.InsertTable(2,2);editor.ChangeTable("addRow");
                Check(Flatten(NoteCodec.Read(editor.ContentJson).Blocks).Count(n=>n.Kind=="row")==3,"Table editing");
                var imageFile=Path.Combine(Root,"obraz.png");
                File.WriteAllBytes(imageFile,png);
                editor.SetContent(null,"");
                editor.ImagePathRequested=()=>imageFile;
                typeof(RichNoteEditor).GetMethod("InsertImageFile",Private).Invoke(editor,null);
                var node=Flatten(NoteCodec.Read(editor.ContentJson).Blocks).Single(n=>n.Kind=="image");
                node.DisplayWidth=128;editor.SetContent(null,"");editor.InsertImage(node);
                string rich=editor.ContentJson;
                var map=Map();map.Cells[0].NotesFormat="richDocument1";map.Cells[0].RichDescription=rich;map.Cells[0].Description=NoteCodec.Plain(rich);
                string first=Path.Combine(Root,"opis.hexmap");MainWindow.WriteProjectData(map,first,false);
                string dir=Path.Combine(Root,"przeniesiona");Directory.CreateDirectory(dir);
                string second=Path.Combine(dir,"mapa.hexmap");File.Move(first,second);
                File.Delete(imageFile);
                var loaded=MainWindow.ReadProjectData(second);
                var doc=NoteCodec.FromJson(loaded.Cells[0].RichDescription);
                Check(Flatten(NoteCodec.Read(NoteCodec.Write(doc)).Blocks).Single(n=>n.Kind=="image").DisplayWidth==128,"Embedded image/width round-trip");
            }
            Console.WriteLine("notes PASS");
        }
        static void Fixture()
        {
            var map=Map();var group=AddGroup(map);GroupRules.Set(group,map.Objects[0].Id,GroupProperty.Size,true);
            PresetCatalog.Write(map.Presets[0],LibraryPaths.Presets,false);
            var path=Path.Combine(Root,"Próba 0.10.0.hexmap");MainWindow.WriteProjectData(map,path,false);RecentProjects.Shared.Touch(path);
            File.WriteAllBytes(Path.Combine(Root,"Obraz próbny.png"),BitmapPng(false));
            File.WriteAllText(Path.Combine(Root,"Treść mieszana.rtf"),Rtf(BitmapPng(false)));
        }
        static void ClipboardSource()
        {
            using(var form=new Form{Text="Próbka RTF 0.10.0",ClientSize=new Size(400,110),StartPosition=FormStartPosition.CenterScreen})
            {
                var copy=new Button{Text="Kopiuj tekst i obraz (RTF)",Dock=DockStyle.Top,Height=36};
                var status=new Label{Dock=DockStyle.Fill,Text="Start → obraz → Koniec",TextAlign=ContentAlignment.MiddleCenter};
                copy.Click+=(s,e)=>{
                    var data=new DataObject();
                    data.SetData(DataFormats.Rtf,Rtf(BitmapPng(false)));
                    data.SetData(DataFormats.UnicodeText,"Start Koniec");
                    Clipboard.SetDataObject(data,true);
                    status.Text="Skopiowano tekst i obraz.";
                };
                form.Controls.Add(status);form.Controls.Add(copy);Application.Run(form);
            }
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                LibraryPaths.EnsureDirectories(Root);
                switch(args.Single())
                {
                    case "groups":Groups();break;
                    case "panel":Panel();break;
                    case "notes":Notes();break;
                    case "fixture":Fixture();break;
                    case "clipboard":ClipboardSource();break;
                    default:throw new ArgumentException("Unknown Case");
                }
                return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
