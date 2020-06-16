using System;
using System.IO;
using EasyAssetBundle.Common;
using UnityEditor;
using Object = UnityEngine.Object;

namespace EasyAssetBundle.Editor
{
    public static class BundleExtension
    {
        public static int FindIndex(this SerializedProperty bundles, string abName)
        {
            for (int i = 0; i < bundles.arraySize; i++)
            {
                using (var item = bundles.GetArrayElementAtIndex(i))
                using (var nameSp = item.FindPropertyRelative(Bundle.nameOfName))
                    if (nameSp.stringValue == abName)
                    {
                        return i;
                    }
            }

            return -1;
        }

        public static SerializedProperty FindBundle(this SerializedProperty bundles, string name)
        {
            for (int i = 0; i < bundles.arraySize; i++)
            {
                var b = bundles.GetArrayElementAtIndex(i);
                using (var nameSp = b.FindPropertyRelative(Bundle.nameOfName))
                {
                    if (nameSp.stringValue != name)
                    {
                        b.Dispose();
                        continue;
                    }

                    return b;
                }
            }

            return null;
        }

        public static string AddBundle(this SerializedProperty bundles, Object asset,
            BundleType bundleType = BundleType.Static)
        {
            return bundles.AddBundle(AssetDatabase.GetAssetPath(asset), bundleType);
        }

        public static string AddBundle(this SerializedProperty bundles, string assetPath,
            BundleType bundleType = BundleType.Static)
        {
            string abName = AssetDatabase.GetImplicitAssetBundleName(assetPath);
            if (string.IsNullOrEmpty(abName))
            {
                abName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
                if (bundles.FindIndex(abName) >= 0)
                {
                    abName += $"_conflict_{DateTime.Now.ToBinary()}";
                }

                var importer = AssetImporter.GetAtPath(assetPath);
                importer.assetBundleName = abName;
            }
            else
            {
                if (bundles.FindIndex(abName) >= 0)
                {
                    return string.Empty;
                }
            }

            return bundles.AddBundleByAbName(abName, bundleType);
        }

        public static string AddBundleByAbName(this SerializedProperty bundles, string abName,
            BundleType bundleType = BundleType.Static)
        {
            string guid = Guid.NewGuid().ToString("N");
            using (var newItem = bundles.GetArrayElementAtIndex(bundles.arraySize++))
            {
                using (var guidSp = newItem.FindPropertyRelative(Bundle.nameOfGuide)) 
                    guidSp.stringValue = guid;

                using (var nameSp = newItem.FindPropertyRelative(Bundle.nameOfName)) 
                    nameSp.stringValue = abName;

                using (var typeSp = newItem.FindPropertyRelative(Bundle.nameOfType)) 
                    typeSp.enumValueIndex = (int) bundleType;
            }
            bundles.serializedObject.ApplyModifiedProperties();
            return guid;
        }

        public static string AddBundle(this SerializedProperty bundles, string abName, BundleType bundleType,
            params string[] assetPaths)
        {
            foreach (string path in assetPaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                
                var importer = AssetImporter.GetAtPath(path);
                string oldName = importer.assetBundleName;
                importer.assetBundleName = abName;
                if (!string.IsNullOrEmpty(oldName))
                {
                    AssetDatabase.RemoveAssetBundleName(oldName, false);
                }
            }

            return bundles.AddBundleByAbName(abName, bundleType);
        }

        public static void SetBundleType(this SerializedProperty bundles, string name, BundleType bundleType)
        {
            var b = bundles.FindBundle(name);
            if (b == null)
            {
                return;
            }

            using (b) b.SetBundleType(bundleType);
        }

        public static void SetBundleType(this SerializedProperty bundles, int index, BundleType bundleType)
        {
            using (var b = bundles.GetArrayElementAtIndex(index))
                b.SetBundleType(bundleType);
        }

        public static void SetBundleType(this SerializedProperty bundle, BundleType bundleType)
        {
            using (var typeSp = bundle.FindPropertyRelative(Bundle.nameOfType)) 
                typeSp.enumValueIndex = (int) bundleType;
            bundle.serializedObject.ApplyModifiedProperties();
        }

        public static void AddOrUpdateBundle(this SerializedProperty bundles, string name, BundleType bundleType, params string[] assetPaths)
        {
            var b = bundles.FindBundle(name);
            if (b == null)
            {
                bundles.AddBundle(name, bundleType, assetPaths);
                return;
            }
            
            b.SetBundleType(bundleType);
            b.Dispose();
        }

        public static void RemoveBundle(this SerializedProperty bundles, int index)
        {
            var bundle = bundles.GetArrayElementAtIndex(index);
            string name = bundle.FindPropertyRelative(Bundle.nameOfName).stringValue;
            bundles.MoveArrayElement(index, bundles.arraySize - 1);
            --bundles.arraySize;
            bundles.serializedObject.ApplyModifiedProperties();
            AssetDatabase.RemoveAssetBundleName(name, true);
        }

        public static void RemoveBundle(this SerializedProperty bundles, string abName)
        {
            bundles.RemoveBundle(bundles.FindIndex(abName));    
        }
    }
}