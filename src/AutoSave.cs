using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class AutoSaveSchedule
    {
        internal const long Interval=5*60*1000;
        internal Func<long> Clock=()=>Stopwatch.GetTimestamp()*1000/Stopwatch.Frequency;
        internal long Due;
        internal void Reset(){Due=Clock()+Interval;}
        internal bool BeginCycle()
        {
            long now=Clock();
            if(now<Due)return false;
            Due+=((now-Due)/Interval+1)*Interval;
            return true;
        }
    }
    public sealed partial class MainWindow
    {
        const string AutoSaveBusyMessage="Trwa autozapis. Poczekaj na zakończenie; zamknięcie okna anuluje oczekujące przygotowanie.";
        readonly System.Windows.Forms.Timer autoTimer=new System.Windows.Forms.Timer{Interval=1000};
        readonly AutoSaveSchedule autoSchedule=new AutoSaveSchedule();
        bool autoSaving { get { return autoJob!=null; } }
        readonly Queue<DocumentSession> autoBatch=new Queue<DocumentSession>();
        readonly List<string> autoFailures=new List<string>();
        bool autoCycle;
        int autoWritten;
        sealed class AutoSaveJob
        {
            internal DocumentSession Document;internal MapData Snapshot;internal long Revision;internal string Path;internal ProjectFileVersion Expected;
            internal CancellationTokenSource Cancel=new CancellationTokenSource();internal ManualResetEvent Permit=new ManualResetEvent(false);internal bool Authorized;
        }
        AutoSaveJob autoJob;
        internal int AutoSaveCount;internal string AutoSaveError;
        internal Action AutoPreparedForTest;
        static bool SameProjectPath(string a,string b){return !String.IsNullOrEmpty(a)&&!String.IsNullOrEmpty(b)&&String.Equals(System.IO.Path.GetFullPath(a),System.IO.Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);}
        void InitAutoSave()
        {
            autoSchedule.Reset();autoTimer.Tick+=(s,e)=>PollAutoSave();autoTimer.Start();
            Disposed+=(s,e)=>{autoTimer.Dispose();if(autoJob!=null)autoJob.Cancel.Cancel();};
        }
        bool IsAutoSaving(DocumentSession document){return autoJob!=null&&ReferenceEquals(autoJob.Document,document);}
        bool AutoSafe(){return !autoSaving&&!reviewingExit&&!returningToWelcome&&!InkCommitBusy&&modalDepth==0&&gestureButton==MouseButtons.None&&inkBefore==null&&!rightResizing&&propertyEditKey==null&&!TextInputFocused(this);}
        static bool CanAutoSave(DocumentSession document)
        {return document.Path!=null&&document.Unsaved&&!document.AutoSaveConflict;}
        void PollAutoSave()
        {
            if(IsDisposed||!IsHandleCreated||documents.Count==0||!AutoSafe())return;
            if(!autoCycle)
            {
                if(!autoSchedule.BeginCycle())return;
                foreach(var document in documents)
                    if(CanAutoSave(document))autoBatch.Enqueue(document);
                if(autoBatch.Count==0)return;
                autoCycle=true;autoWritten=0;autoFailures.Clear();
            }
            while(autoBatch.Count>0)
            {
                var document=autoBatch.Dequeue();
                if(!documents.Contains(document)||!CanAutoSave(document))continue;
                if(document.Version==null)
                {
                    RecordAutoFailure(document,new ProjectConflictException("Brak potwierdzonej wersji pliku. Użyj Zapisz jako."));
                    continue;
                }
                StartAutoSave(document);return;
            }
            ReportAutoCycle();
        }
        void StartAutoSave(DocumentSession document)
        {
            var job=new AutoSaveJob{Document=document,Revision=document.Revision,Path=document.Path,Expected=document.Version};
            autoJob=job;
            if(ReferenceEquals(document,activeDocument))UpdateCommandState();
            try{job.Snapshot=ProjectSnapshot.Capture(document.Map,PresetsForSave(document));}
            catch(Exception error){FinishAutoSave(job,null,null,error);return;}
            var thread=new Thread(()=>
            {
                byte[] bytes=null;SavedMapState saved=null;ProjectFileVersion version=null;Exception failure=null;
                try
                {
                    job.Cancel.Token.ThrowIfCancellationRequested();bytes=PrepareProjectBytes(job.Snapshot);saved=new SavedMapState();saved.Capture(job.Snapshot);job.Cancel.Token.ThrowIfCancellationRequested();
                    QueueAuto(job,()=>{if(AutoPreparedForTest!=null)AutoPreparedForTest();job.Authorized=AutoMatches(job)&&!job.Cancel.IsCancellationRequested;job.Permit.Set();});
                    WaitHandle.WaitAny(new[]{job.Permit,job.Cancel.Token.WaitHandle});job.Cancel.Token.ThrowIfCancellationRequested();
                    if(!job.Authorized)throw new OperationCanceledException("Stan dokumentu zmienił się przed zapisem.");
                    version=WritePreparedProject(bytes,job.Path,true,job.Expected);
                }
                catch(Exception error){failure=error;}
                finally{if(!QueueAuto(job,()=>FinishAutoSave(job,saved,version,failure))){job.Permit.Dispose();job.Cancel.Dispose();}}
            }){IsBackground=true,Name="HexMapEditor autosave"};thread.SetApartmentState(ApartmentState.MTA);thread.Start();
        }
        bool QueueAuto(AutoSaveJob job,Action action){try{if(!IsDisposed&&IsHandleCreated){BeginInvoke(action);return true;}}catch(InvalidOperationException){}job.Cancel.Cancel();return false;}
        bool AutoMatches(AutoSaveJob job)
        {return !IsDisposed&&ReferenceEquals(autoJob,job)&&documents.Contains(job.Document)&&job.Document.Revision==job.Revision&&SameProjectPath(job.Document.Path,job.Path);}
        void FinishAutoSave(AutoSaveJob job,SavedMapState saved,ProjectFileVersion version,Exception error)
        {
            if(!ReferenceEquals(autoJob,job))return;
            bool matches=AutoMatches(job);autoJob=null;var document=job.Document;
            if(version!=null&&SameProjectPath(document.Path,job.Path))document.Version=version;
            if(version!=null)
            {
                document.Saved=saved;
                document.Dirty=!matches&&saved.CheckAll(document.Map);
                document.FirstSave=false;document.AutoSaveConflict=false;
                autoWritten++;AutoSaveCount++;RecentProjects.Shared.Touch(document.Path);
            }
            else RecordAutoFailure(document,error);
            UpdateTabTitle(document);UpdateCaption();
            job.Permit.Dispose();job.Cancel.Dispose();
            if(autoBatch.Count==0)ReportAutoCycle();
        }
        void RecordAutoFailure(DocumentSession document,Exception error)
        {
            if(error is ProjectConflictException)document.AutoSaveConflict=true;
            autoFailures.Add(ProjectTitle(document.Path,document.Map.Name)+": "+(error==null?"Stan dokumentu zmienił się.":error.Message));
        }
        void ReportAutoCycle()
        {
            autoCycle=false;
            AutoSaveError=autoFailures.Count==0?null:String.Join(Environment.NewLine,autoFailures);
            if(autoWritten==0&&AutoSaveError==null)return;
            statusLabel.Text="Autozapis: zapisano "+autoWritten+", problemy: "+autoFailures.Count+".";
            if(AutoSaveError!=null)tips.SetToolTip(statusLabel,statusLabel.Text+"\n"+AutoSaveError);
        }
        void CancelAutoSave(){autoBatch.Clear();if(autoJob!=null){autoJob.Cancel.Cancel();statusLabel.Text="Anulowanie autozapisu… Poczekaj na zakończenie, a następnie ponów polecenie.";}}
    }
}
