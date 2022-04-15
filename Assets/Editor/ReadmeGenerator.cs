using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace QuickEye.Editor
{
    public static class ReadmeGenerator
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

        [MenuItem("Repo Tools/Generate README.md", priority = -1000)]
        private static void GenerateReadmeFile()
        {
            EditorUtility.DisplayProgressBar("Generate README.md", "Generating...", 0.0f);
            try
            {
                var content = new StringBuilder();
                content.AppendLine(GetReadmeTemplate());

                var icons = GetIcons();
                var table = new HtmlTable(13);
                for (var i = 0; i < icons.Length; i++)
                {
                    var icon = icons[i];
                    EditorUtility.DisplayProgressBar("Generate README.md",
                        $"Generating... ({i + 1}/{icons.Length})", (float)i / icons.Length);
                    var imageFullPath =
                        TextureUtils.ExportIconToDir($"{FullDocumentationDir}/{SmallIconsDir}", icon, true);
                    var imagePath = Path.Combine(DocumentationDir, SmallIconsDir, Path.GetFileName(imageFullPath));
                    CreateIconInfoFile(icon, out var iconInfoPath);
                    table.Append(CreateTableElement(imagePath, iconInfoPath));
                }

                content.AppendLine(table.ToString());
                File.WriteAllText($"{PackageDir}/README.md", content.ToString());

                Debug.Log("'READMD.md' has been generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static string GetReadmeTemplate()
        {
            var template = Resources.Load<TextAsset>("ReadmeTemplate").text;
            template = template.Replace("<UnityVersion>", Application.unityVersion);
            return template;
        }
        private static string GetIconInfoTemplate(string iconPath,string iconName,string fileId)
        {
            var template = Resources.Load<TextAsset>("IconInfoTemplate").text;
            template = template.Replace("<ICON PATH>", iconPath);
            template = template.Replace("<ICON NAME>", iconName);
            template = template.Replace("<ICON FILEID>", fileId);
            return template;
        }
        private static string CreateTableElement(string iconPath, string iconInfoPath)
        {
            return $"<a href=\"{EscapeUrl(iconInfoPath)}\"><img src=\"{EscapeUrl(iconPath)}\"/></a>";
        }

        private static string EscapeUrl(string iconLinkPath)
        {
            return iconLinkPath.Replace(" ", "%20").Replace('\\', '/');
        }

        private static void CreateIconInfoFile(EditorAssetBundleImage img, out string pathRelativeToPackage)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetIconInfoTemplate($"{img.name}.png",img.name,img.fileId.ToString()));
            var path = Path.Combine(FullDocumentationDir, SmallIconsDir, $"{img.name}.md");
            File.WriteAllText(path, sb.ToString());
            pathRelativeToPackage = Path.Combine(DocumentationDir, SmallIconsDir, $"{img.name}.md");
        }
    }

    public class HtmlTable
    {
        private readonly int _columns;
        private readonly List<string> _elements = new List<string>();

        public HtmlTable(int columns)
        {
            _columns = columns;
        }

        public void Append(string element)
        {
            _elements.Add(element);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var rows = GetRowCount(_columns, _elements.Count);
            var index = 0;

            using (new HtmlTag("table", sb))
                for (var row = 0; row < rows; row++)
                {
                    using (new HtmlTag("tr", sb))
                        for (int i = 0; i < _columns && index < _elements.Count; i++, index++)
                        {
                            using (new HtmlTag("td", sb))
                                sb.AppendLine(_elements[index]);
                        }
                }

            return sb.ToString();
        }

        private static int GetRowCount(int columns, int elementCount) =>
            Mathf.CeilToInt((float)elementCount / columns);
    }

    public class HtmlTag : IDisposable
    {
        private readonly string _tag;
        private readonly StringBuilder _sb;

        public HtmlTag(string tag, StringBuilder stringBuilder)
        {
            _tag = tag;
            _sb = stringBuilder;
            _sb.AppendLine($"<{tag}>");
        }

        public void Dispose()
        {
            _sb.AppendLine($"</{_tag}>");
        }
    }
}