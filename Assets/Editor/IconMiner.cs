using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Halak
{
    public static class IconMiner
    {
        [MenuItem("Tasks/GenerateREADME %g")]
        private static void GenerateREADME()
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

            foreach (var assetName in editorAssetBundle.GetAllAssetNames())
            {
                if (assetName.StartsWith(iconsPath, StringComparison.OrdinalIgnoreCase) == false)
                    continue;
                if (assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                var icon = editorAssetBundle.LoadAsset<Texture2D>(assetName);
                if (icon.mipmapCount > 1)
                    continue;

                var name = assetName.Substring(iconsPath.Length);

                var readableTexture = new Texture2D(icon.width, icon.height, icon.format, false);

                Graphics.CopyTexture(icon, readableTexture);

                var path = "Assets/Editor/Icons/" + name;
                PrepareFolder(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, readableTexture.EncodeToPNG());

                var escapedUrl = path.Replace(" ", "%20");
                readmeContents.AppendLine($"| ![{name}]({escapedUrl})  | `{name}` |");
            }

            AssetDatabase.Refresh();

            File.WriteAllText("README.md", readmeContents.ToString());
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

        private static void PrepareFolder(string path)
        {
            if (Directory.Exists(path) == false)
            {
                var parent = Path.GetDirectoryName(path);
                if (Directory.Exists(parent))
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
                else
                    PrepareFolder(parent);
            }
        }
    }
}