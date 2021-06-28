using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static QuickEye.Editor.AssetDatabaseUtil;

namespace QuickEye.Editor
{
    public static class IconMiner
    {
        [MenuItem("Unity Editor Icons/Export All %e", priority = -1001)]
        private static void ExportIcons()
        {
            EditorUtility.DisplayProgressBar("Export Icons", "Exporting...", 0.0f);
            try
            {
                var icons = GetAllEditorIcons();
                for (var i = 0; i < icons.Length; i++)
                {
                    ExportIcon("icons/original/", icons[i]);
                    EditorUtility.DisplayProgressBar("Export Icons", "Exporting...", (float) i / icons.Length);
                }

                Debug.Log($"{icons.Length} icons has been exported!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string ExportIcon(string directory, Texture2D icon)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var iconPath = Path.Combine(directory, icon.name + ".png");

            File.WriteAllBytes(iconPath, EncodeIconToPNG(icon));
            return iconPath;
        }

        private static byte[] EncodeIconToPNG(Texture2D icon)
        {
            var readableTexture = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);
            Graphics.CopyTexture(icon, readableTexture);

            return readableTexture.EncodeToPNG();
        }

        [MenuItem("Unity Editor Icons/Generate README.md %g", priority = -1000)]
        private static void GenerateReadmeFile()
        {
            EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
            try
            {
                var template = Resources.Load<TextAsset>("Template").text;
                template = template.Replace("<UnityVersion>", Application.unityVersion);

                var readmeContents = new StringBuilder();
                readmeContents.AppendLine(template);

                var icons = GetAllEditorIcons();
                for (var i = 0; i < icons.Length; i++)
                {
                    var icon = icons[i];
                    EditorUtility.DisplayProgressBar("Generate README.md",
                        $"Generating... ({i + 1}/{icons.Length})", (float) i / icons.Length);

                    var iconPath = ExportIcon("icons/small/", icon);

                    var fileId = GetFileId(icon);
                    var escapedUrl = iconPath.Replace(" ", "%20").Replace('\\', '/');
                    readmeContents.AppendLine($"| ![]({escapedUrl}) | `{icon.name}` | `{fileId}` |");
                }

                File.WriteAllText("README.md", readmeContents.ToString());

                Debug.Log("'READMD.md' has been generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
