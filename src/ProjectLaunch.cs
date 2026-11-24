using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace HexMapEditor
{
    // Windows starts the EXE with a path for a file association. Each portable
    // installation has its own inbox and stays independent of copies elsewhere.
    internal sealed class ProjectLaunch : IDisposable
    {
        const int MaximumBytes=128*1024;
        readonly string name;
        readonly Mutex instance;
        readonly SecurityIdentifier user;
        readonly ConcurrentQueue<string> inbox=new ConcurrentQueue<string>();
        readonly object gate=new object();
        NamedPipeServerStream listener;
        bool stopping;
        internal readonly bool Primary;

        [DllImport("kernel32.dll",SetLastError=true)]
        static extern bool GetNamedPipeServerProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe,out uint processId);
        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(uint processId);

        internal ProjectLaunch()
        {
            using(var identity=WindowsIdentity.GetCurrent())user=identity.User;
            var root=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
            name="HexMapEditor-"+MapLogic.Hash(root+"|"+user.Value+"|"+Process.GetCurrentProcess().SessionId);
            instance=new Mutex(false,@"Local\"+name);
            try{Primary=instance.WaitOne(0);}
            catch(AbandonedMutexException){Primary=true;}
            if(Primary)new Thread(Listen){IsBackground=true,Name="HexMapEditor project inbox"}.Start();
        }
        internal bool Read(out string path){return inbox.TryDequeue(out path);}
        internal void Forward(string path)
        {
            byte[] bytes=Encoding.UTF8.GetBytes(String.IsNullOrEmpty(path)?"":Path.GetFullPath(path));
            if(bytes.Length>MaximumBytes)throw new IOException("Ścieżka mapy jest za długa.");
            using(var client=new NamedPipeClientStream(".",name,PipeDirection.InOut,PipeOptions.Asynchronous))
            {
                client.Connect(5000);
                uint process;
                if(GetNamedPipeServerProcessId(client.SafePipeHandle,out process))AllowSetForegroundWindow(process);
                byte[] payload=new byte[4+bytes.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(bytes.Length),0,payload,0,4);
                Buffer.BlockCopy(bytes,0,payload,4,bytes.Length);
                client.Write(payload,0,payload.Length);
                if(ReadBytes(client,1)[0]!=1)throw new IOException("Okno edytora nie przyjęło projektu.");
            }
        }
        void Listen()
        {
            var security=new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(user,PipeAccessRights.FullControl,AccessControlType.Allow));
            try
            {
                while(true)
                {
                    NamedPipeServerStream pipe;
                    lock(gate)
                    {
                        if(stopping)return;
                        listener=pipe=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous,4096,4096,security);
                    }
                    using(pipe)
                    {
                        try
                        {
                            pipe.WaitForConnection();
                            int length=BitConverter.ToInt32(ReadBytes(pipe,4),0);
                            if(length<0||length>MaximumBytes)continue;
                            string path=new UTF8Encoding(false,true).GetString(ReadBytes(pipe,length));
                            if(path.Length>0)path=Path.GetFullPath(path);
                            inbox.Enqueue(path);pipe.WriteByte(1);pipe.Flush();
                        }
                        catch(ObjectDisposedException){return;}
                        catch(IOException){}
                        catch(ArgumentException){}
                        catch(TimeoutException){}
                        finally{lock(gate)listener=null;}
                    }
                }
            }
            catch(Exception error){StartupTrace.Write("launch.inbox.failed",null,error);}
        }
        static byte[] ReadBytes(PipeStream pipe,int length)
        {
            var bytes=new byte[length];
            for(int offset=0;offset<length;)
            {
                var pending=pipe.BeginRead(bytes,offset,length-offset,null,null);
                int count;
                using(var handle=pending.AsyncWaitHandle)
                {
                    if(!handle.WaitOne(3000))throw new TimeoutException("Okno edytora nie odpowiedziało.");
                    count=pipe.EndRead(pending);
                }
                if(count==0)throw new EndOfStreamException();
                offset+=count;
            }
            return bytes;
        }
        public void Dispose()
        {
            lock(gate){stopping=true;if(listener!=null)listener.Dispose();}
            if(Primary)instance.ReleaseMutex();
            instance.Dispose();
        }
    }
}
