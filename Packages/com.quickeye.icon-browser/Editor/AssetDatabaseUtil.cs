using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuickEye.Editor
{
    public static class AssetDatabaseUtil
    {
        public static readonly AssetBundle EditorAssetBundle = GetEditorAssetBundle();
        public static readonly string IconsPath = GetIconsPath();
        
        public static Texture2D[] GetAllEditorIcons()
        {
            const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            return (from name in EditorAssetBundle.GetAllAssetNames()
                where name.StartsWith(IconsPath, comparison)
                where name.EndsWith(".png", comparison) || name.EndsWith(".asset", comparison)
                let tex = EditorAssetBundle.LoadAsset<Texture2D>(name)
                where tex != null
                select tex).ToArray();
        }
        public static (Texture2D tex, string path)[] GetAllEditorIconsWithSource()
        {
            const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            return (from name in EditorAssetBundle.GetAllAssetNames()
                where name.StartsWith(IconsPath, comparison)
                where name.EndsWith(".png", comparison) || name.EndsWith(".asset", comparison)
                let tex = EditorAssetBundle.LoadAsset<Texture2D>(name)
                where tex != null
                select (tex,name)).ToArray();
        }
        private static AssetBundle GetEditorAssetBundle()
        {
            var editorGUIUtility = typeof(EditorGUIUtility);
            var getEditorAssetBundle = editorGUIUtility.GetMethod(
                "GetEditorAssetBundle",
                BindingFlags.NonPublic | BindingFlags.Static);

            return (AssetBundle)getEditorAssetBundle.Invoke(null, null);
        }

        public static long GetFileId(Object obj)
        {
#if UNITY_2018_1_OR_NEWER
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long fileId);
            return fileId;
#else
            const string fileIdFieldName = "m_LocalIdentfierInFile"; //note the misspelling!
            var inspectorModeInfo =
                typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            var serializedObject = new SerializedObject(obj);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug);
            return serializedObject.FindProperty(fileIdFieldName).longValue;
#endif
        }

        public static string GetIconsPath()
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
    }
}