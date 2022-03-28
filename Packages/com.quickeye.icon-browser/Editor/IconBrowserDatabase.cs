using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QuickEye.Editor
{
    [Flags]
    public enum IconFilter
    {
        None = 0,
        Everything = ~0,
        AlternativeSkin = 1,
        SmallerVersions = 2,
    }
[Serializable]
    public class IconBrowserDatabase
    {
        private static string[] iconBlacklist =
        {
            "StateMachineEditor.Background",
            "scene-template-empty-scene",
            "scene-template-2d-scene",
        };

        private Texture2D[] AllIcons;
        public Texture2D[] Icons;
        public Texture2D[] SearchResult;

        private string[] iconsWithDarkSkinAlternative;
        private string[] smallerIcons;

        public IconBrowserDatabase(string searchString)
        {
            GetIcons();
            UpdateBySearch(searchString);
        }

        private void GetIcons()
        {
            AllIcons = AssetDatabaseUtil.GetAllEditorIcons();

            iconsWithDarkSkinAlternative = (from icon in AllIcons
                where icon.name.StartsWith("d_")
                select icon.name.Substring(2)).ToArray();
            smallerIcons = (from icon in AllIcons
                where icon.name.EndsWith("@2x")
                select icon.name.Substring(0, icon.name.Length - 3)).ToArray();
            Icons = AllIcons;
        }

        public void SortByColor()
        {
            Icons = (from icon in Icons
                    let hsv = GetIconAverageHSV(icon)
                    orderby hsv.h, hsv.s, hsv.v
                    select icon
                ).ToArray();
        }

        public void SortByName()
        {
            Icons = (from icon in Icons
                    orderby icon.name
                    select icon
                ).ToArray();
        }

        public void UpdateBySearch(string searchString)
        {
            SearchResult = (from icon in Icons
                    let lowerName = icon.name.ToLower()
                    let lowerSearch = searchString.ToLower()
                    where lowerName.Contains(lowerSearch)
                    orderby lowerName.IndexOf(lowerSearch, StringComparison.Ordinal)
                    select icon
                ).ToArray();
        }

        public void UpdateByFilter(IconFilter filter)
        {
            if (filter == IconFilter.Everything)
            {
                Icons = AllIcons;
                return;
            }

            IEnumerable<Texture2D> icons = AllIcons;

            if (!filter.HasFlag(IconFilter.AlternativeSkin))
                icons = icons.Where(icon => EditorGUIUtility.isProSkin
                    ? !iconsWithDarkSkinAlternative.Contains(icon.name)
                    : !icon.name.StartsWith("d_"));
            
            if (!filter.HasFlag(IconFilter.SmallerVersions))
                icons = icons.Where(icon => !icon.name.EndsWith(".Small") || !smallerIcons.Contains(icon.name));
            Icons = icons.ToArray();
        }

        private static (float h, float s, float v) GetIconAverageHSV(Texture2D icon)
        {
            var readableTexture = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);
            Graphics.CopyTexture(icon, readableTexture);
            var averageColor = AverageColorFromTexture(readableTexture);
            Color.RGBToHSV(averageColor, out var h, out var s, out var v);
            UnityEngine.Object.DestroyImmediate(readableTexture);
            return (h, s, v);
        }

        private static Color32 AverageColorFromTexture(Texture2D tex)
        {
            var texColors = tex.GetPixels32();
            var total = texColors.Length;

            float r = 0;
            float g = 0;
            float b = 0;

            for (var i = 0; i < total; i++)
            {
                r += texColors[i].r;
                g += texColors[i].g;
                b += texColors[i].b;
            }

            return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), 255);
        }
    }
}