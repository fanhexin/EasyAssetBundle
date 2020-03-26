using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAssetBundle
{
    public class ProgressDispatcher
    {
        private IProgress<float> _progress;
        readonly List<InnerProgress> _innerProgresses = new List<InnerProgress>();
        private int _topIndex = -1;

        public Handler Create(IProgress<float> progress)
        {
            return new Handler(this, progress);    
        }

        InnerProgress CreateProgress()
        {
            if (_progress == null)
            {
                return null;
            }
            
            if (_topIndex < 0)
            {
                var p = new InnerProgress(this);
                _innerProgresses.Add(p);
                return p;
            }
            
            return _innerProgresses[_topIndex--];
        }

        void Report()
        {
            float sum = 0;
            int num = 0;
            for (int i = _topIndex + 1; i < _innerProgresses.Count; i++)
            {
                sum += _innerProgresses[i].value;
                ++num;
            }
            
            _progress.Report(sum / num);        
        }

        void Reset()
        {
            if (_progress == null)
            {
                return;
            }
            
            _innerProgresses.ForEach(x => x.Reset());
            _topIndex = _innerProgresses.Count - 1;
            _progress = null;
        }

        public struct Handler : IDisposable 
        {
            private readonly ProgressDispatcher _dispatcher;

            public Handler(ProgressDispatcher dispatcher, IProgress<float> progress)
            {
                _dispatcher = dispatcher;
                _dispatcher._progress = progress;
            }
            
            public IProgress<float> CreateProgress()
            {
                return _dispatcher.CreateProgress();
            }
            
            public void Dispose()
            {
                _dispatcher.Reset();
            }
        }

        class InnerProgress : IProgress<float>
        {
            private readonly ProgressDispatcher _dispatcher;
            public float value { get; private set; }

            public InnerProgress(ProgressDispatcher dispatcher)
            {
                _dispatcher = dispatcher;
            }
            
            public void Report(float value)
            {
                this.value = Mathf.Max(this.value, value);
                _dispatcher.Report();
            }

            public void Reset()
            {
                value = 0;
            }
        }
    }
}