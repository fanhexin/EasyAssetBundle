#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
#endif

namespace EasyAssetBundle
{
    public static class AssetBundleLoader
    {
        static IAssetBundleLoader _assetBundleLoader;

        public static IAssetBundleLoader instance
        {
            get
            {
                if (_assetBundleLoader == null)
                {
#if UNITY_EDITOR
                    var settings = Settings.instance;
                    if (settings.mode == Settings.Mode.Virtual)
                    {
                        _assetBundleLoader = new VirtualAssetBundleLoader(settings.manifest);
                    }
                    else
                    {
                        _assetBundleLoader = new RealAssetBundleLoader(Settings.currentTargetCachePath, settings.manifest);
                    }
#else
                    _assetBundleLoader = new RealAssetBundleLoader(Config.streamingAssetsBundlePath, Config.instance.manifest);
#endif
                }

                return _assetBundleLoader;
            }
        }
    }
}