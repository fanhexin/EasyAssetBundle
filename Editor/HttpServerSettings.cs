using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public class HttpServerSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        private const string SAVE_PATH = "Assets/Editor/EasyAssetBundleHttpServerSettings.asset";
        private static HttpServerSettings _instance;

        public static HttpServerSettings instance
        {
            get
            {
                if (_instance == null)
                {
                    if (File.Exists(SAVE_PATH))
                    {
                        _instance = AssetDatabase.LoadAssetAtPath<HttpServerSettings>(SAVE_PATH);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(SAVE_PATH));
                        _instance = CreateInstance<HttpServerSettings>();
                        AssetDatabase.CreateAsset(_instance, SAVE_PATH);
                        AssetDatabase.Refresh();
                    }
                }

                return _instance;
            }    
        }

        [SerializeField]
        private bool _enabled;
        
        // todo 添加自动设置未被占用port的逻辑
        [SerializeField]
        private int _port = 998;

        public int port => _port;
        public bool enabled => _enabled;

        public string url
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

                return $"http://{localIP}/{_port}";
            }
        }
        
        private SimpleHTTPServer _simpleHttpServer;

        private void OnEnable()
        {
            hideFlags = HideFlags.None;
        }

        private void Start()
        {
            if (_simpleHttpServer == null)
            {
                _simpleHttpServer = new SimpleHTTPServer("Assets/../HostedData", _port);
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

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_enabled)
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