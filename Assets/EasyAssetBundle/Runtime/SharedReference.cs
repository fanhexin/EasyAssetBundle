using System;

namespace EasyAssetBundle
{
    public class SharedReference<T> : IDisposable
    {
        readonly T _value;
        readonly Action<T, object> _disposeAction;
        int _refCnt;

        public T GetValue()
        {
            if (_refCnt < 0)
            {
                throw new ObjectDisposedException(nameof(SharedReference<T>));
            }
            
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
            if (_refCnt < 0 || --_refCnt > 0)
            {
                return;
            }

            _refCnt = -1;
            _disposeAction?.Invoke(_value, p);
        }

        public static SharedReference<T> operator ++(SharedReference<T> sr)
        {
            ++sr._refCnt;
            return sr;
        }
    }
}