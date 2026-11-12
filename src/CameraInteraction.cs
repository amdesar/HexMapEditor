using System;
using System.Windows.Forms;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        bool cameraInteraction;
        readonly Timer cameraIdle=new Timer{Interval=120};
        void InitCameraInteraction(){cameraIdle.Tick+=(s,e)=>EndCameraInteraction();Disposed+=(s,e)=>cameraIdle.Dispose();}
        void BeginCameraInteraction(bool wheel)
        {cameraInteraction=true;cameraIdle.Stop();if(wheel)cameraIdle.Start();}
        void EndCameraInteraction(){cameraIdle.Stop();if(!cameraInteraction)return;cameraInteraction=false;canvas.Invalidate();}
    }
}
