using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HexMapEditor
{
    internal static class RecycleFile
    {
        // Windows shell, already installed: no package/runtime download.
        // WANTNUKEWARNING overrides NOCONFIRMATION if recycling is impossible;
        // there is no application fallback to File.Delete.
        internal const uint Flags = 0x20000000 | 0x00080000 | 0x00004000 | 0x00000010 | 0x00000004;
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        static extern int SHCreateItemFromParsingName(string path, IntPtr context, ref Guid iid, out IShellItem item);
        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellItem { }
        [ComImport, Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IFileOperation
        {
            void Advise(IntPtr sink, out uint cookie); void Unadvise(uint cookie); void SetOperationFlags(uint flags);
            void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string message); void SetProgressDialog(IntPtr dialog); void SetProperties(IntPtr properties); void SetOwnerWindow(IntPtr owner);
            void ApplyPropertiesToItem(IntPtr item); void ApplyPropertiesToItems(IntPtr items); void RenameItem(IntPtr item, IntPtr name, IntPtr sink); void RenameItems(IntPtr items, IntPtr name);
            void MoveItem(IntPtr item, IntPtr destination, IntPtr name, IntPtr sink); void MoveItems(IntPtr items, IntPtr destination); void CopyItem(IntPtr item, IntPtr destination, IntPtr name, IntPtr sink); void CopyItems(IntPtr items, IntPtr destination);
            void DeleteItem(IShellItem item, IntPtr sink); void DeleteItems(IntPtr items); void NewItem(IntPtr destination, uint attributes, IntPtr name, IntPtr template, IntPtr sink);
            void PerformOperations(); void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool aborted);
        }
        public static void Send(string path, IntPtr owner)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version < new Version(6, 2)) throw new NotSupportedException("Bezpieczna operacja Kosza wymaga Windows 8 lub nowszego; plik pozostawiono.");
            IFileOperation operation = null; IShellItem item = null;
            try
            {
                operation = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("3AD05575-8857-4850-9277-11B85BDB8E09")));
                operation.SetOperationFlags(Flags); operation.SetOwnerWindow(owner);
                var iid = typeof(IShellItem).GUID; Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(Path.GetFullPath(path), IntPtr.Zero, ref iid, out item));
                operation.DeleteItem(item, IntPtr.Zero); operation.PerformOperations(); bool aborted; operation.GetAnyOperationsAborted(out aborted);
                if (aborted || File.Exists(path)) throw new IOException("Operacja Kosza została anulowana lub nie zakończyła się powodzeniem. Nie zastosowano trwałego usuwania w programie.");
            }
            finally { if (item != null) Marshal.ReleaseComObject(item); if (operation != null) Marshal.ReleaseComObject(operation); }
        }
    }
}
