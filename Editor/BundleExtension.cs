using System;
using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EasyAssetBundle.Editor
{
    public static class BundleExtension
    {
        public static bool Any(this SerializedProperty bundles, string abName)
        {
            for (int i = 0; i < bundles.arraySize; i++)
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
                if (bundles.Any(abName))
                {
                    abName += $"_conflict_{DateTime.Now.ToBinary()}";
                }
                
                var importer = AssetImporter.GetAtPath(assetPath);
                importer.assetBundleName = abName;
                importer.SaveAndReimport();
            }
            else
            {
                if (bundles.Any(abName))
                {
                    return string.Empty;
                }
            }

            return bundles.AddBundleByAbName(abName);
        }

        public static string AddBundleByAbName(this SerializedProperty bundles, string abName)
        {
            string guid = Guid.NewGuid().ToString("N");
            var newItem = bundles.GetArrayElementAtIndex(bundles.arraySize++);
            newItem.FindPropertyRelative("_guid").stringValue = guid;
            newItem.FindPropertyRelative("_name").stringValue = abName;
            newItem.FindPropertyRelative("_type").enumValueIndex = 0;
            bundles.serializedObject.ApplyModifiedProperties();
            return guid;
        }

        public static string AddBundle(this SerializedProperty bundles, string abName, params string[] assetPaths)
        {
            foreach (string path in assetPaths)
            {
                var importer = AssetImporter.GetAtPath(path);
                string oldName = importer.assetBundleName;
                importer.assetBundleName = abName;
                importer.SaveAndReimport();
                if (!string.IsNullOrEmpty(oldName))
                {
                    AssetDatabase.RemoveAssetBundleName(oldName, false);
                }
            }

            return bundles.AddBundleByAbName(abName);
        }
    }
}