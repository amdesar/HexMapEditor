using System;
using System.Windows.Forms;
using System.Drawing;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        bool cameraInteraction,wheelSeries;
        Point wheelAnchor;PointF wheelBasePan;float wheelBaseScale;int wheelSteps;
        internal int cameraFinalRequests;
        readonly Timer cameraIdle=new Timer{Interval=120};
        void InitCameraInteraction(){cameraIdle.Tick+=(s,e)=>EndCameraInteraction();Disposed+=(s,e)=>cameraIdle.Dispose();}
        void BeginCameraInteraction(bool wheel)
        {cameraInteraction=true;cameraIdle.Stop();if(wheel)cameraIdle.Start();}
        void WheelCamera(MouseEventArgs e)
        {
            if(!cameraInteraction||!wheelSeries||wheelAnchor!=e.Location){wheelSeries=true;wheelAnchor=e.Location;wheelBasePan=pan;wheelBaseScale=scale;wheelSteps=0;}
            BeginCameraInteraction(true);wheelSteps+=e.Delta>0?1:-1;
            double proposed=wheelBaseScale*Math.Pow(1.12,wheelSteps);float next=(float)Math.Max(Math.Min(wheelBaseScale,.01f),Math.Min(Math.Max(wheelBaseScale,128f),proposed));
            if(wheelSteps==0){scale=wheelBaseScale;pan=wheelBasePan;}
            else{float factor=next/wheelBaseScale;pan=new PointF(e.X-(e.X-wheelBasePan.X)*factor,e.Y-(e.Y-wheelBasePan.Y)*factor);scale=next;}
            if(proposed!=next&&(next==Math.Min(wheelBaseScale,.01f)||next==Math.Max(wheelBaseScale,128f))){wheelBaseScale=scale;wheelBasePan=pan;wheelSteps=0;}
            UpdateZoomCaption();canvas.Invalidate();
        }
        void EndCameraInteraction(){cameraIdle.Stop();wheelSeries=false;if(!cameraInteraction)return;cameraInteraction=false;cameraFinalRequests++;canvas.Invalidate();}
    }
}
