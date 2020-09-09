using System.Linq;
using EasyAssetBundle.Common.Editor;
using UnityEditor;

namespace EasyAssetBundle.Editor
{
    [CustomPropertyDrawer(typeof(AssetBundleNameAttribute))]
    public class AbNameDrawer : PopupDrawer
    {
        protected override string[] CreateDisplayedOptions()
        {
            var attr = attribute as AssetBundleNameAttribute;
            return Settings.instance.runtimeSettings.bundles
                .Select(x => x.name)
                .Where(x => x.Contains(attr.filter))
                .ToArray();
        }
    }
}