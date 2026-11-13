using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
namespace HexMapEditor
{
    // Opt-in, local, bounded diagnostic. No titles, project paths, content, or exception messages.
    internal static class StartupTrace
    {
        internal static Action<string> Sink;
        static readonly Queue<string> entries=new Queue<string>();
        internal static string[] Entries{get{return entries.ToArray();}}
        internal static void Write(string kind,Form form=null,Exception error=null)
        {
            string line=DateTime.UtcNow.ToString("O")+" "+kind+(form==null?"":" "+form.GetType().Name+" visible="+form.Visible+" disposed="+form.IsDisposed+" handle="+form.IsHandleCreated)+(error==null?"":" exception="+error.GetType().Name+" hresult="+error.HResult);
            if(error!=null&&error.StackTrace!=null)line+="\n"+error.StackTrace;entries.Enqueue(line);while(entries.Count>128)entries.Dequeue();if(Sink!=null)Sink(line);
            string path=Environment.GetEnvironmentVariable("HEXMAPEDITOR_START_TRACE");if(String.IsNullOrWhiteSpace(path))return;
            try{if(File.Exists(path)&&new FileInfo(path).Length>65536)File.WriteAllText(path,"");File.AppendAllText(path,line+Environment.NewLine);}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(NotSupportedException){}catch(System.Security.SecurityException){}
        }
    }
}
