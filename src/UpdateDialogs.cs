using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HexMapEditor
{
    internal sealed class UpdateController
    {
        readonly ReleaseUpdates channel;
        readonly string downloadRoot;
        readonly Func<Form, bool> currentWindow;
        readonly Func<Form, VerifiedInstaller, bool> finish;
        bool busy;

        internal UpdateController(Func<Form, bool> currentWindow, Func<Form, VerifiedInstaller, bool> finish,
            ReleaseUpdates channel = null, string downloadRoot = null)
        {
            this.currentWindow = currentWindow;
            this.finish = finish;
            this.channel = channel ?? new ReleaseUpdates(UpdateChannelConfiguration.Owner, UpdateChannelConfiguration.Repository,
                includePrereleases: UpdateChannelConfiguration.IncludePrereleases);
            this.downloadRoot = downloadRoot ?? Path.GetTempPath();
        }
        internal async void Check(Form owner, bool manual)
        {
            if (!CanPrompt(owner)) return;
            if (!channel.Configured)
            {
                if (manual) Tell(owner, ReleaseUpdates.Unconfigured);
                return;
            }
            if (busy)
            {
                if (manual) Tell(owner, "Trwa sprawdzanie albo pobieranie aktualizacji.");
                return;
            }
            busy = true;
            bool downloading = false;
            try
            {
                var storage = AppStorage.Current;
                bool installed = storage.Installed;
                var offer = await Task.Run(() => channel.Check(new Version(ReleaseMetadata.Version), installed));
                if (!CanPrompt(owner)) return;
                if (offer == null)
                {
                    if (manual) Tell(owner, "Brak nowszej wersji w tym kanale.");
                    return;
                }
                using(var dialog=new UpdateOfferDialog(offer))
                {
                    if(installed)dialog.Download=(token,progress)=>{
                        downloading=true;
                        if(VerifiedInstaller.HasElevatedToken())
                            throw new InvalidOperationException("Uruchom edytor ze zwykłymi uprawnieniami i ponów aktualizację.");
                        return Task.Run(()=>channel.DownloadSetup(offer,downloadRoot,progress.Report,token));
                    };
                    var result=Prompt(owner,()=>dialog.ShowDialog(owner));
                    if(dialog.Error!=null)throw dialog.Error;
                    if(result!=DialogResult.OK||!CanPrompt(owner))return;
                    if(!installed){OpenReleaseLink(offer.Download);return;}
                    var installer=dialog.TakeInstaller();
                    try
                    {
                        installer.LockForLaunch();
                        if(finish(owner,installer))installer=null;
                    }
                    finally { if (installer != null) installer.Dispose(); }
                }
            }
            catch (Exception error)
            {
                if ((manual || downloading) && CanPrompt(owner))
                    Tell(owner, "Nie można przeprowadzić aktualizacji.\n\n" + error.Message, MessageBoxIcon.Warning);
            }
            finally { busy = false; }
        }
        bool CanPrompt(Form owner)
        {
            if (owner == null || owner.IsDisposed || !currentWindow(owner)) return false;
            var editor = owner as MainWindow;
            return editor == null ? ((StartWindow)owner).ReadyForExternalOpen : editor.ReadyForUpdate;
        }
        static DialogResult Prompt(Form owner, Func<DialogResult> prompt)
        {
            var editor = owner as MainWindow;
            return editor != null ? editor.ShowUpdateDialog(prompt) : ((StartWindow)owner).ShowUpdateDialog(prompt);
        }
        static void Tell(Form owner, string message, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            Prompt(owner, () => MessageBox.Show(owner, message, "Aktualizacja HexMapEditor", MessageBoxButtons.OK, icon));
        }
        internal static void OpenReleaseLink(Uri link)
        {
            using (Process.Start(new ProcessStartInfo(link.AbsoluteUri) { UseShellExecute = true })) { }
        }
    }

    internal sealed class UpdateOfferDialog : DarkForm
    {
        readonly Label message=new Label{Bounds=new Rectangle(16,50,493,64)};
        readonly ProgressBar bar=new ProgressBar{Bounds=new Rectangle(16,114,493,22),Visible=false};
        readonly EditorButton notes,accept,cancel;
        CancellationTokenSource cancellation;
        VerifiedInstaller downloaded;
        internal Func<CancellationToken,IProgress<int>,Task<VerifiedInstaller>> Download;
        internal Exception Error {get;private set;}
        internal UpdateOfferDialog(ReleaseOffer offer)
        {
            Text = "Dostępna aktualizacja";
            ClientSize = new Size(525, 206);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            Controls.Add(new Label {
                Text = "HexMapEditor " + offer.Version.ToString(3) + (offer.Label == null ? "" : " · " + offer.Label),
                AutoSize = true, Location = new Point(16, 16)
            });
            message.Text=offer.Setup ? "Program pobierze i sprawdzi instalator. Przed zamknięciem edytora zapiszesz niezapisane mapy i presety albo anulujesz aktualizację."
                : "Zamknij edytor i zastąp pliki programu z ZIP w obecnym folderze. Zachowaj folder Library, ustawienia, recent-projects.json i mapy.";
            notes = new EditorButton { Text = "Otwórz opis wydania", Bounds = new Rectangle(16, 158, 166, 32) };
            notes.Click += (s, e) => {
                try { UpdateController.OpenReleaseLink(offer.Notes); }
                catch (Exception error) { MessageBox.Show(this, error.Message, "Opis wydania", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            accept = new EditorButton { Text = offer.Setup ? "Zainstaluj aktualizację" : "Otwórz link do ZIP",
                Bounds = new Rectangle(190, 158, 186, 32) };
            ChoiceStyle.Apply(accept,true);accept.Click+=(s,e)=>{if(offer.Setup)StartDownload();else DialogResult=DialogResult.OK;};
            cancel = new EditorButton { Text = "Pomiń", Destructive=true, Bounds = new Rectangle(390, 158, 119, 32) };
            cancel.Click+=(s,e)=>{if(cancellation!=null)CancelDownload();else DialogResult=DialogResult.Cancel;};
            var tips=new ToolTip();
            tips.SetToolTip(notes,"Otwórz opis wersji w przeglądarce");
            tips.SetToolTip(accept,offer.Setup?"Pobierz i sprawdź instalator":"Pobierz ręcznie paczkę Portable");
            tips.SetToolTip(cancel,"Pomiń ofertę lub anuluj bieżące pobieranie");
            Disposed+=(s,e)=>{tips.Dispose();if(downloaded!=null)downloaded.Dispose();};
            Controls.AddRange(new Control[] { message,bar,notes,accept,cancel });
            AcceptButton = CancelButton = cancel;
            cancel.DialogResult=DialogResult.None;
        }
        async void StartDownload()
        {
            if(Download==null||cancellation!=null)return;
            using(var tokenSource=new CancellationTokenSource())
            {
                cancellation=tokenSource;
                accept.Enabled=notes.Enabled=false;cancel.Text="Anuluj";cancel.Focus();bar.Visible=true;
                message.Text="Pobieranie instalatora…";
                IProgress<int> progress=new Progress<int>(value=>{
                    if(IsDisposed||cancellation!=tokenSource||tokenSource.IsCancellationRequested)return;
                    bar.Value=value;if(value==100)message.Text="Sprawdzanie pliku instalatora…";
                });
                try{downloaded=await Download(tokenSource.Token,progress);}
                catch(Exception error){if(!tokenSource.IsCancellationRequested)Error=error;}
                finally{cancellation=null;}
                DialogResult=downloaded!=null&&!tokenSource.IsCancellationRequested?DialogResult.OK:DialogResult.Cancel;
            }
        }
        void CancelDownload()
        {cancellation.Cancel();cancel.Enabled=false;message.Text="Anulowanie pobierania…";}
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if(cancellation!=null){CancelDownload();e.Cancel=true;}
            base.OnFormClosing(e);
        }
        internal VerifiedInstaller TakeInstaller(){var result=downloaded;downloaded=null;return result;}
    }

    public sealed partial class MainWindow
    {
        internal Action UpdateRequested;
        bool closingForUpdate;
        internal void CloseForUpdate()
        {
            closingForUpdate = true;
            try { Close(); }
            finally { closingForUpdate = false; }
        }
        internal bool ReadyForUpdate { get { return ReadyForExternalOpen && !autoSaving && Visible; } }
        internal DialogResult ShowUpdateDialog(Func<DialogResult> show) { return RunModal(show); }
    }
}
