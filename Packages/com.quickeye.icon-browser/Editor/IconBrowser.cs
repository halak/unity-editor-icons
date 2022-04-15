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

        private SearchField _searchField;
        private IconBrowserDatabase _database;

        private Rect _sortingButtonRect, _filterButtonRect;
        private float _iconSize = 40;
        private readonly (float min, float max) _iconSizeLimit = (16, 60);
        private int _elementsInRow;
        private float _iconRectWidth;
        private int _listViewControlId;

        [SerializeField]
        private EditorAssetBundleImage _selectedImage;

        [SerializeField]
        private int _selectedIndex;


        private bool HasSearch => !string.IsNullOrWhiteSpace(searchString);
        private EditorAssetBundleImage[] Icons => HasSearch ? _database.SearchResult : _database.Icons;

        private void OnEnable()
        {
            _searchField = new SearchField();
            _database = new IconBrowserDatabase(searchString);
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

        private void ArrowNavigation()
        {
            var ev = Event.current;
            if (ev.type != EventType.KeyDown)
                return;
            switch (ev.keyCode)
            {
                case KeyCode.LeftArrow:
                    OnIconClick(Mathf.Max(_selectedIndex - 1, 0));
                    ev.Use();
                    break;
                case KeyCode.RightArrow:
                    OnIconClick(Mathf.Min(_selectedIndex + 1, Icons.Length - 1));
                    ev.Use();
                    break;
                case KeyCode.UpArrow:
                    OnIconClick(Mathf.Max(_selectedIndex - _elementsInRow, 0));
                    ev.Use();
                    break;
                case KeyCode.DownArrow:
                    OnIconClick(Mathf.Min(_selectedIndex + _elementsInRow, Icons.Length - 1));
                    ev.Use();
                    break;
            }
        }

        private void OnIconClick(int index)
        {
            _selectedImage = Icons[index];
            _selectedIndex = index;
            GUIUtility.keyboardControl = 0;
        }

        private void DrawFooter()
        {
            if (_selectedImage == null || _selectedImage.texture == null)
                return;

            using (new HorizontalScope("box", Height(40), ExpandWidth(true)))
            {
                using (KeepIconAspectRatio(_selectedImage, new Vector2(40, 40)))
                    if (Button(_selectedImage, Styles.CenteredIcon, Width(43), ExpandHeight(true)))
                        ShowNotification(Event.current.shift
                            ? EditorGUIUtility.IconContent(_selectedImage.name)
                            : new GUIContent(_selectedImage));

                using (new VerticalScope(MinWidth(50)))
                {
                    Field(Label, "Name", _selectedImage.name, true);
                    Field(Label, "File ID", _selectedImage.fileId.ToString(), true);
                    Field(Label, "Size", $"{_selectedImage.texture.width}x{_selectedImage.texture.height}", false);
                }

                using (new VerticalScope())
                {
                    Field(SelectableLabel, "Path", _selectedImage.assetBundlePath, true);
                    Field(Label, "Scaling", _selectedImage.RetinaVersion ?? "One Size", false);
                }

                FlexibleSpace();
                using (new VerticalScope(Width(50)))
                {
                    if (Button("Save"))
                        ExportSelectedIcon();
                    if (Button("Icon Content"))
                        CopyToClipboard("Icon Content", $"EditorGUIUtility.IconContent(\"{_selectedImage.name}\")");
                }
            }

            void Field(Action<string, GUILayoutOption[]> control, string label, string value, bool copy)
            {
                using (new HorizontalScope())
                {
                    Label($"{label}: ", Width(45));
                    control(value, null);
                }

                var r = GUILayoutUtility.GetLastRect();
                if (copy && GUI.Button(r, GUIContent.none, GUIStyle.none))
                    CopyToClipboard(label, value);
            }
        }

        private static void SelectableLabel(
            string text,
            params GUILayoutOption[] options)
        {
            var style = EditorStyles.label;
            EditorGUI.SelectableLabel(EditorGUILayout.GetControlRect(false, 18f, style, options), text, style);
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
            var path = EditorUtility.SaveFilePanel("Save icon", "Assets", _selectedImage.name, "png");
            if (string.IsNullOrEmpty(path))
                return;
            TextureUtils.ExportIconToPath(path, _selectedImage.texture);
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
            using (KeepIconAspectRatio(icon, new Vector2(_iconSize, _iconSize)))
            {
                var iconRect = new Rect(rect) { size = new Vector2(_iconSize + 4, _iconSize + 4) };
                iconRect.x += ListIconPadding;
                _iconRectWidth = iconRect.width + ListIconPadding * 2;
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
                    OnIconClick(index);
                }
            }
        }

        private void DrawSelectedBox(Rect rect, EditorAssetBundleImage icon)
        {
            if (_selectedImage == icon)
            {
                EditorGUI.DrawRect(rect, HighlightColor);
            }
        }

        private void DrawGridElement(Rect rect, int rowIndex)
        {
            var iconCount = Icons.Length;
            var iconStyle = Styles.CenteredIcon;
            _iconRectWidth = _iconSize + iconStyle.padding.horizontal + 1; // + style.margin.right;
            var eInRow = rect.width / _iconRectWidth;
            _elementsInRow = Mathf.FloorToInt(eInRow);

            var index = rowIndex * _elementsInRow;

            for (var i = 0; i < _elementsInRow && index < iconCount; i++, index++)
            {
                var icon = Icons[index];

                var content =
                    new GUIContent(filter.HasFlag(IconFilter.RetinaVersions) ? icon.texture : icon.RetinaTexture);

                using (KeepIconAspectRatio(icon, new Vector2(_iconSize, _iconSize)))
                {
                    var buttonRect = new Rect();
                    buttonRect.width = buttonRect.height = _iconRectWidth;
                    buttonRect.y = rect.y;
                    buttonRect.x = i * _iconRectWidth;
                    DrawSelectedBox(buttonRect, icon);

                    if (GUI.Button(buttonRect, content, iconStyle))
                        OnIconClick(index);
                }
            }
        }

        private int GetGridRowCount()
        {
            var iconCount = Icons.Length;
            var style = new GUIStyle("label");
            _iconRectWidth = _iconSize + style.padding.horizontal + 1;
            _elementsInRow = Mathf.FloorToInt(listView.Position.width / _iconRectWidth);
            var x = Mathf.CeilToInt((float)iconCount / _elementsInRow);
            return x;
        }

        private void DrawDebugView()
        {
            if (!debugMode)
                return;
            for (int i = 0; i < _elementsInRow; i++)
            {
                var pos = new Vector2(i * _iconRectWidth, 0);
                EditorGUI.DrawRect(new Rect(pos, new Vector2(_iconRectWidth, 5)), Random.ColorHSV());
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
                IconCount();
                FlexibleSpace();
                IconSizeSlider();
            }
        }

        private void IconCount()
        {
            Label($"({Icons.Length})");
        }

        private void IconSizeSlider()
        {
            _iconSize = HorizontalSlider(_iconSize, _iconSizeLimit.min, _iconSizeLimit.max, MaxWidth(100),
                MinWidth(55));
        }

        private void SearchField()
        {
            using (var s = new EditorGUI.ChangeCheckScope())
            {
                searchString = _searchField.OnToolbarGUI(searchString, MaxWidth(200));
                if (s.changed)
                    _database.UpdateBySearch(searchString);
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

                menu.DropDown(_sortingButtonRect);
            }

            if (Event.current.type == EventType.Repaint)
                _sortingButtonRect = GUILayoutUtility.GetLastRect();
        }

        private void FilterButton()
        {
            if (Button(EditorGUIUtility.IconContent(Styles.FilterButtonIcon), EditorStyles.toolbarDropDown, Width(35)))
            {
                var menu = new GenericMenu();
                AddContextMenuItem("No Filters", IconFilter.None);
                AddContextMenuItem("Alternative Skin", IconFilter.AlternativeSkin);
                AddContextMenuItem("Retina Versions", IconFilter.RetinaVersions);
                AddContextMenuItem("Non Icon Directory", IconFilter.OtherImages);
                menu.DropDown(_filterButtonRect);

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
                _filterButtonRect = GUILayoutUtility.GetLastRect();
        }

        private void UpdateFilterAndSearch()
        {
            _database.UpdateByFilter(filter);
            Sort(sortingMode);
            _database.UpdateBySearch(searchString);
        }

        private void Sort(Sorting newSorting)
        {
            sortingMode = newSorting;
            switch (sortingMode)
            {
                case Sorting.Name:
                    _database.SortByName();
                    break;
                case Sorting.Color:
                    _database.SortByColor();
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawIcons()
        {
            listView.RowCount = layout == Layout.List ? Icons.Length : GetGridRowCount();
            var style = EditorStyles.label;
            listView.ElementHeight = _iconSize + style.padding.vertical + style.margin.vertical;
            if (drawAlternativeBackground)
            {
                var rect = layout == Layout.Grid
                    ? listView.Position
                    : new Rect(listView.Position)
                    {
                        width = _iconRectWidth,
                    };

                EditorGUI.DrawRect(rect, AlternativeSkinBackgroundColor);
            }

            listView.OnGUI();
            ArrowNavigation();
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
                    ? "GridView On"
                    : "ListView On";
#else
                    ? "GridLayoutGroup Icon"
                    : "VerticalLayoutGroup Icon";
#endif
            }
        }
    }
}