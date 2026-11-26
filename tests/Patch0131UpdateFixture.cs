using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0131UpdateFixture
    {
        [STAThread]
        static void Main(string[] args)
        {
            if ((args.Length != 2 && (args.Length != 3 || args[2] != "transport")) ||
                !AppStorage.Current.Installed || AppStorage.Current.MachineInstalled)
                throw new InvalidOperationException("This fixture needs an isolated personal installation.");
            var directory = Path.GetFullPath(args[1]);
            var setup = Path.GetFullPath(args[0]);
            if (!setup.StartsWith(directory + "\\", StringComparison.OrdinalIgnoreCase) ||
                !AppDomain.CurrentDomain.BaseDirectory.StartsWith(directory + "\\", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The fixture and package must use the same test area.");
            string name = Path.GetFileName(setup);
            string version = "0.13.1";
            string digest;
            using (var sha = SHA256.Create())
            using (var file = File.OpenRead(setup))
                digest = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
            const string project = "fixture/update";
            string published = "https://github.com/" + project + "/releases/download/v" + version + "/" + name;
            var release = new[] { new {
                tag_name = "v"+version, draft = false, prerelease = true,
                assets = new[] { new { name, label = "", state = "uploaded", content_type = "application/octet-stream",
                    browser_download_url = published, size = new FileInfo(setup).Length, digest = "sha256:" + digest } }
            } };
            var source = new ReleaseUpdates("fixture","update",(uri,read) => {
                if (uri.Host == "api.github.com")
                    using (var metadata = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(release)))) read(metadata);
                else if (uri.AbsoluteUri == published)
                    using (var package = File.OpenRead(setup)) read(package);
                else throw new IOException("Unexpected fixture request.");
            },true);
            if(args.Length==3)
            {
                var offer=source.Check(new Version(ReleaseMetadata.Version),true);
                if(offer==null)throw new InvalidOperationException("Missing fixture offer.");
                using(var installer=source.DownloadSetup(offer,directory)) installer.Launch();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            VerifiedInstaller pending = null;
            using (var guard = new System.Threading.Mutex(false,AppStorage.InstallMutex))
            using (var window = new StartWindow((map,path)=> { }))
            {
                window.Text = "HexMapEditor — kontrolowana aktualizacja";
                var flow = new UpdateController(w => ReferenceEquals(w,window),(w,installer) => {
                    w.Close();
                    if(!w.IsDisposed) return false;
                    pending = installer;
                    return true;
                },source,directory);
                window.UpdateRequested = () => flow.Check(window,true);
                Application.Run(window);
            }
            if (pending != null) using (pending) pending.Launch();
        }
    }
}
