using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HexMapEditor
{
    // Only the filtered RTF reaches WPF. Project files never contain RTF or XAML.
    internal static class RtfClipboard
    {
        static readonly HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "object", "objdata", "pict", "shp", "shppict", "nonshppict", "filetbl", "datastore", "themedata", "colorschememapping", "htmltag", "datafield", "fldinst" };
        public static string Filter(string source) { return Filter(source,null); }
        internal static string Filter(string source, Func<string,string> picture)
        {
            if (String.IsNullOrEmpty(source) || !source.StartsWith(@"{\rtf", StringComparison.Ordinal)) throw new InvalidDataException("Nieobsługiwany RTF.");
            if (source.Length > NoteImages.MaxClipboardCharacters) throw new ImageBudgetException("Dane RTF schowka przekraczają limit 36 Mi znaków.");
            int at = 0; string result = Group(source, ref at, 0,picture); if (at != source.Length && !String.IsNullOrWhiteSpace(source.Substring(at))) throw new InvalidDataException("Nieprawidłowy RTF."); return result;
        }
        static string Destination(string source, int start)
        {
            int p = start + 1; while (p < source.Length && Char.IsWhiteSpace(source[p])) p++;
            if (p + 1 < source.Length && source[p] == '\\' && source[p + 1] == '*') { p += 2; while (p < source.Length && source[p] == ' ') p++; }
            if (p >= source.Length || source[p] != '\\') return "";
            int begin = ++p; while (p < source.Length && Char.IsLetter(source[p])) p++; return source.Substring(begin, p - begin);
        }
        static string Group(string source, ref int at, int depth,Func<string,string> picture)
        {
            if (depth > 64 || at >= source.Length || source[at] != '{') throw new InvalidDataException("Zbyt złożony RTF.");
            int start=at;string destination = Destination(source, at); bool discard = excluded.Contains(destination), objectGroup = destination.Equals("object", StringComparison.OrdinalIgnoreCase); at++; var output = new StringBuilder("{"); var objectResult = new StringBuilder();
            while (at < source.Length)
            {
                char c = source[at];
                if (c == '}') { at++; if(picture!=null&&destination=="pict")return picture(source.Substring(start,at-start)); output.Append('}'); return objectGroup ? "{" + objectResult + "}" : destination=="shppict"&&picture!=null?objectResult.ToString():discard ? "" : output.ToString(); }
                if (c == '{') { bool result = objectGroup && Destination(source, at).Equals("result", StringComparison.OrdinalIgnoreCase); var nested = Destination(source,at);
                    var allowPicture = !discard || result || destination=="shppict";
                    string child = Group(source, ref at, depth + 1,allowPicture&&nested!="nonshppict"?picture:null); if (result || destination=="shppict"&&nested=="pict") objectResult.Append(child); if (!discard) output.Append(child); continue; }
                if (c != '\\') { if (!discard) output.Append(c); at++; continue; }
                int begin = at++; if (at >= source.Length) break;
                if (!Char.IsLetter(source[at]))
                { char symbol = source[at++]; if (symbol == '\'') { if (at + 2 > source.Length) throw new InvalidDataException("Uszkodzony RTF."); at += 2; } if (!discard) output.Append(source, begin, at - begin); continue; }
                int wordStart = at; while (at < source.Length && Char.IsLetter(source[at])) at++; string word = source.Substring(wordStart, at - wordStart);
                if (word.Equals("bin", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Binarny RTF wymaga uproszczenia do tekstu.");
                if (at < source.Length && source[at] == '-') at++; while (at < source.Length && Char.IsDigit(source[at])) at++; if (at < source.Length && source[at] == ' ') at++;
                // A field becomes ordinary formatted text; its instruction group is excluded above.
                if (!discard && !word.Equals("field", StringComparison.OrdinalIgnoreCase) && !word.Equals("fldrslt", StringComparison.OrdinalIgnoreCase) && !word.Equals("result", StringComparison.OrdinalIgnoreCase)) output.Append(source, begin, at - begin);
            }
            throw new InvalidDataException("Niedomknięty RTF.");
        }
    }
}
