using System.Collections.Generic;
using System.Drawing.Drawing2D;
namespace HexMapEditor
{
    internal static class InkPathCache
    {
        sealed class Entry{internal InkStroke Stroke;internal string Text;internal GraphicsPath Path;internal int Points;}
        static readonly LinkedList<Entry> lru=new LinkedList<Entry>();
        static readonly Dictionary<InkStroke,LinkedListNode<Entry>> entries=new Dictionary<InkStroke,LinkedListNode<Entry>>();
        static int points;
        internal static GraphicsPath Get(InkStroke stroke)
        {
            LinkedListNode<Entry> node;
            if(entries.TryGetValue(stroke,out node))
            {if(node.Value.Text==stroke.Surface){lru.Remove(node);lru.AddLast(node);return node.Value.Path;}Remove(node);}
            var path=InkSurface.DrawingPath(InkSurface.Geometry(stroke));var item=new Entry{Stroke=stroke,Text=stroke.Surface,Path=path,Points=path.PointCount};
            while(lru.Count>0&&(lru.Count>=256||points+item.Points>InkGeometry.MaxPoints))Remove(lru.First);
            entries.Add(stroke,lru.AddLast(item));points+=item.Points;return path;
        }
        static void Remove(LinkedListNode<Entry> node){points-=node.Value.Points;entries.Remove(node.Value.Stroke);lru.Remove(node);node.Value.Path.Dispose();}
        internal static void Clear(){while(lru.Count>0)Remove(lru.First);}
    }
}
