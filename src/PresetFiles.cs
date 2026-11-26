using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HexMapEditor
{
    internal static class PresetFiles
    {
        internal const string Extension = ".json";
        internal static string Stem(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            string stem = new string((name ?? "").Normalize().Select(c => invalid.Contains(c) || Char.IsControl(c) ? '_' : c).ToArray()).Trim().TrimEnd('.', ' ');
            if (stem.Length == 0) stem = "Preset";
            string device = stem.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (new[] { "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$" }.Contains(device) || (device.Length == 4 && (device.StartsWith("COM") || device.StartsWith("LPT")) && "123456789¹²³".Contains(device[3]))) stem = "_" + stem;
            return stem;
        }
        internal static string Shorten(string value, int length)
        { if (value.Length <= length) return value; if (Char.IsHighSurrogate(value[length - 1])) length--; return value.Substring(0, length).TrimEnd('.', ' '); }
        internal static string Destination(string directory, string name, string current, bool allowSuffix = true)
        {
            directory = Path.GetFullPath(directory); if (current != null) current = Path.GetFullPath(current); string stem = Stem(name);
            // Reserve room for AtomicWrite's GUID temporary suffix on classic .NET/Windows.
            int limit = Math.Min(80, 210 - directory.Length - 1 - Extension.Length);
            if (limit < 8) throw new IOException("Ścieżka katalogu presetów jest zbyt długa. Skróć lokalizację programu; nie zmieniono pliku.");
            var occupied = Directory.GetFileSystemEntries(directory);
            for (int n = 1; n <= 10000; n++)
            {
                string suffix = n == 1 ? "" : " (" + n + ")";
                string path = Path.Combine(directory, Shorten(stem, limit - suffix.Length) + suffix + Extension);
                if (!occupied.Any(p => String.Equals(p, path, StringComparison.OrdinalIgnoreCase) && !String.Equals(p, current, StringComparison.OrdinalIgnoreCase))) return path;
                if(!allowSuffix)throw new IOException("Nazwa pliku jest już używana. Podaj inną nazwę; nie nadpisano pliku.");
            }
            throw new IOException("Zbyt wiele kolizji nazw plików presetów. Nie nadpisano żadnego pliku.");
        }
        internal static void Write(string destination, string current, string serialized)
        { Write(destination, current, serialized, (temp, target) => File.Replace(temp, target, null)); }
        internal static void Write(string destination, string current, string serialized, Action<string, string> replace)
        {
            destination = Path.GetFullPath(destination); if (current != null) current = Path.GetFullPath(current);
            if (current == null) { MapLogic.AtomicWrite(destination, serialized, false); return; }
            if (String.Equals(current, destination, StringComparison.Ordinal)) { MapLogic.AtomicWrite(current, serialized, true); return; }
            string temporary = Path.Combine(Path.GetDirectoryName(current), ".preset-" + Guid.NewGuid().ToString("N") + ".tmp");
            bool moved = false;
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(serialized);
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                // Move preserves the old bytes and has no overwrite flag. There is only
                // one active .json source, even between move and replacement.
                File.Move(current, destination); moved = true;
                replace(temporary, destination);
            }
            catch (Exception error)
            {
                if (moved) try { File.Move(destination, current); }
                    catch (Exception rollback) { throw new IOException("Zapis nie powiódł się; plik pozostał pod nazwą: " + destination + ". Nie usuwaj go. " + rollback.Message, error); }
                throw;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
