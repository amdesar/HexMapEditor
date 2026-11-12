using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
namespace HexMapEditor
{
    internal sealed class RecentProjects
    {
        internal static readonly RecentProjects Shared=new RecentProjects(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"recent-projects.json"));
        readonly string path;
        List<string> entries=new List<string>();
        internal string Warning {get;private set;}
        internal string[] Entries {get{return entries.ToArray();}}
        internal RecentProjects(string path){this.path=path;try{if(!File.Exists(path))return;if(new FileInfo(path).Length>262144)throw new InvalidDataException("Historia projektów jest zbyt duża.");var read=MapLogic.Serializer().Deserialize<string[]>(File.ReadAllText(path,Encoding.UTF8));if(read==null||read.Length>30)throw new InvalidDataException("Nieprawidłowa historia projektów.");foreach(var item in read){var normalized=Normalize(item);if(!entries.Contains(normalized,StringComparer.OrdinalIgnoreCase))entries.Add(normalized);}}catch(Exception ex){entries.Clear();Warning="Nie odczytano historii projektów: "+ex.Message;}}
        static string Normalize(string item){if(String.IsNullOrWhiteSpace(item)||item.Length>32767)throw new InvalidDataException("Nieprawidłowa ścieżka projektu.");return Path.GetFullPath(item);}
        internal void Touch(string item)
        {
            string temp=null;
            try
            {
                var normalized=Normalize(item);var next=new[]{normalized}.Concat(entries.Where(p=>!String.Equals(p,normalized,StringComparison.OrdinalIgnoreCase))).Take(30).ToList();
                temp=Path.Combine(Path.GetDirectoryName(path),".recent-"+Guid.NewGuid().ToString("N")+".tmp");var bytes=new UTF8Encoding(false).GetBytes(MapLogic.Serializer().Serialize(next));
                if(bytes.Length>262144)throw new InvalidDataException("Historia ścieżek przekracza 256 KiB; poprzedni plik pozostawiono bez zmian.");
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);temp=null;entries=next;Warning=null;
            }catch(Exception ex){Warning="Nie zapisano historii projektów: "+ex.Message;}
            finally{if(temp!=null)try{File.Delete(temp);}catch{}}
        }
        internal string[] Available(Func<string,bool> exists){return entries.Where(p=>{try{return exists(p);}catch{return false;}}).Take(5).ToArray();}
    }
    // Availability is advisory. Opening always repeats full document validation.
    // One local checker plus up to two remote checkers, reused across refreshes.
    internal static class RecentAvailability
    {
        static readonly object gate=new object();
        static readonly Dictionary<string,bool?> checkedPaths=new Dictionary<string,bool?>(StringComparer.OrdinalIgnoreCase);
        static int remoteWorkers;
        static bool localWorker;
        static readonly HashSet<string> localPending=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static void Refresh(string[] paths,Action changed)
        {
            lock(gate)foreach(var p in paths)if(!checkedPaths.ContainsKey(p)||checkedPaths[p].HasValue)checkedPaths[p]=null;
            lock(gate){foreach(var p in paths.Where(p=>!p.StartsWith(@"\\",StringComparison.Ordinal)))localPending.Add(p);if(!localWorker){localWorker=true;ThreadPool.QueueUserWorkItem(_=>{while(true){string p;lock(gate){p=localPending.FirstOrDefault();if(p==null){localWorker=false;return;}localPending.Remove(p);}bool value=File.Exists(p);lock(gate)checkedPaths[p]=value;changed();}});}}
            lock(gate)while(remoteWorkers<2&&paths.Any(p=>p.StartsWith(@"\\",StringComparison.Ordinal)&&!checkedPaths[p].HasValue))
            {
                // Mark a pending path separately so subsequent refreshes cannot duplicate blocked IO.
                string chosen=paths.FirstOrDefault(p=>p.StartsWith(@"\\",StringComparison.Ordinal)&&!checkedPaths[p].HasValue&&!remotePending.Contains(p));if(chosen==null)break;
                remoteWorkers++;remotePending.Add(chosen);
                ThreadPool.QueueUserWorkItem(_=>{string current=chosen;while(current!=null){bool value=File.Exists(current);lock(gate){checkedPaths[current]=value;remotePending.Remove(current);}changed();lock(gate){current=paths.FirstOrDefault(p=>p.StartsWith(@"\\",StringComparison.Ordinal)&&!checkedPaths[p].HasValue&&!remotePending.Contains(p));if(current!=null)remotePending.Add(current);else remoteWorkers--;}}});
            }
        }
        static readonly HashSet<string> remotePending=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static string[] Visible(string[] paths){lock(gate)return paths.Where(p=>checkedPaths.ContainsKey(p)&&checkedPaths[p]==true).Take(5).ToArray();}
    }
}
