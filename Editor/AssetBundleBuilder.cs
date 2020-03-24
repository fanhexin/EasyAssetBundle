using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace EasyAssetBundle.Editor
{
    public static class AssetBundleBuilder
    {
        public static bool hasBuilded => Directory.Exists(Config.currentTargetCachePath);

        public static void Build(BuildAssetBundleOptions buildOptions)
        {
            Build(buildOptions, GetProcessors());
        }

        public static void Build(BuildAssetBundleOptions buildOptions, IEnumerable<AbstractBuildProcessor> processors)
        {
            foreach (var processor in processors)
            {
                processor.BeforeBuild();
            }
            
            Build(EditorUserBuildSettings.activeBuildTarget, buildOptions);
            
            foreach (var processor in processors)
            {
                processor.AfterBuild();
            }
        }

        public static void ClearCache()
        {
            Directory.Delete(Config.currentTargetCachePath, true);
        }
        
        static void Build(BuildTarget buildTarget,
            BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.ChunkBasedCompression,
            params AssetBundleBuild[] buildMap)
        {
            string cachePath = Path.Combine(Config.cacheBasePath, buildTarget.ToString());
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            
            if (buildMap != null && buildMap.Length != 0)
            {
                BuildPipeline.BuildAssetBundles(cachePath, buildMap, buildOptions, buildTarget);
            }
            else
            {
                BuildPipeline.BuildAssetBundles(cachePath, buildOptions, buildTarget);
            }
            
        }

        public static void CopyToStreamingAssets()
        {
            Directory.CreateDirectory(Config.streamingAssetsBundlePath);
            DirectoryCopy(Config.currentTargetCachePath, Config.streamingAssetsBundlePath, true);
            AssetDatabase.Refresh();
        }

        public static void DeleteStreamingAssetsBundlePath()
        {
            Directory.Delete(Config.streamingAssetsBundlePath, true);
            AssetDatabase.Refresh();
        }
        
        static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
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
            // 排除掉.manifest文件，其只用来在编辑器端做增量构建用，运行时并不需哟
            var files = dir.EnumerateFiles()
                .Where(x => Path.GetExtension(x.Name) != ".manifest");
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
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
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