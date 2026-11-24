using System;
using System.Collections.Generic;
using System.Drawing;
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
        PresetReviewPanel presetReview;
        bool reviewWasCollapsed;

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
        bool ReviewPresetDrafts(IEnumerable<PresetWorkspace> workspaces,DocumentSession closingOne=null)
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
                    var answer=ConfirmPresetForTest!=null?ConfirmPresetForTest(preset):ShowEditorMessage("Preset „"+preset.Name+"” ma niezapisane zmiany. Zapisać przed zamknięciem?","Niezapisany preset",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Warning);
                    if(answer!=DialogResult.No&&(answer!=DialogResult.Yes||!(SavePresetAllForTest!=null?SavePresetAllForTest(preset):SaveClosingPreset(workspace,preset,candidate))))return false;
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
            if(presetReview==null)reviewWasCollapsed=leftCollapsed;
            if(presetReview!=null)presetReview.Dispose();
            leftCollapsed=false;PositionOverlays();tabBar.Invalidate();
            presetReview=new PresetReviewPanel(candidate,tips){Dock=DockStyle.Fill};
            left.Controls.Add(presetReview);presetReview.BringToFront();
            PaintBeforeClosePrompt();
        }
        void HidePresetReview()
        {
            if(presetReview==null)return;
            presetReview.Dispose();presetReview=null;
            leftCollapsed=reviewWasCollapsed;PositionOverlays();tabBar.Invalidate();
        }
        bool SaveClosingPreset(PresetWorkspace workspace,TerrainPreset preset,TerrainPreset candidate)
        {
            using(var dialog=new SavePresetDialog(preset,preset.Id!=PresetCatalog.DefaultId&&StoredPreset(preset)!=null,true))
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

    internal sealed class PresetReviewPanel:Panel
    {
        readonly FlowLayoutPanel rows;
        internal PresetReviewPanel(TerrainPreset preset,ToolTip tips)
        {
            BackColor=Color.FromArgb(34,38,48);AccessibleName="Roboczy preset: "+preset.Name;
            rows=new BufferedFlowPanel{Dock=DockStyle.Fill,AutoScroll=true,WrapContents=false,FlowDirection=FlowDirection.TopDown,Padding=new Padding(8)};
            Controls.Add(rows);
            var title=AddRow(preset.Name,null,tips);
            title.Font=new Font(Font,FontStyle.Bold);
            title.Disposed+=(sender,args)=>title.Font.Dispose();
            AddRow("Tereny ("+preset.Terrains.Count+")",null,tips);
            foreach(var terrain in preset.Terrains)
            {
                var swatch=new Bitmap(20,16);
                using(var g=Graphics.FromImage(swatch))g.Clear(ColorTranslator.FromHtml(terrain.Color));
                AddRow(terrain.Name,swatch,tips);
            }
            AddRow("Ikony ("+preset.Icons.Count+")",null,tips);
            foreach(var icon in preset.Icons)AddRow(icon.Name,Thumbnail(icon.Asset),tips);
            AddRow("Obiekty ("+preset.Objects.Count+")",null,tips);
            foreach(var item in preset.Objects)AddRow(item.Name,Thumbnail(item.Asset),tips);
        }
        Label AddRow(string text,Image thumbnail,ToolTip tips)
        {
            var label=new Label{Text=text,AutoEllipsis=true,Width=224,Height=24,TextAlign=ContentAlignment.MiddleLeft,
                ImageAlign=ContentAlignment.MiddleLeft,Padding=new Padding(thumbnail==null?0:25,0,0,0),Image=thumbnail};
            if(thumbnail!=null)
            {
                label.Image=null;
                label.Paint+=(s,e)=>e.Graphics.DrawImageUnscaled(thumbnail,0,(label.Height-thumbnail.Height)/2);
                label.Disposed+=(s,e)=>thumbnail.Dispose();
            }
            tips.SetToolTip(label,text);
            rows.Controls.Add(label);
            return label;
        }
        static Image Thumbnail(IconAsset asset)
        {
            if(asset==null)return null;
            using(var image=IconAssets.Decode(asset))
            {
                var thumbnail=new Bitmap(20,20);
                using(var g=Graphics.FromImage(thumbnail))g.DrawImage(image,0,0,20,20);
                return thumbnail;
            }
        }
    }
}
