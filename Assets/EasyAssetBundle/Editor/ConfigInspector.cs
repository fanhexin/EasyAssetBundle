using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomEditor(typeof(Config))]
    public class ConfigInspector : UnityEditor.Editor
    {
        const string CACHE_BASE_PATH = "Assets/../Library/EasyAssetBundleCache";
        const string STREAMING_ASSETS = "StreamingAssets";
        Config _target;
        bool _showProcessors;
        BuildAssetBundleOptions _buildAbOptions = BuildAssetBundleOptions.ChunkBasedCompression;

        void OnEnable()
        {
            _target = target as Config;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUI.BeginChangeCheck();
            Config.Mode mode = (Config.Mode) EditorGUILayout.EnumPopup("Mode", _target.mode);
            if (mode == Config.Mode.Real && !File.Exists(Path.Combine(Application.streamingAssetsPath, _target.manifestName)))
            {
                EditorGUILayout.HelpBox("Please build assetbundle first!", MessageType.Error);    
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                _target.mode = mode;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            IEnumerable<AbstractBuildProcessor> processors = GetProcessors();
            if (processors.Any())
            {
                _showProcessors = EditorGUILayout.Foldout(_showProcessors, $"Build Processors ({processors.Count()})");
                if (_showProcessors)
                {
                    int indentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    foreach (var processor in processors)
                    {
                        EditorGUILayout.ObjectField(processor, processor.GetType());
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel = indentLevel;
                }
            }
            
            EditorGUI.BeginChangeCheck();
            var options = (BuildAssetBundleOptions) EditorGUILayout.EnumFlagsField("Build Options", _target.buildOptions);
            if (EditorGUI.EndChangeCheck())
            {
                _target.buildOptions = options;
            }
            
            if (GUILayout.Button("Build Asset Bundle"))
            {
                BuildAssetBundle(_target.manifestName, _buildAbOptions, processors);
            }
        }

        static IEnumerable<AbstractBuildProcessor> GetProcessors()
        {
            string[] guids = AssetDatabase.FindAssets($"t: {nameof(AbstractBuildProcessor)}");
            var processors = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AbstractBuildProcessor>);
            return processors;
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

        public static void BuildAssetBundle()
        {
            var config = Config.instance;
            BuildAssetBundle(config.manifestName, config.buildOptions, GetProcessors());
        }

        static void BuildAssetBundle(string manifestName, BuildAssetBundleOptions buildOptions, IEnumerable<AbstractBuildProcessor> processors)
        {
            foreach (var processor in processors)
            {
                processor.BeforeBuild();
            }
            
            BuildAssetBundle(EditorUserBuildSettings.activeBuildTarget, buildOptions, manifestName);
            
            foreach (var processor in processors)
            {
                processor.AfterBuild();
            }
        }

        static void BuildAssetBundle(BuildTarget buildTarget,
            BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.ChunkBasedCompression,
            string manifestName = "Manifest",
            params AssetBundleBuild[] buildMap)
        {
            string cachePath = Path.Combine(CACHE_BASE_PATH, buildTarget.ToString());
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
            
            string destPath = Application.streamingAssetsPath;
            if (Directory.Exists(destPath))
            {
                ClearFolder(destPath);
            }
            else
            {
                AssetDatabase.CreateFolder("Assets", STREAMING_ASSETS);
            }

            DirectoryCopy(cachePath, destPath, true);

            File.Move($"{destPath}/{buildTarget}", $"{destPath}/{manifestName}");
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
    }
}