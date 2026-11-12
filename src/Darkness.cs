using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal static class LightGeometry
    {
        static Point Axial(MapData m,int c,int r){return m.Orientation=="PointyTop"?new Point(c-(r+m.RowParity-((r+m.RowParity)&1))/2,r):new Point(c,r-(c+m.ColumnParity-((c+m.ColumnParity)&1))/2);}
        internal static HashSet<int> Coverage(MapData map)
        {
            var result=new HashSet<int>();
            foreach(var item in map.Objects)
            {
                var obj=GroupRules.Effective(map,item);if(!obj.LightEnabled)continue;
                var seed=ObjectGeometry.Nearest(map,new PointF(obj.X,obj.Y));var a=Axial(map,seed.X,seed.Y);int radius=obj.LightRadius,extent=radius*2+2;
                // Only existing cells are visited, even for sources outside the map.
                for(int r=Math.Max(0,seed.Y-extent);r<=Math.Min(map.Rows-1,seed.Y+extent);r++)
                for(int c=Math.Max(0,seed.X-extent);c<=Math.Min(map.Columns-1,seed.X+extent);c++)
                {var b=Axial(map,c,r);int q=b.X-a.X,s=b.Y-a.Y;if(Math.Max(Math.Abs(q),Math.Max(Math.Abs(s),Math.Abs(q+s)))<=radius)result.Add(r*map.Columns+c);}
            }
            return result;
        }
    }
    public sealed partial class MainWindow
    {
        MapData lightMap;HashSet<int> lightCells;readonly HashSet<int> noLightCells=new HashSet<int>();
        void InvalidateLight(){lightCells=null;}
        HashSet<int> LitCells(){if(hideObjects)return noLightCells;if(lightCells==null||!Object.ReferenceEquals(map,lightMap)){lightMap=map;lightCells=LightGeometry.Coverage(map);}return lightCells;}
        bool EffectiveDarkness(int index){return map.Cells[index].Darkness&&!LitCells().Contains(index);}
        bool CellMasked(int index){return index>=0&&((!previewWithoutFog&&map.Cells[index].Fog)||(!hideDarkness&&EffectiveDarkness(index)));}
        double MaskVisible(RectangleF bounds){return FogVisibility.VisibleFraction(map,bounds,CellMasked);}
        void DrawDarkness(Graphics g,Rectangle clip)
        {
            if(hideDarkness)return;
            for(int i=0;i<map.Cells.Count;i++)
            {
                if(!EffectiveDarkness(i))continue;
                var center=ScreenCenter(i%map.Columns,i/map.Columns);float radius=BaseRadius*scale;
                if(!clip.IntersectsWith(Rectangle.Ceiling(new RectangleF(center.X-radius-3,center.Y-radius-3,2*radius+6,2*radius+6))))continue;
                using(var path=HexPath(center,radius))
                {
                    var fill=map.Cells[i].DarkFill;
                    if(fill.Graphic!=null)DrawTerrainTexture(g,path,center,radius,fill.Graphic);
                    else if(fill.Gradient!=null)GradientRules.Fill(g,path,fill.Gradient);
                    else using(var b=new SolidBrush(ColorTranslator.FromHtml(fill.Color)))g.FillPath(b,path);
                    using(var p=new Pen(Color.FromArgb(135,19,23,30),1.2f))g.DrawPath(p,path);
                }
            }
        }
        void AttachDarkAppearanceMenu()
        {
            foreach(var tool in new[]{EditorTool.DarkBrush,EditorTool.DarkBucket})
            {var menu=new ContextMenuStrip{BackColor=BackColor,ForeColor=ForeColor};menu.Items.Add("Wygląd ograniczenia widoczności…",null,(s,e)=>EditDarkAppearance());var button=toolButtons[tool];button.ContextMenuStrip=menu;button.Disposed+=(s,e)=>menu.Dispose();tips.SetToolTip(button,button.AccessibleName+". Klik/przeciągnięcie: nakłada lub usuwa bazowe ograniczenie widoczności, tryb z pierwszego pola. PPM przycisku: wygląd.");}
        }
        void EditDarkAppearance()
        {
            try
            {
                using(var d=new AppearancePoolDialog("Wygląd ograniczenia widoczności",null,map.DarkPalette,true,map.Orientation=="FlatTop","Zastosuj wygląd do obecnie przyciemnionych heksów"))
                {
                    d.ChooseGraphic=ChooseTerrainGraphic;d.EditCrop=ShowTerrainCrop;
                    d.CanAccept=()=>{try{CheckTextureBudget(d.Fills.Select(f=>f.Graphic));}catch(Exception ex){MessageBox.Show(d,ex.Message,"Budżet grafik");return false;}return !d.ApplyToMap||GuardLayer(MapLayer.Darkness,d);};
                    var answer=RunModal(()=>d.ShowDialog(this));RememberModalClick(d);if(answer==DialogResult.OK)ChangeDarkAppearance(d.Fills,d.ApplyToMap,d.PreserveLayout);
                }
            }finally{ArmCanvasInput();}
        }
        bool ChangeDarkAppearance(List<FillVariant> fills,bool apply,bool preserve)
        {
            FillRules.ValidatePool(fills);CheckTextureBudget(fills.Select(f=>f.Graphic));EndCanvasGesture(true);
            var action=new EditAction{Label="Wygląd ograniczenia widoczności"};
            if(FillRules.Signature(map.DarkPalette)!=FillRules.Signature(fills)){action.BeforeDarkPalette=FillRules.Copy(map.DarkPalette);action.AfterDarkPalette=FillRules.Copy(fills);}
            if(apply)foreach(var cell in map.Cells.Where(c=>c.Darkness))
            {var next=preserve?fills.FirstOrDefault(f=>f.Id==cell.DarkFill.Id):NextFill(fills);if(next==null||FillRules.Signature(new[]{next})==FillRules.Signature(new[]{cell.DarkFill}))continue;action.Layers.Add(new LayerChange{CellId=cell.Id,DarkAppearance=true,BeforeDarkFill=cell.DarkFill.Copy(),AfterDarkFill=next.Copy()});}
            if(action.BeforeDarkPalette==null&&action.Layers.Count==0)return false;
            ApplyAction(action,true);PushAction(action);MarkDirty();return true;
        }
    }
}
