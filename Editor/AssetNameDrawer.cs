using System.IO;
using System.Linq;
using UnityEditor;

namespace EasyAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetNameAttribute))]
    public class AssetNameDrawer : PopupDrawer
    {
        protected override string[] CreateDisplayedOptions()
        {
            var attr = attribute as AssetNameAttribute;
            return AssetDatabase.GetAssetPathsFromAssetBundle(attr.assetBundleName)
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }
    }
}