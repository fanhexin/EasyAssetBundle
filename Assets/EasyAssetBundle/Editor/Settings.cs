using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    [Serializable]
    public class Settings : ISerializationCallbackReceiver
    {
        private const string SAVE_PATH = "Assets/../ProjectSettings/EasyAssetBundleSettings.json";
        private static Settings _instance;

        public static Settings instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!File.Exists(SAVE_PATH))
                    {
                        File.WriteAllText(SAVE_PATH, "{}");
                    }

                    string content = File.ReadAllText(SAVE_PATH);
                    _instance = JsonUtility.FromJson<Settings>(content);
                }

                return _instance;
            }    
        }

        public void Save()
        {
            string content = JsonUtility.ToJson(this);
            File.WriteAllText(SAVE_PATH, content);
        }

        public int version = 1;
        public string remoteUrl;
        public BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.ChunkBasedCompression;
        
        public void OnGUI()
        {
            UnityEditor.Editor.CreateEditor(HttpServerSettings.instance).OnInspectorGUI();
            EditorGUILayout.LabelField("Url", HttpServerSettings.instance.url);
            
            EditorGUILayout.Separator();
            
            EditorGUI.BeginChangeCheck();
            version = EditorGUILayout.IntField("Version", version);
            remoteUrl = EditorGUILayout.TextField("RemoteUrl", remoteUrl);
            buildOptions = (BuildAssetBundleOptions) EditorGUILayout.EnumFlagsField("BuildOptions", buildOptions);
            if (EditorGUI.EndChangeCheck())
            {
                Save();
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }
    }
}