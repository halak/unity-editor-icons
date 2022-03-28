using System.IO;
using UnityEngine;

namespace QuickEye.Editor
{
    public static class TextureUtils
    {
        public static string ExportIconToDir(string directory, Texture2D icon)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var iconPath = Path.Combine(directory, icon.name + ".png");

            ExportIconToPath(iconPath, icon);
            return iconPath;
        }

        public static void ExportIconToPath(string path, Texture2D icon)
        {
            File.WriteAllBytes(path, EncodeIconToPNG(icon));
        }

        public static byte[] EncodeIconToPNG(Texture2D icon)
        {
            var readableTexture = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);
            Graphics.CopyTexture(icon, readableTexture);

            return readableTexture.EncodeToPNG();
        }
    }
}