using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuickEye.Editor
{
    [Serializable]
    public class EditorAssetBundleImage
    {
        public readonly Texture2D texture;
        public readonly string assetBundlePath;
        public readonly long fileId;
        public string name => texture.name;
        public Texture2D RetinaTexture { get; private set; }
        public string RetinaVersion { get; private set; }
        private List<Texture2D> retinaTextures = new List<Texture2D>();

        public EditorAssetBundleImage(Texture2D texture, string assetBundlePath)
        {
            this.texture = RetinaTexture = texture;
            this.assetBundlePath = assetBundlePath;
            fileId = AssetDatabaseUtil.GetFileId(texture);
        }

        public void AddRetinaTexture(Texture2D tex)
        {
            retinaTextures.Add(tex);
            retinaTextures = retinaTextures.OrderBy(t => t.name).ToList();
            UpdateRetinaData();
            void UpdateRetinaData()
            {
                RetinaTexture = retinaTextures.Last();
                var n = RetinaTexture.name;
                RetinaVersion = n.Substring(n.Length - 3);
            }
        }
        
        public static implicit operator Texture2D(EditorAssetBundleImage img) => img.texture;
    }
}