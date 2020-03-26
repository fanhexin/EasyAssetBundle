using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Common.Editor
{
    public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        
        private static T _instance;

        public static T instance
        {
            get
            {
                if (_instance == null)
                {
                    string path = Path.Combine("Assets", "Editor", $"{typeof(T).FullName}.asset");
                    if (File.Exists(path))
                    {
                        _instance = AssetDatabase.LoadAssetAtPath<T>(path);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        _instance = CreateInstance<T>();
                        AssetDatabase.CreateAsset(_instance, path);
                        AssetDatabase.Refresh();
                    }
                }

                return _instance;
            }    
        }
    }
}