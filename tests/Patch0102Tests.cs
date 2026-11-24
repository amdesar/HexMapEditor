using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Wpf = System.Windows;
using Doc = System.Windows.Documents;

namespace HexMapEditor.Tests
{
    internal static class Patch0102Tests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static T Get<T>(object target,string field){return (T)target.GetType().GetField(field,Private).GetValue(target);}
        static void Set(object target,string field,object value){target.GetType().GetField(field,Private).SetValue(target,value);}
        static void Call(object target,string name,params object[] args){target.GetType().GetMethod(name,Private).Invoke(target,args);}

        static MapData Map()
        {
            var map=MainWindow.CreateProjectData(8,7,"PointyTop","Próba 0.10.2",new TerrainPreset{Name="Próba panelu"});
            PresetCatalog.Write(map.Presets[0],LibraryPaths.Presets,false);
            var asset=IconAssets.Starters()[0];map.IconAssets.Add(asset);
            for(int i=0;i<5;i++)
            {
                var state=new ObjectStateDefinition{Id="probe-"+i,Name="Stan "+(i+1),Color="#426A86",CanReveal=true};
                map.ObjectStates.Add(state);
                File.WriteAllText(Path.Combine(LibraryPaths.Root,"Objects","States","state-"+state.Id+".json"),MapLogic.Serializer().Serialize(state));
            }
            for(int i=0;i<3;i++)
            {
                var point=BrushGeometry.Center(map,2+i,2+i%2);
                map.Objects.Add(new MapObject{Name="Obiekt próbny "+(i+1),TemplateId="probe",AssetId=asset.Id,
                    Size=24,Side="Ally",X=point.X,Y=point.Y});
            }
            map.Groups.Add(new ObjectGroup{Name="Grupa próbna",Members=map.Objects.Take(2).Select(o=>o.Id).ToList()});
            map.Cells[0].Name="Heks próbny";
            return map;
        }

        static void CheckLiveResize(MainWindow window,string view)
        {
            var right=Get<DetailsDock>(window,"right");
            var grip=Get<Panel>(window,"rightResizeGrip");
            var canvas=Get<Panel>(window,"canvas");
            canvas.Refresh();
            var paint=new List<Rectangle>();
            PaintEventHandler observed=(s,e)=>paint.Add(e.ClipRectangle);
            canvas.Paint+=observed;
            int anchor=grip.PointToScreen(new Point(3,15)).X;
            Call(window,"BeginRightResize",anchor);
            int first=right.Left,steps=0,exposures=0,maxPaintWidth=0;
            var timer=Stopwatch.StartNew();
            foreach(int dx in new[]{-5,-12,-26,-180,-240,-155,-150,-61,0})
            {
                paint.Clear();
                int previous=right.Left;
                Call(window,"MoveRightResize",anchor+dx);
                Check(right.Left==first+dx&&right.Right==window.ClientSize.Width,view+" edge tracks input");
                Check(grip.PointToScreen(new Point(3,15)).X==anchor+dx,view+" grip tracks input");
                if(previous<right.Left)
                {
                    int x=previous-canvas.Left;
                    Check(paint.Any(r=>r.Left<=x&&r.Right>=right.Left-canvas.Left),view+" strip repainted before release");
                    maxPaintWidth=Math.Max(maxPaintWidth,paint.Max(p=>p.Width));
                    exposures++;
                }
                if(dx==-26) // Observe the committed geometry without releasing or sending another move.
                {
                    Check(Get<bool>(window,"rightResizing")&&grip.Capture,view+" lost capture during gesture");
                    int paints=paint.Count;
                    Call(window,"MoveRightResize",anchor+dx);
                    Check(paint.Count==paints,view+" unexpected repaint without a width change");
                }
                steps++;
            }
            Call(window,"EndRightResize");
            canvas.Paint-=observed;
            Console.WriteLine("{0}: {1} steps, {2} exposed strips before release, largest paint {3}/{4} px, {5:0.0} ms",
                view,steps,exposures,maxPaintWidth,canvas.Width,timer.Elapsed.TotalMilliseconds);
        }

        static Point CheckHeader(DetailNameHeader header,Control relative)
        {
            var title=header.Controls.OfType<DetailCaption>().Single();
            var input=header.Controls.OfType<TextBox>().Single();
            Check(!input.Visible&&title.Visible,"Unexpected draft");
            Call(header,"Begin");
            Check(input.Visible&&input.Font.Size==title.Font.Size&&input.Font.Bold&&input.Top>=0&&input.Bottom<=header.Height,
                "Large header draft "+input.Visible+" "+input.Font+" / "+title.Font+" "+input.Bounds+" / "+header.Height);
            var buttons=header.Controls.OfType<Button>().Where(b=>b.Visible).ToArray();
            Check(buttons.Length==2&&buttons.All(b=>b.Left>=input.Right&&b.Bottom<=header.Height),"Confirmation and cancel fit");
            Call(header,"Cancel");
            return relative.PointToClient(header.PointToScreen(Point.Empty));
        }
        static void CheckRadius(MainWindow window,string switchName,string numberName)
        {
            var toggle=Get<CheckBox>(window,switchName);
            var number=Get<ScrubNumber>(window,numberName);
            var row=toggle.Parent.Parent;
            Rectangle bounds=row.Bounds;
            toggle.Checked=true;
            Check(row.Bounds==bounds&&number.Visible,"Radius changes row bounds");
            for(Control c=number.Parent;c!=row.Parent;c=c.Parent)
            {
                Point bottom=c.PointToClient(number.PointToScreen(new Point(number.Right-number.Left,number.Height)));
                Check(bottom.X<=c.ClientSize.Width&&bottom.Y<=c.ClientSize.Height,"Clipped frame "+numberName+" within "+c.GetType().Name);
            }
            toggle.Checked=false;
            Check(row.Bounds==bounds&&!number.Visible,"Hiding radius changes row bounds");
        }
        static void Panel()
        {
            var map=Map();
            using(var window=new MainWindow(map,Path.Combine(Root,"Próba 0.10.2.hexmap")))
            {
                window.Show();
                var right=Get<DetailsDock>(window,"right");
                Call(window,"SelectPanelMember",map.Objects[2].Id);
                Check(Get<FlowLayoutPanel>(window,"stateTiles").Controls.Count==9,"Eight states plus add");
                var body=Get<DetailsOverlay>(window,"objectDetailPanel");
                var delete=Get<Button>(window,"instanceDelete");
                Check(!body.VerticalScroll.Visible&&delete.Height==30,"First object layout");
                CheckLiveResize(window,"object");
                Point obj=CheckHeader(Get<DetailNameHeader>(window,"objectName"),right);
                CheckRadius(window,"objectReveal","objectRadius");CheckRadius(window,"objectLight","objectLightRadius");
                Check(!body.VerticalScroll.Visible&&delete.Height==30,"Object layout after radius toggles");
                Call(window,"SelectGroup",map.Groups[0]);
                Point group=CheckHeader(Get<DetailNameHeader>(window,"groupName"),right);
                CheckRadius(window,"groupReveal","groupRadius");CheckRadius(window,"groupLight","groupLightRadius");
                Call(window,"CloseDetailsPanel");Set(window,"selected",0);Set(window,"rightPanelOpen",true);Call(window,"UpdateUI");
                Point hex=CheckHeader(Get<DetailNameHeader>(window,"hexDetailName"),right);
                Check(hex==obj&&obj==group,"Heading margins hex "+hex+", object "+obj+", group "+group);
                var terrain=Get<DetailCaption>(window,"hexTerrain");
                Check(right.PointToClient(terrain.PointToScreen(Point.Empty)).X==hex.X,"Shared hex left line");
                CheckLiveResize(window,"hex");
            }
            using(var dialog=new GraphicSourceDialog("Źródło próbne","ikon"))
            {
                dialog.Show();
                var buttons=dialog.Controls[0].Controls.OfType<Button>().ToArray();
                Check(buttons.Length==4&&buttons.All(b=>b.Size==buttons[0].Size&&b.GetType()==buttons[0].GetType()&&
                    b.FlatStyle==buttons[0].FlatStyle&&b.Bottom<=b.Parent.ClientSize.Height),"Graphic source buttons are inconsistent");
            }
        }

        static byte[] Picture()
        {
            using(var bitmap=new Bitmap(700,900))
            using(var graphics=Graphics.FromImage(bitmap))
            using(var stream=new MemoryStream())
            {
                graphics.FillEllipse(Brushes.Crimson,50,50,600,800);
                bitmap.Save(stream,ImageFormat.Png);
                return stream.ToArray();
            }
        }
        static RichDocument Model(RichNoteEditor editor){return NoteCodec.Read(editor.ContentJson);}
        static RichNode ImageNode(RichNoteEditor editor){return Model(editor).Blocks.SelectMany(b=>b.Children).Single(n=>n.Kind=="image");}
        static void CheckImageLayout(RichNoteEditor editor)
        {
            editor.Box.UpdateLayout();
            var blocks=editor.Box.Document.Blocks.ToArray();
            int index=Array.FindIndex(blocks,b=>b is Doc.BlockUIContainer);
            Check(index>0&&index<blocks.Length-1,"Picture is not between text blocks");
            var image=NoteImages.BlockImage(blocks[index]);
            double height=image.ActualHeight,width=image.ActualWidth;
            Check(width>0&&width<=editor.Box.ActualWidth&&Math.Abs(height/width-900.0/700)<.01,"Measured image size "+width+" by "+height);
            double before=blocks[index-1].ContentEnd.GetCharacterRect(Doc.LogicalDirection.Backward).Bottom;
            double after=blocks[index+1].ContentStart.GetCharacterRect(Doc.LogicalDirection.Forward).Top;
            Check(after-before>=height-1,"Text does not clear the image: "+before+" to "+after+", height "+height);
            Console.WriteLine("Note viewport {0:0} px, picture {1:0} × {2:0} px",editor.Box.ActualWidth,width,height);
        }
        static void Notes()
        {
            byte[] png=Picture();
            using(var window=new Form{ClientSize=new Size(650,760)})
            using(var editor=new RichNoteEditor{Dock=DockStyle.Fill})
            {
                window.Controls.Add(editor);window.Show();
                editor.Error=message=>{throw new InvalidDataException(message);};
                var data=new Wpf.DataObject();
                string before=String.Concat(Enumerable.Repeat("Pierwszy akapit. ",18));
                string after=String.Concat(Enumerable.Repeat("Kolejny akapit. ",18));
                data.SetData(Wpf.DataFormats.Rtf,@"{\rtf1\ansi\b "+before+@"\b0{\pict\pngblip "+BitConverter.ToString(png).Replace("-","")+@"}\i "+after+@"}");
                editor.SetContent(null,"");editor.PasteData(data,"");
                var doc=Model(editor);int index=doc.Blocks.FindIndex(b=>b.Children.Any(n=>n.Kind=="image"));
                Check(index>0&&index+1<doc.Blocks.Count,"Mixed RTF lost order");
                Check(doc.Blocks[index-1].Children.Any(n=>n.Bold&&n.Text.Contains("Pierwszy"))&&
                      doc.Blocks[index+1].Children.Any(n=>n.Italic&&n.Text.Contains("Kolejny")),"Text formatting lost");
                var block=editor.Box.Document.Blocks.OfType<Doc.BlockUIContainer>().Single();
                editor.Box.Selection.Select(block.ElementStart,block.ElementEnd);
                Call(editor,"ResizeImage",2.0);
                Check(ImageNode(editor).DisplayWidth==600,"Image width edit missing");
                editor.Box.ScrollToHome();
                CheckImageLayout(editor);
                window.ClientSize=new Size(390,760);window.Update();CheckImageLayout(editor);
                Check(ImageNode(editor).DisplayWidth==600,"Viewport change modified saved width");

                var map=Map();
                map.Cells[0].RichDescription=editor.ContentJson;
                map.Cells[0].Description=NoteCodec.Plain(map.Cells[0].RichDescription);
                map.Cells[0].NotesFormat="richDocument1";
                string path=Path.Combine(Root,"Opis.hexmap");
                MainWindow.WriteProjectData(map,path,false);
                var moved=Path.Combine(Directory.CreateDirectory(Path.Combine(Root,"przeniesiona")).FullName,"Opis.hexmap");
                File.Move(path,moved);
                editor.SetContent(MainWindow.ReadProjectData(moved).Cells[0].RichDescription,"");
                Check(ImageNode(editor).DisplayWidth==600,"Image width lost after map roundtrip");
                CheckImageLayout(editor);

                editor.SetContent(null,"");
                var picture=new Wpf.DataObject();picture.SetData("PNG",new MemoryStream(png));
                editor.PasteData(picture,"");
                Check(ImageNode(editor).Png.Length>0&&editor.Box.Document.Blocks.OfType<Doc.BlockUIContainer>().Count()==1,"Image-only paste");
                var file=Path.Combine(Root,"Obraz próbny.png");File.WriteAllBytes(file,png);
                editor.SetContent(null,"");editor.ImagePathRequested=()=>file;Call(editor,"InsertImageFile");
                Check(ImageNode(editor).Png.Length>0,"File insertion");

                string spaces=new String(' ',15);
                editor.SetContent(null,"słowo"+spaces+"drugie");
                var run=((Doc.Paragraph)editor.Box.Document.Blocks.FirstBlock).Inlines.FirstInline as Doc.Run;
                editor.Box.CaretPosition=run.ContentStart.GetPositionAtOffset(2);
                Call(editor,"ToggleDecoration",Wpf.TextDecorationLocation.Underline);
                var underline=Model(editor).Blocks[0].Children;
                Check(String.Concat(underline.Where(n=>n.Underline).Select(n=>n.Text))=="słowo","Automatic underline includes whitespace");
                Check(String.Concat(underline.Select(n=>n.Text))=="słowo"+spaces+"drugie","Formatting modified text");
                editor.SetContent(null,"słowo"+spaces+"drugie");
                run=((Doc.Paragraph)editor.Box.Document.Blocks.FirstBlock).Inlines.FirstInline as Doc.Run;
                editor.Box.Selection.Select(run.ContentStart,run.ContentStart.GetPositionAtOffset(9));
                Call(editor,"Execute",Doc.EditingCommands.ToggleUnderline);
                Check(String.Concat(Model(editor).Blocks[0].Children.Where(n=>n.Underline).Select(n=>n.Text))=="słowo    ","Explicit selection lost spaces");
            }
        }

        static void Fixture()
        {
            var map=Map();
            foreach(string name in new[]{"Próba 0.10.2","Bardzo długa nazwa projektu z polskimi znakami i wielokropkiem widocznym przed numerem ostatniego zapisu","Mapa bez metadanej"})
            {
                var path=Path.Combine(Root,name+".hexmap");MainWindow.WriteProjectData(map,path,false);
                if(name=="Mapa bez metadanej")
                    File.WriteAllText(path,File.ReadAllText(path).Replace("\"LastSavedAppVersion\":\""+MainWindow.Version+"\",",""));
                RecentProjects.Shared.Touch(path);
            }
            File.WriteAllBytes(Path.Combine(Root,"Obraz próbny.png"),Picture());
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                LibraryPaths.EnsureDirectories(Root);
                string name=args.Single();
                switch(name){case "panel":Panel();break;case "notes":Notes();break;case "fixture":Fixture();break;default:throw new ArgumentException("Unknown Case");}
                Console.WriteLine(name+" PASS");return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
