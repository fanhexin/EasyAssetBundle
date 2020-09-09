using UnityEngine;

namespace EasyAssetBundle
{
    public class AssetNameAttribute : PropertyAttribute
    {
        public readonly string assetBundleName;

        public AssetNameAttribute(string assetBundleName)
        {
            this.assetBundleName = assetBundleName;
        }    
    }
}