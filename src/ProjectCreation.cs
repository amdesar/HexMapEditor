using System;
using System.IO;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class CreatedProject {internal MapData Data;internal string Path;}
    internal static class ProjectCreation
    {
        internal static Func<SaveFileDialog,IWin32Window,DialogResult> ShowSaveDialog=(dialog,owner)=>dialog.ShowDialog(owner);
        internal static CreatedProject Save(MapData data,string path,Func<string,bool> confirm)
        {
            path=System.IO.Path.GetFullPath(path.EndsWith(".hexmap",StringComparison.OrdinalIgnoreCase)?path:path+".hexmap");
            bool exists=File.Exists(path);if(exists&&!confirm(path))return null;
            MainWindow.WriteProjectData(data,path,exists);RecentProjects.Shared.Touch(path);
            return new CreatedProject{Data=data,Path=path};
        }
        internal static CreatedProject ChooseFile(IWin32Window owner,MapData data)
        {
            try
            {
            LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory);
            string name=data.Name;foreach(char c in System.IO.Path.GetInvalidFileNameChars())name=name.Replace(c,'_');
            using(var dialog=new SaveFileDialog{Title="Zapisz nowy projekt",Filter="Projekt HexMapEditor (*.hexmap)|*.hexmap",DefaultExt="hexmap",AddExtension=true,OverwritePrompt=false,RestoreDirectory=true,InitialDirectory=LibraryPaths.Hexmaps,FileName=name})
            {
                if(ShowSaveDialog(dialog,owner)!=DialogResult.OK)return null;
                return Save(data,dialog.FileName,path=>MessageBox.Show(owner,"Plik już istnieje:\n\n"+path+"\n\nCzy chcesz go zastąpić?","Potwierdź nadpisanie",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes);
            }
            }
            catch(Exception ex){MessageBox.Show(owner,"Nie można zapisać nowego projektu. Bieżący dokument pozostaje bez zmian.\n\n"+ex.Message,"Błąd zapisu",MessageBoxButtons.OK,MessageBoxIcon.Error);return null;}
        }
    }
}
