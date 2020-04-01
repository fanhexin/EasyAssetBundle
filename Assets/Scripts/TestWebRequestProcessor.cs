using EasyAssetBundle.Common;
using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu(fileName = nameof(TestWebRequestProcessor), menuName = nameof(TestWebRequestProcessor))]
public class TestWebRequestProcessor : WebRequestProcessor
{
    public override string HandleUrl(string url)
    {
        Debug.Log($"{nameof(TestWebRequestProcessor)} {nameof(HandleUrl)} {url}");
        return url;
    }

    public override UnityWebRequest HandleRequest(UnityWebRequest request)
    {
        Debug.Log($"{nameof(TestWebRequestProcessor)} {nameof(HandleRequest)} {request.uri}");
        return request;
    }
}
