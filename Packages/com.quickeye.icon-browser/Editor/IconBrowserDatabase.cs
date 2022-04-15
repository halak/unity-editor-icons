using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        RetinaVersions = 2,
        OtherImages = 4
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

        private static string[] iconPathsBlacklist =
        {
            "devicesimulator"
        };

        private EditorAssetBundleImage[] AllIcons;
        private Dictionary<string, EditorAssetBundleImage> IconDictionary = new Dictionary<string, EditorAssetBundleImage>();
        public EditorAssetBundleImage[] Icons;
        public EditorAssetBundleImage[] SearchResult;

        private HashSet<EditorAssetBundleImage> darkSkinIcons, lightSkinIcons, retinaIcons, nonIconDirectoryImages;

        public IconBrowserDatabase(string searchString)
        {
            GetIcons();
            UpdateBySearch(searchString);
            UpdateByFilter(IconFilter.None);
            SortByName();
        }

        private void GetIcons()
        {
            AllIcons = AssetDatabaseUtil.GetEditorAssetBundleImages()
                .Where(i => !i.assetBundlePath.StartsWith("devicesimulator"))
                .Where(i => !i.assetBundlePath.StartsWith("cursors"))
                .Where(i => !i.assetBundlePath.StartsWith("brushes"))
                .Where(i => !i.assetBundlePath.StartsWith("avatar"))
                .Where(i => !i.name.ToLower().EndsWith(".small"))
                .ToArray();
            IconDictionary = AllIcons.ToDictionary(i => i.assetBundlePath, i => i);
            
            nonIconDirectoryImages = new HashSet<EditorAssetBundleImage>(AllIcons.Where(IsNonIconImage).ToArray());
            darkSkinIcons = new HashSet<EditorAssetBundleImage>(AllIcons.Where(IsDarkSkinIcon).ToArray());
            lightSkinIcons = new HashSet<EditorAssetBundleImage>(AllIcons.Where(IsLightSkinIcon).ToArray());
            retinaIcons = new HashSet<EditorAssetBundleImage>(AllIcons.Where(IsRetinaIcon).ToArray());

            InjectHiRezIcons();

            Icons = AllIcons;
        }

        private void InjectHiRezIcons()
        {
            foreach (var retinaIcon in retinaIcons)
            {
                var regularIconName = Path.GetFileNameWithoutExtension(retinaIcon.assetBundlePath);
                regularIconName = regularIconName.Substring(0, retinaIcon.name.Length - 3);
                regularIconName += Path.GetExtension(retinaIcon.assetBundlePath);
                
                var regularIconPath = Path.GetDirectoryName(retinaIcon.assetBundlePath)+$"/{regularIconName}";
                if (IconDictionary.TryGetValue(regularIconPath, out var icon))
                    icon.AddRetinaTexture(retinaIcon);
            }
        }

        private static bool IsDarkSkinIcon(EditorAssetBundleImage icon)
        {
            return icon.name.StartsWith("d_") ||
                   DoesPathContainsFolder(icon.assetBundlePath, "dark", "darkskin");
        }

        private bool IsLightSkinIcon(EditorAssetBundleImage icon)
        {
            return darkSkinIcons.Any(i => i.name == $"d_{icon.name}") ||
                   DoesPathContainsFolder(icon.assetBundlePath, "light", "lightskin");
        }

        private bool IsRetinaIcon(EditorAssetBundleImage icon)
        {
            return Regex.IsMatch(icon.name, @".*@\dx");
        }
        
        private bool IsNonIconImage(EditorAssetBundleImage img)
        {
            return !img.assetBundlePath.StartsWith("icon");
        }

        private static bool DoesPathContainsFolder(string path, params string[] folderNames)
        {
            var dirName = Path.GetDirectoryName(path);
            if (dirName == null)
                return false;
            var pathFolders = dirName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return pathFolders.Any(folderNames.Contains);
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
                    let lowerPath = icon.assetBundlePath.ToLower()
                    let lowerSearch = searchString.ToLower()
                    where lowerPath.Contains(lowerSearch)
                    orderby lowerPath.IndexOf(lowerSearch, StringComparison.Ordinal)
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

            IEnumerable<EditorAssetBundleImage> icons = AllIcons;

            if (!filter.HasFlag(IconFilter.AlternativeSkin))
                icons = icons.Where(icon => EditorGUIUtility.isProSkin
                    ? !lightSkinIcons.Contains(icon)
                    : !darkSkinIcons.Contains(icon));

            if (!filter.HasFlag(IconFilter.RetinaVersions))
                icons = icons.Where(i=>!IsRetinaIcon(i));
            if (!filter.HasFlag(IconFilter.OtherImages))
                icons = icons.Where(i => !IsNonIconImage(i));
            Icons = icons.ToArray();
        }

        private static (float h, float s, float v) GetIconAverageHSV(EditorAssetBundleImage icon)
        {
            var texture = icon.texture;
            var readableTexture = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 1);
            Graphics.CopyTexture(texture, readableTexture);
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