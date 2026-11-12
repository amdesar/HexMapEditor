using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        sealed class PendingInkCommit
        {
            internal MapData Document;internal List<InkStroke> Expected,Before;
            internal long Revision;
            internal InkEraseGesture Preview;
            internal CancellationTokenSource Cancellation=new CancellationTokenSource();
            internal readonly Dictionary<Control,bool> Enabled=new Dictionary<Control,bool>();
        }
        PendingInkCommit pendingInk;
        Panel inkProcessing;
        Label inkProcessingLabel;
        long documentRevision;
        internal InkTiming LastInkCommitTiming;
        bool InkCommitBusy {get{return pendingInk!=null;}}
        const string InkBusyMessage="Przetwarzanie gumki… Poczekaj na wynik albo wybierz Anuluj wymazywanie.";
        bool BlockInkCommit()
        {
            if(!InkCommitBusy)return false;statusLabel.Text=InkBusyMessage;return true;
        }
        void CancelInkCommit()
        {
            if(pendingInk==null)return;pendingInk.Cancellation.Cancel();
            inkProcessingLabel.Text="Anulowanie… Wynik nie zostanie zastosowany.";
            statusLabel.Text=inkProcessingLabel.Text;
        }
        void StartInkCommit(List<InkStroke> before,InkEraseGesture gesture)
        {
            if(pendingInk!=null)throw new InvalidOperationException("Poprzednia operacja gumki nie została zakończona.");
            LastInkCommitTiming=null;var watch=Stopwatch.StartNew();var work=gesture.Capture(before);
            var job=new PendingInkCommit{Document=map,Expected=map.Ink,Before=before,Revision=documentRevision,Preview=gesture};
            pendingInk=job;
            // Count immutable retained versions once. Worker only adds the cost of genuinely new surfaces.
            var retained=new HashSet<InkStroke>(before);
            foreach(var a in undo){if(a.BeforeInk!=null)retained.UnionWith(a.BeforeInk);if(a.AfterInk!=null)retained.UnionWith(a.AfterInk);}
            long points=retained.Sum(s=>(long)InkGeometry.PointCost(s)),characters=retained.Sum(s=>(long)(s.Surface==null?0:s.Surface.Length)),stops=retained.Sum(s=>(long)(s.Gradient==null?0:s.Gradient.Stops.Count));
            double snapshotMs=watch.Elapsed.TotalMilliseconds;
            foreach(var c in new Control[]{canvas,shellToolbar,left,right,leftToggle}){job.Enabled[c]=c.Enabled;c.Enabled=false;}
            if(inkProcessing==null)
            {
                inkProcessing=new Panel{BackColor=Color.FromArgb(44,51,66),Height=44,Width=600};
                inkProcessingLabel=new Label{AutoSize=false,Bounds=new Rectangle(12,12,390,24),Text=InkBusyMessage};
                var cancel=new Button{Text="Anuluj wymazywanie",Bounds=new Rectangle(402,7,184,30),FlatStyle=FlatStyle.Flat};StyleButton(cancel);cancel.Click+=(s,e)=>CancelInkCommit();
                inkProcessing.Controls.AddRange(new Control[]{inkProcessingLabel,cancel});Controls.Add(inkProcessing);
            }
            inkProcessingLabel.Text="Przetwarzanie… Dokładne wymazywanie w tle.";inkProcessing.Location=new Point(canvas.Left+16,canvas.Top+12);inkProcessing.Visible=true;inkProcessing.BringToFront();statusLabel.Text=InkBusyMessage;
            var thread=new Thread(()=>
            {
                InkEraseResult result=null;Exception error=null;
                try
                {
                    result=work.Execute(job.Cancellation.Token);result.Timing.Snapshot=snapshotMs;
                    foreach(int i in Enumerable.Range(0,result.Strokes.Count).Where(i=>result.Changed.Contains(result.OriginalIndices[i])))
                    {var s=result.Strokes[i];points+=InkGeometry.PointCost(s);characters+=s.Surface==null?0:s.Surface.Length;stops+=s.Gradient==null?0:s.Gradient.Stops.Count;}
                    if(points>2000000||characters>64*1024*1024||stops>100000)throw new System.IO.InvalidDataException("Limit historii rysunków: 2 mln punktów / 64 Mi znaków / 100000 punktów gradientów. Gest odrzucono.");
                }catch(Exception ex){error=ex;}
                try{if(!IsDisposed&&IsHandleCreated)BeginInvoke((Action)(()=>CompleteInkCommit(job,result,error)));}catch(InvalidOperationException){}
            }){IsBackground=true,Name="HexMapEditor exact eraser"};
            thread.SetApartmentState(ApartmentState.MTA);thread.Start();
        }
        void CompleteInkCommit(PendingInkCommit job,InkEraseResult result,Exception error)
        {
            if(!ReferenceEquals(job,pendingInk))return;
            var watch=Stopwatch.StartNew();bool current=ReferenceEquals(map,job.Document)&&ReferenceEquals(map.Ink,job.Expected)&&documentRevision==job.Revision;
            bool cancelled=job.Cancellation.IsCancellationRequested;
            pendingInk=null;job.Preview.Dispose();job.Cancellation.Dispose();inkProcessing.Hide();
            foreach(var c in job.Enabled)if(!c.Key.IsDisposed)c.Key.Enabled=c.Value;
            if(current&&!cancelled&&error==null&&result.HasChange)
            {
                var after=new List<InkStroke>();
                for(int i=0;i<result.Strokes.Count;i++)
                {
                    int at=result.OriginalIndices[i];var original=job.Before[at];var s=result.Strokes[i];
                    if(!result.Changed.Contains(at))s=original;
                    else if(s.Gradient!=null){s.Points=original.Points;s.Gradient=original.Gradient;s.ColorTrace=original.ColorTrace;}
                    after.Add(s);
                }
                map.Ink=after;
                var history=Stopwatch.StartNew();PushAction(new EditAction{Label="Gumka",BeforeInk=job.Before,AfterInk=after});
                dirty=savedState.CheckInkSerialized(result.Serialized);UpdateUI();result.Timing.History=history.Elapsed.TotalMilliseconds;
            }
            else UpdateUI();
            if(result!=null){result.Timing.ApplyUI=watch.Elapsed.TotalMilliseconds;LastInkCommitTiming=result.Timing;}
            statusLabel.Text=cancelled?"Wymazywanie anulowane — rysunki bez zmian.":!current?"Stan dokumentu zmienił się — spóźniony wynik gumki odrzucono.":error!=null?"Nie można wymazać: "+error.Message:"Wymazywanie zakończone.";
            canvas.Invalidate();
        }
    }
}
