using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal static class HexObjectCoverage
    {
        // The sprite renderer maps the source rectangle linearly into world
        // space. Intersect the opaque source pixel spans with the hex polygon.
        internal static Rectangle[] AlphaSpans(Bitmap bitmap)
        {
            var spans=new List<Rectangle>();
            for(int y=0;y<bitmap.Height;y++)
            {
                int start=-1;
                for(int x=0;x<=bitmap.Width;x++)
                {
                    bool opaque=x<bitmap.Width&&bitmap.GetPixel(x,y).A>=96;
                    if(opaque&&start<0)start=x;
                    if(!opaque&&start>=0){spans.Add(new Rectangle(start,y,x-start,1));start=-1;}
                }
            }
            return spans.ToArray();
        }
        internal static bool Covers(PointF[] hex,RectangleF sprite,Size dimensions,Rectangle[] spans)
        {
            if(sprite.Width<=0||sprite.Height<=0)return false;
            float left=hex.Min(p=>p.X),right=hex.Max(p=>p.X),top=hex.Min(p=>p.Y),bottom=hex.Max(p=>p.Y);
            double threshold=Area(hex,hex.Length)*.2;
            var box=RectangleF.FromLTRB(left,top,right,bottom);
            var overlap=RectangleF.Intersect(box,sprite);
            if(overlap.Width*overlap.Height+1e-9<threshold)return false;
            var a=new PointF[16];var b=new PointF[16];
            float sx=sprite.Width/dimensions.Width,sy=sprite.Height/dimensions.Height;
            double area=0;
            foreach(var span in spans)
            {
                float y0=sprite.Y+span.Top*sy,y1=sprite.Y+span.Bottom*sy;
                if(y1<=top)continue;
                if(y0>=bottom)break;
                float x0=sprite.X+span.Left*sx,x1=sprite.X+span.Right*sx;
                if(x1<=left||x0>=right)continue;
                Array.Copy(hex,a,hex.Length);
                int n=Clip(a,hex.Length,b,x0,true,true);
                n=Clip(b,n,a,x1,true,false);
                n=Clip(a,n,b,y0,false,true);
                n=Clip(b,n,a,y1,false,false);
                area+=Area(a,n);
                if(area+1e-7>=threshold)return true;
            }
            return false;
        }
        static int Clip(PointF[] from,int length,PointF[] to,float line,bool vertical,bool greater)
        {
            if(length==0)return 0;
            var previous=from[length-1];int count=0;
            float previousAxis=vertical?previous.X:previous.Y;
            bool previousInside=greater?previousAxis>=line:previousAxis<=line;
            for(int i=0;i<length;i++)
            {
                var point=from[i];float axis=vertical?point.X:point.Y;
                bool inside=greater?axis>=line:axis<=line;
                if(inside!=previousInside)
                {
                    float t=(line-previousAxis)/(axis-previousAxis);
                    to[count++]=vertical?new PointF(line,previous.Y+(point.Y-previous.Y)*t):
                        new PointF(previous.X+(point.X-previous.X)*t,line);
                }
                if(inside)to[count++]=point;
                previous=point;previousAxis=axis;previousInside=inside;
            }
            return count;
        }
        static double Area(PointF[] points,int length)
        {
            if(length<3)return 0;
            var origin=points[0];double sum=0;
            for(int i=1;i<length-1;i++)
                sum+=(double)(points[i].X-origin.X)*(points[i+1].Y-origin.Y)-(double)(points[i].Y-origin.Y)*(points[i+1].X-origin.X);
            return Math.Abs(sum)*.5;
        }
    }

    public sealed partial class MainWindow
    {
        DetailNameHeader hexDetailName;
        DetailCaption hexTerrain;
        Label hexIcon;
        Panel hexPreview,hexBody,hexNoteFooter;
        FlowLayoutPanel hexObjects;
        TableLayoutPanel hexSummary;
        Label hexObjectTitle;
        Button expandDescription;
        bool descriptionExpanded;
        string hexMemberKey;
        readonly Dictionary<string,Rectangle[]> noteCoverageSpans=new Dictionary<string,Rectangle[]>();

        void BuildHexDetails()
        {
            var scroll=new BufferedPanel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(12)};
            hexDetailScroll=scroll;
            hexBody=new Panel{Dock=DockStyle.Top};
            scroll.Controls.Add(hexBody);right.Controls.Add(scroll);
            hexSummary=new TableLayoutPanel {Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1};
            hexDetailName=new DetailNameHeader("Własna nazwa heksa",RenameSelectedHex,tips,true);
            hexDetailName.Margin=Padding.Empty;
            hexSummary.Controls.Add(hexDetailName);
            var detailsFont=DetailMetadataFont();
            hexTerrain=new DetailCaption{Contrast=true,Dock=DockStyle.Fill,Font=detailsFont,Margin=Padding.Empty};
            hexIcon=new DetailCaption{Font=detailsFont,Dock=DockStyle.Fill,Margin=Padding.Empty};
            var summaryRows=new TableLayoutPanel{Dock=DockStyle.Top,Height=82,ColumnCount=2,RowCount=2,Margin=Padding.Empty};
            summaryRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            summaryRows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,84));
            summaryRows.RowStyles.Add(new RowStyle(SizeType.Percent,50));
            summaryRows.RowStyles.Add(new RowStyle(SizeType.Percent,50));
            summaryRows.Disposed+=(s,e)=>detailsFont.Dispose();
            summaryRows.Controls.Add(hexTerrain,0,0);summaryRows.Controls.Add(hexIcon,0,1);
            hexPreview=new BufferedPanel{Dock=DockStyle.Fill,AccessibleName="Podgląd heksa bez obiektów"};
            hexPreview.Paint+=PaintHexDetail;summaryRows.Controls.Add(hexPreview,1,0);summaryRows.SetRowSpan(hexPreview,2);
            hexSummary.Controls.Add(summaryRows);
            hexObjectTitle=DetailSection("Obiekty na heksie");
            hexObjectTitle.Dock=DockStyle.Top;
            hexSummary.Controls.Add(hexObjectTitle);
            hexObjects=new BufferedFlowPanel{Dock=DockStyle.Top,WrapContents=true,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink};
            hexSummary.Controls.Add(hexObjects);
            hexNoteFooter=new Panel{Dock=DockStyle.Top,Height=28};
            hexNoteFooter.Paint+=(s,e)=>e.Graphics.DrawLine(Pens.DimGray,0,0,hexNoteFooter.Width,0);
            var title=DetailSection("Opis");title.Dock=DockStyle.Fill;
            expandDescription=PropertyButton("Powiększ opis",()=>{descriptionExpanded=!descriptionExpanded;UpdateHexDescriptionLayout();});
            hexNoteFooter.Controls.Add(title);
            cellDescription.AddExpandButton(expandDescription);
            cellDescription.Dock=DockStyle.Fill;
            hexBody.Controls.Add(cellDescription);hexBody.Controls.Add(hexNoteFooter);hexBody.Controls.Add(hexSummary);
            scroll.Layout+=(s,e)=>SizeHexBody();
            hexSummary.SizeChanged+=(s,e)=>SizeHexBody();
        }
        void SizeHexBody()
        {
            int height=Math.Max(hexDetailScroll.ClientSize.Height-24,(descriptionExpanded?0:hexSummary.Height)+hexNoteFooter.Height+160);
            if(hexBody.Height!=height)hexBody.Height=height;
        }
        void UpdateHexDescriptionLayout()
        {
            hexSummary.Visible=!descriptionExpanded;
            hexNoteFooter.Visible=!descriptionExpanded;
            expandDescription.Text=descriptionExpanded?"Wróć do szczegółów":"Powiększ opis";
            SizeHexBody();
        }
        void UpdateHexDetails()
        {
            var cell=map.Cells[selected];
            bool hidden=CellMasked(selected);
            string key=cell.Id+(hidden?":hidden":":visible");
            if(editorCellId!=key)
            {
                descriptionExpanded=false;
                textHistory.BreakGroups();editorCellId=key;
                cellDescription.SetContent(hidden?null:cell.RichDescription,hidden?"Treść ukryta przez mgłę lub ograniczenie widoczności.":cell.Description);
                cellDescription.UpdateTools();
            }
            cellDescription.Enabled=!hidden;
            string coordinate=Coordinate(selected);
            bool named=!hidden&&!String.IsNullOrWhiteSpace(cell.Name);
            hexDetailName.Display(key,hidden?"":cell.Name,named?cell.Name:coordinate,named?"("+coordinate+")":null,ChoiceStyle.ActiveBorder,!hidden);
            hexTerrain.Text=hidden?"Dane heksa ukryte":MapLogic.IsUnpainted(cell)?"Brak terenu":cell.Terrain;
            var color=hidden?Color.Silver:RepresentativeColors.Get(cell.Color,cell.Texture,cell.Gradient,map.Orientation=="FlatTop");
            hexTerrain.ForeColor=color.A==0?ParseColor(cell.Color):Color.FromArgb(255,color);
            bool hasIcon=!hidden&&cell.IconId!=null;
            hexIcon.Text=hasIcon?cell.IconName??map.IconAssets.First(a=>a.Id==cell.IconId).Name:"";
            hexIcon.Visible=hasIcon;
            hexPreview.Visible=!hidden;
            hexPreview.Invalidate();
            RefreshHexMembers(key,hidden);
            UpdateHexDescriptionLayout();
        }
        void PaintHexDetail(object sender,PaintEventArgs e)
        {
            if(selected<0||selected>=map.Cells.Count||CellMasked(selected))return;
            var cell=map.Cells[selected];
            var client=((Control)sender).ClientSize;
            var center=new PointF(client.Width/2f,client.Height/2f);
            float radius=Math.Max(1,Math.Min(client.Width,client.Height)/2f-4);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var path=HexPath(center,radius))
            using(var brush=new SolidBrush(ParseColor(cell.Color)))
            {
                e.Graphics.FillPath(brush,path);
                if(cell.Gradient!=null)GradientRules.Fill(e.Graphics,path,cell.Gradient);
                if(cell.Texture!=null)DrawTerrainTexture(e.Graphics,path,center,radius,cell.Texture);
                e.Graphics.DrawPath(Pens.DimGray,path);
            }
            if(cell.IconId==null)return;
            var image=AssetBitmap(map.IconAssets.First(a=>a.Id==cell.IconId),false);
            float ratio=radius*1.20f/Math.Max(image.Width,image.Height);
            e.Graphics.DrawImage(image,center.X-image.Width*ratio/2,center.Y-image.Height*ratio/2,image.Width*ratio,image.Height*ratio);
        }
        void RefreshHexMembers(string key,bool hidden)
        {
            string signature=key+":"+documentRevision+":"+hideObjects+":"+previewWithoutFog+":"+hideDarkness;
            if(signature==hexMemberKey)return;
            hexMemberKey=signature;
            var objects=(hidden||hideObjects?Enumerable.Empty<MapObject>():HexMembers(selected)).ToArray();
            hexObjects.SuspendLayout();
            try
            {
                foreach(Control c in hexObjects.Controls.Cast<Control>().ToArray())c.Dispose();
                foreach(var obj in objects)
                {
                    string id=obj.Id;
                    var button=IconButton(new LibraryIcon{Id=id,Name=obj.Name,Asset=map.IconAssets.First(a=>a.Id==obj.AssetId)},()=>SelectPanelMember(id));
                    button.AccessibleName="Wybierz obiekt "+obj.Name;hexObjects.Controls.Add(button);
                }
            }
            finally{hexObjects.ResumeLayout(true);}
            hexObjectTitle.Text="Obiekty na heksie ("+objects.Length+")";
            hexObjectTitle.Visible=hexObjects.Visible=objects.Length!=0;
        }
        IEnumerable<MapObject> HexMembers(int index)
        {
            var center=BrushGeometry.Center(map,index%map.Columns,index/map.Columns);
            using(var hex=ObjectGeometry.Hex(map,center,BaseRadius))
            {
                var bounds=hex.GetBounds();
                var polygon=hex.PathPoints;
                foreach(var obj in map.Objects)
                {
                    // No sprite exceeds 1024 world units on either axis.
                    if(obj.X<bounds.Left-512||obj.X>bounds.Right+512||obj.Y<bounds.Top-512||obj.Y>bounds.Bottom+512)continue;
                    var sprite=ObjectBounds(obj);
                    if(!sprite.IntersectsWith(bounds))continue;
                    bool anchored=ObjectGeometry.Hit(map,new PointF(obj.X,obj.Y))==index;
                    if(!anchored)
                    {
                        var asset=map.IconAssets.First(a=>a.Id==obj.AssetId);
                        Rectangle[] spans;
                        if(!noteCoverageSpans.TryGetValue(asset.Id,out spans))
                        {
                            if(noteCoverageSpans.Count>=16)noteCoverageSpans.Clear();
                            spans=HexObjectCoverage.AlphaSpans(AssetBitmap(asset,false));
                            noteCoverageSpans.Add(asset.Id,spans);
                        }
                        if(!HexObjectCoverage.Covers(polygon,sprite,new Size(asset.Width,asset.Height),spans))continue;
                    }
                    if(ObjectFogLevel(obj)==0)yield return obj;
                }
            }
        }
    }
}
