using System;
using System.Diagnostics;
using System.Windows.Forms;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        internal static ProcessStartInfo LibraryFolderStartInfo()
        {return new ProcessStartInfo(LibraryPaths.Root){UseShellExecute=true,Verb="open"};}
        void OpenLibraryFolder()
        {
            try{LibraryPaths.EnsureDirectories(AppDomain.CurrentDomain.BaseDirectory);using(var opened=Process.Start(LibraryFolderStartInfo())){}}
            catch(Exception ex){ShowEditorMessage("Nie można otworzyć folderu biblioteki:\n"+LibraryPaths.Root+"\n\n"+ex.Message,"Biblioteka",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}

