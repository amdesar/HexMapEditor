using System;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace HexMapEditor {
    public sealed partial class MainWindow : IMessageFilter {
        int canvasBarrier; bool canvasBarrierSet;
        Point? closingClick; int closingClickTime;
        [DllImport("user32.dll")]internal static extern int GetMessageTime();
        void ArmCanvasInput(){canvasBarrier=Environment.TickCount;canvasBarrierSet=true;requireFreshCanvasDown=true;waitingForMouseRelease=Control.MouseButtons!=MouseButtons.None;}
        internal bool OldCanvasInput(int timestamp){return canvasBarrierSet && unchecked(timestamp-canvasBarrier)<=0;}
        void RememberModalClick(AppearancePoolDialog dialog){closingClick=dialog.ClosingClick;closingClickTime=dialog.ClosingClickTime;}
        internal bool RejectCanvasInput(int timestamp,Point screen){
            if(OldCanvasInput(timestamp))return true;
            int elapsed=unchecked(timestamp-closingClickTime);var distance=SystemInformation.DoubleClickSize;
            return closingClick.HasValue && elapsed>=0 && elapsed<=SystemInformation.DoubleClickTime &&
                Math.Abs(screen.X-closingClick.Value.X)<=distance.Width/2 && Math.Abs(screen.Y-closingClick.Value.Y)<=distance.Height/2;
        }
        public bool PreFilterMessage(ref Message message){
            // Covers owned WinForms dialogs/native text boxes too; WPF has its exact-modifier handler.
            if ((message.Msg==0x100 || message.Msg==0x104) && message.WParam.ToInt64()==(int)Keys.Z && Control.ModifierKeys==(Keys.Control|Keys.Shift)) return true;
            if(!canvas.IsHandleCreated || message.HWnd!=canvas.Handle ||
                (message.Msg!=0x201 && message.Msg!=0x203 && message.Msg!=0x207 && message.Msg!=0x209 && message.Msg!=0x204 && message.Msg!=0x206))return false;
            long point=message.LParam.ToInt64();
            return RejectCanvasInput(GetMessageTime(),canvas.PointToScreen(new Point((short)(point&65535),(short)((point>>16)&65535))));
        }
    }
}
