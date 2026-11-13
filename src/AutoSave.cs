using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
namespace HexMapEditor
{
    internal sealed class AutoSaveSchedule
    {
        internal const long Interval=5*60*1000;
        internal Func<long> Clock=()=>Stopwatch.GetTimestamp()*1000/Stopwatch.Frequency;
        internal long Due;internal bool Pending,Suspended;
        internal void Reset(){Due=Clock()+Interval;Pending=false;Suspended=false;}
        internal bool Ready(bool path,bool dirty,bool safe){if(Suspended)return false;if(!path){Pending=false;return false;}if(Clock()>=Due)Pending=true;return Pending&&dirty&&safe;}
        internal void Failed(bool conflict){Due=Clock()+Interval;Pending=false;Suspended=conflict;}
    }
    public sealed partial class MainWindow
    {
        const string AutoSaveBusyMessage="Trwa autozapis. Poczekaj na zakończenie; zamknięcie okna anuluje oczekujące przygotowanie.";
        readonly AutoSaveSchedule autoSchedule=new AutoSaveSchedule();
        readonly System.Windows.Forms.Timer autoTimer=new System.Windows.Forms.Timer{Interval=1000};
        ProjectFileVersion fileVersion;
        bool autoSaving;
        sealed class AutoSaveJob
        {
            internal MapData Document,Snapshot;internal long Revision;internal string Path;internal ProjectFileVersion Expected;
            internal CancellationTokenSource Cancel=new CancellationTokenSource();internal ManualResetEvent Permit=new ManualResetEvent(false);internal bool Authorized;
        }
        AutoSaveJob autoJob;
        internal int AutoSaveCount;internal string AutoSaveError;
        internal Action AutoPreparedForTest;
        static bool SameProjectPath(string a,string b){return !String.IsNullOrEmpty(a)&&!String.IsNullOrEmpty(b)&&String.Equals(System.IO.Path.GetFullPath(a),System.IO.Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);}
        void InitAutoSave()
        {
            autoTimer.Tick+=(s,e)=>PollAutoSave();autoSchedule.Reset();autoTimer.Start();
            Disposed+=(s,e)=>{autoTimer.Dispose();if(autoJob!=null)autoJob.Cancel.Cancel();};
        }
        void ResetSaveClock(ProjectFileVersion version){fileVersion=version;autoSchedule.Reset();AutoSaveError=null;}
        bool AutoSafe(){return !autoSaving&&!InkCommitBusy&&modalDepth==0&&gestureButton==MouseButtons.None&&inkBefore==null&&!rightResizing&&propertyEditKey==null&&!TextInputFocused(this);}
        void PollAutoSave()
        {
            if(IsDisposed||!IsHandleCreated||!autoSchedule.Ready(!String.IsNullOrEmpty(currentPath),HasUnsavedChanges,AutoSafe()))return;
            if(fileVersion==null){autoSchedule.Failed(true);AutoSaveError="Brak potwierdzonej wersji pliku. Użyj ręcznego Zapisz jako.";statusLabel.Text=AutoSaveError;return;}
            var job=new AutoSaveJob{Document=map,Revision=documentRevision,Path=currentPath,Expected=fileVersion};autoSaving=true;autoJob=job;AutoSaveError=null;
            try{job.Snapshot=ProjectSnapshot.Capture(map);}catch(Exception error){FinishAutoSave(job,null,null,error);return;}
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
        bool AutoMatches(AutoSaveJob job){return !IsDisposed&&ReferenceEquals(autoJob,job)&&ReferenceEquals(map,job.Document)&&documentRevision==job.Revision&&SameProjectPath(currentPath,job.Path);}
        void FinishAutoSave(AutoSaveJob job,SavedMapState saved,ProjectFileVersion version,Exception error)
        {
            if(!ReferenceEquals(autoJob,job))return;bool matches=AutoMatches(job);autoSaving=false;autoJob=null;
            if(version!=null&&matches){savedState=saved;savedSnapshot=null;dirty=false;needsFirstSave=false;ResetSaveClock(version);AutoSaveCount++;UpdateCaption();RecentProjects.Shared.Touch(currentPath);statusLabel.Text="Zapisano automatycznie";}
            else{autoSchedule.Failed(error is ProjectConflictException);AutoSaveError=error==null?"Stan dokumentu zmienił się — autozapis odrzucono.":error is OperationCanceledException?"Autozapis anulowany. Niezapisane zmiany pozostają w pamięci.":"Nie można zapisać automatycznie. "+error.Message;statusLabel.Text=AutoSaveError;}
            job.Permit.Dispose();job.Cancel.Dispose();
        }
        void CancelAutoSave(){if(autoJob!=null){autoJob.Cancel.Cancel();statusLabel.Text="Anulowanie autozapisu… Poczekaj na zakończenie, a następnie ponów polecenie.";}}
    }
}
