using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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
            [SerializeField] bool _enabled = true;
            [SerializeField] int _port = 8888;

            public bool enabled => _enabled;
            public int port => _port;
        }

        [SerializeField] HttpServiceSettings _httpServiceSettings;
        [SerializeField] RuntimeSettings _runtimeSettings;
        [SerializeField] BuildAssetBundleOptions _buildOptions = BuildAssetBundleOptions.ChunkBasedCompression;
        [SerializeField, HideInInspector] Mode _mode;

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


        SimpleHTTPServer _simpleHttpServer;

        public string simulateUrl
        {
            get
            {
                var address = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                                x.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                                x.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                    .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                    .First(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                return $"http://{address.Address}:{_httpServiceSettings.port}";
            }
        }

        public static string cacheBasePath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "EasyAssetBundleCache");
        
        public static string currentTargetCachePath =>
            Path.Combine(cacheBasePath , EditorUserBuildSettings.activeBuildTarget.ToString());

        void Start()
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

        void Stop()
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