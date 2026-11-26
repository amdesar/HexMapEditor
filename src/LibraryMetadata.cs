using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal static class LibraryMetadata
    {
        const string Folder = "Metadata";
        static readonly HashSet<string> organized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static string Catalog(string entryPath)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(entryPath)));
            if (directory.Parent != null && directory.Name.Equals(Folder, StringComparison.OrdinalIgnoreCase))
            {
                LibraryImageStore.RequireOrdinaryPath(directory.FullName);
                return directory.Parent.FullName;
            }
            return directory.FullName;
        }

        static string EnsureFolder(string catalog)
        {
            LibraryImageStore.CheckCatalog(catalog);
            string metadata = Path.Combine(catalog, Folder);
            Directory.CreateDirectory(metadata);
            LibraryImageStore.RequireOrdinaryPath(metadata);
            return metadata;
        }

        internal static string Destination(string catalog, string stem, string current, bool allowSuffix = true)
        { return PresetFiles.Destination(EnsureFolder(catalog), stem, current, allowSuffix); }

        internal static IEnumerable<string> Files(string catalog, string pattern, List<string> errors = null)
        {
            if (!Directory.Exists(catalog)) yield break;
            string metadata = Path.Combine(catalog, Folder);
            string[] current = new string[0];
            try
            {
                if (Directory.Exists(metadata))
                {
                    LibraryImageStore.RequireOrdinaryPath(metadata);
                    current = Directory.GetFiles(metadata, pattern);
                }
            }
            catch (IOException error) { if (errors != null) errors.Add(Folder + ": " + error.Message); }
            catch (UnauthorizedAccessException error) { if (errors != null) errors.Add(Folder + ": " + error.Message); }
            foreach (var path in current.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)) yield return path;
            foreach (var path in Directory.GetFiles(catalog, pattern).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)) yield return path;
        }

        internal static List<T> Read<T>(string catalog, string pattern, Func<string,T> read, Func<T,string> identity,
            Action<T,string> relocated, int limit, List<string> errors, Action<List<T>,T> validate = null)
        {
            catalog = Path.GetFullPath(catalog);
            Directory.CreateDirectory(catalog);
            LibraryImageStore.CheckCatalog(catalog);
            bool organize;
            lock (organized) organize = organized.Add(catalog);
            string metadata = null;
            try { metadata = EnsureFolder(catalog); }
            catch (IOException error) { errors.Add(Folder + ": " + error.Message); }
            catch (UnauthorizedAccessException error) { errors.Add(Folder + ": " + error.Message); }
            var result = new List<T>();
            var ids = new HashSet<string>();
            foreach (var path in Files(catalog, pattern, metadata == null ? null : errors))
            {
                try
                {
                    LibraryImageStore.RequireOrdinaryPath(path);
                    var item = read(path);
                    if (organize && metadata != null && String.Equals(Path.GetDirectoryName(path), catalog, StringComparison.OrdinalIgnoreCase))
                    {
                        string target = Path.Combine(metadata, Path.GetFileName(path));
                        try { File.Move(path, target); relocated(item, target); }
                        catch (IOException error) { errors.Add(Path.GetFileName(path) + ": pozostawiono dotychczasowy plik. " + error.Message); }
                        catch (UnauthorizedAccessException error) { errors.Add(Path.GetFileName(path) + ": pozostawiono dotychczasowy plik. " + error.Message); }
                    }
                    if (result.Count >= limit || !ids.Add(identity(item)))
                        throw new InvalidDataException("Powtórzone ID lub przekroczony limit pozycji biblioteki.");
                    if (validate != null) validate(result, item);
                    result.Add(item);
                }
                catch (Exception error) { errors.Add(Path.GetFileName(path) + ": " + error.Message); }
            }
            return result;
        }

        internal static string ReadIdentity(string path)
        {
            try
            {
                LibraryImageStore.RequireOrdinaryPath(path);
                return PresetCatalog.ReadIdentity(path).Id;
            }
            catch (Exception) { return null; }
        }
    }
}
