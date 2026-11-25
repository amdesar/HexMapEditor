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
        readonly ReleaseUpdates channel = new ReleaseUpdates(UpdateChannelConfiguration.Owner, UpdateChannelConfiguration.Repository,
            includePrereleases: UpdateChannelConfiguration.IncludePrereleases);
        readonly Func<Form, bool> currentWindow;
        readonly Func<Form, VerifiedInstaller, bool> finish;
        bool busy;

        internal UpdateController(Func<Form, bool> currentWindow, Func<Form, VerifiedInstaller, bool> finish)
        {
            this.currentWindow = currentWindow;
            this.finish = finish;
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
                bool canLaunch = storage.CanLaunchInstaller;
                var offer = await Task.Run(() => channel.Check(new Version(ReleaseMetadata.Version), installed));
                if (!CanPrompt(owner)) return;
                if (offer == null)
                {
                    if (manual) Tell(owner, "Brak nowszej wersji w tym kanale.");
                    return;
                }
                using (var dialog = new UpdateOfferDialog(offer, canLaunch))
                    if (Prompt(owner, () => dialog.ShowDialog(owner)) != DialogResult.OK) return;
                if (!canLaunch)
                {
                    OpenReleaseLink(offer.Download);
                    return;
                }

                downloading = true;
                using (var progress = new Form {
                    Text = "Aktualizacja HexMapEditor", ClientSize = new Size(360, 65),
                    FormBorderStyle = FormBorderStyle.FixedToolWindow, StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false,
                    ControlBox = false
                })
                {
                    progress.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Pobieranie i sprawdzanie instalatora…", TextAlign = ContentAlignment.MiddleCenter });
                    progress.Show(owner);
                    VerifiedInstaller installer;
                    try { installer = await Task.Run(() => channel.DownloadSetup(offer, Path.GetTempPath())); }
                    finally { progress.Close(); }
                    try
                    {
                        if (!CanPrompt(owner)) return;
                        var decision = Prompt(owner, () => MessageBox.Show(owner,
                            "Instalator został sprawdzony. Zamknąć edytor i rozpocząć aktualizację?",
                            "Aktualizacja HexMapEditor", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2));
                        if (decision == DialogResult.Yes && finish(owner, installer)) installer = null;
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
        internal UpdateOfferDialog(ReleaseOffer offer, bool canLaunchInstaller)
        {
            bool manualSetup = offer.Setup && !canLaunchInstaller;
            Text = "Dostępna aktualizacja";
            ClientSize = new Size(525, manualSetup ? 245 : 206);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            Controls.Add(new Label {
                Text = "HexMapEditor " + offer.Version.ToString(3) + (offer.Label == null ? "" : " · " + offer.Label),
                AutoSize = true, Location = new Point(16, 16)
            });
            Controls.Add(new Label {
                Text = manualSetup ? "Pobierz Setup, porównaj jego SHA-256 i zamknij edytor. Uruchom Setup zwyczajnie, w trybie „Tylko dla mnie”, w bieżącej lokalizacji."
                    : offer.Setup ? "Po pobraniu i sprawdzeniu instalatora możesz zamknąć edytor i zaktualizować bieżącą instalację."
                    : "Zamknij edytor i zastąp pliki programu z ZIP w obecnym folderze. Zachowaj folder Library, ustawienia, recent-projects.json i mapy.",
                Bounds = new Rectangle(16, 50, 493, 61)
            });
            var notes = new EditorButton { Text = "Otwórz opis wydania", Bounds = new Rectangle(16, 110, 185, 30) };
            notes.Click += (s, e) => {
                try { UpdateController.OpenReleaseLink(offer.Notes); }
                catch (Exception error) { MessageBox.Show(this, error.Message, "Opis wydania", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            if (manualSetup)
            {
                Controls.Add(new Label { Text = "SHA-256", Bounds = new Rectangle(16, 149, 66, 25) });
                Controls.Add(new TextBox { Text = offer.Sha256, ReadOnly = true, Font = new Font("Consolas", 8),
                    AccessibleName = "Oczekiwana suma SHA-256 instalatora", Bounds = new Rectangle(82, 147, 427, 25) });
            }
            int y = manualSetup ? 197 : 158;
            var ok = new EditorButton { Text = manualSetup ? "Otwórz link do Setup" : offer.Setup ? "Pobierz instalator" : "Otwórz link do ZIP",
                DialogResult = DialogResult.OK, Bounds = new Rectangle(216, y, 160, 32) };
            var cancel = new EditorButton { Text = "Pomiń", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(390, y, 119, 32) };
            Controls.AddRange(new Control[] { notes, ok, cancel });
            AcceptButton = CancelButton = cancel;
        }
    }

    public sealed partial class MainWindow
    {
        internal Action UpdateRequested;
        internal bool ReadyForUpdate { get { return ReadyForExternalOpen && !autoSaving && Visible; } }
        internal DialogResult ShowUpdateDialog(Func<DialogResult> show) { return RunModal(show); }
    }
}
