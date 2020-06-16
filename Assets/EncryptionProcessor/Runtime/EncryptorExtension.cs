using System;
using System.Text;
#if UNITY_EDITOR
using EasyAssetBundle.Common.Editor;
#endif
using UnityEngine;

namespace EncryptionProcessor
{
    public static class EncryptorExtension
    {
        public static string Decrypt(this Encryptor encryptor, TextAsset textAsset)
        {
#if UNITY_EDITOR
            if (Settings.instance.mode == Settings.Mode.Virtual)
            {
                return textAsset.text;
            }
#endif
            
            byte[] buffer = Convert.FromBase64String(textAsset.text);
            encryptor.Decrypt(ref buffer, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }
    }
}