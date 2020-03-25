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
                AssetBundleBuilder.Build(Settings.instance.buildOptions);    
            }
            
            var so = new SerializedObject(Config.instance);
            so.FindProperty("_version").intValue = Settings.instance.version;
            
            var httpServer = HttpServerSettings.instance;
            var url = httpServer.enabled ? httpServer.url : Settings.instance.remoteUrl;
            so.FindProperty("_remoteUrl").stringValue = url;
            so.ApplyModifiedProperties();

            AssetBundleBuilder.CopyToStreamingAssets();
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            AssetBundleBuilder.DeleteStreamingAssetsBundlePath();
        }
    }
}