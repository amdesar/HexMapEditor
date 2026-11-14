using System.Linq;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        // Called only on document adoption, never on ordinary draft selection/refresh.
        void AdoptLibraryPanel(MapData document)
        {
            document.Presets=document.Presets.Select(embedded=>{
                var local=libraryPresets.FirstOrDefault(p=>p.Id==embedded.Id);
                return local==null?embedded:local.Copy();
            }).ToList();
            quickSets.Clear();quickPreset=null;
        }
    }
}
