using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetReference))]
    public class AssetReferenceDrawer : GuidDrawer
    {
        protected override string _guidKeyName => "_guid";
        protected override string _assetKeyName => "_assetName";
    }
}