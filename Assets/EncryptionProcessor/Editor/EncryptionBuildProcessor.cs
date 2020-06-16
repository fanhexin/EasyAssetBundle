using System;
using System.IO;
using EasyAssetBundle.Editor;
using UnityEditor;
using UnityEngine;

namespace EncryptionProcessor.Editor
{
    [CreateAssetMenu(fileName = nameof(EncryptionBuildProcessor), menuName = "EncryptionProcessor/EncryptionBuildProcessor")]
    public class EncryptionBuildProcessor : AbstractBuildProcessor
    {
        [SerializeField] DataSource _dataSource;
        [SerializeField] Encryptor _encryptor;
        
        [ContextMenu(nameof(OnBeforeBuild))]
        public override void OnBeforeBuild()
        {
            Process(path =>
            {
                byte[] buffer = File.ReadAllBytes(path);
                _encryptor.Encrypt(ref buffer, buffer.Length);
                string base64 = Convert.ToBase64String(buffer);
                File.WriteAllText(path, base64);
            });
        }

        [ContextMenu(nameof(OnAfterBuild))]
        public override void OnAfterBuild()
        {
            Process(path =>
            {
                string base64 = File.ReadAllText(path);
                byte[] buffer = Convert.FromBase64String(base64);
                _encryptor.Decrypt(ref buffer, buffer.Length);
                File.WriteAllBytes(path, buffer);
            });
        }

        public override void OnCancelBuild()
        {
            OnAfterBuild();
        }

        void Process(Action<string> fn)
        {
            if (_dataSource == null || _encryptor == null)
            {
                return;
            }
            
            foreach (string path in _dataSource)
            {
                fn?.Invoke(path);
                AssetDatabase.ImportAsset(path);
            }
        }
    }
}