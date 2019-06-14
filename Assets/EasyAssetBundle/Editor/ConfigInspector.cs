using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomEditor(typeof(Config))]
    public class ConfigInspector : UnityEditor.Editor
    {
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
            Config.Mode mode = (Config.Mode) EditorGUILayout.EnumPopup("Mode", _target.mode);
            if (mode == Config.Mode.Real && !File.Exists(Path.Combine(Application.streamingAssetsPath, _target.manifestName)))
            {
                EditorGUILayout.HelpBox("Please build assetbundle first!", MessageType.Error);    
            }
            
            if (GUI.changed)
            {
                EditorPrefs.SetInt(Config.MODE_SAVE_KEY, (int) mode);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            string[] guids = AssetDatabase.FindAssets($"t: {nameof(AbstractBuildProcessor)}");
            var processors = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AbstractBuildProcessor>);
            if (guids.Length > 0)
            {
                _showProcessors = EditorGUILayout.Foldout(_showProcessors, $"Build Processors ({guids.Length})");
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
            
            _buildAbOptions = (BuildAssetBundleOptions) EditorGUILayout.EnumFlagsField("Build Options", _buildAbOptions);
            if (GUILayout.Button("Build Asset Bundle"))
            {
                foreach (var processor in processors)
                {
                    processor.BeforeBuild();
                }
                
                BuildAssetBundle(EditorUserBuildSettings.activeBuildTarget);
                
                foreach (var processor in processors)
                {
                    processor.AfterBuild();
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

        void BuildAssetBundle(BuildTarget buildTarget, params AssetBundleBuild[] buildMap)
        {
            string destPath = Application.streamingAssetsPath;
            if (Directory.Exists(destPath))
            {
                ClearFolder(destPath);
            }
            else
            {
                AssetDatabase.CreateFolder("Assets", STREAMING_ASSETS);
            }

            if (buildMap != null && buildMap.Length != 0)
            {
                BuildPipeline.BuildAssetBundles(destPath, buildMap, _buildAbOptions, buildTarget);
            }
            else
            {
                BuildPipeline.BuildAssetBundles(destPath, _buildAbOptions, buildTarget);
            }
            
            File.Move($"{destPath}/{STREAMING_ASSETS}", $"{destPath}/{_target.manifestName}");
            File.Move($"{destPath}/{STREAMING_ASSETS}.manifest", $"{destPath}/{_target.manifestName}.manifest");
            AssetDatabase.Refresh();
        }
    }
}