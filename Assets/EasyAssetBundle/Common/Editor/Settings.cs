using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Common.Editor
{
    [Serializable]
    public class Settings : ScriptableObjectSingleton<Settings>, ISerializationCallbackReceiver
    {
        public enum Mode
        {
            Virtual,    
            Real
        }
        
        [Serializable]
        public class HttpServiceSettings
        {
            [SerializeField] private bool _enabled = true;
            [SerializeField] private int _port = 8888;

            public bool enabled => _enabled;
            public int port => _port;
        }

        [SerializeField] private HttpServiceSettings _httpServiceSettings;
        [SerializeField] private RuntimeSettings _runtimeSettings;
        [SerializeField] private BuildAssetBundleOptions _buildOptions = BuildAssetBundleOptions.ChunkBasedCompression;
        [SerializeField, HideInInspector] private Mode _mode;

        public static SerializedProperty GetBundlesSp(SerializedObject so)
        {
            return so.FindProperty(nameof(_runtimeSettings)).FindPropertyRelative("_bundles");
        }

        public static SerializedProperty GetVersionSp(SerializedObject so)
        {
            return so.FindProperty(nameof(_runtimeSettings)).FindPropertyRelative("_version");
        }

        public static SerializedProperty GetModeSp(SerializedObject so)
        {
            return so.FindProperty(nameof(_mode));
        }

        public HttpServiceSettings httpServiceSettings => _httpServiceSettings;
        public RuntimeSettings runtimeSettings => _runtimeSettings;
        public BuildAssetBundleOptions buildOptions => _buildOptions;
        public Mode mode => _mode;
        

        private SimpleHTTPServer _simpleHttpServer;

        public string simulateUrl
        {
            get
            {
                string localIP = "0.0.0.0";
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }

                return $"http://{localIP}:{_httpServiceSettings.port}";
            }
        }

        public static string cacheBasePath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library",
            "EasyAssetBundleCache");
        
        public static string currentTargetCachePath =>
            Path.Combine(cacheBasePath , EditorUserBuildSettings.activeBuildTarget.ToString());
        
        private void Start()
        {
            if (_simpleHttpServer == null)
            {
                _simpleHttpServer = new SimpleHTTPServer("Assets/../HostedData", _httpServiceSettings.port);
                return;
            }

            if (_simpleHttpServer.start)
            {
                return;
            }

            _simpleHttpServer.Start();
        }

        private void Stop()
        {
            if (_simpleHttpServer == null)
            {
                return;
            }

            if (!_simpleHttpServer.start)
            {
                return;
            }

            _simpleHttpServer.Stop();
        }

        public void OnGUI()
        {
            UnityEditor.Editor.CreateEditor(this).OnInspectorGUI();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (_httpServiceSettings.enabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }
}