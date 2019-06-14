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
                    Config config = Resources.Load<Config>("EasyAssetBundleConfig");
#if UNITY_EDITOR
                    if (config.mode == Config.Mode.Virtual)
                    {
                        _assetBundleLoader = new VirtualAssetBundleLoader();
                    }
                    else
                    {
                        _assetBundleLoader = new RealAssetBundleLoader(Application.streamingAssetsPath, config.manifestName);
                    }
#else
                    _assetBundleLoader = new RealAssetBundleLoader(Application.streamingAssetsPath, config.manifestName);
#endif
                }

                return _assetBundleLoader;
            }
        }
    }
}