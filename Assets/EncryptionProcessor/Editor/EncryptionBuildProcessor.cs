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
        [SerializeField] private DataSource _dataSource;
        [SerializeField] private Encryptor _encryptor;
        
        public override void OnBeforeBuild()
        {
            Process(buffer => _encryptor.Encrypt(ref buffer, buffer.Length));
        }

        public override void OnAfterBuild()
        {
            Process(buffer => _encryptor.Decrypt(ref buffer, buffer.Length));
        }

        public override void OnCancelBuild()
        {
            OnAfterBuild();
        }

        void Process(Action<byte[]> fn)
        {
            if (_dataSource == null || _encryptor == null)
            {
                return;
            }
            
            foreach (string path in _dataSource)
            {
                byte[] buffer = File.ReadAllBytes(path);
                fn?.Invoke(buffer);
                File.WriteAllBytes(path, buffer);
                AssetDatabase.ImportAsset(path);
            }
        }
    }
}