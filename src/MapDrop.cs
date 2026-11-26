using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal static class MapDrop
    {
        internal const int Limit=8;
        internal static void Attach(Control target,Func<bool> ready,Action<string[]> receive)
        {
            target.AllowDrop=true;
            DragEventHandler offer=(s,e)=>{
                e.Effect=ready()&&e.Data!=null&&e.Data.GetDataPresent(DataFormats.FileDrop,false)
                    ?e.AllowedEffect&DragDropEffects.Copy:DragDropEffects.None;
            };
            target.DragEnter+=offer;
            target.DragOver+=offer;
            target.DragDrop+=(s,e)=>{
                offer(s,e);
                if(e.Effect!=DragDropEffects.Copy)return;
                string[] paths=e.Data.GetData(DataFormats.FileDrop,false) as string[];
                if(paths==null)return;
                // Release the shell's drag callback before opening documents or showing dialogs.
                target.BeginInvoke((Action)(()=>{if(!target.IsDisposed&&ready())receive(paths);}));
            };
        }

        internal static List<string> Validate(string[] paths,string library,List<string> errors)
        {
            var valid=new List<string>();
            if(paths.Length>Limit){errors.Add("Upuść najwyżej "+Limit+" plików .hexmap naraz.");return valid;}
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string import=Path.Combine(library,"Import")+Path.DirectorySeparatorChar;
            foreach(string input in paths)
            {
                try
                {
                    string path=Path.GetFullPath(input);
                    if(path.StartsWith(import,StringComparison.OrdinalIgnoreCase)||InLibraryImport(path))
                        throw new IOException("Pliki z Library\\Import służą kolejce importu.");
                    if(!String.Equals(Path.GetExtension(path),".hexmap",StringComparison.OrdinalIgnoreCase)||Directory.Exists(path))
                        throw new IOException("Wybierz plik .hexmap.");
                    if(!File.Exists(path))throw new IOException("Plik jest niedostępny.");
                    if(seen.Add(path))valid.Add(path);
                }
                catch(Exception error)
                {
                    if(!(error is IOException||error is ArgumentException||error is NotSupportedException||error is UnauthorizedAccessException))throw;
                    errors.Add((input??"Plik")+Environment.NewLine+error.Message);
                }
            }
            return valid;
        }
        static bool InLibraryImport(string path)
        {
            for(var directory=new FileInfo(path).Directory;directory!=null;directory=directory.Parent)
                if(directory.Name.Equals("Import",StringComparison.OrdinalIgnoreCase)&&directory.Parent!=null&&
                    directory.Parent.Name.Equals("Library",StringComparison.OrdinalIgnoreCase))return true;
            return false;
        }
    }

    public sealed partial class MainWindow
    {
        bool ReadyForDrop(){return ReadyForExternalOpen&&!autoSaving&&!canvas.Capture&&!dragging&&!panGesture;}
        void AttachMapDrop()
        {
            foreach(var target in new Control[]{this,canvas,shellToolbar,quickStrip})
                MapDrop.Attach(target,ReadyForDrop,OpenDroppedProjects);
        }
        internal void OpenDroppedProjects(string[] paths)
        {
            if(!ReadyForDrop())return;
            var errors=new List<string>();
            foreach(string path in MapDrop.Validate(paths,LibraryPaths.Root,errors))
            {
                if(!ReadyForDrop()){errors.Add("Dokończ bieżącą operację przed otwarciem pozostałych map.");break;}
                string error;bool migrated;
                if(!TryLoadProject(path,out error,out migrated))
                    errors.Add(Path.GetFileName(path)+Environment.NewLine+error);
            }
            if(errors.Count!=0)ShowEditorMessage(String.Join("\n\n",errors),"Otwieranie upuszczonych map",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }
}
