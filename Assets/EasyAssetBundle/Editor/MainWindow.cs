using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EasyAssetBundle.Common.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyAssetBundle.Editor
{
    public class MainWindow : EditorWindow
    {
        public static MainWindow instance { get; private set; }
        private static readonly GUIContent _contentBuild = new GUIContent("Build");
        private static readonly GUIContent _publishBuild = new GUIContent("Publish Build");
        
        private TreeViewState _treeViewState;
        private BundleTreeView _bundleTreeView;
        
        private SerializedObject _settingsSo;
        private SearchField _searchField;

        [MenuItem("Window/EasyAssetBundle")]
        static void Init()
        {
            var win = GetWindow<MainWindow>();
            win.titleContent = new GUIContent("EasyAssetBundle");
            win.Show();
        }

        void OnEnable()
        {
            instance = this;
            _searchField = new SearchField();
            if (_treeViewState == null)
                _treeViewState = new TreeViewState();
            
            _settingsSo = new SerializedObject(Settings.instance);

            var bundlesSp = Settings.GetBundlesSp(_settingsSo);
            _bundleTreeView = new BundleTreeView(_treeViewState, bundlesSp, this);
            string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            if (bundlesSp.arraySize == 0 && assetBundleNames.Length != 0)
            {
                bool ret = EditorUtility.DisplayDialog("Notice",
                    "Bundle list is empty. Would you like to import existing assetbundles?",
                    "ok",
                    "cancel");

                if (ret)
                {
                    _bundleTreeView.Import(assetBundleNames);
                }
            }
            _bundleTreeView.multiColumnHeader.ResizeToFit();
        }

        private void OnDestroy()
        {
            instance = null;
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            _bundleTreeView.searchString = _searchField.OnToolbarGUI(_bundleTreeView.searchString);
            
            GUILayout.Space(5);

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                SettingsWindow.Display();
            }
            
            EditorGUI.BeginChangeCheck();

            IEnumerable<AbstractBuildProcessor> processors = AssetBundleBuilder.GetProcessors();
            if (processors.Any())
            {
                DropdownMenuButton(new GUIContent($"Processors({processors.Count()})"), menu =>
                {
                    foreach (var processor in processors)
                    {
                        menu.AddItem(new GUIContent(processor.name), false,
                            item => EditorGUIUtility.PingObject(item as Object), processor);
                    }
                });
            }

            var setting = Settings.instance;
            EnumDropDownButton(new GUIContent($"Mode: {setting.mode}"), setting.mode, mode =>
            {
                if (mode == Settings.Mode.Real && !AssetBundleBuilder.hasBuilded)
                {
                    ShowNotification(new GUIContent("Please build assetbundle first!"));
                }
                else
                {
                    Settings.GetModeSp(_settingsSo).enumValueIndex = (int) mode;
                    _settingsSo.ApplyModifiedProperties();
                }
            });
            
            DropdownMenuButton(_contentBuild, menu =>
            {
                menu.AddItem(new GUIContent("Build Content"), false, () => BuildContent(processors));

                var settings = Settings.instance.runtimeSettings;
                if (!Uri.IsWellFormedUriString(settings.cdnUrl, UriKind.Absolute))
                {
                    menu.AddDisabledItem(_publishBuild);
                }
                else
                {
                    menu.AddItem(_publishBuild, false, async () =>
                    {
                        await TryUpdateLocalVersion();
                        BuildContent(processors);
                    });
                }

                menu.AddItem(new GUIContent("Rebuild"), false, () =>
                {
                    AssetBundleBuilder.ClearCache();
                    BuildContent(processors);
                });

                menu.AddItem(new GUIContent("Try Build"), false,
                    () => AssetBundleBuilder.Build(
                        Settings.instance.buildOptions | BuildAssetBundleOptions.DryRunBuild,
                        processors));
                
                menu.AddItem(new GUIContent("Clear Build Cache"), false, AssetBundleBuilder.ClearCache);
                menu.AddItem(new GUIContent("Clear Runtime Cache"), false, () => Caching.ClearCache());
            });

            EditorGUILayout.EndHorizontal();

            float offset = EditorStyles.toolbar.fixedHeight;
            _bundleTreeView.OnGUI(new Rect(0, offset, position.width, position.height - offset));
            
            if (EditorGUI.EndChangeCheck())
            {
                _settingsSo.ApplyModifiedProperties();
            }
        }

        void ShowFetchVersionProgressBar(float progress)
        {
            EditorUtility.DisplayCancelableProgressBar("Operation", "Fetch remote version", progress);
        }

        async Task TryUpdateLocalVersion()
        {
            var settings = Settings.instance.runtimeSettings;
            string url = $"{settings.cdnUrl}/{Application.platform.ToGenericName()}/version";
            if (settings.webRequestProcessor != null)
            {
                url = settings.webRequestProcessor.HandleUrl(url);
            }

            ShowFetchVersionProgressBar(0);
            var req = WebRequest.Create(url);
            try
            {
                using (var resp = await req.GetResponseAsync())
                {
                    using (var stream = resp.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            int version = int.Parse(reader.ReadToEnd());
                            ShowFetchVersionProgressBar(1);
                            await Task.Delay(TimeSpan.FromSeconds(0.5));
                            EditorUtility.ClearProgressBar();    
                            
                            bool ret = EditorUtility.DisplayDialog("Alert",
                                $"Remote version is {version}. Update local version to {version + 1}?", "ok", "cancel");
                            if (!ret)
                            {
                                return;
                            }

                            Settings.GetVersionSp(_settingsSo).intValue = version + 1;
                            _settingsSo.ApplyModifiedProperties();
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();    
            }
        }

        void BuildContent(IEnumerable<AbstractBuildProcessor> processors)
        {
            AssetBundleBuilder.Build(Settings.instance.buildOptions, processors);
            ShowNotification(new GUIContent("Build success!"));
        }

        void DropdownMenuButton(GUIContent label, Action<GenericMenu> addMenuItems)
        {
            Rect rect = GUILayoutUtility.GetRect(label, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false));
            if (!EditorGUI.DropdownButton(rect, label, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            addMenuItems?.Invoke(menu);
            menu.DropDown(rect);
        }

        void EnumDropDownButton<T>(GUIContent label, T value, Action<T> onValueChanged) where T : Enum
        {
            Rect rect = GUILayoutUtility.GetRect(label, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false));
            if (!EditorGUI.DropdownButton(rect, label, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                return;
            }

            var menu = new GenericMenu();
            foreach (string name in Enum.GetNames(typeof(T)))
            {
                menu.AddItem(new GUIContent(name), value.ToString() == name, v =>
                {
                    T ret = (T) Enum.Parse(typeof(T), (string) v);
                    if (!value.Equals(ret))
                    {
                        onValueChanged?.Invoke(ret);
                    }
                }, name);
            }

            menu.DropDown(rect);
        }

        public void Reload()
        {
            _settingsSo.Update();
            _bundleTreeView.Reload();
        }
    }
}