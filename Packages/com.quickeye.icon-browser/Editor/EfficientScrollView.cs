using System;
using UnityEngine;

namespace QuickEye.Editor.IconWindow
{
    [Serializable]
    public class EfficientScrollView
    {
        [SerializeField]
        private Vector2 scrollPos;

        public Rect Position { get; private set; }
        public Rect ViewRect { get; private set; }
        public float ElementHeight { get; set; }

        public Action<Rect, int> DrawElement { get; set; }
        public int RowCount { get; set; }

        public void OnGUI()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                Position = rect;
            ViewRect = new Rect(Position)
            {
                height = RowCount * ElementHeight,
                width = Position.width - GUI.skin.verticalScrollbar.fixedWidth
            };

            var visibleRowCount = Mathf.CeilToInt(Position.height / ElementHeight) + 2;
            var listIndex = Mathf.FloorToInt(scrollPos.y / ElementHeight);

            var elementSize = new Vector2(ViewRect.width, ElementHeight);

            using (var s = new GUI.ScrollViewScope(Position, scrollPos, ViewRect))
            using (new GUI.GroupScope(ViewRect))
            {
                for (var i = 0; i < visibleRowCount && listIndex < RowCount; i++, listIndex++)
                {
                    var pos = new Vector2(0, ElementHeight * listIndex);
                    var elementRect = new Rect
                    {
                        position = pos,
                        size = elementSize
                    };
                    DrawElement?.Invoke(elementRect, listIndex);
                }

                scrollPos = s.scrollPosition;
            }

            // var debugText = $"{scrollPos} | visible: {listIndex + visibleRowCount}/{rowCount}/{visibleRowCount}";
            // GUI.Label(new Rect(0, 20, 1000, 20), debugText, EditorStyles.boldLabel);
        }
    }
}