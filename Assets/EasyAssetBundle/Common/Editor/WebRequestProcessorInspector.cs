using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Common.Editor
{
    [CustomEditor(typeof(WebRequestProcessor), true)]
    public class WebRequestProcessorInspector : UnityEditor.Editor
    {
        const string LAST_SAVE_PATH_KEY = "WebRequestProcessorInspector_lastSavePath";

        WebRequestProcessor _target;
        int _index;
        string[] _bundles;
        string _url;

        void OnEnable()
        {
            _target = target as WebRequestProcessor;
            _bundles = Settings.instance.runtimeSettings.bundles
                .Where(x => x.type != BundleType.Static)
                .Select(x => x.name)
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorUtility.ClearProgressBar();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                _index = EditorGUILayout.Popup(_index, _bundles);
                if (cc.changed || string.IsNullOrEmpty(_url))
                {
                    string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
                    _url = _target.HandleUrl($"{Settings.instance.runtimeSettings.cdnUrl}/{platformName}/{_bundles[_index]}");
                }
            }
            EditorGUILayout.TextField(_url);
            
            if (GUILayout.Button("Download"))
            {
                Download(_url, _bundles[_index]);                 
            }
            
            EditorGUILayout.EndVertical();
        }

        async void Download(string url, string fileName)
        {
            string lastSavePath = EditorPrefs.GetString(LAST_SAVE_PATH_KEY, "./");
            string savePath = EditorUtility.SaveFilePanel("Select Save Path", lastSavePath, fileName, string.Empty);
            if (string.IsNullOrEmpty(savePath))
            {
                return;
            }
            EditorPrefs.SetString(LAST_SAVE_PATH_KEY, savePath);

            using (var response = await WebRequest.Create(url).GetResponseAsync())
            {
                ShowProgress(0f, fileName);
                using (Stream stream = response.GetResponseStream())
                {
                    using (FileStream fileStream = File.Open(savePath, FileMode.Create))
                    {
                        var buffer = new byte[1024];
                        int cnt;
                        int sum = 0;
                        bool cancelled = false;
                    
                        do
                        {
                            cnt = stream.Read(buffer, 0, buffer.Length);
                            fileStream.Write(buffer, 0, cnt);
                            sum += cnt;
                            if (ShowProgress((float)sum / stream.Length, fileName))
                            {
                                cancelled = true;
                                break;
                            }
                        } while (cnt > 0);

                        if (cancelled)
                        {
                            File.Delete(savePath);
                        }
                        else
                        {
                            await fileStream.FlushAsync();
                            fileStream.Close();
                        }
                    }
                }    
            }

            EditorUtility.ClearProgressBar();
            
            AssetDatabase.Refresh();
        }

        bool ShowProgress(float progress, string fileName)
        {
            return EditorUtility.DisplayCancelableProgressBar(target.name, $"{nameof(Download)} {fileName}....", progress);
        }
    }
}