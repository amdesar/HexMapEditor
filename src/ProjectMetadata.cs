using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace HexMapEditor
{
    internal static class ProjectMetadata
    {
        internal const string Key = "LastSavedAppVersion";
        static readonly Regex VersionPattern = new Regex(@"\A[0-9]{1,6}(\.[0-9]{1,6}){1,3}\z", RegexOptions.CultureInvariant);
        static readonly Regex Prefix = new Regex(@"\A\s*\{\s*(?:""FormatVersion""\s*:\s*\d+\s*,\s*)?""LastSavedAppVersion""\s*:\s*""([^""]{1,31})""\s*[,}]",
            RegexOptions.CultureInvariant);

        internal static string VersionFrom(IDictionary<string, object> map)
        {
            object value;
            return map.TryGetValue(Key, out value) ? ValidVersion(value as string) : null;
        }

        static string ValidVersion(string value)
        {
            return value != null && value.Length <= 31 && VersionPattern.IsMatch(value) ? value : null;
        }

        internal static string ReadPrefix(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 512))
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 512))
                {
                    var buffer = new char[512];
                    var match = Prefix.Match(new string(buffer, 0, reader.ReadBlock(buffer, 0, buffer.Length)));
                    return match.Success ? ValidVersion(match.Groups[1].Value) : null;
                }
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        internal static string IncompatibleMessage(IDictionary<string, object> map)
        {
            string version = VersionFrom(map);
            return (version == null ? "Wersja HexMapEditor, która wykonała ostatni zapis mapy, jest nieznana." :
                "Ostatni zapis tej mapy wykonano w HexMapEditor " + version + ".")
                + "\nTa wersja programu nie może jej otworzyć.";
        }
    }
}
