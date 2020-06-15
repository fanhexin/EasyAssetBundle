using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public abstract class AbstractBuildProcessor : ScriptableObject
    {
        public abstract void OnBeforeBuild();
        public abstract void OnAfterBuild();
        public abstract void OnCancelBuild();
    }
}