using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EasyAssetBundle.Editor
{
    public static class BundleExtension
    {
        public static string Guid(this Object obj)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.AssetPathToGUID(path);
        }
        
        public static bool Any(this SerializedProperty bundles, string abName)
        {
            for (int i = 0; i < bundles.arraySize - 1; i++)
            {
                if (bundles.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("_name").stringValue == abName)
                {
                    return true;
                }
            }

            return false;
        }

        public static string AddBundle(this SerializedProperty bundles, Object asset)
        {
            return bundles.AddBundle(AssetDatabase.GetAssetPath(asset));
        }
        
        public static string AddBundle(this SerializedProperty bundles, string assetPath)
        {
            string abName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            if (string.IsNullOrEmpty(abName))
            {
                abName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
            }
            else
            {
                if (bundles.Any(abName))
                {
                    return string.Empty;
                }
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (bundles.Any(abName))
            {
                abName += $"_conflict_{guid}";
            }
            
            var importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = abName;
            importer.SaveAndReimport();

            var newItem = bundles.GetArrayElementAtIndex(bundles.arraySize++);
            newItem.FindPropertyRelative("_guid").stringValue = guid;
            newItem.FindPropertyRelative("_name").stringValue = abName;
            newItem.FindPropertyRelative("_type").enumValueIndex = 0;
            bundles.serializedObject.ApplyModifiedProperties();
            return guid;
        }
    }
}