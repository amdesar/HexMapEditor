using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        sealed class SharedPreset
        {
            internal TerrainPreset Palette;
            internal List<PresetIcon> DraftIcons;
            internal List<PresetObject> DraftObjects;
            internal long IconRevision,ObjectRevision;
        }
        readonly Dictionary<string,SharedPreset> sharedPresets=new Dictionary<string,SharedPreset>(StringComparer.OrdinalIgnoreCase);
        SharedPreset SharedFor(TerrainPreset preset)
        {
            SharedPreset shared;
            return preset!=null&&preset.FilePath!=null&&sharedPresets.TryGetValue(Path.GetFullPath(preset.FilePath),out shared)
                &&shared.Palette.Id==preset.Id?shared:null;
        }
        TerrainPreset BindPreset(TerrainPreset preset)
        {
            if(preset.FilePath==null)return preset.Copy();
            var shared=SharedFor(preset);
            if(shared==null)
            {
                shared=new SharedPreset{Palette=preset.Copy()};
                sharedPresets[Path.GetFullPath(preset.FilePath)]=shared;
            }
            return shared.Palette.Copy();
        }
        void AdoptLibraryPanel(DocumentSession document)
        {
            document.Presets=document.Presets.Select(embedded=>{
                var local=embedded.FilePath!=null?embedded:libraryPresets.FirstOrDefault(p=>p.Id==embedded.Id);
                return local==null?embedded:BindPreset(local);
            }).ToList();
        }
        IEnumerable<TerrainPreset> PresetsForSave(DocumentSession document)
        {return PresetsForSave(document.Panel);}
        IEnumerable<TerrainPreset> PresetsForSave(PresetWorkspace workspace)
        {
            return workspace.Presets.Select(p=>{
                var shared=SharedFor(p);
                return shared==null?p:shared.Palette;
            });
        }
        void ResolveWorkingPresets(DocumentSession document)
        {
            for(int i=0;i<document.Presets.Count;i++)
            {
                var shared=SharedFor(document.Presets[i]);
                if(shared!=null)document.Presets[i]=shared.Palette.Copy();
            }
        }
        IEnumerable<TerrainPreset> ChangedPresets(PaletteState before,PaletteState after)
        {
            if(before==null||after==null)yield break;
            foreach(var preset in after.Presets)
            {
                var old=before.Presets.FirstOrDefault(p=>p.Id==preset.Id);
                if(old!=null&&json.Serialize(old)!=json.Serialize(preset))yield return preset;
            }
        }
        void CheckSharedPaletteHistory(PaletteState from,PaletteState target)
        {
            foreach(var changed in ChangedPresets(from,target))
            {
                var expected=from.Presets.First(p=>p.Id==changed.Id);
                var shared=SharedFor(expected);
                if(shared!=null&&json.Serialize(expected)!=json.Serialize(shared.Palette))
                    throw new IOException("Roboczy preset „"+shared.Palette.Name+"” zmienił się w innej karcie. Zachowano obie historie.");
            }
        }
        void PublishPaletteChanges(PaletteState before,PaletteState after)
        {
            foreach(var preset in ChangedPresets(before,after))
            {
                var shared=SharedFor(preset);
                if(shared==null)continue;
                var copy=preset.Copy();
                copy.FilePath=shared.Palette.FilePath;
                shared.Palette=copy;
            }
        }
        void TrackPresetFile(TerrainPreset previous,TerrainPreset saved)
        {
            if(saved==null)return;
            var shared=SharedFor(previous)??SharedFor(saved);
            if(shared==null){BindPreset(saved);return;}
            sharedPresets[Path.GetFullPath(saved.FilePath)]=shared;
            var palette=shared.Palette.Copy();
            palette.FilePath=saved.FilePath;
            shared.Palette=palette;
        }
        TerrainPreset StoredPreset(TerrainPreset working)
        {
            return libraryPresets.FirstOrDefault(p=>p.Id==working.Id&&
                (working.Id==PresetCatalog.DefaultId||SameProjectPath(p.FilePath,working.FilePath)||
                    p.FilePath!=null&&SharedFor(p)!=null&&ReferenceEquals(SharedFor(p),SharedFor(working))));
        }
        PresetFileChange PrepareWorkingPresetWrite(PresetWorkspace workspace,TerrainPreset current,TerrainPreset candidate,
            string directory,bool asNew,string name,PresetSections sections,out bool replaceDraft)
        {
            if(sections==0)throw new InvalidDataException("Wybierz sekcję presetu.");
            var stored=StoredPreset(current);
            replaceDraft=asNew&&current.Id!=PresetCatalog.DefaultId&&stored==null&&sections==PresetSections.All;
            if(asNew)
            {
                if(!replaceDraft&&workspace.Presets.Count>=100)throw new InvalidDataException("Limit 100 roboczych presetów w projekcie.");
                if(!replaceDraft)candidate.Id=Guid.NewGuid().ToString("N");
                candidate.Name=(name??"").Trim();
                if(workspace.Presets.Any(p=>p.Id!=current.Id&&PresetCatalog.SameName(p.Name,candidate.Name)))
                    throw new InvalidDataException("Nazwa jest już używana przez inny preset projektu.");
            }
            else if(stored==null)throw new IOException("Preset nie ma zapisanego pliku. Zapisz jako nowy.");
            var latest=asNew?null:PresetCatalog.ReadFile(stored.FilePath);
            return PresetCatalog.PrepareWrite(PresetSets.Merge(candidate,latest,sections),directory,!asNew,asNew?null:stored);
        }
        static void ApplySavedSections(TerrainPreset working,TerrainPreset saved,PresetSections sections)
        {
            working.FilePath=saved.FilePath;
            if(sections.HasFlag(PresetSections.Icons))working.Icons=saved.Icons.Select(i=>i.Copy()).ToList();
            if(sections.HasFlag(PresetSections.Objects))working.Objects=saved.Objects.Select(o=>o.Copy()).ToList();
        }
    }
}
