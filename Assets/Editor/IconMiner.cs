using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using static QuickEye.Editor.AssetDatabaseUtil;

// ReSharper disable BadChildStatementIndent

namespace QuickEye.Editor
{
    public static class IconMiner
    {
        private const string PackageDir = "Packages/com.quickeye.icon-browser/";
        private const string DocumentationDir = "Documentation~/";
        private const string SmallIconsDir = "icons/small/";

        private const string FullDocumentationDir = PackageDir + DocumentationDir;

        private static EditorAssetBundleImage[] GetIcons()
        {
            var db = new IconBrowserDatabase("");
            db.SortByColor();
            return db.Icons;
        }

        [MenuItem("Unity Editor Icons/Export All %e", priority = -1001)]
        private static void ExportIcons()
        {
            EditorUtility.DisplayProgressBar("Export Icons", "Exporting...", 0.0f);
            try
            {
                var icons = GetIcons();
                for (var i = 0; i < icons.Length; i++)
                {
                    TextureUtils.ExportIconToDir("icons/original/", icons[i], false);
                    EditorUtility.DisplayProgressBar("Export Icons", "Exporting...", (float)i / icons.Length);
                }

                Debug.Log($"{icons.Length} icons has been exported!");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Unity Editor Icons/Generate README.md %g", priority = -1000)]
        private static void GenerateReadmeFile()
        {
            EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
            try
            {
                var template = Resources.Load<TextAsset>("Template").text;
                template = template.Replace("<UnityVersion>", Application.unityVersion);

                var readmeContents = new StringBuilder();
                readmeContents.AppendLine(template);

                var icons = GetIcons();
                for (var i = 0; i < icons.Length; i++)
                {
                    var icon = icons[i];
                    EditorUtility.DisplayProgressBar("Generate README.md",
                        $"Generating... ({i + 1}/{icons.Length})", (float)i / icons.Length);

                    var iconFullPath =
                        TextureUtils.ExportIconToDir($"{FullDocumentationDir}/{SmallIconsDir}", icon, true);
                    var iconLinkPath = Path.Combine(DocumentationDir, SmallIconsDir, Path.GetFileName(iconFullPath));
                    var escapedUrl = iconLinkPath.Replace(" ", "%20").Replace('\\', '/');
                    var fileId = GetFileId(icon);
                    readmeContents.AppendLine($"| ![]({escapedUrl}) | `{icon.name}` | `{fileId}` |");
                }

                File.WriteAllText($"{PackageDir}/README.md", readmeContents.ToString());

                Debug.Log("'READMD.md' has been generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Unity Editor Icons/Generate README.md2", priority = -1000)]
        private static void GenerateReadmeFile2()
        {
            EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
            try
            {
                var template = Resources.Load<TextAsset>("Template").text;
                template = template.Replace("<UnityVersion>", Application.unityVersion);

                var readmeContents = new StringBuilder();
                readmeContents.AppendLine(template);

                var icons = GetIcons();
                var table = new HtmlTable(13);
                for (var i = 0; i < icons.Length; i++)
                {
                    var icon = icons[i];
                    EditorUtility.DisplayProgressBar("Generate README.md",
                        $"Generating... ({i + 1}/{icons.Length})", (float)i / icons.Length);
                    var iconFullPath =
                        TextureUtils.ExportIconToDir($"Assets/readmetest/{SmallIconsDir}", icon, true);
                    var infoPath = CreateIconInfoFile(icon);
                    var iconLinkPath = Path.Combine(SmallIconsDir, Path.GetFileName(iconFullPath));
   
                    table.Append(CreateTableElement(iconLinkPath,infoPath));
                }

                readmeContents.AppendLine(table.ToString());
                File.WriteAllText($"Assets/readmetest/README.md", readmeContents.ToString());

                Debug.Log("'READMD.md' has been generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string CreateTableElement(string iconPath, string iconInfoPath)
        {
            return $"<a href=\"{EscapeUrl(iconInfoPath)}\"><img src=\"{EscapeUrl(iconPath)}\"/></a>";
        }

        private static string EscapeUrl(string iconLinkPath)
        {
            return iconLinkPath.Replace(" ", "%20").Replace('\\', '/');
        }

        private static string CreateIconInfoFile(EditorAssetBundleImage img)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Name: {img.name}");
            sb.AppendLine($"File ID: {img.fileId}");
            sb.AppendLine($"Scaling: {img.RetinaVersion ?? "One Size"}");
            //sb.AppendLine($"Has Dark Skin Version: true/false");
            File.WriteAllText($"Assets/readmetest/{SmallIconsDir}/{img.name}.txt", sb.ToString());
            return $"{SmallIconsDir}/{img.name}.txt";
        }
    }

    public class HtmlTable
    {
        private int _columns;
        private List<string> elements = new List<string>();

        public HtmlTable(int columns)
        {
            _columns = columns;
        }

        public void Append(string element)
        {
            elements.Add(element);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var rows = GetRowCount(_columns, elements.Count);
            var index = 0;

            using (new HtmlTag("table", sb))
                for (var row = 0; row < rows; row++)
                {
                    using (new HtmlTag("tr", sb))
                        for (int i = 0; i < _columns && index < elements.Count; i++, index++)
                        {
                            using (new HtmlTag("td", sb))
                                sb.AppendLine(elements[index]);
                        }
                }

            return sb.ToString();
        }

        private static int GetRowCount(int columns, int elementCount) =>
            Mathf.CeilToInt((float)elementCount / columns);
    }

    public class HtmlTag : IDisposable
    {
        private readonly string tag;
        private readonly StringBuilder sb;

        public HtmlTag(string tag, StringBuilder stringBuilder)
        {
            this.tag = tag;
            sb = stringBuilder;
            sb.AppendLine($"<{tag}>");
        }

        public void Dispose()
        {
            sb.AppendLine($"</{tag}>");
        }
    }
}