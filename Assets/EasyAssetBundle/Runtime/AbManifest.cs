using System.Collections.Generic;
using UnityEngine;

namespace EasyAssetBundle
{
    public class AbManifest : BaseAbManifest
    {
        readonly AssetBundleManifest _manifest;
        readonly HashSet<string> _abs;

        public AbManifest(int version, AssetBundleManifest manifest) : base(version)
        {
            _manifest = manifest;
            _abs = new HashSet<string>(manifest.GetAllAssetBundles());
        }

        public override Hash128 GetAssetBundleHash(string abName)
        {
            return _manifest.GetAssetBundleHash(abName);
        }

        public override string[] GetAllDependencies(string abName)
        {
            return _manifest.GetAllDependencies(abName);
        }

        public override bool Contains(string abName)
        {
            return _abs.Contains(abName);
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return _abs.GetEnumerator();
        }
    }
}