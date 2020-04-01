using System;
using System.IO;
using System.Linq;
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
            var settings = Settings.instance;
            if (settings.runtimeSettings.bundles.Any(x => x.type == BundleType.Remote) &&
                !settings.httpServiceSettings.enabled && 
                string.IsNullOrEmpty(settings.runtimeSettings.cdnUrl))
            {
                EditorUtility.DisplayDialog("Error", "Need enable http service or specify a cdn url!", "ok");
                SettingsWindow.Display();
                return;
            }
            
            AssetBundleBuilder.Build(Settings.instance.buildOptions);

            var config = ScriptableObject.CreateInstance<Config>();
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