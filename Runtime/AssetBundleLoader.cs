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
                        _assetBundleLoader = new VirtualAssetBundleLoader(settings.runtimeSettings);
                    }
                    else
                    {
                        _assetBundleLoader = new RealAssetBundleLoader(Settings.currentTargetCachePath, settings.runtimeSettings);
                    }
#else
                    _assetBundleLoader = new RealAssetBundleLoader(Config.streamingAssetsBundlePath, Config.instance.runtimeSettings);
#endif
                }

                return _assetBundleLoader;
            }
        }
        
#if UNITY_EDITOR
        private static IAssetBundleLoader _virtualAbLoader;
        /// <summary>
        /// 直接获取虚拟模式的Loader，只应该在Editor脚本中使用
        /// </summary>
        public static IAssetBundleLoader virtualTypeInstance =>
            _virtualAbLoader ??
            (_virtualAbLoader = new VirtualAssetBundleLoader(Settings.instance.runtimeSettings));
#endif
    }
}