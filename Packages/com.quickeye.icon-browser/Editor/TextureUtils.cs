using System.IO;
using UnityEditor;
using UnityEngine;

namespace QuickEye.Editor.IconWindow
{
    public static class TextureUtils
    {
        public static string ExportIconToDir(string directory, Texture2D icon, bool smallVersion)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var iconPath = Path.Combine(directory, icon.name + ".png");

            ExportIconToPath(iconPath, icon, smallVersion);
            return iconPath;
        }

        [MenuItem("TEST/ExportSmallerIcon")]
        public static void DebugExport(Texture2D icon)
        {
            ExportIconToPath("Assets/TestExport.png", icon, true);
            AssetDatabase.ImportAsset("Assets/TestExport.png");
        }

        public static void ExportIconToPath(string path, Texture2D icon)
        {
            File.WriteAllBytes(path, EncodeIconToPNG(icon, false));
        }
        private static void ExportIconToPath(string path, Texture2D icon, bool smallVersion)
        {
            File.WriteAllBytes(path, EncodeIconToPNG(icon, smallVersion));
        }

        public static byte[] EncodeIconToPNG(Texture2D icon, bool smallVersion)
        {
            var readableTexture = smallVersion && TryGetScaledSize(icon.width,icon.height,32,32,out var newSize)
                ? Resize(icon, newSize.width, newSize.height)
                : CopyTexture();

            return readableTexture.EncodeToPNG();

            Texture2D CopyTexture()
            {
                var tex = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);
                Graphics.CopyTexture(icon, tex);
                return tex;
            }
        }

        private static bool TryGetScaledSize(int width, int height, int maxWidth, int maxHeight,
            out (int width, int height) newSize)
        {
            var scaleWidth = maxWidth / (float)width;
            var scaleHeight = maxHeight / (float)height;
            var scale = Mathf.Min(scaleHeight, scaleWidth);
            newSize = (Mathf.CeilToInt(width * scale), Mathf.CeilToInt(height * scale));
            return width > maxWidth || height > maxHeight;
        }

        static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            var rt = new RenderTexture(targetX, targetY, 24);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            var result = new Texture2D(targetX, targetY);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            return result;
        }
    }
}