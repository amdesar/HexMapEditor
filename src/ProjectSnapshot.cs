using System;
using System.Collections.Generic;
using System.Linq;
namespace HexMapEditor
{
    internal static class ProjectSnapshot
    {
        internal static MapData Capture(MapData map)
        {
            var sources=new Dictionary<GraphicSource,GraphicSource>();var graphics=new Dictionary<GraphicDefinition,GraphicDefinition>();
            Func<GraphicSource,GraphicSource> source=s=>{if(s==null)return null;GraphicSource copy;if(!sources.TryGetValue(s,out copy))sources.Add(s,copy=s.SnapshotCopy());return copy;};
            Func<GraphicDefinition,GraphicDefinition> graphic=g=>{if(g==null)return null;GraphicDefinition copy;if(!graphics.TryGetValue(g,out copy)){copy=g.Copy();copy.Source=source(g.Source);graphics.Add(g,copy);}return copy;};
            Func<FillVariant,FillVariant> fill=f=>{if(f==null)return null;var copy=f.Copy();copy.Graphic=graphic(f.Graphic);return copy;};
            var copyMap=new MapData{FormatVersion=map.FormatVersion,Name=map.Name,Columns=map.Columns,Rows=map.Rows,Orientation=map.Orientation,LabelMode=map.LabelMode,RowParity=map.RowParity,ColumnParity=map.ColumnParity,ActivePresetId=map.ActivePresetId};
            copyMap.Cells=map.Cells.Select(c=>{var copy=c.SnapshotCopy();copy.Texture=graphic(c.Texture);copy.FogFill=fill(c.FogFill);copy.DarkFill=fill(c.DarkFill);return copy;}).ToList();
            copyMap.Presets=map.Presets.Select(p=>{var copy=p.Copy();foreach(var t in copy.Terrains){t.Texture=graphic(t.Texture);t.Variants=t.Variants.Select(fill).ToList();}return copy;}).ToList();
            copyMap.IconAssets=map.IconAssets.Select(a=>new IconAsset{Id=a.Id,Name=a.Name,Png=a.Png,Width=a.Width,Height=a.Height}).ToList();
            copyMap.TerrainAssets=map.TerrainAssets.Select(graphic).ToList();copyMap.GraphicSources=map.GraphicSources.Select(source).ToList();
            copyMap.FogPalette=map.FogPalette.Select(fill).ToList();copyMap.DarkPalette=map.DarkPalette.Select(fill).ToList();
            copyMap.Ink=map.Ink.Select(s=>new InkStroke{Id=s.Id,Width=s.Width,Color=s.Color,Surface=s.Surface,CycleWorld=s.CycleWorld,Gradient=s.Gradient==null?null:s.Gradient.Copy(),Points=s.Points.Select(p=>new InkPoint{X=p.X,Y=p.Y,Distance=p.Distance}).ToList()}).ToList();
            copyMap.Background=map.Background==null?null:map.Background.Copy();if(copyMap.Background!=null)copyMap.Background.Source=source(map.Background.Source);
            copyMap.Objects=map.Objects.Select(o=>o.Copy()).ToList();copyMap.Groups=map.Groups.Select(g=>g.Copy()).ToList();copyMap.ObjectStates=map.ObjectStates.Select(s=>s.Copy()).ToList();copyMap.ExcludedStateIds=new List<string>(map.ExcludedStateIds);return copyMap;
        }
    }
}
