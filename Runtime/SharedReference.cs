using System;

namespace EasyAssetBundle
{
    public class SharedReference<T> : IDisposable
    {
        private readonly T _value;
        private readonly Action<T, object> _disposeAction;
        private int _refCnt;

        public T GetValue()
        {
            ++_refCnt;
            return _value;
        }

        public SharedReference(T value, Action<T, object> disposeAction)
        {
            _value = value;
            _disposeAction = disposeAction;
            _refCnt = 0;
        }

        public void Dispose()
        {
            Dispose(default);
        }

        public void Dispose(object p)
        {
            if (--_refCnt != 0)
            {
                return;
            }

            _disposeAction(_value, p);
        }

        public static SharedReference<T> operator ++(SharedReference<T> sr)
        {
            ++sr._refCnt;
            return sr;
        }
    }
}