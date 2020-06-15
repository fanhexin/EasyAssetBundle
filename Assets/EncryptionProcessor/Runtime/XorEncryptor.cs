using System.Text;
using UnityEngine;

namespace EncryptionProcessor
{
    [CreateAssetMenu(fileName = nameof(XorEncryptor), menuName = "EncryptionProcessor/XorEncryptor")]
    public class XorEncryptor : Encryptor
    {
        [SerializeField] private string _secretKey;

        public override void Encrypt(ref byte[] buffer, int cnt)
        {
            byte[] keyBytes = Encoding.ASCII.GetBytes(_secretKey);
            for (var i = 0; i < cnt;)
            {
                foreach (byte kb in keyBytes)
                {
                    byte v = buffer[i];
                    buffer[i++] = (byte) (v ^ kb);
                    if (i >= cnt)
                    {
                        break;
                    }
                }
            }
        }

        public override void Decrypt(ref byte[] buffer, int cnt)
        {
            Encrypt(ref buffer, cnt);            
        }
    }
}