using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using Object = UnityEngine.Object;
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
                var icons = GetAllIcons(GetEditorAssetBundle()).ToArray();
                for (int i = 0; i < icons.Length; i++)
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

        [MenuItem("Unity Editor Icons/Generate README.md %g", priority = -1000)]
        private static void GenerateReadmeFile()
        {
            EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
            try
            {
                var editorAssetBundle = GetEditorAssetBundle();
                var iconsPath = AssetDatabaseUtil.GetIconsPath();
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
                var icons = GetAllIcons(GetEditorAssetBundle()).ToArray();
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

                Debug.Log($"'READMD.md' has been generated.");
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

        private static IEnumerable<Texture2D> GetAllIcons(AssetBundle editorAssetBundle)
        {
            const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            return from name in editorAssetBundle.GetAllAssetNames()
                where name.StartsWith(IconsPath, comparison)
                where name.EndsWith(".png", comparison) || name.EndsWith(".asset", comparison)
                let tex = editorAssetBundle.LoadAsset<Texture2D>(name)
                where tex != null
                select tex;
        }
    }

    public static class AssetDatabaseUtil
    {
        private static readonly PropertyInfo InspectorModeInfo =
            typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

        private const string FileIdFieldName = "m_LocalIdentfierInFile"; //note the misspelling!
        public static readonly string IconsPath = GetIconsPath();

        public static long GetFileId(Object obj)
        {
#if UNITY_2018_1_OR_NEWER && false
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long fileId);
            return fileId;
#else
            var serializedObject = new SerializedObject(obj);
            InspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug);
            return serializedObject.FindProperty(FileIdFieldName).longValue;
#endif
        }

        private static string GetIconsPath()
        {
#if UNITY_2018_3_OR_NEWER
            return EditorResources.iconsPath;
#else
            var assembly = typeof(EditorGUIUtility).Assembly;
            var editorResourcesUtility = assembly.GetType("UnityEditorInternal.EditorResourcesUtility");

            var iconsPathProperty = editorResourcesUtility.GetProperty(
                "iconsPath",
                BindingFlags.Static | BindingFlags.Public);

            return (string)iconsPathProperty.GetValue(null, new object[] { });
#endif
        }

        public static AssetBundle GetEditorAssetBundle()
        {
            var editorGUIUtility = typeof(EditorGUIUtility);
            var getEditorAssetBundle = editorGUIUtility.GetMethod(
                "GetEditorAssetBundle",
                BindingFlags.NonPublic | BindingFlags.Static);

            return (AssetBundle) getEditorAssetBundle.Invoke(null, null);
        }
    }
}