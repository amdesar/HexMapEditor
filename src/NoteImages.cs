using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace HexMapEditor
{
    internal static class NoteImages
    {
        internal const int MaxSourceBytes = 8 * 1024 * 1024;
        internal const int MaxInputSide = 8192;
        internal const long MaxInputPixels = 24L * 1024 * 1024;
        internal const int MaxPerNote = 12;
        internal const int MaxPixels = 2048;
        internal const int MaxPngBytes = 17 * 1024 * 1024;
        internal const int MaxClipboardCharacters = 36 * 1024 * 1024;

        internal static RichNode FromBytes(byte[] bytes)
        {
            if(bytes==null||bytes.Length<4)throw new InvalidDataException("Brak danych obrazu.");
            if(bytes.Length>MaxSourceBytes)throw new ImageBudgetException("Zakodowane źródło obrazu może mieć najwyżej 8 MiB.");
            bool png=bytes[0]==137&&bytes[1]==80&&bytes[2]==78&&bytes[3]==71;
            bool jpeg=bytes[0]==255&&bytes[1]==216&&bytes[2]==255;
            if(!png&&!jpeg)throw new InvalidDataException("Osadzany obraz musi być PNG lub JPEG.");
            using(var stream=new MemoryStream(bytes))
            using(var bitmap=System.Drawing.Image.FromStream(stream,true,false))
            {
                ClipboardRaster.ValidateDimensions(bitmap.Width,bitmap.Height,MaxInputSide,MaxInputPixels);
                using(var output=new MemoryStream())
                {
                    double scale=Math.Min(1,MaxPixels/(double)Math.Max(bitmap.Width,bitmap.Height));
                    int width=Math.Max(1,(int)Math.Round(bitmap.Width*scale)),height=Math.Max(1,(int)Math.Round(bitmap.Height*scale));
                    using(var copy=new System.Drawing.Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using(var g=System.Drawing.Graphics.FromImage(copy))
                        {
                            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                            g.DrawImage(bitmap,new System.Drawing.Rectangle(0,0,width,height));
                        }
                        copy.Save(output,System.Drawing.Imaging.ImageFormat.Png);
                    }
                    return FromPng(output.ToArray());
                }
            }
        }

        internal static RichNode FromBitmap(BitmapSource bitmap)
        {
            ClipboardRaster.ValidateDimensions(bitmap.PixelWidth,bitmap.PixelHeight,MaxInputSide,MaxInputPixels);
            double scale=Math.Min(1,MaxPixels/(double)Math.Max(bitmap.PixelWidth,bitmap.PixelHeight));
            if(scale<1)bitmap=new TransformedBitmap(bitmap,new System.Windows.Media.ScaleTransform(scale,scale));
            return FromPng(ClipboardRaster.Encode(bitmap,MaxPngBytes,MaxPixels,(long)MaxPixels*MaxPixels));
        }

        static RichNode FromPng(byte[] bytes)
        {
            var node=new RichNode{Kind="image",Png=Convert.ToBase64String(bytes),DisplayWidth=Math.Max(24,Math.Min(300,Dimension(bytes,16)))};
            Validate(node);
            return node;
        }

        static int Dimension(byte[] bytes,int at)
        {
            return bytes[at]<<24|bytes[at+1]<<16|bytes[at+2]<<8|bytes[at+3];
        }

        internal static byte[] Validate(RichNode node)
        {
            if(node.Png==null||node.Png.Length>MaxPngBytes*4/3+8||Double.IsNaN(node.DisplayWidth)||node.DisplayWidth<24||node.DisplayWidth>900)
                throw new InvalidDataException("Nieprawidłowy obraz opisu lub przekroczony limit PNG.");
            var bytes=Convert.FromBase64String(node.Png);
            if(bytes.Length<33||bytes.Length>MaxPngBytes||bytes[0]!=137||bytes[1]!=80||bytes[2]!=78||bytes[3]!=71||
                Dimension(bytes,12)!=0x49484452||Dimension(bytes,16)<1||Dimension(bytes,16)>MaxPixels||Dimension(bytes,20)<1||Dimension(bytes,20)>MaxPixels)
                throw new InvalidDataException("Nieprawidłowy PNG opisu.");
            return bytes;
        }

        internal static InlineUIContainer Create(RichNode node)
        {
            var bytes=Validate(node);
            var bitmap=new BitmapImage();
            using(var stream=new MemoryStream(bytes))
            {
                bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();
                bitmap.Freeze();
            }
            var image=new Image{Source=bitmap,Width=node.DisplayWidth,Stretch=System.Windows.Media.Stretch.Uniform,Tag=node.Png};
            System.Windows.Automation.AutomationProperties.SetName(image,"Obraz opisu");
            return new InlineUIContainer(image){BaselineAlignment=BaselineAlignment.Center};
        }

        internal static RichNode Read(Image image)
        {
            // XamlPackage round-trip and WPF clipboard can recreate Image without its Tag.
            var encoded=image.Tag as string;
            if(encoded==null)
            {
                var bitmap=image.Source as BitmapSource;
                if(bitmap==null)return null;
                var node=FromBitmap(bitmap);
                if(!Double.IsNaN(image.Width))node.DisplayWidth=Math.Max(24,Math.Min(900,image.Width));
                return node;
            }
            return new RichNode{Kind="image",Png=encoded,DisplayWidth=image.Width};
        }
    }

    internal static class NoteHtmlClipboard
    {
        // Only text and embedded raster payloads are interpreted. No HTML renderer,
        // navigation, resource loading, styles, scripts, or linked image fetches.
        static readonly Regex Tokens=new Regex(@"<!--(?:.|\n)*?-->|<(?:""[^""]*""|'[^']*'|[^'""<>])*>|[^<]+|<",
            RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(300));
        static readonly Regex Source=new Regex(@"(?:^|\s)src\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+))",
            RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));

        internal static FlowDocument Parse(string html)
        {
            if(String.IsNullOrEmpty(html))return null;
            if(html.Length>NoteImages.MaxClipboardCharacters)throw new ImageBudgetException("Dane HTML schowka przekraczają limit 36 Mi znaków.");
            int start=html.IndexOf("<!--StartFragment-->",StringComparison.OrdinalIgnoreCase);
            int end=html.IndexOf("<!--EndFragment-->",StringComparison.OrdinalIgnoreCase);
            if(start>=0&&end>start)html=html.Substring(start+20,end-start-20);
            else
            {
                start=html.IndexOf("<html",StringComparison.OrdinalIgnoreCase);
                if(start>0)html=html.Substring(start);
            }
            var doc=NoteCodec.FromPlain("");
            var paragraph=(Paragraph)doc.Blocks.FirstBlock;
            int images=0;
            long imageCharacters=0;
            string skip=null;
            foreach(Match token in Tokens.Matches(html))
            {
                string value=token.Value;
                if(value.StartsWith("<!--",StringComparison.Ordinal))continue;
                if(value.StartsWith("<",StringComparison.Ordinal)&&value.Length>1)
                {
                    int at=value[1]=='/'?2:1, nameEnd=at;
                    while(nameEnd<value.Length&&Char.IsLetterOrDigit(value[nameEnd]))nameEnd++;
                    string name=value.Substring(at,nameEnd-at).ToLowerInvariant();
                    if(skip!=null){if(at==2&&name==skip)skip=null;continue;}
                    if(at==1&&(name=="script"||name=="style"||name=="head"||name=="object"||name=="iframe")){skip=name;continue;}
                    if(at==1&&name=="img")
                    {
                        var src=Source.Match(value);
                        var capture=src.Groups.Cast<Group>().Skip(1).FirstOrDefault(g=>g.Success);
                        string data=capture==null?null:capture.Value;
                        int comma=data==null?-1:data.IndexOf(',');
                        if(comma>0)
                        {
                            string mime=data.Substring(0,comma).ToLowerInvariant();
                            if(mime=="data:image/png;base64"||mime=="data:image/jpeg;base64")
                            {
                                if(images>=NoteImages.MaxPerNote)throw new ImageBudgetException("Limit 12 obrazów w opisie.");
                                if(data.Length-comma-1>GraphicLimits.Encoded(NoteImages.MaxSourceBytes))throw new ImageBudgetException("Zakodowane źródło obrazu może mieć najwyżej 8 MiB.");
                                try
                                {
                                    var node=NoteImages.FromBytes(Convert.FromBase64String(data.Substring(comma+1)));
                                    imageCharacters+=node.Png.Length;
                                    if(imageCharacters>NoteCodec.MaxCharacters)throw new ImageBudgetException("Obrazy przekraczają limit 24 Mi znaków opisu.");
                                    paragraph.Inlines.Add(NoteImages.Create(node));images++;
                                }
                                catch(ImageBudgetException) { throw; }
                                catch(FormatException) { } catch(IOException) { } catch(ArgumentException) { } catch(System.Runtime.InteropServices.ExternalException) { }
                            }
                        }
                    }
                    if(name=="br"||at==2&&(name=="p"||name=="div"||name=="li"))paragraph.Inlines.Add(new LineBreak());
                }
                else if(skip==null)paragraph.Inlines.Add(new Run(System.Net.WebUtility.HtmlDecode(value)));
            }
            return doc;
        }
    }
}
