using UnityEngine;

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
                    Config config = Config.instance;
#if UNITY_EDITOR
                    if (config.mode == Config.Mode.Virtual)
                    {
                        _assetBundleLoader = new VirtualAssetBundleLoader();
                    }
                    else
                    {
                        _assetBundleLoader = new RealAssetBundleLoader(Config.currentTargetCachePath);
                    }
#else
                    _assetBundleLoader = new RealAssetBundleLoader(Config.streamingAssetsBundlePath);
#endif
                }

                return _assetBundleLoader;
            }
        }
    }
}