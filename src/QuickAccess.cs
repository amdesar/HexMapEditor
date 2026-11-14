using System;
using System.Collections.Generic;
using System.Linq;
namespace HexMapEditor
{
    // Session ordering, separate from library ownership and document dirty state.
    internal sealed class QuickAccess
    {
        readonly List<string> entries=new List<string>();
        internal string[] Visible{get{return entries.Take(12).ToArray();}}
        internal string[] All{get{return entries.ToArray();}}
        internal void Restore(IEnumerable<string> ids){entries.Clear();foreach(string id in ids.Where(x=>!String.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).Take(12))entries.Add(id);}
        internal void RestoreHistory(IEnumerable<string> ids){entries.Clear();entries.AddRange(ids.Where(x=>!String.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).Take(24));}
        internal void Added(string id){if(String.IsNullOrEmpty(id))return;entries.Remove(id);entries.Insert(0,id);Trim();}
        internal void Used(string id){if(String.IsNullOrEmpty(id))return;int at=entries.IndexOf(id);if(at>=0&&at<12)return;Added(id);}
        internal bool Remove(string id){return entries.Remove(id);}
        void Trim(){if(entries.Count>24)entries.RemoveRange(24,entries.Count-24);}
    }
}
