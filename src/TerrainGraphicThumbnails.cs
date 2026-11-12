using System;
using System.Collections.Generic;
using System.Drawing;
namespace HexMapEditor
{
    // Only small derived previews, never full source payloads. IDs include both transforms and source hash.
    internal static class TerrainGraphicThumbnails
    {
        sealed class Entry {internal string Key;internal Bitmap Image;}
        static readonly LinkedList<Entry> cache=new LinkedList<Entry>();
        internal static Bitmap Get(GraphicDefinition asset,bool flat)
        {
            string key=asset.Id+":"+flat;var node=cache.First;
            while(node!=null&&node.Value.Key!=key)node=node.Next;
            if(node!=null){cache.Remove(node);cache.AddLast(node);return node.Value.Image;}
            TerrainTextures.Validate(asset);
            var result=new Bitmap(88,72);
            try
            {
                using(var source=GraphicSources.Decode(asset.Source))using(var g=Graphics.FromImage(result))using(var mask=TerrainTextures.Mask(new PointF(44,36),33,flat))
                {g.Clear(Color.FromArgb(34,38,48));g.SetClip(mask);g.Clear(Color.SlateGray);CropMath.Draw(g,source,flat?asset.Flat:asset.Pointy,new RectangleF(11,3,66,66));g.ResetClip();g.DrawPath(Pens.Gray,mask);}
                if(cache.Count>=32){cache.First.Value.Image.Dispose();cache.RemoveFirst();}
                cache.AddLast(new Entry{Key=key,Image=result});return result;
            }catch{result.Dispose();throw;}
        }
        internal static void Clear(){foreach(var entry in cache)entry.Image.Dispose();cache.Clear();}
    }
}
