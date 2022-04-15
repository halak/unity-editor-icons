using UnityEditor;
using UnityEngine;

namespace QuickEye.Editor.IconWindow
{
    public static class QuickEyeGUIUtility
    {
        public static EditorGUIUtility.IconSizeScope KeepIconAspectRatio(Texture icon, Vector2 size)
        {
            if (icon == null)
                return new EditorGUIUtility.IconSizeScope(size);
            if (icon.width > icon.height)
            {
                var r = icon.width / size.x;
                size.y = icon.height / r;
            }
            else
            {
                var r = icon.height / size.y;
                size.x = icon.width / r;
            }

            return new EditorGUIUtility.IconSizeScope(size);
        }
    }
}