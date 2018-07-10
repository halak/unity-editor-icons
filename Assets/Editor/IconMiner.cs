using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

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
                var editorAssetBundle = GetEditorAssetBundle();
                var iconsPath = GetIconsPath();
                var count = 0;
                foreach (var assetName in EnumerateIcons(editorAssetBundle, iconsPath))
                {
                    var icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);
                    if (icon == null)
                        continue;

                    var readableTexture = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);

                    Graphics.CopyTexture(icon, readableTexture);

                    var folderPath = Path.GetDirectoryName(Path.Combine("icons/original/", assetName.Substring(iconsPath.Length)));
                    if (Directory.Exists(folderPath) == false)
                        Directory.CreateDirectory(folderPath);

                    var iconPath = Path.Combine(folderPath, icon.name + ".png");
                    File.WriteAllBytes(iconPath, readableTexture.EncodeToPNG());

                    count++;
                }

                Debug.Log($"{count} icons has been exported!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Unity Editor Icons/Generate README.md %g", priority = -1000)]
        private static void GenerateREADME()
        {
            EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
            try
            {
                var editorAssetBundle = GetEditorAssetBundle();
                var iconsPath = GetIconsPath();
                var readmeContents = new StringBuilder();

                readmeContents.AppendLine($"Unity Editor Built-in Icons");
                readmeContents.AppendLine($"==============================");
                readmeContents.AppendLine($"Icons what can load using `EditorGUIUtility.IconContent`");
                readmeContents.AppendLine();
                readmeContents.AppendLine($"| Icon | Name |");
                readmeContents.AppendLine($"|------|------|");

                foreach (var assetName in EnumerateIcons(editorAssetBundle, iconsPath))
                {
                    var icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);
                    if (icon == null)
                        continue;

                    var readableTexture = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);

                    Graphics.CopyTexture(icon, readableTexture);

                    var folderPath = Path.GetDirectoryName(Path.Combine("icons/small/", assetName.Substring(iconsPath.Length)));
                    if (Directory.Exists(folderPath) == false)
                        Directory.CreateDirectory(folderPath);

                    var iconPath = Path.Combine(folderPath, icon.name + ".png");
                    File.WriteAllBytes(iconPath, readableTexture.EncodeToPNG());

                    var escapedUrl = iconPath.Replace(" ", "%20").Replace('\\', '/');
                    readmeContents.AppendLine($"| ![]({escapedUrl}) | `{icon.name}` |");
                }

                File.WriteAllText("README.md", readmeContents.ToString());

                Debug.Log($"'READMD.md' has been generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static IEnumerable<string> EnumerateIcons(AssetBundle editorAssetBundle, string iconsPath)
        {
            foreach (var assetName in editorAssetBundle.GetAllAssetNames())
            {
                if (assetName.StartsWith(iconsPath, StringComparison.OrdinalIgnoreCase) == false)
                    continue;
                if (assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false &&
                    assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                yield return assetName;
            }
        }

        private static AssetBundle GetEditorAssetBundle()
        {
            var editorGUIUtility = typeof(EditorGUIUtility);
            var getEditorAssetBundle = editorGUIUtility.GetMethod(
                "GetEditorAssetBundle",
                BindingFlags.NonPublic | BindingFlags.Static);

            return (AssetBundle)getEditorAssetBundle.Invoke(null, new object[] { });
        }

        private static string GetIconsPath()
        {
            var assembly = typeof(EditorGUIUtility).Assembly;
            var editorResourcesUtility = assembly.GetType("UnityEditorInternal.EditorResourcesUtility");

            var iconsPathProperty = editorResourcesUtility.GetProperty(
                "iconsPath",
                BindingFlags.Static | BindingFlags.Public);

            return (string)iconsPathProperty.GetValue(null, new object[] { });
        }
    }
}