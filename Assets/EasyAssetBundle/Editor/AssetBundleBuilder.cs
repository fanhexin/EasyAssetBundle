using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EasyAssetBundle.Common;
using EasyAssetBundle.Common.Editor;
using UnityEditor;

namespace EasyAssetBundle.Editor
{
    public static class AssetBundleBuilder
    {
        public static bool hasBuilded => Directory.Exists(Settings.currentTargetCachePath);

        public static void Build(BuildAssetBundleOptions buildOptions)
        {
            Build(buildOptions, GetProcessors());
        }

        public static void Build(BuildAssetBundleOptions buildOptions, IEnumerable<AbstractBuildProcessor> processors)
        {
            foreach (var processor in processors)
            {
                processor.OnBeforeBuild();
            }

            try
            {
                Build(EditorUserBuildSettings.activeBuildTarget, buildOptions);
            }
            catch (Exception e)
            {
                foreach (var processor in processors)
                {
                    processor.OnCancelBuild();    
                }
                throw e;
            }
            
            foreach (var processor in processors)
            {
                processor.OnAfterBuild();
            }
        }

        public static void ClearCache()
        {
            Directory.Delete(Settings.currentTargetCachePath, true);
        }
        
        static void Build(BuildTarget buildTarget,
            BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.ChunkBasedCompression,
            params AssetBundleBuild[] buildMap)
        {
            string cachePath = Path.Combine(Settings.cacheBasePath, buildTarget.ToString());
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            var settings = Settings.instance;
            // 如果是中国版加入AssetBundle加密Build
            if (Enum.GetNames(typeof(BuildAssetBundleOptions)).Contains("EnableProtection") && 
                buildOptions.HasFlag(Enum.Parse(typeof(BuildAssetBundleOptions), "EnableProtection") as Enum) &&
                !string.IsNullOrEmpty(settings.runtimeSettings.encryptKey))
            {
                // var method = BuildPipeline.SetAssetBundleEncryptKey()
                var method = typeof(BuildPipeline).GetMethod("SetAssetBundleEncryptKey",
                    BindingFlags.Static | BindingFlags.Public);
                method.Invoke(null, new[] {settings.runtimeSettings.encryptKey});
            }
            
            if (buildMap != null && buildMap.Length != 0)
            {
                BuildPipeline.BuildAssetBundles(cachePath, buildMap, buildOptions, buildTarget);
            }
            else
            {
                BuildPipeline.BuildAssetBundles(cachePath, buildOptions, buildTarget);
            }

            File.WriteAllText(Path.Combine(cachePath, "version"), settings.runtimeSettings.version.ToString());

            if (settings.httpServiceSettings.enabled ||
                Uri.IsWellFormedUriString(settings.runtimeSettings.cdnUrl, UriKind.Absolute))
            {
                CopyToHostedData(buildTarget);
            }
        }

        private static void CopyToHostedData(BuildTarget buildTarget)
        {
            string path = $"Assets/../HostedData/{buildTarget}";
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
            DirectoryCopy(Settings.currentTargetCachePath, path, true, f =>
            {
                // manifest文件要放行
                if (f.Name == EditorUserBuildSettings.activeBuildTarget.ToString() || f.Name == "version")
                {
                    return true;
                }

                // 排除掉.manifest文件，其只用来在编辑器端做增量构建用，运行时并不需哟
                string extensionName = Path.GetExtension(f.Name);
                return extensionName != ".manifest" &&
                       Settings.instance.runtimeSettings.bundles.Any(b => b.name == f.Name && b.type != BundleType.Static);
            });
        }

        public static void CopyToStreamingAssets()
        {
            Directory.CreateDirectory(Config.streamingAssetsBundlePath);
            DirectoryCopy(Settings.currentTargetCachePath, Config.streamingAssetsBundlePath, true, x =>
            {
                // manifest文件要放行
                if (x.Name == EditorUserBuildSettings.activeBuildTarget.ToString())
                {
                    return true;
                }
                
                // 排除掉.manifest文件，其只用来在编辑器端做增量构建用，运行时并不需哟
                string extensionName = Path.GetExtension(x.Name);
                return extensionName != ".manifest" && 
                       Settings.instance.runtimeSettings.bundles.Any(b => b.name == x.Name && b.type != BundleType.Remote);
            });
        }

        public static void DeleteStreamingAssetsBundlePath()
        {
            Directory.Delete(Config.streamingAssetsBundlePath, true);
        }
        
        static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, Func<FileInfo, bool> filter = null)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
        
            // Get the files in the directory and copy them to the new location.
            var files = dir.EnumerateFiles();
            if (filter != null)
            {
                files = files.Where(filter);
            }
            
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs, filter);
                }
            }
        }
        
        static void ClearFolder(string folderPath)
        {
            string[] dirs = Directory.GetDirectories(folderPath);
            foreach (string dir in dirs)
            {
                Directory.Delete(dir, true);
            }

            string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                File.Delete(f);
            }
        }
        
        public static IEnumerable<AbstractBuildProcessor> GetProcessors()
        {
            string[] guids = AssetDatabase.FindAssets($"t: {nameof(AbstractBuildProcessor)}");
            var processors = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AbstractBuildProcessor>);
            return processors;
        }
    }
}