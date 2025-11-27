// File: PublicPCControl.Client/Services/IconHelper.cs
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PublicPCControl.Client.Services
{
    public static class IconHelper
    {
        public static ImageSource? LoadIcon(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                return null;
            }

            try
            {
                using var icon = Icon.ExtractAssociatedIcon(executablePath);
                if (icon == null)
                {
                    return null;
                }

                using var stream = new MemoryStream();
                icon.ToBitmap().Save(stream, ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
