using System;
using System.IO;
using EasyAssetBundle.Common;
using UnityEditor;
using EasyAssetBundle.Common.Editor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [InitializeOnLoad]
    internal static class Setup
    {
        static Setup()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);    
        }

        private static void OnBuildPlayer(BuildPlayerOptions options)
        {
            AssetBundleBuilder.Build(Settings.instance.buildOptions);

            var config = ScriptableObject.CreateInstance<Config>();
            var settings = Settings.instance;
            config.runtimeSettings = new RuntimeSettings();
            config.runtimeSettings.Init(settings.runtimeSettings);
            if (settings.httpServiceSettings.enabled)
            {
                var so = new SerializedObject(config);
                so.FindProperty("_runtimeSettings").FindPropertyRelative("_cdnUrl").stringValue = settings.simulateUrl;
                so.ApplyModifiedProperties();
            }
            string path = Path.Combine("Assets", "Resources", $"{Config.FILE_NAME}.asset");
            AssetDatabase.CreateAsset(config, path);

            AssetBundleBuilder.CopyToStreamingAssets();
            try
            {
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            }
            finally
            {
                AssetBundleBuilder.DeleteStreamingAssetsBundlePath();
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}