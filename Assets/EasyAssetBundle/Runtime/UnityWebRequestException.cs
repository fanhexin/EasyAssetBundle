using System;

namespace EasyAssetBundle
{
    public class UnityWebRequestException : Exception
    {
        public readonly string url;

        public UnityWebRequestException(string msg, string url)
            :base(msg)
        {
            this.url = url;
        }        
    }
}