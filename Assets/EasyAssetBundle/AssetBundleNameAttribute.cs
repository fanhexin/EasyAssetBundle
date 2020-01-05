using PropertyAttribute = UnityEngine.PropertyAttribute;

namespace EasyAssetBundle
{
    public class AssetBundleNameAttribute : PropertyAttribute
    {
        public readonly string filter;

        public AssetBundleNameAttribute()
        {
            this.filter = string.Empty;
        }

        public AssetBundleNameAttribute(string filter)
        {
            this.filter = filter;
        }
    }
}