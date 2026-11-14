using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace HexMapEditor
{
    public sealed class PresetIcon
    {
        public string Id{get;set;} public string Name{get;set;} public string AssetId{get;set;}
        internal IconAsset Asset;
        internal PresetIcon Copy(){return (PresetIcon)MemberwiseClone();}
        internal LibraryIcon Icon(){return new LibraryIcon{Id=Id,Name=Name,Asset=Asset};}
    }
    public sealed class PresetObject
    {
        public string Id{get;set;} public string Name{get;set;} public string AssetId{get;set;}
        public bool LightEnabled{get;set;} public int LightRadius{get;set;}
        internal IconAsset Asset;
        internal PresetObject Copy(){return (PresetObject)MemberwiseClone();}
        internal ObjectTemplate Template(){return new ObjectTemplate{Id=Id,Name=Name,Asset=Asset,LightEnabled=LightEnabled,LightRadius=LightRadius};}
    }
    [Flags]internal enum PresetSections{None=0,Terrains=1,Icons=2,Objects=4,All=7}
    internal static class PresetSets
    {
        internal static List<IconAsset> Assets(IEnumerable<TerrainPreset> presets)
        {
            var assets=presets.SelectMany(p=>p.Icons.Select(i=>i.Asset).Concat(p.Objects.Select(o=>o.Asset))).Where(a=>a!=null).ToList();
            var result=new List<IconAsset>();foreach(var group in assets.GroupBy(a=>a.Id)){var first=group.First();if(group.Any(a=>a.Png!=first.Png||a.Width!=first.Width||a.Height!=first.Height))throw new InvalidDataException("Sprzeczne źródło zestawu startowego.");result.Add(first);}return result;
        }
        internal static void Attach(TerrainPreset p,IEnumerable<IconAsset> assets)
        {
            if(p.Icons==null||p.Objects==null)throw new InvalidDataException("Brak list zestawów startowych presetu.");
            var table=assets.ToDictionary(a=>a.Id);foreach(var i in p.Icons){IconAsset asset;if(i==null||!table.TryGetValue(i.AssetId??"",out asset))throw new InvalidDataException("Brak obrazu ikony presetu.");i.Asset=asset;}
            foreach(var o in p.Objects){IconAsset asset;if(o==null||!table.TryGetValue(o.AssetId??"",out asset))throw new InvalidDataException("Brak obrazu obiektu presetu.");o.Asset=asset;}
        }
        internal static void Validate(TerrainPreset p)
        {
            if(p.Icons==null||p.Objects==null||p.Icons.Count>12||p.Objects.Count>12)throw new InvalidDataException("Limit zestawu startowego:12 ikon i12 obiektów.");
            if(p.Icons.Any(i=>i==null||String.IsNullOrWhiteSpace(i.Id)||i.Id.Length>200||String.IsNullOrWhiteSpace(i.Name)||i.Name.Length>200||i.Asset==null||i.Asset.Id!=i.AssetId)||p.Icons.Select(i=>i.Id).Distinct().Count()!=p.Icons.Count)throw new InvalidDataException("Nieprawidłowe ikony presetu.");
            if(p.Objects.Any(i=>i==null||String.IsNullOrWhiteSpace(i.Id)||i.Id.Length>200||String.IsNullOrWhiteSpace(i.Name)||i.Name.Length>200||i.Asset==null||i.Asset.Id!=i.AssetId||i.LightRadius<0||i.LightRadius>20)||p.Objects.Select(i=>i.Id).Distinct().Count()!=p.Objects.Count)throw new InvalidDataException("Nieprawidłowe obiekty presetu.");
            IconAssets.Validate(Assets(new[]{p}));
        }
        internal static TerrainPreset Merge(TerrainPreset working,TerrainPreset stored,PresetSections sections)
        {
            var result=working.Copy();if(!sections.HasFlag(PresetSections.Terrains))result.Terrains=stored==null?new List<TerrainDefinition>():stored.Terrains.Select(t=>t.Copy()).ToList();
            if(!sections.HasFlag(PresetSections.Icons))result.Icons=stored==null?new List<PresetIcon>():stored.Icons.Select(i=>i.Copy()).ToList();
            if(!sections.HasFlag(PresetSections.Objects))result.Objects=stored==null?new List<PresetObject>():stored.Objects.Select(o=>o.Copy()).ToList();return result;
        }
    }
}
