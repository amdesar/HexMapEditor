using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class GraphicCropSaveDialog:EditorDialog
    {
        readonly RadioButton overwrite=new RadioButton{Text="Nadpisz dotychczasową pozycję",AutoSize=true},create=new RadioButton{Text="Utwórz nową",AutoSize=true};
        readonly ComboBox target=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Name"};
        readonly TextBox name=new DarkTextBox{MaxLength=200};
        internal bool AsNew{get{return create.Checked;}}
        internal string NewName{get{return name.Text.Trim();}}
        internal TerrainGraphic Target{get{return target.SelectedItem as TerrainGraphic;}}
        internal string NameForSave{get{return AsNew||target.Items.Count==1?NewName:Target.Name;}}
        internal GraphicCropSaveDialog(TerrainGraphic[] targets,string proposed):base("Zapisz zmieniony kadr",265)
        {
            foreach(var entry in targets)target.Items.Add(entry);if(targets.Length==1)target.SelectedIndex=0;
            Row(0,"",overwrite,true);Row(1,"Pozycja",target,true);Row(2,"",create,true);Row(3,"Nowa nazwa",name,true);name.Text=proposed;
            Buttons(4,()=>{if(AsNew?NewName.Length>0:Target!=null)DialogResult=DialogResult.OK;});
            Action state=()=>{name.Enabled=AsNew;target.Enabled=!AsNew;Ok.Enabled=AsNew?NewName.Length>0:Target!=null;};
            overwrite.CheckedChanged+=(s,e)=>state();create.CheckedChanged+=(s,e)=>state();target.SelectedIndexChanged+=(s,e)=>state();name.TextChanged+=(s,e)=>state();overwrite.Checked=true;state();
        }
    }
    public sealed partial class MainWindow
    {
        sealed class GraphicOrigin{internal string Id;}
        readonly ConditionalWeakTable<GraphicDefinition,GraphicOrigin> graphicOrigins=new ConditionalWeakTable<GraphicDefinition,GraphicOrigin>();
        GraphicDefinition RememberGraphic(TerrainGraphic entry){var copy=entry.Asset.DocumentCopy();graphicOrigins.Add(copy,new GraphicOrigin{Id=entry.Id});return copy;}
        TerrainGraphic SaveCroppedGraphic(IWin32Window owner,TerrainGraphic[] targets,GraphicDefinition changed,string proposed)
        {
            using(var dialog=new GraphicCropSaveDialog(targets,proposed))while(dialog.ShowDialog(owner)==DialogResult.OK){TerrainGraphic saved;string error;
                if(TryCommitGraphicCrop(dialog.Target,changed,dialog.AsNew,dialog.NameForSave,LibraryPaths.TerrainGraphics,out saved,out error))return saved;
                MessageBox.Show(owner,error,"Nie można zapisać kadru",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }return null;
        }
        bool TryCommitGraphicCrop(TerrainGraphic before,GraphicDefinition changed,bool asNew,string name,string directory,out TerrainGraphic saved,out string error)
        {
            saved=null;error=null;try{
                name=(name??"").Trim();
                if(String.IsNullOrWhiteSpace(name)||name.Length>200)throw new IOException("Podaj nazwę grafiki (1–200 znaków).");
                if(terrainGraphics.Any(t=>(asNew||before==null||t.Id!=before.Id)&&PresetCatalog.SameName(t.Name,name)))throw new IOException("Grafika o tej nazwie już istnieje. Podaj inną nazwę.");
                if(!asNew&&(before==null||!terrainGraphics.Any(t=>t.Id==before.Id&&t.Path==before.Path)))throw new IOException("Pozycja biblioteki zmieniła się. Wybierz ją ponownie.");
                var next=TerrainGraphicLibrary.Prepare(directory,asNew?null:before,name,changed.DocumentCopy(),terrainGraphics,false);
                ChangeTerrainGraphic(asNew?null:before,next);saved=next;return true;
            }catch(Exception ex){error=ex.Message;return false;}
        }
    }
}
