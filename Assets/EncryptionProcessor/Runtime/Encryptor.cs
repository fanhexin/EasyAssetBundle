using UnityEngine;

namespace EncryptionProcessor
{
    public abstract class Encryptor : ScriptableObject
    {
        public abstract void Encrypt(ref byte[] buffer, int cnt);
        public abstract void Decrypt(ref byte[] buffer, int cnt);
    }
}