using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace EasyAssetBundle.Editor
{
    internal class BundleAssetTreeViewItem : TreeViewItem
    {
        public string path;

        public void Ping()
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
        }

        public void Delete()
        {
            var importer = AssetImporter.GetAtPath(path);
            if (string.IsNullOrEmpty(importer.assetBundleName))
            {
                return;
            }
            
            importer.assetBundleName = string.Empty;
        }
    }
}