using UnityEditor;

namespace EasyAssetBundle.Editor
{
    [InitializeOnLoad]
    public static class Setup
    {
        static Setup()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);    
        }

        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            if (!AssetBundleBuilder.hasBuilded)
            {
                AssetBundleBuilder.Build(Config.instance.buildOptions);    
            }
            
            AssetBundleBuilder.CopyToStreamingAssets();
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            AssetBundleBuilder.DeleteStreamingAssetsBundlePath();
        }
    }
}