using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuickEye.Editor.IconWindow
{
    public static class AssetDatabaseUtil
    {
        public static readonly AssetBundle EditorAssetBundle = GetEditorAssetBundle();
        public static readonly string IconsPath = GetIconsPath();
        
        public static EditorAssetBundleImage[] GetEditorAssetBundleImages()
        {
            return (from path in EditorAssetBundle.GetAllAssetNames()
                let tex = EditorAssetBundle.LoadAsset<Texture2D>(path)
                where tex != null
                select new EditorAssetBundleImage(tex, path)).ToArray();
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