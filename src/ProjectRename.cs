using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal static class ProjectRename
    {
        internal static string Move(string previous,string name,ProjectFileVersion expected)
        {
            if(expected==null)throw new IOException("Brak potwierdzonej wersji pliku. Otwórz poprawny plik lub użyj Zapisz jako.");
            previous=Path.GetFullPath(previous);string next=ProjectCreation.PathForName(Path.GetDirectoryName(previous),name);
            ProjectFileVersion.Check(previous,expected);if(String.Equals(previous,next,StringComparison.Ordinal))return previous;
            bool same=String.Equals(previous,next,StringComparison.OrdinalIgnoreCase);
            if(Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(previous)).Any(p=>String.Equals(Path.GetFileName(p),Path.GetFileName(next),StringComparison.OrdinalIgnoreCase)&&!String.Equals(p,previous,StringComparison.OrdinalIgnoreCase)))throw new IOException("Projekt o tej nazwie już istnieje. Zmień nazwę.");
            string held=same?Path.Combine(Path.GetDirectoryName(previous),".rename-"+Guid.NewGuid().ToString("N")+".tmp"):next;
            File.Move(previous,held);
            try{ProjectFileVersion.Check(held,expected);if(same)File.Move(held,next);return next;}
            catch(Exception error){try{File.Move(held,previous);}catch(Exception rollback){throw new IOException("Nie można przywrócić poprzedniej ścieżki. Dane zachowano: "+held+". "+rollback.Message,error);}throw;}
        }
    }
    public sealed partial class MainWindow
    {
        internal bool TryRenameProject(string name,out string error)
        {
            error=null;if(BlockInkCommit()){error=autoSaving?AutoSaveBusyMessage:InkBusyMessage;return false;}if(String.IsNullOrEmpty(currentPath)){error="Najpierw zapisz nową mapę.";return false;}
            EndCanvasGesture(true);if(BlockInkCommit()){error=InkBusyMessage;return false;}
            try{string old=currentPath,next=ProjectRename.Move(old,name,fileVersion);if(next==old)return true;currentPath=next;documentRevision++;RecentProjects.Shared.Renamed(old,next);UpdateCaption();statusLabel.Text=RecentProjects.Shared.Warning??"Zmieniono nazwę pliku. Niezapisane zmiany pozostają w pamięci.";return true;}
            catch(Exception ex){error=ex.Message;return false;}
        }
        void RenameProject()
        {
            if(BlockInkCommit()||String.IsNullOrEmpty(currentPath))return;
            using(var d=new RenameProjectDialog(Path.GetFileName(currentPath),name=>{string error;if(TryRenameProject(name,out error))return true;ShowEditorMessage(error,"Nie można zmienić nazwy pliku",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}))RunModal(()=>d.ShowDialog(this));
        }
    }
    internal sealed class RenameProjectDialog:EditorDialog
    {
        readonly TextBox name=new DarkTextBox{MaxLength=200};
        internal RenameProjectDialog(string initial,Func<string,bool> accept):base("Zmień nazwę pliku",170)
        {
            string display=initial.EndsWith(".hexmap",StringComparison.OrdinalIgnoreCase)?initial.Substring(0,initial.Length-7):initial;
            name.Text=display;Row(0,"Obecna nazwa:",new Label{Text=display,AutoEllipsis=true,TextAlign=System.Drawing.ContentAlignment.MiddleLeft},true);Row(1,"Nowa nazwa:",name,true);
            name.Dock=DockStyle.None;name.Anchor=AnchorStyles.Left|AnchorStyles.Right;name.Height=name.PreferredHeight;
            foreach(var label in Layout.Controls.OfType<Label>().Where(c=>Layout.GetColumn(c)==0))label.TextAlign=System.Drawing.ContentAlignment.MiddleRight;
            name.Margin=new Padding(3,Math.Max(0,(38-name.PreferredHeight)/2),3,0);
            Buttons(2,()=>{if(accept(name.Text))DialogResult=DialogResult.OK;});
        }
    }
}
