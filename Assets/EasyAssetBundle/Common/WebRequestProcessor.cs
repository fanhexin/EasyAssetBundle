using UnityEngine;
using UnityEngine.Networking;

namespace EasyAssetBundle.Common
{
    public abstract class WebRequestProcessor : ScriptableObject
    {
        public abstract string HandleUrl(string url);
        public abstract UnityWebRequest HandleRequest(UnityWebRequest request);
    }
}