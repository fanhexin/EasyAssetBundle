using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public override void OnBeforeBuild()
        {
            Process(Encrypt); 
        }

        public override void OnAfterBuild()
        {
            Process(Decrypt);
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

            string[] paths = _dataSource.ToArray();
            if (paths.Length < SystemInfo.processorCount)
            {
                foreach (string path in paths)
                {
                    fn?.Invoke(path);
                    AssetDatabase.ImportAsset(path);
                }

                return;
            }
            
            Task[] tasks = new Task[SystemInfo.processorCount - 1];
            int jobNumPerTask = paths.Length / SystemInfo.processorCount;
            for (int i = 0; i < SystemInfo.processorCount - 1; i++)
            {
                int index = i;
                
                tasks[i] = Task.Run(() =>
                {
                    foreach (string p in paths.Skip(index * jobNumPerTask).Take(jobNumPerTask))
                    {
                        fn(p);    
                    }
                });
            }
            
            foreach (string p in paths.Skip((SystemInfo.processorCount - 1) * jobNumPerTask))
            {
                fn(p);    
            }
            
            Task.WhenAll(tasks).Wait();

            string operationName = fn.Method.Name;
            try
            {
                float progress = 0f;
                foreach (string p in paths)
                {
                    EditorUtility.DisplayProgressBar(nameof(EncryptionBuildProcessor), operationName, progress);
                    AssetDatabase.ImportAsset(p);
                    progress += 1f / paths.Length;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);    
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void Encrypt(string path)
        {
            byte[] buffer = File.ReadAllBytes(path);
            _encryptor.Encrypt(ref buffer, buffer.Length);
            string base64 = Convert.ToBase64String(buffer);
            File.WriteAllText(path, base64);
        }
        
        void Decrypt(string path)
        {
            string base64 = File.ReadAllText(path);
            byte[] buffer = Convert.FromBase64String(base64);
            _encryptor.Decrypt(ref buffer, buffer.Length);
            File.WriteAllBytes(path, buffer);
        }
    }
}