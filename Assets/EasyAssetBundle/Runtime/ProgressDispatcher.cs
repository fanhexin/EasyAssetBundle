using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyAssetBundle
{
    public class ProgressDispatcher
    {
        static ProgressDispatcher _instance;
        public static ProgressDispatcher instance => _instance ?? (_instance = new ProgressDispatcher());

        readonly Queue<Handler> _handlers = new Queue<Handler>();

        ProgressDispatcher() { }

        public Handler Create(IProgress<float> progress)
        {
            var handler = _handlers.Count > 0 ? _handlers.Dequeue() 
                : new Handler(this);
            handler.progress = progress;
            return handler;
        }

        void Recycle(Handler handler)
        {
            _handlers.Enqueue(handler);    
        }

        public class Handler : IDisposable 
        {
            readonly ProgressDispatcher _dispatcher;
            readonly List<InnerProgress> _innerProgresses = new List<InnerProgress>();
            public IProgress<float> progress { private get; set; }
            int _topIndex = -1;

            public Handler(ProgressDispatcher dispatcher)
            {
                _dispatcher = dispatcher;
            }
            
            public IProgress<float> CreateProgress()
            {
                if (progress == null)
                {
                    return null;
                }

                InnerProgress p;
                if (_topIndex < 0)
                {
                    p = new InnerProgress(this);
                    _innerProgresses.Add(p);
                }
                else
                {
                    p = _innerProgresses[_topIndex--];
                    p.Reset();
                }
                
                return p;
            }
            
            public void Dispose()
            {
                _topIndex = _innerProgresses.Count - 1;
                progress = null;
                _dispatcher.Recycle(this);
            }

            void Report()
            {
                if (progress == null)
                {
                    return;
                }
                
                float sum = 0;
                int num = 0;
                for (int i = _topIndex + 1; i < _innerProgresses.Count; i++)
                {
                    sum += _innerProgresses[i].value;
                    ++num;
                }
                
                progress.Report(sum / num);        
            }
            
            class InnerProgress : Progress<float>
            {
                readonly Handler _handler;
                public float value { get; private set; }

                public InnerProgress(Handler handler)
                {
                    _handler = handler;
                }

                protected override void OnReport(float value)
                {
                    base.OnReport(value);
                    this.value = Mathf.Clamp01(Mathf.Max(this.value, value));
                    _handler.Report();
                }

                public void Reset()
                {
                    value = 0;
                }
            }
        }
    }
}