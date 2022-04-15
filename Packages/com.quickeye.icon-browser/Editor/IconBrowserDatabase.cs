using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace QuickEye.Editor.IconWindow
{
    [Serializable]
    public class IconBrowserDatabase
    {
        private static string[] _iconBlacklist =
        {
            "StateMachineEditor.Background",
            "scene-template-empty-scene",
            "scene-template-2d-scene",
        };

        private static string[] _iconPathsBlacklist =
        {
            "devicesimulator",
            "icons/avatarinspector",
            "cursors",
            "brushes",
            "avatar"
        };

        private EditorAssetBundleImage[] _allIcons;

        private Dictionary<string, EditorAssetBundleImage> _iconDictionary =
            new Dictionary<string, EditorAssetBundleImage>();

        public EditorAssetBundleImage[] Icons { get; private set; }
        public EditorAssetBundleImage[] SearchResult { get; private set; }

        private HashSet<EditorAssetBundleImage> _darkSkinIcons, _lightSkinIcons, _retinaIcons, _nonIconDirectoryImages;

        public IconBrowserDatabase(string searchString)
        {
            GetIcons();
            UpdateBySearch(searchString);
            UpdateByFilter(IconFilter.None);
            SortByName();
        }

        private void GetIcons()
        {
            _allIcons = AssetDatabaseUtil.GetEditorAssetBundleImages()
                .Where(i => _iconPathsBlacklist.All(p => !i.assetBundlePath.StartsWith(p)))
                .Where(i => _iconBlacklist.All(n => i.name != n))
                .Where(i => !i.name.ToLower().EndsWith(".small"))
                .ToArray();

            _iconDictionary = _allIcons.ToDictionary(i => i.assetBundlePath, i => i);
            _nonIconDirectoryImages =
                new HashSet<EditorAssetBundleImage>(_allIcons.Where(IsNonIconImage).ToArray());
            _darkSkinIcons = new HashSet<EditorAssetBundleImage>(_allIcons.Where(IsDarkSkinIcon).ToArray());
            _lightSkinIcons = new HashSet<EditorAssetBundleImage>(_allIcons.Where(IsLightSkinIcon).ToArray());
            _retinaIcons = new HashSet<EditorAssetBundleImage>(_allIcons.Where(IsRetinaIcon).ToArray());

            InjectHiRezIcons();

            Icons = _allIcons;
        }

        private void InjectHiRezIcons()
        {
            foreach (var retinaIcon in _retinaIcons)
            {
                var regularIconName = Path.GetFileNameWithoutExtension(retinaIcon.assetBundlePath);
                regularIconName = regularIconName.Substring(0, retinaIcon.name.Length - 3);
                regularIconName += Path.GetExtension(retinaIcon.assetBundlePath);

                var regularIconPath = Path.GetDirectoryName(retinaIcon.assetBundlePath) + $"/{regularIconName}";
                if (_iconDictionary.TryGetValue(regularIconPath, out var icon))
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
            return _iconDictionary.ContainsKey($"d_{icon.name}") ||
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
                    orderby hsv.h, hsv.s, icon.name, hsv.v
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
                this.Icons = _allIcons;
                return;
            }

            IEnumerable<EditorAssetBundleImage> icons = _allIcons;

            if (!filter.HasFlag(IconFilter.AlternativeSkin))
                icons = icons.Where(icon => EditorGUIUtility.isProSkin
                    ? !_lightSkinIcons.Contains(icon)
                    : !_darkSkinIcons.Contains(icon));

            if (!filter.HasFlag(IconFilter.RetinaVersions))
                icons = icons.Where(i => !IsRetinaIcon(i));
            if (!filter.HasFlag(IconFilter.OtherImages))
                icons = icons.Where(i => !IsNonIconImage(i));
            this.Icons = icons.ToArray();
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
            var newTotal = total;
            float r = 0;
            float g = 0;
            float b = 0;

            for (var i = 0; i < total; i++)
            {
                if (texColors[i].a == 0)
                {
                    newTotal--;
                    continue;
                }

                var mul = (float)texColors[i].a / 255;
                r += texColors[i].r * mul;
                g += texColors[i].g * mul;
                b += texColors[i].b * mul;
            }

            r /= newTotal;
            g /= newTotal;
            b /= newTotal;
            return new Color32((byte)r, (byte)g, (byte)b, 255);
        }
    }
}