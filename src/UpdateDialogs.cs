using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
                using (var dialog = new UpdateOfferDialog(offer))
                    if (Prompt(owner, () => dialog.ShowDialog(owner)) != DialogResult.OK) return;
                if (!installed)
                {
                    OpenReleaseLink(offer.Download);
                    return;
                }
                if (VerifiedInstaller.HasElevatedToken())
                {
                    Tell(owner,"Uruchom edytor ze zwykłymi uprawnieniami i ponów aktualizację.");
                    return;
                }

                downloading = true;
                using (var progress = new Form {
                    Text = "Aktualizacja HexMapEditor", ClientSize = new Size(410, 86),
                    FormBorderStyle = FormBorderStyle.FixedToolWindow, StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false,
                    ControlBox = false
                })
                {
                    var message = new Label { Bounds = new Rectangle(14,12,382,24), Text = "Pobieranie instalatora…" };
                    var bar = new ProgressBar { Bounds = new Rectangle(14,45,382,22) };
                    progress.Controls.AddRange(new Control[] { message, bar });
                    IProgress<int> received = new Progress<int>(value => {
                        if (progress.IsDisposed) return;
                        bar.Value = value;
                        if (value == 100) message.Text = "Sprawdzanie pliku instalatora…";
                    });
                    progress.Show(owner);
                    VerifiedInstaller installer;
                    try { installer = await Task.Run(() => channel.DownloadSetup(offer, downloadRoot, received.Report)); }
                    finally { progress.Close(); }
                    try
                    {
                        if (!CanPrompt(owner)) return;
                        installer.LockForLaunch(); // Verify before asking to save or closing anything.
                        if (finish(owner, installer)) installer = null;
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
            Controls.Add(new Label {
                Text = offer.Setup ? "Program pobierze i sprawdzi instalator. Przed zamknięciem edytora zapiszesz niezapisane mapy i presety albo anulujesz aktualizację."
                    : "Zamknij edytor i zastąp pliki programu z ZIP w obecnym folderze. Zachowaj folder Library, ustawienia, recent-projects.json i mapy.",
                Bounds = new Rectangle(16, 50, 493, 61)
            });
            var notes = new EditorButton { Text = "Otwórz opis wydania", Bounds = new Rectangle(16, 110, 185, 30) };
            notes.Click += (s, e) => {
                try { UpdateController.OpenReleaseLink(offer.Notes); }
                catch (Exception error) { MessageBox.Show(this, error.Message, "Opis wydania", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            var ok = new EditorButton { Text = offer.Setup ? "Zainstaluj aktualizację" : "Otwórz link do ZIP",
                DialogResult = DialogResult.OK, Bounds = new Rectangle(204, 158, 172, 32) };
            var cancel = new EditorButton { Text = "Pomiń", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(390, 158, 119, 32) };
            Controls.AddRange(new Control[] { notes, ok, cancel });
            AcceptButton = CancelButton = cancel;
        }
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
