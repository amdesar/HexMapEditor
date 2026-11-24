using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace HexMapEditor.Tests
{
    internal static class Patch0120Tests
    {
        static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
        static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        static void Refuses<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(message);
        }
        static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static void Channel(string setupPath)
        {
            byte[] bytes = File.ReadAllBytes(setupPath);
            Check(!new ReleaseUpdates("", "").Configured, "Production empty channel should be unconfigured");
            const string project = "fixture/public-releases";
            const string tag = "v0.12.1";
            const string setup = "HexMapEditor-0.12.1-Setup.exe";
            const string zip = "HexMapEditor-0.12.1-Portable.zip";
            string root = "https://github.com/" + project + "/releases/download/" + tag + "/";
            var normal = new Dictionary<string, object> {
                {"name",setup},{"state","uploaded"},{"label","Poprawki błędów"},{"content_type","application/octet-stream"},
                {"browser_download_url",root+setup},{"size",bytes.Length},{"digest","sha256:"+Hash(bytes)}
            };
            var portable = new Dictionary<string, object>(normal);
            portable["name"] = zip;
            portable["browser_download_url"] = root + zip;
            portable["content_type"] = "application/zip";
            portable["label"] = "Nieznana etykieta";
            var release = new Dictionary<string, object> {
                {"tag_name",tag},{"draft",false},{"prerelease",false},{"assets",new[]{normal,portable}}
            };
            var client = new ReleaseUpdates("fixture", "public-releases", (uri, read) => {
                if (uri.Host == "api.github.com")
                {
                    Check(uri.AbsolutePath == "/repos/" + project + "/releases" && uri.Query=="?per_page=30&page=1", "Metadata URL used other source");
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(new[]{release})))) read(stream);
                }
                else
                {
                    Check(uri.AbsoluteUri == root + setup, "Read unexpected asset");
                    using (var stream = new MemoryStream(bytes)) read(stream);
                }
            });
            var current = new Version(0, 12, 0);
            var offer = client.Check(current, true);
            Check(offer.Version == new Version(0,12,1) && offer.Label == "Poprawki błędów" && offer.Setup,"Wrong setup offer");
            Check(offer.Notes.AbsoluteUri == "https://github.com/" + project + "/releases/tag/" + tag, "Bad notes link");
            var portableOffer = client.Check(current, false);
            Check(portableOffer.FileName == zip && portableOffer.Label == null && !portableOffer.Setup, "ZIP choice or optional label");
            Refuses<InvalidOperationException>(()=>client.DownloadSetup(portableOffer,Root),"Portable can launch Setup");
            using (var installer = client.DownloadSetup(offer, Root))
            {
                Check(File.ReadAllBytes(installer.FilePath).SequenceEqual(bytes), "Downloaded bytes changed");
                using (installer.OpenVerified()) { }
                File.WriteAllBytes(installer.FilePath, bytes.Take(bytes.Length - 1).ToArray());
                Refuses<InvalidDataException>(()=>{using(installer.OpenVerified()){}}, "Changed installer accepted");
            }
            string digest = (string)normal["digest"];
            normal["digest"] = "sha256:" + new string('0', 64);
            var wrongHash = client.Check(current, true);
            Refuses<InvalidDataException>(()=>client.DownloadSetup(wrongHash, Root), "Bad digest accepted");
            normal.Remove("digest");
            Refuses<InvalidDataException>(()=>client.Check(current,true), "No digest accepted");
            normal["digest"] = digest;
            release["assets"] = new[] { portable };
            Refuses<InvalidDataException>(()=>client.Check(current,true), "Missing Setup accepted");
            release["assets"] = new[] { portable, normal };
            normal["browser_download_url"] = "https://example.invalid/" + setup;
            Refuses<InvalidDataException>(()=>client.Check(current,true), "Unexpected host accepted");
            normal["browser_download_url"] = root + setup;
            normal["content_type"] = "text/html";
            Refuses<InvalidDataException>(()=>client.Check(current,true), "HTML accepted");
            normal["content_type"] = "application/octet-stream";
            Check(client.Check(new Version(0,12,1),true)==null && client.Check(new Version(0,13,0),true)==null, "Equal or older offered");
            Check(client.Check(new Version(0,9,9),true)!=null, "Versions compared as strings");
            release["prerelease"] = true;
            Check(client.Check(current,true)==null, "Prerelease offered");
            release["prerelease"] = false;
            release["draft"] = true;
            Check(client.Check(current,true)==null, "Draft offered");
            release["draft"] = false;
            release["tag_name"] = "v0.12.1-rc.1";
            Check(client.Check(current,true)==null, "Prerelease tag offered");
            release["tag_name"]=tag;
            var pages=new List<int>();
            var newer=new Dictionary<string,object>(release);
            const string newTag="v0.12.10";
            newer["tag_name"]=newTag;newer["prerelease"]=true;
            var newAssets=new[]{new Dictionary<string,object>(normal),new Dictionary<string,object>(portable)};
            foreach(var asset in newAssets)
            {
                asset["name"]=((string)asset["name"]).Replace("0.12.1","0.12.10");
                asset["browser_download_url"]=((string)asset["browser_download_url"]).Replace("0.12.1","0.12.10");
            }
            newer["assets"]=newAssets;
            var futureDraft=new Dictionary<string,object>(release);
            futureDraft["tag_name"]="v9.0.0";futureDraft["draft"]=true;
            var testing=new ReleaseUpdates("fixture","public-releases",(uri,read)=>{
                Check(uri.Host=="api.github.com"&&uri.AbsolutePath=="/repos/"+project+"/releases","Unexpected paging host");
                bool first=uri.Query=="?per_page=30&page=1";
                Check(first||uri.Query=="?per_page=30&page=2","Unexpected page");
                pages.Add(first?1:2);
                var data=first?Enumerable.Repeat(release,ReleaseUpdates.ReleasesPerPage).ToArray():new[]{newer,futureDraft};
                using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(MapLogic.Serializer().Serialize(data))))read(stream);
            },includePrereleases:true);
            Check(testing.Check(current,true).Version==new Version(0,12,10)&&pages.SequenceEqual(new[]{1,2}),"Test channel missed numeric latest on second page");
            Check(testing.Check(current,false).Download.AbsolutePath.EndsWith("/"+newTag+"/HexMapEditor-0.12.10-Portable.zip"),"Wrong Portable link");
            newer["assets"]=new[]{newAssets[1]};
            Refuses<InvalidDataException>(()=>testing.Check(current,true),"Missing newest Setup looked up to date or fell back");
            var denied = new ReleaseUpdates("fixture", "private", (uri,read)=>{throw new IOException("HTTP 403");});
            Refuses<IOException>(()=>denied.Check(current,true), "Access failure represented as up-to-date");
            Check(ReleaseUpdates.AllowedAssetRedirect(new Uri("https://release-assets.githubusercontent.com/github-production-release-asset-2e65be/fixture?a=b")) &&
                !ReleaseUpdates.AllowedAssetRedirect(new Uri("https://release-assets.githubusercontent.com.attacker.invalid/github-production-release-asset-2e65be/fixture")) &&
                !ReleaseUpdates.AllowedAssetRedirect(new Uri("http://release-assets.githubusercontent.com/github-production-release-asset-2e65be/fixture")),
                "Redirect whitelist");
        }
        static void Data()
        {
            string app = Directory.CreateDirectory(Path.Combine(Root,"Portable")).FullName;
            string profile = Directory.CreateDirectory(Path.Combine(Root,"Profile")).FullName;
            var absent = new AppStorage(app);
            Check(!absent.Installed && absent.DataRoot==app, "Loose executable must be portable");
            string marker = Path.Combine(app,AppStorage.ModeFile);
            File.WriteAllText(marker,AppStorage.PortableMode);
            Check(new AppStorage(app).DataRoot==app, "Portable path");
            File.WriteAllText(marker,AppStorage.InstalledMode);
            File.WriteAllText(Path.Combine(app,AppStorage.LibraryLocationFile),profile);
            var installed = new AppStorage(app);
            Check(installed.Installed && installed.LibraryRoot==profile && installed.DataRoot==Path.Combine(app,"Data"), "Split installed paths");
            File.WriteAllText(marker,"invalid");
            Refuses<InvalidDataException>(()=>new AppStorage(app),"Invalid marker silently used wrong root");
            Check(RecentProjects.Shared.Entries.Length==0, "Fixture acquired MRU entries");
            LibraryPaths.EnsureDirectories();
            string warning;
            var defaults = PresetCatalog.Load(LibraryPaths.Presets,out warning);
            Check(warning.Length==0 && defaults.Count==1 && defaults[0].Id==PresetCatalog.DefaultId &&
                defaults[0].Terrains.Count==6 && defaults[0].Icons.Count==12 && defaults[0].Objects.Count==12, "Default contents");
            Check(!Directory.GetFiles(LibraryPaths.Presets).Any(), "Synthetic factory preset file");
            var icons = IconLibrary.Load(Path.Combine(LibraryPaths.Root,"Icons"),out warning);
            Check(warning.Length==0 && icons.Count==20, "Missing icon starter sets");
            var objects = ObjectLibrary.Load(Path.Combine(LibraryPaths.Root,"Objects"),out warning);
            Check(warning.Length==0 && objects.Count==12, "Missing object starter sets");
            var project = MainWindow.CreateProjectData(8,6,"PointyTop","Mapa do próby paczki",defaults[0]);
            string path = ProjectCreation.CreateInHexmaps(project).Path;
            var reopened = MainWindow.ReadProjectData(path);
            Check(reopened.FormatVersion==12 && reopened.LastSavedAppVersion==ReleaseMetadata.Version && reopened.Cells.Count==48 &&
                RecentProjects.Shared.Entries.Single()==path, "Fresh Map12 round trip");
        }
        static void Set(MainWindow window,string name,object val)
        {
            typeof(MainWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,val);
        }
        static void CloseFlow()
        {
            var project = MainWindow.CreateProjectData(4,4,"PointyTop","Aktualizacja i anulowanie",PresetCatalog.Default());
            var path = ProjectCreation.CreateInHexmaps(project).Path;
            var verified = new VerifiedInstaller(Path.Combine(Root,"unused","setup.exe"),"",0);
            Exception failure = null;
            using (var context = new StartupContext(path))
            using (var timer = new Timer { Interval = 50 })
            {
                timer.Tick += (s,e) => {
                    var editor=context.MainForm as MainWindow;
                    if(editor==null||!editor.Visible)return;
                    timer.Stop();
                    try
                    {
                        Set(editor,"selected",0);
                        typeof(MainWindow).GetMethod("RenameSelectedHex",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(editor,new object[]{"Praca przed aktualizacją"});
                        editor.ConfirmUnsavedForTest=()=>DialogResult.Cancel;
                        Check(!context.FinishForUpdate(editor,verified)&&context.PendingInstaller==null&&!editor.IsDisposed,"Cancel still schedules installer");
                        editor.ConfirmUnsavedForTest=()=>DialogResult.Yes;
                        Check(context.FinishForUpdate(editor,verified)&&editor.IsDisposed,"Successful save did not allow close");
                    }
                    catch(Exception ex) { failure=ex;editor.Dispose();context.ExitThread(); }
                };
                timer.Start(); Application.Run(context);
                if(failure!=null)throw failure;
                Check(ReferenceEquals(context.PendingInstaller,verified),"Installer lost between close and exit");
                Check(MainWindow.ReadProjectData(path).Cells[0].Name=="Praca przed aktualizacją","Pre-upgrade save lost");
            }
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                switch(args[0]) {
                    case "channel":Channel(args[1]);break;
                    case "paths":Data();break;
                    case "close":CloseFlow();break;
                    default:throw new ArgumentException("Pass one explicit Case.");
                }
                Console.WriteLine(args[0]+" PASS");
                return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
        }
    }
}
