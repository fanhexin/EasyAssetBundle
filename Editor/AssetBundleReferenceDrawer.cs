using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetBundleReference))]
    public class AssetBundleReferenceDrawer : GuidDrawer
    {
        protected override string _guidKeyName => "_guid";
        protected override string _assetKeyName => string.Empty;
    }
}