using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed partial class MainWindow
    {
        void LoadProgramIcon()
        {
            Icon loaded = null;
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app", "icon.ico");
                using (var stream = File.OpenRead(path))
                using (var external = new Icon(stream))
                    loaded = (Icon)external.Clone();
            }
            catch (Exception)
            {
                try { loaded = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
                catch (Exception) { }
            }

            if (loaded != null)
            {
                Icon = loaded;
                Disposed += (sender, args) => loaded.Dispose();
            }
        }
    }
}
