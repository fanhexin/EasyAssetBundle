using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyAssetBundle.Common;
using EasyAssetBundle.Common.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public class PrepareUpdatesWindow : EditorWindow
    {
        static IReadOnlyList<(string, string)> _changeList;
        static int _remoteVersion;
        static SerializedObject _settingsSo;
        ListView _listView;

        static void ShowWindow()
        {
            var window = CreateInstance<PrepareUpdatesWindow>();
            window.titleContent = new GUIContent("Chang List");
            window.position = new Rect(Screen.width/2, Screen.height/2, 200, 200);
            window.ShowUtility();
        }
        
        public static void ShowWindow(AssetBundleManifest localManifest, AssetBundleManifest remoteManifest,
            int remoteVersion, SerializedObject settingsSo)
        {
            var bundles = Settings.instance.runtimeSettings.bundles
                .Where(x => x.type != BundleType.Static)
                .Select(x => x.name)
                .ToArray();
            
            var bundleSet = new HashSet<string>(bundles);
            var changeList = remoteManifest.GetAllAssetBundles()
                .Where(bundleSet.Contains)
                .Where(x => remoteManifest.GetAssetBundleHash(x) != localManifest.GetAssetBundleHash(x))
                .Select(x => (x, "changed"));
            
            var addList = bundles
                .Except(remoteManifest.GetAllAssetBundles())
                .Select(x => (x, "add"));
            
            _changeList = changeList.Concat(addList).ToList();
            _settingsSo = settingsSo;
            _remoteVersion = remoteVersion;
            ShowWindow();
        }

        void OnEnable()
        {
            if (_changeList.Count == 0)
            {
                return;
            }
            
            _listView = new ListView(new TreeViewState(), _changeList);
            _listView.multiColumnHeader.ResizeToFit();
        }

        void OnGUI()
        {
            float offset = EditorStyles.toolbar.fixedHeight;
            var rect = GUILayoutUtility.GetRect(position.width, position.height - offset);
            _listView?.OnGUI(rect);

            GUI.enabled = _listView != null;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField($"v{_remoteVersion}->v{_remoteVersion + 1}", GUILayout.ExpandWidth(false), GUILayout.MaxWidth(100));
            if (GUILayout.Button("Prepare", EditorStyles.toolbarButton))
            {
                Settings.GetVersionSp(_settingsSo).intValue = _remoteVersion + 1;
                _settingsSo.ApplyModifiedProperties();
                CopyChangedFiles();
                Close();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        static void CopyChangedFiles()
        {
            if (_changeList.Count == 0)
            {
                return;
            }

            int version = _remoteVersion + 1;
            string basePath = $"Assets/../HostedData/v{version}";
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }

            string target = EditorUserBuildSettings.activeBuildTarget.ToString();
            string path = $"{basePath}/{target}";
            Directory.CreateDirectory(path);

            string destVersionPath = Path.Combine(path, "version");
            File.Copy(Path.Combine(Settings.currentTargetCachePath, "version"), destVersionPath);
            File.Copy(Path.Combine(Settings.currentTargetCachePath, target), Path.Combine(path, target));
            File.WriteAllText(destVersionPath, version.ToString());
            foreach ((string name, _) in _changeList)
            {
                string srcPath = Path.Combine(Settings.currentTargetCachePath, name);
                string targetPath = Path.Combine(path, name);
                File.Copy(srcPath, targetPath);
            }
        }

        class ListView : TreeView
        {
            int _id = -1;
            readonly IReadOnlyList<(string name, string state)> _items;

            public ListView(TreeViewState state, IReadOnlyList<(string name, string state)> items) 
                : base(state, CreateColumnHeader())
            {
                showAlternatingRowBackgrounds = true;
                _items = items;
                Reload();
            }
            
            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem {id = _id++, depth = -1, displayName = ""};
                foreach ((string name, _) in _items)
                {
                    root.AddChild(new TreeViewItem {id = _id++, depth = 0, displayName = name});    
                }
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                base.RowGUI(args);
                EditorGUI.LabelField(args.GetCellRect(1), _items[args.row].state); 
            }

            static MultiColumnHeader CreateColumnHeader()
            {
                var columns = new MultiColumnHeaderState(new[]
                {
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("Name"),
                        headerTextAlignment = TextAlignment.Left,
                        canSort = false,
                        minWidth = 100,
                        autoResize = true,
                        allowToggleVisibility = false
                    },

                    new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent("State"),
                        headerTextAlignment = TextAlignment.Left,
                        canSort = false,
                        width = 60,
                        minWidth = 60,
                        maxWidth = 100,
                        autoResize = true,
                        allowToggleVisibility = false
                    }
                });

                return new MultiColumnHeader(columns);
            }
        }
    }
}