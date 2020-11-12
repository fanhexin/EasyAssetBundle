using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EasyAssetBundle.Common;
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
        static readonly GUIContent _contentBuild = new GUIContent("Build");
        static readonly GUIContent _checkForUpdates = new GUIContent("Check for updates");

        TreeViewState _treeViewState;
        BundleTreeView _bundleTreeView;

        SerializedObject _settingsSo;
        SearchField _searchField;

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

        void OnDestroy()
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

                menu.AddItem(new GUIContent("Rebuild"), false, () =>
                {
                    AssetBundleBuilder.ClearCache();
                    Caching.ClearCache();
                    BuildContent(processors);
                });

                menu.AddItem(new GUIContent("Try Build"), false,
                    () => AssetBundleBuilder.Build(
                        Settings.instance.buildOptions | BuildAssetBundleOptions.DryRunBuild,
                        processors));
                
                var settings = Settings.instance.runtimeSettings;
                bool isCdnFieldFilled = Uri.IsWellFormedUriString(settings.cdnUrl, UriKind.Absolute);
                if (isCdnFieldFilled)
                {
                    menu.AddItem(_checkForUpdates, false, () => CheckForUpdatesAsync());
                }
                else
                {
                    menu.AddDisabledItem(_checkForUpdates);
                }
                
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

        void ShowCheckUpdatesProgressBar(float progress)
        {
            EditorUtility.DisplayCancelableProgressBar("Operation", "Check for updates....", progress);
        }

        async Task<AssetBundleManifest> GetManifestAsync(string url, TimeSpan timeout)
        {
            var req = WebRequest.Create(url);
            req.Timeout = (int) timeout.TotalMilliseconds;
            
            using (var response = await WebRequest.Create(url).GetResponseAsync())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (var mem = new MemoryStream())
                    {
                        await stream.CopyToAsync(mem);
                        var ab = AssetBundle.LoadFromMemory(mem.ToArray());
                        if (ab == null)
                        {
                            return null;
                        }

                        AssetBundleManifest manifest = ab.LoadAsset<AssetBundleManifest>(nameof(AssetBundleManifest));
                        ab.Unload(false);
                        return manifest;
                    }
                }    
            }    
        }

        bool CheckIfNeedUpdate(AssetBundleManifest localManifest, AssetBundleManifest remoteManifest)
        {
            var bundles = Settings.instance.runtimeSettings.bundles;
            foreach (Bundle bundle in bundles.Where(x => x.type != BundleType.Static))
            {
                if (localManifest.GetAssetBundleHash(bundle.name) != remoteManifest.GetAssetBundleHash(bundle.name))
                {
                    Debug.Log($"bundel name: {bundle.name} hash different!");
                    return true;
                }
            }

            return false;
        }

        async Task CheckForUpdatesAsync()
        {
            var settings = Settings.instance.runtimeSettings;
            string platformName = Application.platform.ToGenericName();
            string baseUrl = $"{settings.cdnUrl}/{platformName}";
            
            string remoteManifestUrl = $"{baseUrl}/{platformName}";
            if (settings.webRequestProcessor != null)
            {
                remoteManifestUrl = settings.webRequestProcessor.HandleUrl(remoteManifestUrl);
            }

            var timeout = TimeSpan.FromSeconds(10);
            ShowCheckUpdatesProgressBar(0);
            try
            {
                var remoteManifest = await GetManifestAsync(remoteManifestUrl, timeout);
                if (remoteManifest == null)
                {
                    ShowNotification(new GUIContent("Load remote manifest error!"));
                    return;
                }

                ShowCheckUpdatesProgressBar(0.3f);
                string localManifestUrl = $"file://{Settings.currentTargetCachePath}/{platformName}";
                var localManifest = await GetManifestAsync(localManifestUrl, timeout);
                if (localManifest == null)
                {
                    ShowNotification(new GUIContent("Load local manifest error!"));
                    return;
                }

                if (CheckIfNeedUpdate(remoteManifest, localManifest))
                {
                    string versionUrl = $"{baseUrl}/version";
                    if (settings.webRequestProcessor != null)
                    {
                        versionUrl = settings.webRequestProcessor.HandleUrl(versionUrl);
                    }

                    int remoteVersion = await GetRemoteVersionAsync(versionUrl, timeout);
                    ShowCheckUpdatesProgressBar(1);
                    PrepareUpdatesWindow.ShowWindow(localManifest, remoteManifest, remoteVersion, _settingsSo);
                    return;
                }

                ShowNotification(new GUIContent("No need for updates."));
            }
            catch (Exception e)
            {
                Debug.LogError(e);    
            }
            finally
            {
                FinishCheckUpdatesProgressBar();
            }
        }

        async Task FinishCheckUpdatesProgressBar()
        {
            ShowCheckUpdatesProgressBar(1);
            await Task.Delay(TimeSpan.FromSeconds(0.5));
            EditorUtility.ClearProgressBar();
        }

        async Task<int> GetRemoteVersionAsync(string versionUrl, TimeSpan timeout)
        {
             var req = WebRequest.Create(versionUrl);
             req.Timeout = (int) timeout.TotalMilliseconds;
             
             try
             {
                 using (var resp = await req.GetResponseAsync())
                 {
                     using (var stream = resp.GetResponseStream())
                     {
                         using (var reader = new StreamReader(stream))
                         {
                             int version = int.Parse(await reader.ReadToEndAsync());
                             return version;
                         }
                     }
                 }
             }
             catch (Exception e)
             {
                 Debug.LogError(e);
                 return -1;
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