using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Halak.AssetDatabaseUtil;
namespace Halak
{
    public static class IconMiner
    {
        [MenuItem("Unity Editor Icons/Export All %e", priority = -1001)]
        private static void ExportIcons()
        {
            EditorUtility.DisplayProgressBar("Export Icons", "Exporting...", 0.0f);
            try
            {
                var count = 0;
                foreach (var icon in GetAllEditorIcons())
                {
                    ExportIcon("icons/original/", icon);
                    count++;
                }

                Debug.Log($"{count} icons has been exported!");
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
        private static void GenerateREADME()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
            try
            {
                var readmeContents = new StringBuilder();

                readmeContents.AppendLine("Unity Editor Built-in Icons");
                readmeContents.AppendLine("==============================");
                readmeContents.AppendLine($"Unity version: {Application.unityVersion}");
                readmeContents.AppendLine("Icons what can load using `EditorGUIUtility.IconContent`");
                readmeContents.AppendLine();
                readmeContents.AppendLine("File ID");
                readmeContents.AppendLine("-------------");
                readmeContents.AppendLine("You can change script icon by file id");
                readmeContents.AppendLine("1. Open `*.cs.meta` in Text Editor");
                readmeContents.AppendLine(
                    "2. Modify line `icon: {instanceID: 0}` to `icon: {fileID: <FILE ID>, guid: 0000000000000000d000000000000000, type: 0}`");
                readmeContents.AppendLine("3. Save and focus Unity Editor");
                readmeContents.AppendLine();
                readmeContents.AppendLine("| Icon | Name | File ID |");
                readmeContents.AppendLine("|------|------|---------|");

                var icons = GetAllEditorIcons().ToArray();
                for (var i = 0; i < icons.Length; i++)
                {
                    var iconTexture = icons[i];
                    EditorUtility.DisplayProgressBar("Generate README.md",
                        $"Generating... ({i + 1}/{icons.Length})", (float) i / icons.Length);

                    var iconPath = ExportIcon("icons/small/", iconTexture);

                    var fileId = AssetDatabaseUtil.GetFileId(iconTexture);
                    var escapedUrl = iconPath.Replace(" ", "%20").Replace('\\', '/');
                    readmeContents.AppendLine($"| ![]({escapedUrl}) | `{iconTexture.name}` | `{fileId}` |");
                }

                File.WriteAllText("README.md", readmeContents.ToString());

                Debug.Log($"'READMD.md' has been generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                watch.Stop();
                var elapsed = watch.Elapsed;
                Debug.Log($"Done in: {elapsed}");
            }
        }

        
    }
}