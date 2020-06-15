using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EncryptionProcessor.Editor
{
    public abstract class DataSource : ScriptableObject, IEnumerable<string>
    {
        public abstract IEnumerator<string> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}