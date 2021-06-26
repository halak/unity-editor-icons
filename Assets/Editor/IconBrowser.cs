using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Halak;
using UnityEditor;
using UnityEngine;

public class IconBrowser : EditorWindow
{
    private (Texture2D tex, Color32 color)[] icons;
    private Vector2 scrollPos;

    [MenuItem("Icons/Browser")]
    static void OpenWindow() => GetWindow<IconBrowser>();

    private void OnEnable()
    {
        icons = (from icon in AssetDatabaseUtil.GetAllEditorIcons()
                where !icon.name.EndsWith("@2x")
                let hsv = GetIconAverageHSV(icon)
                orderby hsv.h, hsv.s, hsv.v
                select (icon, hsv.color)
            ).ToArray();
    }

    private (float h, float s, float v, Color32 color) GetIconAverageHSV(Texture2D icon)
    {
        var readableTexture = new Texture2D(icon.width, icon.height, icon.format, icon.mipmapCount > 1);
        Graphics.CopyTexture(icon, readableTexture);
        var averageColor = AverageColorFromTexture(readableTexture);
        Color.RGBToHSV(averageColor, out var h, out var s, out var v);
        DestroyImmediate(readableTexture);
        return (h, s, v, averageColor);
    }

    private void OnGUI()
    {
        int len = icons.Length;
        int rowSize = 10;
        using (new EditorGUIUtility.IconSizeScope(new Vector2(40, 40)))
        using (var s = new GUILayout.ScrollViewScope(scrollPos))
        {
            for (int i = 0; i < len; i += rowSize)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (int j = 0; j < rowSize && i + j < len; j++)
                    {
                        var icon = icons[i + j];
                        var content = new GUIContent(icon.tex, icon.tex.name);
                        //content.image = Texture2D.whiteTexture;
                        //Debug.Log($"{icon.color}");
                        //GUI.color = icon.color;
                        GUILayout.Label(content);
                        //GUI.color = Color.white;
                    }
                }
            }

            scrollPos = s.scrollPosition;
        }
    }

    private Color32 AverageColorFromTexture(Texture2D tex)
    {
        Color32[] texColors = tex.GetPixels32();

        int total = texColors.Length;

        float r = 0;
        float g = 0;
        float b = 0;

        for (int i = 0; i < total; i++)
        {
            r += texColors[i].r;

            g += texColors[i].g;

            b += texColors[i].b;
        }

        return new Color32((byte) (r / total), (byte) (g / total), (byte) (b / total), 255);
    }
}