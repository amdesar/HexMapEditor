using System;
using System.IO;
using System.Linq;

namespace HexMapEditor
{
    internal static class LibraryFiles
    {
        internal static void Transition(string source, byte[] expected, string target, byte[] replacement)
        {
            if (source != null && (!File.Exists(source) || !File.ReadAllBytes(source).SequenceEqual(expected))) throw new IOException("Plik biblioteki zmienił się poza programem lub zniknął. Nie zmieniono biblioteki ani historii.\n"+source);
            if (target != null && !String.Equals(source,target,StringComparison.OrdinalIgnoreCase) && (File.Exists(target) || Directory.Exists(target))) throw new IOException("Ścieżka odtwarzanego pliku jest zajęta. Nie nadpisano danych ani nie przesunięto historii.\n"+target);
            string parked = null;
            try
            {
                if (source != null)
                {
                    parked = Path.Combine(Path.GetDirectoryName(source),".library-session-"+Guid.NewGuid().ToString("N")+".tmp");
                    File.Move(source,parked);
                    // Recheck the actual bytes moved, including a replacement between check and Move.
                    if (!File.ReadAllBytes(parked).SequenceEqual(expected)) throw new IOException("Plik zmienił się w trakcie operacji; przywracamy poprzednią ścieżkę.");
                }
                if (target != null)
                {
                    // CreateNew + atomic move never replaces a racing external file.
                    Create(target, replacement);
                }
            }
            catch(Exception error)
            {
                if (parked != null && File.Exists(parked)) try { File.Move(parked,source); parked=null; }
                    catch(Exception rollback) { throw new IOException("Nie wykonano zmiany. Zachowano plik do odzyskania: "+parked+". Nie nadpisano zajętej ścieżki. "+rollback.Message,error); }
                throw;
            }
            if (parked != null) try { File.Delete(parked); } catch { /* Own inactive safety copy; never delete an active file during cleanup. */ }
        }
        static void Create(string target, byte[] bytes)
        {
            string temp = Path.Combine(Path.GetDirectoryName(target), ".library-write-" + Guid.NewGuid().ToString("N") + ".tmp");
            try { using (var stream = new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { stream.Write(bytes,0,bytes.Length); stream.Flush(true); } File.Move(temp,target); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
