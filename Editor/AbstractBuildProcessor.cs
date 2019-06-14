using UnityEngine;

namespace EasyAssetBundle.Editor
{
    public abstract class AbstractBuildProcessor : ScriptableObject
    {
        public abstract void BeforeBuild();
        public abstract void AfterBuild();
    }
}