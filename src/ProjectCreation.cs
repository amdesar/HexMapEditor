using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class CreatedProject {internal MapData Data;internal string Path;}
    internal static class ProjectCreation
    {
        internal static string StartPath(string name)
        {
            if(String.IsNullOrWhiteSpace(name)||name!=name.Trim()||name.EndsWith(".")||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0)
                throw new IOException("Nazwa projektu zawiera niedozwolone znaki, kropkę lub spację na końcu.");
            string stem=name.EndsWith(".hexmap",StringComparison.OrdinalIgnoreCase)?name.Substring(0,name.Length-7):name;
            if(String.IsNullOrWhiteSpace(stem)||stem.EndsWith(".")||stem.EndsWith(" ")||Regex.IsMatch(stem.Split('.')[0].TrimEnd(' '),@"^(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant))
                throw new IOException("Wybierz poprawną nazwę pliku. Ta nazwa jest pusta lub zastrzeżona przez Windows.");
            string root=Path.GetFullPath(LibraryPaths.Hexmaps),path=Path.GetFullPath(Path.Combine(root,stem+".hexmap"));
            if(!String.Equals(Path.GetDirectoryName(path),root,StringComparison.OrdinalIgnoreCase)||path.Length>=260)
                throw new IOException("Niepoprawna lub zbyt długa ścieżka projektu. Wybierz krótszą nazwę.");
            return path;
        }
        internal static CreatedProject CreateInHexmaps(MapData data)
        {
            string path=StartPath(data.Name);LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory);
            if(Directory.EnumerateFileSystemEntries(LibraryPaths.Hexmaps).Any(p=>String.Equals(Path.GetFileName(p),Path.GetFileName(path),StringComparison.OrdinalIgnoreCase)))
                throw new IOException("Projekt o tej nazwie już istnieje. Zmień nazwę.");
            data.LoadedFileVersion=MainWindow.WriteProjectData(data,path,false);RecentProjects.Shared.Touch(path);
            return new CreatedProject{Data=data,Path=path};
        }
        internal static void ConfigureStart(NewMapDialog dialog,List<ObjectStateDefinition> states,Action<string> report=null)
        {
            dialog.AcceptProject=()=>{try{var data=MainWindow.CreateProjectData(dialog.Columns,dialog.Rows,dialog.Orientation,dialog.ProjectName,dialog.Preset);data.ObjectStates=states.Select(s=>s.Copy()).ToList();dialog.CreatedProject=CreateInHexmaps(data);return true;}catch(Exception ex){string message="Nie można utworzyć projektu.\n\n"+ex.Message;if(report!=null)report(message);else MessageBox.Show(dialog,message,"Nowy projekt",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}};
        }
        internal static Func<SaveFileDialog,IWin32Window,DialogResult> ShowSaveDialog=(dialog,owner)=>dialog.ShowDialog(owner);
        internal static CreatedProject Save(MapData data,string path,Func<string,bool> confirm)
        {
            path=System.IO.Path.GetFullPath(path.EndsWith(".hexmap",StringComparison.OrdinalIgnoreCase)?path:path+".hexmap");
            bool exists=File.Exists(path);if(exists&&!confirm(path))return null;
            data.LoadedFileVersion=MainWindow.WriteProjectData(data,path,exists);RecentProjects.Shared.Touch(path);
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
