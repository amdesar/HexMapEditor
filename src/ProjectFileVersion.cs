using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
namespace HexMapEditor
{
    internal sealed class ProjectConflictException : IOException {internal ProjectConflictException(string message):base(message){}}
    internal sealed class ProjectFileVersion
    {
        readonly long length;readonly byte[] hash;
        ProjectFileVersion(long length,byte[] hash){this.length=length;this.hash=hash;}
        internal static ProjectFileVersion Bytes(byte[] bytes){using(var sha=SHA256.Create())return new ProjectFileVersion(bytes.LongLength,sha.ComputeHash(bytes));}
        internal static ProjectFileVersion Read(string path){using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){using(var sha=SHA256.Create())return new ProjectFileVersion(stream.Length,sha.ComputeHash(stream));}}
        internal bool Matches(string path){try{var other=Read(path);return length==other.length&&hash.SequenceEqual(other.hash);}catch(FileNotFoundException){return false;}catch(DirectoryNotFoundException){return false;}}
        internal static void Check(string path,ProjectFileVersion expected){if(expected!=null&&!expected.Matches(path))throw new ProjectConflictException("Plik mapy zmienił się poza programem lub zniknął. Autozapis wstrzymano. Rozwiąż konflikt ręcznie lub użyj Zapisz jako.");}
    }
}
