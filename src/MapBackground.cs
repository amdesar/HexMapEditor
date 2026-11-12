using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HexMapEditor
{
    public sealed class MapBackground
    {
        public string SourceId { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int Opacity { get; set; }
        public int Saturation { get; set; }
        public int Blur { get; set; }
        internal GraphicSource Source;
        internal string LibraryId;
        public MapBackground() { Width = Height = 1; Opacity = Saturation = 100; }
        internal MapBackground Copy() { return (MapBackground)MemberwiseClone(); }
    }
    internal static class BackgroundRenderer
    {
        internal static void Validate(MapBackground settings)
        {
            if (settings == null) return;
            if (settings.Source == null || settings.SourceId != settings.Source.Id ||
                !GraphicSources.Finite(settings.Left) || !GraphicSources.Finite(settings.Top) ||
                !GraphicSources.Finite(settings.Width) || !GraphicSources.Finite(settings.Height) ||
                settings.Left < 0 || settings.Top < 0 || settings.Width < .02 || settings.Height < .02 ||
                settings.Left + settings.Width > 1.000000001 || settings.Top + settings.Height > 1.000000001 ||
                settings.Opacity < 0 || settings.Opacity > 100 || settings.Saturation < 0 || settings.Saturation > 200 || settings.Blur < 0 || settings.Blur > 24)
                throw new InvalidDataException("Nieprawidłowe ustawienia tła.");
        }
        internal static Bitmap Render(MapBackground settings, Size viewport, Bitmap prepared = null, int maxSide = GraphicLimits.BackgroundSide)
        {
            Validate(settings);
            float factor = Math.Min(1, (float)maxSide / Math.Max(1, Math.Max(viewport.Width, viewport.Height)));
            var size = new Size(Math.Max(1, (int)(viewport.Width * factor)), Math.Max(1, (int)(viewport.Height * factor)));
            var result = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (var lease = prepared == null ? GraphicSources.Decode(settings.Source) : null)
                using (var g = Graphics.FromImage(result))
                using (var attributes = new ImageAttributes())
                {
                    var source = prepared ?? lease;
                    var crop = new RectangleF((float)(settings.Left * source.Width), (float)(settings.Top * source.Height),
                        (float)(settings.Width * source.Width), (float)(settings.Height * source.Height));
                    // Geometry follows the viewport, never the rounded working bitmap dimensions.
                    float cover = Math.Max(viewport.Width / crop.Width, viewport.Height / crop.Height);
                    float width = viewport.Width / cover, height = viewport.Height / cover;
                    crop = new RectangleF(crop.X + (crop.Width - width) / 2, crop.Y + (crop.Height - height) / 2, width, height);
                    float s = settings.Saturation / 100f, r = .2126f * (1 - s), green = .7152f * (1 - s), b = .0722f * (1 - s);
                    attributes.SetColorMatrix(new ColorMatrix(new[] {
                        new[]{r+s,r,r,0f,0f}, new[]{green,green+s,green,0f,0f}, new[]{b,b,b+s,0f,0f},
                        new[]{0f,0f,0f,1f,0f}, new[]{0f,0f,0f,0f,1f} }));
                    attributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(source, new Rectangle(0, 0, size.Width, size.Height), crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, attributes);
                }
                if (settings.Blur > 0) Blur(result, Math.Max(1, (int)Math.Round(settings.Blur * factor)));
                return result;
            }
            catch { result.Dispose(); throw; }
        }
        static void Blur(Bitmap image, int radius)
        {
            var locked = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int stride = locked.Stride, width = image.Width, height = image.Height;
                var pixels = new byte[stride * height]; var horizontal = new byte[pixels.Length]; var output = new byte[pixels.Length];
                Marshal.Copy(locked.Scan0, pixels, 0, pixels.Length);
                for (int y = 0; y < height; y++)
                for (int channel = 0; channel < 4; channel++)
                {
                    int sum = 0, count = 0;
                    for (int i = 0; i <= Math.Min(radius, width - 1); i++) { sum += pixels[y * stride + i * 4 + channel]; count++; }
                    for (int x = 0; x < width; x++)
                    {
                        horizontal[y * stride + x * 4 + channel] = (byte)(sum / count);
                        int remove = x - radius, add = x + radius + 1;
                        if (remove >= 0) { sum -= pixels[y * stride + remove * 4 + channel]; count--; }
                        if (add < width) { sum += pixels[y * stride + add * 4 + channel]; count++; }
                    }
                }
                for (int x = 0; x < width; x++)
                for (int channel = 0; channel < 4; channel++)
                {
                    int sum = 0, count = 0;
                    for (int i = 0; i <= Math.Min(radius, height - 1); i++) { sum += horizontal[i * stride + x * 4 + channel]; count++; }
                    for (int y = 0; y < height; y++)
                    {
                        output[y * stride + x * 4 + channel] = (byte)(sum / count);
                        int remove = y - radius, add = y + radius + 1;
                        if (remove >= 0) { sum -= horizontal[remove * stride + x * 4 + channel]; count--; }
                        if (add < height) { sum += horizontal[add * stride + x * 4 + channel]; count++; }
                    }
                }
                Marshal.Copy(output, 0, locked.Scan0, output.Length);
            }
            finally { image.UnlockBits(locked); }
        }
        // Small, coalesced interaction preview. The final preview uses Render at full preview size.
        internal static Bitmap PreviewEffects(Bitmap covered,int saturation,int blur,float factor)
        {
            var result=new Bitmap(covered.Width,covered.Height,PixelFormat.Format32bppArgb);
            try
            {
                using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes())
                {
                    float s=saturation/100f,r=.2126f*(1-s),green=.7152f*(1-s),b=.0722f*(1-s);
                    attributes.SetColorMatrix(new ColorMatrix(new[]{new[]{r+s,r,r,0f,0f},new[]{green,green+s,green,0f,0f},new[]{b,b,b+s,0f,0f},new[]{0f,0f,0f,1f,0f},new[]{0f,0f,0f,0f,1f}}));
                    g.DrawImage(covered,new Rectangle(0,0,result.Width,result.Height),0,0,covered.Width,covered.Height,GraphicsUnit.Pixel,attributes);
                }
                if(blur>0)Blur(result,Math.Max(1,(int)Math.Round(blur*factor)));
                return result;
            }
            catch { result.Dispose();throw; }
        }
    }
    public sealed partial class MainWindow
    {
        Bitmap backgroundBitmap;
        string backgroundKey, backgroundSourceKey;
        Bitmap backgroundSource;
        bool hideBackground, backgroundResizing;
        void InitBackgroundResize()
        {
            Disposed+=(s,e)=>ClearBackgroundCache();
            ResizeBegin+=(s,e)=>{backgroundResizing=true;canvas.Invalidate();};
            ResizeEnd+=(s,e)=>{backgroundResizing=false;ClearBackgroundRendered();canvas.Invalidate();};
        }
        void ClearBackgroundRendered()
        {
            if(backgroundBitmap!=null)backgroundBitmap.Dispose();
            backgroundBitmap=null;backgroundKey=null;
        }
        void ClearBackgroundCache()
        {
            ClearBackgroundRendered();
            if(backgroundSource!=null)backgroundSource.Dispose();
            backgroundSource=null;backgroundSourceKey=null;
        }
        void DrawBackground(Graphics g)
        {
            if (lowQuality || hideBackground || map.Background == null) return;
            if(backgroundSourceKey!=map.Background.SourceId)
            {
                var source=GraphicSources.Decode(map.Background.Source);
                ClearBackgroundCache();backgroundSource=source;backgroundSourceKey=map.Background.SourceId;
            }
            string key = json.Serialize(map.Background) + ":" + canvas.ClientSize + ":" + backgroundResizing;
            if (key != backgroundKey)
            {
                var rendered = BackgroundRenderer.Render(map.Background, canvas.ClientSize, backgroundSource, backgroundResizing?320:GraphicLimits.BackgroundSide);
                ClearBackgroundRendered(); backgroundBitmap = rendered; backgroundKey = key;
            }
            using (var attributes = new ImageAttributes())
            {
                var matrix = new ColorMatrix(); matrix.Matrix33 = map.Background.Opacity / 100f;
                attributes.SetColorMatrix(matrix);
                g.DrawImage(backgroundBitmap, canvas.ClientRectangle, 0, 0, backgroundBitmap.Width, backgroundBitmap.Height, GraphicsUnit.Pixel, attributes);
            }
        }
        bool ChangeBackground(MapBackground settings)
        {
            BackgroundRenderer.Validate(settings);
            if (json.Serialize(settings) == json.Serialize(map.Background)) return false;
            GraphicSources.Collect(DocumentGraphics().Where(g => g != null).Select(g => g.Source)
                .Concat(settings == null ? new GraphicSource[0] : new[] { settings.Source }));
            EndCanvasGesture(true);
            var action = new EditAction { Label = "Tło mapy", BackgroundChange = true, BeforeBackground = map.Background == null ? null : map.Background.Copy(),
                AfterBackground = settings == null ? null : settings.Copy() };
            ApplyAction(action, true); PushAction(action); MarkDirty(); return true;
        }
        void EditBackground()
        {
            using (var dialog = new BackgroundDialog(map.Background, canvas.ClientSize))
            {
                dialog.CanAccept = () => GuardLayer(MapLayer.Background, dialog);
                dialog.ChooseLibrary = () => ChooseBackground(dialog);
                dialog.SaveAndApply = (settings,id) => AcceptBackground(dialog,settings,id,true);
                RunModal(() => dialog.ShowDialog(this));
            }
        }
    }
}
