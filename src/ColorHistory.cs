using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class ColorDraftHistory
    {
        readonly List<string> states=new List<string>();readonly Func<string> capture;readonly Action<string> restore;int cursor;bool applying;
        internal ColorDraftHistory(Func<string> capture,Action<string> restore){this.capture=capture;this.restore=restore;states.Add(capture());}
        internal bool Applying{get{return applying;}}
        internal void Commit(){if(applying)return;string state=capture();if(state==states[cursor])return;states.RemoveRange(cursor+1,states.Count-cursor-1);states.Add(state);cursor++;if(states.Count>101){states.RemoveAt(0);cursor--;}}
        internal bool Move(bool redo){if(redo?cursor==states.Count-1:cursor==0)return false;cursor+=redo?1:-1;applying=true;try{restore(states[cursor]);}finally{applying=false;}return true;}
        internal int Count{get{return states.Count-1;}}
    }
    internal sealed class ColorHexBox:DarkTextBox
    {
        internal Action Commit;
        protected override bool ProcessCmdKey(ref Message msg,Keys keys){if(keys==Keys.Enter&&Commit!=null){Commit();return true;}return base.ProcessCmdKey(ref msg,keys);}
    }
    internal sealed class PaintDraftState
    {
        public string Uniform{get;set;}public GradientDefinition Gradient{get;set;}public bool GradientMode{get;set;}
    }
}
