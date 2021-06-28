using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static UnityEngine.GUILayout;

namespace QuickEye.Editor
{
// add list view and grid view
// icon size slider
// sorting buttons
    public class IconBrowser : EditorWindow
    {
        private string[] iconBlacklist =
        {
            "StateMachineEditor.Background"
        };

        private Texture2D[] icons;
        private Vector2 scrollPos;
        private SearchField searchField;
        private Texture2D[] searchResult;
        private string searchText;
        private float iconSize = 40;
        private bool HasSearch => !string.IsNullOrWhiteSpace(searchText);

        [SerializeField]
        private Sorting sortingMode;

        private Rect sortingButtonRect;
        private void OnEnable()
        {
            searchField = new SearchField();
            GetIcons();
            Sort();
        }

        private void OnGUI()
        {
            DrawToolbar();

            DrawIcons();
        }

        private void DrawToolbar()
        {
            using (new HorizontalScope(EditorStyles.toolbar, ExpandWidth(true)))
            {
                using (var s = new EditorGUI.ChangeCheckScope())
                {
                    searchText = searchField.OnToolbarGUI(searchText, MaxWidth(200));
                    if (s.changed)
                        UpdateBySearch();
                }

                FlexibleSpace();
                iconSize = HorizontalSlider(iconSize, 16, 60, MaxWidth(100), MinWidth(55));
                SortingButton();
            }
        }

        private void SortingButton()
        {
            if (Button("Sorting", EditorStyles.toolbarDropDown, Width(60)))
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Name"), sortingMode == Sorting.Name, SortByName);
                menu.AddItem(new GUIContent("Color"), sortingMode == Sorting.Color, SortByColor);

                menu.DropDown(sortingButtonRect);
            }

            if (Event.current.type == EventType.Repaint)
                sortingButtonRect = GUILayoutUtility.GetLastRect();
        }

        [MenuItem("Window/Icon Browser")]
        private static void OpenWindow()
        {
            var w = GetWindow<IconBrowser>("Icon Browser");
            w.titleContent.image = EditorGUIUtility.IconContent("Search Icon").image;
        }

        private void GetIcons()
        {
            icons = (from icon in AssetDatabaseUtil.GetAllEditorIcons()
                    where !icon.name.EndsWith("@2x")
                    where !icon.name.StartsWith("d_")
                    select icon
                ).ToArray();
        }

        private void Sort()
        {
            switch (sortingMode)
            {
                case Sorting.Name: SortByName(); break;
                case Sorting.Color: SortByColor(); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void SortByColor()
        {
            sortingMode = Sorting.Color;
            icons = (from icon in icons
                    let hsv = GetIconAverageHSV(icon)
                    orderby hsv.h, hsv.s, hsv.v
                    select icon
                ).ToArray();
        }

        private void SortByName()
        {
            sortingMode = Sorting.Name;
            icons = (from icon in icons
                    orderby icon.name
                    select icon
                ).ToArray();
        }

        private void UpdateBySearch()
        {
            searchResult = icons.Where(i => i.name.Contains(searchText.ToLower())).ToArray();
        }

        private static (float h, float s, float v) GetIconAverageHSV(Texture2D icon)
        {
            var readableTexture = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);
            Graphics.CopyTexture(icon, readableTexture);
            var averageColor = AverageColorFromTexture(readableTexture);
            Color.RGBToHSV(averageColor, out var h, out var s, out var v);
            DestroyImmediate(readableTexture);
            return (h, s, v);
        }

        private void DrawIcons()
        {
            var collection = HasSearch ? searchResult : icons;
            var len = collection.Length;
            var style = EditorStyles.label;
            var elementWidth = iconSize + style.padding.horizontal + style.margin.horizontal;
            var rowSize = Mathf.FloorToInt(position.width / elementWidth);
            using (var s = new ScrollViewScope(scrollPos))
            {
                for (var i = 0; i < len; i += rowSize)
                    using (new HorizontalScope(Height(iconSize)))
                    {
                        for (var j = 0; j < rowSize && i + j < len; j++)
                        {
                            var icon = collection[i + j];
                            var content = new GUIContent(icon, icon.name);

                            using (KeepIconAspectRatio(icon, new Vector2(iconSize, iconSize)))
                            {
                                if (Button(content, "label", ExpandHeight(true)))
                                {
                                    GUIUtility.systemCopyBuffer = icon.name;
                                    Selection.activeObject = icon;
                                    Debug.Log($"Name: {icon.name} FileID: {AssetDatabaseUtil.GetFileId(icon)}");
                                }
                            }
                        }
                    }

                scrollPos = s.scrollPosition;
            }
        }

        private static EditorGUIUtility.IconSizeScope KeepIconAspectRatio(Texture icon, Vector2 size)
        {
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

            return new Color32((byte) (r / total), (byte) (g / total), (byte) (b / total), 255);
        }

        [Serializable]
        private enum Sorting
        {
            Name,
            Color
        }
    }
}