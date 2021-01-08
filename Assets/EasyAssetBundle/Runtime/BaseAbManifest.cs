using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAssetBundle
{
    public abstract class BaseAbManifest : IEnumerable<string>
    {
        public virtual int version { get; }

        protected BaseAbManifest(int version)
        {
            this.version = version;
        }
        
        public abstract Hash128 GetAssetBundleHash(string abName);
        public abstract string[] GetAllDependencies(string abName);
        public abstract bool Contains(string abName);
        public abstract IEnumerator<string> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}