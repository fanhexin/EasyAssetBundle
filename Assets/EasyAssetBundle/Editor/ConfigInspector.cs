using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [CustomEditor(typeof(Config))]
    public class ConfigInspector : UnityEditor.Editor
    {
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
            if (mode == Config.Mode.Real && !AssetBundleBuilder.hasBuilded)
            {
                EditorGUILayout.HelpBox("Please build assetbundle first!", MessageType.Error);    
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                _target.mode = mode;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            IEnumerable<AbstractBuildProcessor> processors = AssetBundleBuilder.GetProcessors();
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
                AssetBundleBuilder.Build(_buildAbOptions, processors);
            }

            if (GUILayout.Button("Clear Cache"))
            {
                AssetBundleBuilder.ClearCache();
            }
        }
    }
}