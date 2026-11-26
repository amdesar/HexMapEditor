using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        internal static ProcessStartInfo LibraryFolderStartInfo()
        {return new ProcessStartInfo(LibraryPaths.Root){UseShellExecute=true,Verb="open"};}
        void OpenLibraryFolder()
        {OpenLibraryFolder(null);}
        void OpenImportFolder()
        {
            try { using (Process.Start(new ProcessStartInfo(FolderImport.Current.EnsureFolders()) { UseShellExecute = true, Verb = "open" })) { } }
            catch (Exception ex) { ShowEditorMessage(ex.Message,"Nie można otworzyć folderu importu",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        }
        void OpenImportQueue()
        {
            using (var dialog = new FolderImportDialog(FolderImport.Current))
            {
                RunModal(() => dialog.ShowDialog(this));
                if (!dialog.Saved) return;
            }
            terrainGraphics = null;
            backgroundLibrary = null;
            LoadIcons();
            LoadObjects();
        }
        internal static IEnumerable<KeyValuePair<string,string>> LibraryFolders()
        {
            string[] names={"Tereny","Presety terenów","Grafiki terenów","Ikony","Obiekty","Stany obiektów","Tła","Mapy","Eksport obrazów","Eksport tekstu"};
            string[] paths={"Terrains",@"Terrains\Presets",@"Terrains\Graphics","Icons","Objects",@"Objects\States","Backgrounds","Hexmaps",@"Exports\Images",@"Exports\Text"};
            for(int i=0;i<names.Length;i++)yield return new KeyValuePair<string,string>(names[i],paths[i]);
        }
        void OpenLibraryFolder(string relative)
        {
            string folder=relative==null?LibraryPaths.Root:relative=="Hexmaps"?LibraryPaths.Hexmaps:Path.Combine(LibraryPaths.Root,relative);
            try{LibraryPaths.EnsureDirectories();using(var opened=Process.Start(new ProcessStartInfo(folder){UseShellExecute=true,Verb="open"})){} }
            catch(Exception ex){ShowEditorMessage("Nie można otworzyć folderu biblioteki:\n"+LibraryPaths.Root+"\n\n"+ex.Message,"Biblioteka",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
    }
}

