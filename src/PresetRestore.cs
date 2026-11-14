using System;
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
        void RestorePresetSections()
        {
            using(var d=new PresetRestoreDialog())if(RunModal(()=>d.ShowDialog(this))==DialogResult.OK){string error;if(!TryRestorePreset(d.Sections,out error))ShowEditorMessage(error,"Nie można przywrócić presetu",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        bool TryRestorePreset(PresetSections sections,out string error)
        {return TryRestorePreset(LibraryPaths.Presets,sections,out error);}
        bool TryRestorePreset(string directory,PresetSections sections,out string error)
        {
            error=null;if(sections==0)return false;try{
                var p=ActivePreset();var known=libraryPresets.FirstOrDefault(s=>s.Id==p.Id);var saved=p.Id==PresetCatalog.DefaultId?PresetCatalog.Default():PresetCatalog.ReadFile(known!=null&&known.FilePath!=null?known.FilePath:PresetCatalog.FindFile(directory,p.Id));
                CheckTextureBudget(saved.Terrains.SelectMany(t=>t.Variants).Select(f=>f.Graphic));
                // Read and validate everything before touching either the palette or quick lists.
                var beforeQuick=CaptureQuick();bool changed=ChangePalette("Przywrócenie sekcji presetu",()=>{if(sections.HasFlag(PresetSections.Terrains))p.Terrains=saved.Terrains.Select(t=>t.Copy()).ToList();if(sections.HasFlag(PresetSections.Icons))p.Icons=saved.Icons.Select(i=>i.Copy()).ToList();if(sections.HasFlag(PresetSections.Objects))p.Objects=saved.Objects.Select(o=>o.Copy()).ToList();},action=>{action.QuickPresetId=p.Id;action.BeforeQuick=beforeQuick;RestoreQuickSections(p,sections);action.AfterQuick=CaptureQuick();});
                if(!changed)RestoreQuickSections(p,sections);UpdatePresetStatus();return true;
            }catch(Exception ex){error=ex.Message;return false;}
        }
        bool WorkingPresetDirty(TerrainPreset p)
        {if(p.Id==PresetCatalog.DefaultId)return false;var stored=libraryPresets.FirstOrDefault(s=>s.Id==p.Id);return stored==null||PresetContent(p)!=PresetContent(stored);}
        internal Func<TerrainPreset,DialogResult> ConfirmPresetForTest;
        internal Func<TerrainPreset,bool> SavePresetAllForTest;
        bool ConfirmPresetsBeforeClose()
        {
            foreach(string id in map.Presets.Where(WorkingPresetDirty).Select(p=>p.Id).ToArray()){
                SelectPreset(id);var p=ActivePreset();var answer=ConfirmPresetForTest!=null?ConfirmPresetForTest(p):ShowEditorMessage("Preset „"+p.Name+"” ma niezapisane zmiany. Zapisać wszystkie trzy sekcje przed zamknięciem?","Niezapisany preset",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Warning);
                if(answer==DialogResult.Cancel)return false;if(answer==DialogResult.Yes&&!(SavePresetAllForTest!=null?SavePresetAllForTest(p):SavePresetCore(true)))return false;
            }return true;
        }
    }
}
