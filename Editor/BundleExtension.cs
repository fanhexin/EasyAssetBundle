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
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string abName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            if (string.IsNullOrEmpty(abName))
            {
                abName = Path.GetFileName(assetPath).ToLower();
            }
            else
            {
                if (bundles.Any(abName))
                {
                    return string.Empty;
                }
            }

            if (bundles.Any(abName))
            {
                abName += $"_conflict_{asset.GetHashCode()}";
            }
            
            var importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = abName;
            importer.SaveAndReimport();

            var newItem = bundles.GetArrayElementAtIndex(bundles.arraySize++);
            string guid = asset.Guid();
            newItem.FindPropertyRelative("_guid").stringValue = asset.Guid();
            newItem.FindPropertyRelative("_name").stringValue = abName;
            newItem.FindPropertyRelative("_type").enumValueIndex = 0;
            bundles.serializedObject.ApplyModifiedProperties();
            return guid;
        }
    }
}