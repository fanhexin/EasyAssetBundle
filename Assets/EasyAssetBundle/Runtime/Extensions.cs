#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using EasyAssetBundle.Common;
using UnityEngine;

namespace EasyAssetBundle
{
    public static class Extensions
    {
        public static string ToGenericName(this RuntimePlatform platform)
        {
#if UNITY_EDITOR
            return EditorUserBuildSettings.activeBuildTarget.ToString();
#else
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                default:
                    return platform.ToString();
            }
#endif
        }
    }
}