using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class PresetRestoreDialog:EditorDialog
    {
        internal PresetSections Sections;
        internal PresetRestoreDialog():base("Przywróć zapisane dane",210)
        {
            int row=0;foreach(var item in new[]{new{Text="Tereny",Flag=PresetSections.Terrains},new{Text="Ikony",Flag=PresetSections.Icons},new{Text="Obiekty",Flag=PresetSections.Objects}}){var s=new SwitchBox{Text=item.Text,AutoSize=true};var flag=item.Flag;s.CheckedChanged+=(sender,e)=>{if(s.Checked)Sections|=flag;else Sections&=~flag;Ok.Enabled=Sections!=0;};Row(row++,"",s,true);}Buttons(3,()=>DialogResult=DialogResult.OK);Ok.Enabled=false;
        }
    }
    public sealed partial class MainWindow
    {
        sealed class PresetWorkspace
        {
            internal List<TerrainPreset> Presets;
            internal readonly Dictionary<string,QuickPair> QuickSets=new Dictionary<string,QuickPair>();
        }
        readonly List<PresetWorkspace> retainedPresets=new List<PresetWorkspace>();
        TerrainPreset presetReview;
        QuickPair reviewQuick;
        bool reviewWasCollapsed;
        int reviewTerrainOffset;

        void RestorePresetSections()
        {
            using(var d=new PresetRestoreDialog())if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK){string error;if(!TryRestorePreset(d.Sections,out error))ShowEditorMessage(error,"Nie można przywrócić presetu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        bool TryRestorePreset(PresetSections sections,out string error)
        {return TryRestorePreset(LibraryPaths.Presets,sections,out error);}
        bool TryRestorePreset(string directory,PresetSections sections,out string error)
        {
            error=null;if(sections==0)return false;try{
                var p=ActivePreset();var known=StoredPreset(p);var saved=p.Id==PresetCatalog.DefaultId?PresetCatalog.Default():PresetCatalog.ReadFile(known!=null&&known.FilePath!=null?known.FilePath:PresetCatalog.FindFile(directory,p.Id));
                CheckTextureBudget(saved.Terrains.SelectMany(t=>t.Variants).Select(f=>f.Graphic));
                // Read and validate everything before touching either the palette or quick lists.
                var beforeQuick=CaptureQuick();bool changed=ChangePalette("Przywrócenie sekcji presetu",()=>{if(sections.HasFlag(PresetSections.Terrains))p.Terrains=saved.Terrains.Select(t=>t.Copy()).ToList();if(sections.HasFlag(PresetSections.Icons))p.Icons=saved.Icons.Select(i=>i.Copy()).ToList();if(sections.HasFlag(PresetSections.Objects))p.Objects=saved.Objects.Select(o=>o.Copy()).ToList();},action=>{action.QuickPresetId=p.Id;action.QuickSections=sections;action.BeforeQuick=beforeQuick;RestoreQuickSections(p,sections);action.AfterQuick=CaptureQuick();});
                if(!changed)RestoreQuickSections(p,sections);UpdatePresetStatus();return true;
            }catch(Exception ex){error=ex.Message;return false;}
        }
        bool WorkingPresetDirty(TerrainPreset p)
        {return WorkingPresetDirty(activeDocument.Panel,p);}
        bool WorkingPresetDirty(PresetWorkspace workspace,TerrainPreset p)
        {
            var stored=StoredPreset(p);var shared=SharedFor(p);
            if (stored == null) return p.Id != PresetCatalog.DefaultId;
            QuickPair quick;
            workspace.QuickSets.TryGetValue(p.Id,out quick);
            var icons=(shared!=null?shared.DraftIcons:quick==null?null:quick.DraftIcons)??p.Icons;
            var objects=(shared!=null?shared.DraftObjects:quick==null?null:quick.DraftObjects)??p.Objects;
            return p.Name != stored.Name || SectionContent(p.Terrains) != SectionContent(stored.Terrains)
                || SectionContent(icons) != SectionContent(stored.Icons)
                || SectionContent(objects) != SectionContent(stored.Objects);
        }
        internal Func<TerrainPreset,DialogResult> ConfirmPresetForTest;
        internal Func<TerrainPreset,bool> SavePresetAllForTest;
        bool ReviewPresetDrafts(IEnumerable<PresetWorkspace> workspaces,DocumentSession closingOne=null,bool mustSave=false)
        {
            var reviewed=new HashSet<SharedPreset>();
            foreach(var workspace in workspaces)
            {
                foreach(var preset in PresetsForSave(workspace).ToArray())
                {
                    var shared=SharedFor(preset);
                    if(shared!=null&&(closingOne!=null&&documents.Any(other=>other!=closingOne&&other.Presets.Any(p=>ReferenceEquals(SharedFor(p),shared)))||!reviewed.Add(shared)))continue;
                    if(!WorkingPresetDirty(workspace,preset))continue;
                    var candidate=ClosingPresetCandidate(workspace,preset);
                    ShowPresetReview(candidate);
                    var answer=ConfirmPresetForTest!=null?ConfirmPresetForTest(preset):ShowEditorMessage(
                        "Preset „"+preset.Name+"” ma niezapisane zmiany. "+(mustSave?"Zapisz przed aktualizacją albo ją anuluj.":"Zapisać przed zamknięciem?"),
                        "Niezapisany preset",mustSave?MessageBoxButtons.OKCancel:MessageBoxButtons.YesNoCancel,MessageBoxIcon.Warning,
                        mustSave?MessageBoxDefaultButton.Button2:MessageBoxDefaultButton.Button1);
                    if(!mustSave&&answer==DialogResult.No)continue;
                    if(answer!=DialogResult.Yes&&(!mustSave||answer!=DialogResult.OK))return false;
                    if(!(SavePresetAllForTest!=null?SavePresetAllForTest(preset):SaveClosingPreset(workspace,preset,candidate,mustSave)))return false;
                }
            }
            return true;
        }
        TerrainPreset ClosingPresetCandidate(PresetWorkspace workspace,TerrainPreset preset)
        {
            var candidate=preset.Copy();
            QuickPair quick;
            if(workspace.QuickSets.TryGetValue(preset.Id,out quick))
            {
                SyncQuickDraft(preset,quick);
                candidate.Icons=quick.Icons.Visible.Select(id=>quick.IconRefs.ContainsKey(id)?quick.IconRefs[id]:libraryIcons.FirstOrDefault(i=>i.Id==id)).Where(i=>i!=null)
                    .Select(i=>new PresetIcon{Id=i.Id,Name=i.Name,AssetId=i.Asset.Id,Asset=i.Asset}).ToList();
                candidate.Objects=quick.Objects.Visible.Select(id=>quick.ObjectRefs.ContainsKey(id)?quick.ObjectRefs[id]:libraryObjects.FirstOrDefault(o=>o.Id==id)).Where(o=>o!=null)
                    .Select(o=>new PresetObject{Id=o.Id,Name=o.Name,AssetId=o.Asset.Id,Asset=o.Asset,LightEnabled=o.LightEnabled,LightRadius=o.LightRadius}).ToList();
            }
            else
            {
                var shared=SharedFor(preset);
                if(shared!=null){candidate.Icons=shared.DraftIcons??candidate.Icons;candidate.Objects=shared.DraftObjects??candidate.Objects;}
            }
            return candidate;
        }
        void ShowPresetReview(TerrainPreset candidate)
        {
            if(presetReview==null){reviewWasCollapsed=leftCollapsed;reviewTerrainOffset=terrainOffset;}
            presetReview=candidate;
            reviewQuick=BuildQuickSet(candidate);
            terrainOffset=0;
            leftCollapsed=false;leftSections.Visible=true;PositionOverlays();
            RefreshPresetPicker();
            PaintBeforeClosePrompt();
        }
        void HidePresetReview()
        {
            if(presetReview==null)return;
            presetReview=null;reviewQuick=null;
            leftCollapsed=reviewWasCollapsed;
            terrainOffset=reviewTerrainOffset;
            leftSections.Visible=HasDocuments;
            PositionOverlays();
            if(HasDocuments){RefreshPresetPicker();RefreshIconPreview();RefreshObjectPreview();}
        }
        bool SaveClosingPreset(PresetWorkspace workspace,TerrainPreset preset,TerrainPreset candidate,bool mustSave)
        {
            using(var dialog=new SavePresetDialog(preset,preset.Id!=PresetCatalog.DefaultId&&StoredPreset(preset)!=null,true,mustSave))
            {
                if(RunModal(()=>dialog.ShowDialog(this))!=DialogResult.OK)return false;
                try
                {
                    QuickPair quick;
                    workspace.QuickSets.TryGetValue(preset.Id,out quick);
                    bool replaceDraft;
                    var file=PrepareWorkingPresetWrite(workspace,preset,candidate,LibraryPaths.Presets,dialog.AsNew,dialog.PresetName,dialog.Sections,out replaceDraft);
                    file.Index=file.Before==null?libraryPresets.Count:libraryPresets.IndexOf(StoredPreset(preset));ApplyPresetLibrary(file,true);
                    int index=workspace.Presets.FindIndex(p=>p.Id==preset.Id);
                    if(replaceDraft)workspace.Presets[index]=file.After.Copy();
                    else if(dialog.AsNew)workspace.Presets.Add(file.After.Copy());
                    else
                    {
                        var shared=SharedFor(file.After);
                        var working=preset.Copy();ApplySavedSections(working,file.After,dialog.Sections);workspace.Presets[index]=working;
                        if(shared!=null){shared.Palette=working.Copy();if(dialog.Sections.HasFlag(PresetSections.Icons)){shared.DraftIcons=null;shared.IconRevision++;}if(dialog.Sections.HasFlag(PresetSections.Objects)){shared.DraftObjects=null;shared.ObjectRevision++;}}
                        if(quick!=null){if(dialog.Sections.HasFlag(PresetSections.Icons))quick.DraftIcons=null;if(dialog.Sections.HasFlag(PresetSections.Objects))quick.DraftObjects=null;}
                    }
                    if(HasDocuments){ResolveWorkingPresets(activeDocument);RefreshPresetPicker();}
                    return true;
                }
                catch(Exception error){ShowEditorMessage(error.Message,"Nie można zapisać presetu",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;}
            }
        }
    }

}
