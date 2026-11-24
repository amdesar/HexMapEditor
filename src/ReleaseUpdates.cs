using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.Win32.SafeHandles;

namespace HexMapEditor
{
    internal sealed class ReleaseOffer
    {
        internal readonly Version Version;
        internal readonly string Label, FileName, Sha256;
        internal readonly Uri Notes, Download;
        internal readonly long Bytes;
        internal readonly bool Setup;
        internal ReleaseOffer(Version version, string label, string fileName, Uri notes, Uri download, string sha256, long bytes, bool setup)
        {
            Version = version; Label = label; FileName = fileName; Notes = notes; Download = download; Sha256 = sha256; Bytes = bytes; Setup = setup;
        }
    }

    internal sealed class ReleaseUpdates
    {
        const int MetadataLimit = 2 * 1024 * 1024;
        internal const int ReleasesPerPage = 30;
        const int PageLimit = 20;
        const long PackageLimit = 64L * 1024 * 1024;
        internal const string Unconfigured = "Kanał aktualizacji nie jest skonfigurowany.";
        static readonly Regex RepositoryPart = new Regex(@"^[a-zA-Z0-9_-][a-zA-Z0-9_.-]{0,99}\z");
        static readonly Regex VersionTag = new Regex(@"^v?(0|[1-9][0-9]{0,8})\.(0|[1-9][0-9]{0,8})\.(0|[1-9][0-9]{0,8})\z");
        static readonly Regex Digest = new Regex(@"^sha256:[a-fA-F0-9]{64}\z");
        readonly string project;
        readonly Action<Uri, Action<Stream>> receive;
        readonly bool includePrereleases;

        internal ReleaseUpdates(string owner, string repository, Action<Uri, Action<Stream>> receive = null, bool includePrereleases = false)
        {
            if (RepositoryPart.IsMatch(owner) && RepositoryPart.IsMatch(repository))
                project = owner + "/" + repository;
            this.receive = receive ?? Receive;
            this.includePrereleases = includePrereleases;
        }
        internal bool Configured { get { return project != null; } }

        sealed class ReleaseMetadataJson
        {
            public string tag_name { get; set; }
            public bool? draft { get; set; }
            public bool? prerelease { get; set; }
            public ReleaseAssetJson[] assets { get; set; }
        }
        sealed class ReleaseAssetJson
        {
            public string name { get; set; }
            public string label { get; set; }
            public string state { get; set; }
            public string content_type { get; set; }
            public string browser_download_url { get; set; }
            public long size { get; set; }
            public string digest { get; set; }
        }

        internal ReleaseOffer Check(Version current, bool installed)
        {
            if (!Configured) throw new InvalidOperationException(Unconfigured);
            var serializer = new JavaScriptSerializer { MaxJsonLength = MetadataLimit, RecursionLimit = 24 };
            Version latest = current;
            ReleaseMetadataJson release = null;
            for (int page = 1; ; page++)
            {
                if (page > PageLimit) throw new InvalidDataException("Lista wydań przekracza limit sprawdzania kanału aktualizacji.");
                var endpoint = new Uri("https://api.github.com/repos/" + project + "/releases?per_page=" + ReleasesPerPage + "&page=" + page);
                var releases = serializer.Deserialize<ReleaseMetadataJson[]>(ReadMetadata(endpoint));
                if (releases == null || releases.Length > ReleasesPerPage)
                    throw new InvalidDataException("Kanał zwrócił nieprawidłową listę wydań.");
                foreach (var item in releases)
                {
                    if (item == null || item.draft == null || item.prerelease == null)
                        throw new InvalidDataException("Kanał zwrócił niepełne metadane wydania.");
                    Version version;
                    string tag = item.tag_name ?? "";
                    if (item.draft.Value || (item.prerelease.Value && !includePrereleases) ||
                        !VersionTag.IsMatch(tag) || !Version.TryParse(tag.TrimStart('v'), out version) || version <= current) continue;
                    if (version > latest || (version == latest && release.prerelease.Value && !item.prerelease.Value))
                    {
                        latest = version;
                        release = item;
                    }
                }
                if (releases.Length < ReleasesPerPage) break;
            }
            if (release == null) return null;
            return PackageFor(latest, release, installed);
        }

        string ReadMetadata(Uri endpoint)
        {
            string json = null;
            receive(endpoint, source => {
                using (var memory = new MemoryStream())
                {
                    CopyBounded(source, memory, MetadataLimit);
                    json = new UTF8Encoding(false, true).GetString(memory.ToArray());
                }
            });
            return json;
        }

        ReleaseOffer PackageFor(Version latest, ReleaseMetadataJson release, bool installed)
        {
            string tag = release.tag_name;
            string name = "HexMapEditor-" + latest.ToString(3) + (installed ? "-Setup.exe" : "-Portable.zip");
            var matching = (release.assets ?? new ReleaseAssetJson[0]).Where(asset => asset != null && asset.name == name).ToArray();
            if (matching.Length != 1)
                throw new InvalidDataException("W nowszym wydaniu brakuje jednoznacznej paczki " + name + ".");
            var selected = matching[0];
            var download = new Uri("https://github.com/" + project + "/releases/download/" + tag + "/" + name);
            if (selected.state != "uploaded" || selected.size <= 0 || selected.size > PackageLimit ||
                !ValidContentType(selected.content_type, installed) || !SameDownload(selected.browser_download_url, download))
                throw new InvalidDataException("Metadane paczki aktualizacji mają nieprawidłowy typ, rozmiar lub źródło.");
            if (!Digest.IsMatch(selected.digest ?? ""))
                throw new InvalidDataException("Brakuje poprawnej sumy SHA-256 paczki aktualizacji.");
            string label = new[] { "Poprawki błędów", "Nowe funkcje", "Ważna poprawka" }.Contains(selected.label) ? selected.label : null;
            var notes = new Uri("https://github.com/" + project + "/releases/tag/" + tag);
            return new ReleaseOffer(latest, label, name, notes, download, selected.digest.Substring(7), selected.size, installed);
        }

        static bool ValidContentType(string type, bool setup)
        {
            return type == "application/octet-stream" || (setup ? type == "application/x-msdownload" ||
                type == "application/vnd.microsoft.portable-executable" : type == "application/zip" || type == "application/x-zip-compressed");
        }
        static bool SameDownload(string raw, Uri expected)
        {
            Uri url;
            return Uri.TryCreate(raw, UriKind.Absolute, out url) && SecureHost(url, "github.com") &&
                String.IsNullOrEmpty(url.Query) && url.AbsolutePath.Equals(expected.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }
        static bool SecureHost(Uri url, string host)
        {
            return url != null && url.IsAbsoluteUri && url.Scheme == Uri.UriSchemeHttps && url.Port == 443 &&
                url.Host.Equals(host, StringComparison.OrdinalIgnoreCase) && url.UserInfo.Length == 0 && url.Fragment.Length == 0;
        }
        internal static bool AllowedAssetRedirect(Uri url)
        {
            return (SecureHost(url, "release-assets.githubusercontent.com") || SecureHost(url, "objects.githubusercontent.com")) &&
                url.AbsolutePath.StartsWith("/github-production-release-asset-", StringComparison.Ordinal);
        }
        static void Receive(Uri url, Action<Stream> read)
        {
            bool api = SecureHost(url, "api.github.com");
            for (int redirects = 0; ; redirects++)
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.AllowAutoRedirect = false;
                request.UseDefaultCredentials = false;
                request.Credentials = null;
                request.UserAgent = "HexMapEditor/" + ReleaseMetadata.Version;
                request.Timeout = 12000;
                request.ReadWriteTimeout = 12000;
                request.Accept = api ? "application/vnd.github+json" : "application/octet-stream";
                if (api) request.Headers["X-GitHub-Api-Version"] = "2026-03-10";
                try
                {
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (var contents = response.GetResponseStream()) read(contents);
                            return;
                        }
                        Uri next;
                        int code = (int)response.StatusCode;
                        if (api || redirects >= 1 || (code != 302 && code != 303 && code != 307) ||
                            !Uri.TryCreate(response.Headers["Location"], UriKind.Absolute, out next) || !AllowedAssetRedirect(next))
                            throw new IOException("Kanał aktualizacji zwrócił nieoczekiwane przekierowanie.");
                        url = next;
                    }
                }
                catch (WebException error)
                {
                    var response = error.Response as HttpWebResponse;
                    if (response == null) throw new IOException("Nie można połączyć się z kanałem aktualizacji.", error);
                    using (response)
                        throw new IOException("Kanał aktualizacji lub paczka są niedostępne (HTTP " + (int)response.StatusCode + ").", error);
                }
            }
        }
        static void CopyBounded(Stream source, Stream destination, long limit)
        {
            var time = Stopwatch.StartNew();
            byte[] buffer = new byte[64 * 1024];
            long bytes = 0;
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                bytes += count;
                if (time.ElapsedMilliseconds > 90000) throw new IOException("Przekroczono czas transferu aktualizacji.");
                if (bytes > limit) throw new InvalidDataException("Przekroczono oczekiwany rozmiar aktualizacji.");
                destination.Write(buffer, 0, count);
            }
        }
        internal VerifiedInstaller DownloadSetup(ReleaseOffer offer, string temporaryRoot)
        {
            if (!offer.Setup) throw new InvalidOperationException("Kopia przenośna aktualizuje się z ZIP.");
            var directory = Path.Combine(temporaryRoot, "HexMapEditor-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var result = new VerifiedInstaller(Path.Combine(directory, offer.FileName), offer.Sha256, offer.Bytes);
            try
            {
                using (var file = new FileStream(result.FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    receive(offer.Download, source => CopyBounded(source, file, offer.Bytes));
                using (result.OpenVerified()) { }
                // Preserve Windows' internet-origin checks for an unsigned download.
                MarkInternetOrigin(result.FilePath);
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern SafeFileHandle CreateFile(string path, uint access, uint sharing, IntPtr security, uint disposition, uint flags, IntPtr template);

        static void MarkInternetOrigin(string path)
        {
            // Framework FileStream rejects named NTFS streams when passed their path.
            const uint genericWrite = 0x40000000, createAlways = 2, normal = 0x80;
            using (var handle = CreateFile(path + ":Zone.Identifier", genericWrite, 0, IntPtr.Zero, createAlways, normal, IntPtr.Zero))
            {
                if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows nie mógł oznaczyć instalatora jako pobranego z Internetu.");
                using (var zone = new FileStream(handle, FileAccess.Write))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("[ZoneTransfer]\r\nZoneId=3\r\n");
                    zone.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }

    internal sealed class VerifiedInstaller : IDisposable
    {
        internal readonly string FilePath;
        readonly string sha256;
        readonly long bytes;
        bool launched;
        internal VerifiedInstaller(string path, string digest, long expectedBytes)
        {
            FilePath = path; sha256 = digest; bytes = expectedBytes;
        }
        internal FileStream OpenVerified()
        {
            var file = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                using (var hash = SHA256.Create())
                {
                    if (file.Length != bytes || !String.Equals(BitConverter.ToString(hash.ComputeHash(file)).Replace("-", ""), sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Rozmiar lub SHA-256 instalatora nie są zgodne z metadanymi.");
                }
                file.Position = 0;
                using (var header = new BinaryReader(file, Encoding.ASCII, true))
                {
                    if (file.Length < 64 || header.ReadUInt16() != 0x5a4d)
                        throw new InvalidDataException("Paczka nie jest instalatorem Windows EXE.");
                    file.Position = 60;
                    int pe = header.ReadInt32();
                    if (pe < 64 || pe > file.Length - 4)
                        throw new InvalidDataException("Nagłówek instalatora Windows jest nieprawidłowy.");
                    file.Position = pe;
                    if (header.ReadUInt32() != 0x4550)
                        throw new InvalidDataException("Brakuje nagłówka PE instalatora Windows.");
                }
                file.Position = 0;
                return file;
            }
            catch { file.Dispose(); throw; }
        }
        internal void Launch()
        {
            if (!AppStorage.Current.Installed) throw new InvalidOperationException("Instalator można uruchomić tylko z wersji zainstalowanej.");
            string installDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            using (OpenVerified())
            using (var editor = Process.GetCurrentProcess())
            using (var installer = Process.Start(new ProcessStartInfo(FilePath) {
                Arguments = "/WaitForEditorPid=" + editor.Id + " /DIR=\"" + installDirectory + "\" /NORESTART",
                UseShellExecute = true
            }))
            {
                if (installer == null) throw new IOException("Windows nie zwrócił procesu instalatora.");
                launched = true;
            }
        }
        public void Dispose()
        {
            if (launched) return;
            try { File.Delete(FilePath); Directory.Delete(Path.GetDirectoryName(FilePath)); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
