using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HexMapEditor
{
    [Flags]
    internal enum EraserTargets
    {
        Terrain=1, Name=2, Description=4, Icons=8, Objects=16, Darkness=32, Fog=64, Ink=128
    }

    internal sealed class ErasedCellText
    {
        internal string Id,Name,Description,RichDescription,Format;
        internal bool RemoveName,RemoveDescription;
        internal void Apply(CellData cell,bool erase)
        {
            if(RemoveName)cell.Name=erase?"":Name;
            if(!RemoveDescription)return;
            cell.Description=erase?"":Description;
            cell.RichDescription=erase?null:RichDescription;
            cell.NotesFormat=erase?"plainText":Format;
        }
    }

    internal sealed class EraserSelection
    {
        readonly MapData map;
        readonly double radius;
        readonly HashSet<Point> sampledHexes=new HashSet<Point>();
        readonly List<PointF> path=new List<PointF>();
        PointF? previous;
        internal readonly HashSet<int> Cells=new HashSet<int>();
        internal readonly EraserTargets Targets;

        internal EraserSelection(MapData map,EraserTargets targets,double radius)
        {this.map=map;Targets=targets;this.radius=radius;}

        internal void BreakPath(){previous=null;}
        internal void Add(PointF point)
        {
            if(Targets.HasFlag(EraserTargets.Objects))
            {
                if(!previous.HasValue&&path.Count>0)path.Add(new PointF(Single.NaN,Single.NaN));
                path.Add(point);
            }
            if((Targets&~(EraserTargets.Objects|EraserTargets.Ink))!=0)
            {
                var from=previous??point;
                double dx=point.X-from.X,dy=point.Y-from.Y;
                int steps=Math.Max(1,(int)Math.Ceiling(Math.Sqrt(dx*dx+dy*dy)/12));
                for(int i=1;i<=steps;i++)
                {
                    Point hex=ObjectGeometry.Nearest(map,new PointF(from.X+(float)(dx*i/steps),from.Y+(float)(dy*i/steps)));
                    if(sampledHexes.Add(hex))Cells.UnionWith(BrushGeometry.CircleCells(map,hex,radius));
                }
            }
            previous=point;
        }

        internal bool Hits(PointF center)
        {
            PointF? last=null;
            foreach(var point in path)
            {
                if(Single.IsNaN(point.X)){last=null;continue;}
                var from=last??point;
                double dx=point.X-from.X,dy=point.Y-from.Y,length=dx*dx+dy*dy;
                double fraction=length==0?0:Math.Max(0,Math.Min(1,((center.X-from.X)*dx+(center.Y-from.Y)*dy)/length));
                if(BrushGeometry.InsideCircle(center.X-from.X-dx*fraction,center.Y-from.Y-dy*fraction,radius))return true;
                last=point;
            }
            return false;
        }
    }

    public sealed partial class MainWindow
    {
        EraserTargets eraserTargets=EraserTargets.Ink;
        EraserSelection eraserSelection;
        static readonly string[] EraserNames={"Teren","Nazwa heksa","Opis heksa","Ikony","Obiekty","Ograniczenie widoczności","Mgła","Rysik"};
        static EraserTargets EraserFlag(int index){return (EraserTargets)(1<<index);}

        void AttachEraserMenu()
        {
            var button=toolButtons[EditorTool.Eraser];
            var menu=new ContextMenuStrip{Renderer=new FitMenuRenderer(),BackColor=BackColor,ForeColor=ForeColor};
            for(int index=0;index<EraserNames.Length;index++)
            {
                var flag=EraserFlag(index);
                var item=new ToolStripMenuItem(EraserNames[index]){Checked=eraserTargets.HasFlag(flag)};
                item.Click+=(s,e)=>{if(BlockInkCommit())return;eraserTargets^=flag;item.Checked=eraserTargets.HasFlag(flag);UpdateEraserHint();};
                menu.Items.Add(item);
            }
            menu.Closing+=(s,e)=>{if(e.CloseReason==ToolStripDropDownCloseReason.ItemClicked)e.Cancel=true;};
            MenuFence(menu);button.ContextMenuStrip=menu;button.Disposed+=(s,e)=>menu.Dispose();
            button.Paint+=(s,e)=>{
                if(eraserTargets==EraserTargets.Ink)return;
                int size=Math.Max(5,(int)Math.Round(e.Graphics.DpiX/16));
                using(var mark=new SolidBrush(ChoiceStyle.ActiveBorder))e.Graphics.FillEllipse(mark,button.Width-size-5,3,size,size);
            };
            UpdateEraserHint();
        }

        void UpdateEraserHint()
        {
            var button=toolButtons[EditorTool.Eraser];
            string scope=eraserTargets==0?"brak warstw":String.Join(", ",EraserNames.Where((name,index)=>eraserTargets.HasFlag(EraserFlag(index))));
            tips.SetToolTip(button,"Gumka: "+scope+". PPM na przycisku: wybierz zakres.");
            button.AccessibleDescription=scope;button.Invalidate();
        }

        bool GuardEraserLayers(MouseButtons button)
        {
            if(button==MouseButtons.Right)return eraserTargets.HasFlag(EraserTargets.Ink)&&GuardLayer(MapLayer.Ink);
            var targets=new[]{MapLayer.Terrain,MapLayer.Terrain,MapLayer.Terrain,MapLayer.Icons,MapLayer.Objects,MapLayer.Darkness,MapLayer.Fog,MapLayer.Ink};
            for(int i=0;i<targets.Length;i++)if(eraserTargets.HasFlag(EraserFlag(i))&&!GuardLayer(targets[i]))return false;
            return eraserTargets!=0;
        }

        EditAction PrepareSelectiveErase(EraserSelection selection)
        {
            if(selection==null)return null;
            var targets=selection.Targets;
            var action=new EditAction { Label="Gumka",ErasedText=new List<ErasedCellText>() };
            foreach(int index in selection.Cells.OrderBy(i=>i))
            {
                var cell=map.Cells[index];
                if(targets.HasFlag(EraserTargets.Terrain)&&!MapLogic.IsUnpainted(cell))
                    action.Changes.Add(new CellChange{CellId=cell.Id,Before=new TerrainState(cell),After=MapLogic.UnpaintedState});
                bool name=targets.HasFlag(EraserTargets.Name)&&!String.IsNullOrEmpty(cell.Name);
                bool description=targets.HasFlag(EraserTargets.Description)&&(!String.IsNullOrEmpty(cell.Description)||cell.RichDescription!=null);
                if(name||description)action.ErasedText.Add(new ErasedCellText{Id=cell.Id,RemoveName=name,RemoveDescription=description,
                    Name=cell.Name,Description=cell.Description,RichDescription=cell.RichDescription,Format=cell.NotesFormat});
                bool icons=targets.HasFlag(EraserTargets.Icons)&&cell.IconId!=null;
                // As for the revealing fog brush, removing fog cannot cover an active explorer.
                bool fog=targets.HasFlag(EraserTargets.Fog)&&cell.Fog;
                bool dark=targets.HasFlag(EraserTargets.Darkness)&&cell.Darkness;
                if(icons||fog||dark)action.Layers.Add(new LayerChange{CellId=cell.Id,Icons=icons,BeforeIcon=cell.IconId,BeforeEntry=cell.IconEntryId,BeforeName=cell.IconName,
                    Fog=fog,BeforeFog=cell.Fog,Dark=dark,BeforeDark=cell.Darkness});
            }
            if(targets.HasFlag(EraserTargets.Objects))
            {
                var ids=new HashSet<string>(map.Objects.Where(o=>selection.Hits(new PointF(o.X,o.Y))&&!ObjectListMasked(o)).Select(o=>o.Id));
                AddObjectDeletions(action,ids);
            }
            return action.Changes.Count+action.Layers.Count+action.ErasedText.Count+action.Objects.Count==0?null:action;
        }

        void CommitSelectiveErase(EditAction action)
        {
            if(action==null)return;
            ApplyAction(action,true);PushAction(action);MarkDirty();
        }
    }
}
