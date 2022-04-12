using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static UnityEngine.GUILayout;
using Random = UnityEngine.Random;

namespace QuickEye.Editor
{
    public class IconBrowser : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Window/Icon Browser")]
        private static void OpenWindow()
        {
            var w = GetWindow<IconBrowser>("Icon Browser");
            w.titleContent.image = EditorGUIUtility.IconContent("Search Icon").image;
        }

        private static readonly Color LightSkinColor = new Color32(194, 194, 194, 255);
        private static readonly Color DarkSkinColor = new Color32(56, 56, 56, 255);
        private static readonly Color HighlightColor = new Color32(255, 255, 255, 20);
        private const int ListIconPadding = 4;

        private static Color BackgroundColor =>
            EditorGUIUtility.isProSkin ? DarkSkinColor : LightSkinColor;

        private static Color AlternativeSkinBackgroundColor =>
            EditorGUIUtility.isProSkin ? LightSkinColor : DarkSkinColor;

        private Color SelectedBackgroundColor =>
            drawAlternativeBackground ? AlternativeSkinBackgroundColor : BackgroundColor;

        [SerializeField]
        private string searchString = "";

        [SerializeField]
        private Sorting sortingMode;

        [SerializeField]
        private Layout layout;

        [SerializeField]
        private IconFilter filter;

        [SerializeField]
        private bool debugMode;

        [SerializeField]
        private bool drawAlternativeBackground;

        [SerializeField]
        private EfficientScrollView listView = new EfficientScrollView();

        private SearchField searchField;
        private IconBrowserDatabase database;

        private Rect sortingButtonRect, filterButtonRect;
        private float iconSize = 40;
        private readonly (float min, float max) iconSizeLimit = (16, 60);
        private int elementsInRow;
        private float iconRectWidth;
        private SelectedItem selectedItem;
        private bool HasSearch => !string.IsNullOrWhiteSpace(searchString);
        private Texture2D[] Icons => HasSearch ? database.SearchResult : database.Icons;

        private void OnEnable()
        {
            searchField = new SearchField();
            database = new IconBrowserDatabase(searchString);
            Sort(sortingMode);
            UpdateFilterAndSearch();
            UpdateLayout();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawIcons();
            DrawFooter();
            DrawDebugView();
        }

        private void OnIconClick(Texture2D icon)
        {
            selectedItem = new SelectedItem(icon);
        }

        private void DrawFooter()
        {
            if (selectedItem == null || selectedItem.icon == null)
                return;
            using (new HorizontalScope("box", Height(40)))
            {
                using (KeepIconAspectRatio(selectedItem.icon, new Vector2(40, 40)))
                    if (Button(selectedItem.icon, Styles.CenteredIcon, Width(43), ExpandHeight(true)))
                        ShowNotification(new GUIContent(selectedItem.icon));

                using (new VerticalScope())
                {
                    Field("Name", selectedItem.name, true);
                    Field("File ID", selectedItem.fileId.ToString(), true);
                    Field("Size", $"{selectedItem.icon.width}x{selectedItem.icon.height}", false);
                }

                using (new VerticalScope(Width(50)))
                {
                    if (Button("Save"))
                        ExportSelectedIcon();
                    if (Button("Icon Content"))
                        CopyToClipboard("Icon Content", $"EditorGUIUtility.IconContent(\"{selectedItem.name}\")");
                    if (debugMode && Button("Debug Export"))
                    {
                        TextureUtils.DebugExport(selectedItem.icon);
                    }
                }
            }

            void Field(string label, string value, bool copy)
            {
                using (new HorizontalScope())
                {
                    Label($"{label}: ", Width(45));
                    Label(value);
                }

                var r = GUILayoutUtility.GetLastRect();
                if (copy && GUI.Button(r, GUIContent.none, GUIStyle.none))
                    CopyToClipboard(label, value);
            }
        }

        private void CopyToClipboard(string valueName, string value)
        {
#if UNITY_2019_1_OR_NEWER
            ShowNotification(new GUIContent($"Copied {valueName}"), .2f);
#else
            ShowNotification(new GUIContent($"Copied {valueName}"));
#endif
            GUIUtility.systemCopyBuffer = value;
        }

        private void ExportSelectedIcon()
        {
            var path = EditorUtility.SaveFilePanel("Save icon", "Assets", selectedItem.name, "png");
            if (string.IsNullOrEmpty(path))
                return;
            TextureUtils.ExportIconToPath(path, selectedItem.icon);
            if (path.StartsWith(Application.dataPath))
            {
                path = path.Remove(0, Application.dataPath.Length);
                AssetDatabase.ImportAsset($"Assets{path}");
            }
        }

        private void DrawListElement(Rect rect, int index)
        {
            var icon = Icons[index];
            var iconContent = new GUIContent(icon);
            var textContent = new GUIContent(icon.name);
            using (KeepIconAspectRatio(icon, new Vector2(iconSize, iconSize)))
            {
                var iconRect = new Rect(rect) { size = new Vector2(iconSize + 4, iconSize + 4) };
                iconRect.x += ListIconPadding;
                iconRectWidth = iconRect.width + ListIconPadding * 2;
                DrawSelectedBox(rect, icon);
                GUI.Label(iconRect, iconContent, Styles.CenteredIcon);

                var labelRect = new Rect(rect)
                {
                    xMin = iconRect.xMax + ListIconPadding
                };
                var labelStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                };

                GUI.Label(labelRect, textContent, labelStyle);

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                {
                    OnIconClick(icon);
                }
            }
        }

        private void DrawSelectedBox(Rect rect, Texture2D icon)
        {
            if (selectedItem?.icon == icon)
            {
                EditorGUI.DrawRect(rect, HighlightColor);
            }
        }

        private void DrawGridElement(Rect rect, int rowIndex)
        {
            var iconCount = Icons.Length;
            var iconStyle = Styles.CenteredIcon;
            iconRectWidth = iconSize + iconStyle.padding.horizontal + 1; // + style.margin.right;
            var eInRow = rect.width / iconRectWidth;
            elementsInRow = Mathf.FloorToInt(eInRow);

            var index = rowIndex * elementsInRow;

            for (var j = 0; j < elementsInRow && index < iconCount; j++, index++)
            {
                var icon = Icons[index];
                var content = new GUIContent(icon);

                using (KeepIconAspectRatio(icon, new Vector2(iconSize, iconSize)))
                {
                    var buttonRect = new Rect();
                    buttonRect.width = buttonRect.height = iconRectWidth;
                    buttonRect.y = rect.y;
                    buttonRect.x = j * iconRectWidth;
                    DrawSelectedBox(buttonRect, icon);

                    if (GUI.Button(buttonRect, content, iconStyle))
                        OnIconClick(icon);
                }
            }
        }

        private int GetGridRowCount()
        {
            var iconCount = Icons.Length;
            var style = new GUIStyle("label");
            iconRectWidth = iconSize + style.padding.horizontal + 1;
            elementsInRow = Mathf.FloorToInt(listView.Position.width / iconRectWidth);
            var x = Mathf.CeilToInt((float)iconCount / elementsInRow);
            return x;
        }

        private void DrawDebugView()
        {
            if (!debugMode)
                return;
            for (int i = 0; i < elementsInRow; i++)
            {
                var pos = new Vector2(i * iconRectWidth, 0);
                EditorGUI.DrawRect(new Rect(pos, new Vector2(iconRectWidth, 5)), Random.ColorHSV());
            }
        }

        private void DrawToolbar()
        {
            using (new HorizontalScope(EditorStyles.toolbar, ExpandWidth(true)))
            {
                SortingButton();
                ViewModeButton();
                BackgroundToggle();
                FilterButton();
                Space(2);
                SearchField();
                FlexibleSpace();
                IconSizeSlider();
            }
        }

        private void IconSizeSlider()
        {
            iconSize = HorizontalSlider(iconSize, iconSizeLimit.min, iconSizeLimit.max, MaxWidth(100),
                MinWidth(55));
        }

        private void SearchField()
        {
            using (var s = new EditorGUI.ChangeCheckScope())
            {
                searchString = searchField.OnToolbarGUI(searchString, MaxWidth(200));
                if (s.changed)
                    database.UpdateBySearch(searchString);
            }
        }

        private void BackgroundToggle()
        {
            drawAlternativeBackground =
                Toggle(drawAlternativeBackground, EditorGUIUtility.IconContent("SceneViewLighting"),
                    EditorStyles.toolbarButton,
                    Width(30));
        }

        private void ViewModeButton()
        {
            var iconName = Styles.GetLayoutIcon(layout);
            var iconContent = EditorGUIUtility.IconContent(iconName);
            using (KeepIconAspectRatio(iconContent.image, new Vector2(13, 13)))
                if (Button(iconContent, EditorStyles.toolbarButton, Width(30)))
                {
                    layout = layout == Layout.Grid ? Layout.List : Layout.Grid;
                    UpdateLayout();
                }
        }

        private void SortingButton()
        {
            if (Button("Sorting", EditorStyles.toolbarDropDown, Width(60)))
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Name"), sortingMode == Sorting.Name, () => Sort(Sorting.Name));
                menu.AddItem(new GUIContent("Color"), sortingMode == Sorting.Color, () => Sort(Sorting.Color));

                menu.DropDown(sortingButtonRect);
            }

            if (Event.current.type == EventType.Repaint)
                sortingButtonRect = GUILayoutUtility.GetLastRect();
        }

        private void FilterButton()
        {
            if (Button(EditorGUIUtility.IconContent(Styles.FilterButtonIcon), EditorStyles.toolbarDropDown, Width(35)))
            {
                var menu = new GenericMenu();
                AddContextMenuItem("No Filters", IconFilter.None);
                AddContextMenuItem("Alternative Skin", IconFilter.AlternativeSkin);
                AddContextMenuItem("Smaller Sizes", IconFilter.SmallerVersions);
                menu.DropDown(filterButtonRect);

                void AddContextMenuItem(string label, IconFilter filterToToggle)
                {
                    var isOn = filterToToggle == IconFilter.None
                        ? filter == IconFilter.None
                        : filter.HasFlag(filterToToggle);
                    menu.AddItem(new GUIContent(label), isOn, () =>
                    {
                        if (filterToToggle == IconFilter.None)
                            filter = IconFilter.None;
                        else
                            filter ^= filterToToggle;
                        UpdateFilterAndSearch();
                    });
                }
            }

            if (Event.current.type == EventType.Repaint)
                filterButtonRect = GUILayoutUtility.GetLastRect();
        }

        private void UpdateFilterAndSearch()
        {
            database.UpdateByFilter(filter);
            Sort(sortingMode);
            database.UpdateBySearch(searchString);
        }

        private void Sort(Sorting newSorting)
        {
            sortingMode = newSorting;
            switch (sortingMode)
            {
                case Sorting.Name:
                    database.SortByName();
                    break;
                case Sorting.Color:
                    database.SortByColor();
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawIcons()
        {
            listView.RowCount = layout == Layout.List ? Icons.Length : GetGridRowCount();
            var style = EditorStyles.label;
            listView.ElementHeight = iconSize + style.padding.vertical + style.margin.vertical;
            if (drawAlternativeBackground)
            {
                var rect = layout == Layout.Grid
                    ? listView.Position
                    : new Rect(listView.Position)
                    {
                        width = iconRectWidth,
                    };

                EditorGUI.DrawRect(rect, AlternativeSkinBackgroundColor);
            }

            listView.OnGUI();
        }

        private void UpdateLayout()
        {
            listView.DrawElement = layout == Layout.Grid ? (Action<Rect, int>)DrawGridElement : DrawListElement;
        }

        private static EditorGUIUtility.IconSizeScope KeepIconAspectRatio(Texture icon, Vector2 size)
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

        [Serializable]
        private enum Sorting
        {
            Name,
            Color
        }

        [Serializable]
        private enum Layout
        {
            Grid,
            List
        }

        [Serializable]
        private class SelectedItem
        {
            public string name;
            public long fileId;
            public Texture2D icon;

            public SelectedItem(Texture2D icon)
            {
                name = icon.name;
                fileId = AssetDatabaseUtil.GetFileId(icon);
                this.icon = icon;
            }
        }


        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Debug Mode"), debugMode, () => debugMode ^= true);
        }

        private static class Styles
        {
            public static readonly GUIStyle CenteredIcon = new GUIStyle("label")
            {
                alignment = TextAnchor.MiddleCenter
            };
            
            public const string FilterButtonIcon = 
#if UNITY_2021_1_OR_NEWER
                "Filter Icon";
#else
                "FilterByLabel";
#endif

            public static string GetLayoutIcon(Layout layout)
            {
                return layout == Layout.Grid
#if UNITY_2021_1_OR_NEWER
                ? "GridView On" : "ListView On";
#else
                    ? "GridLayoutGroup Icon"
                    : "VerticalLayoutGroup Icon";
#endif
            }
        }
    }
}