using System;
using EasyAssetBundle;
using FakeItEasy;
using FakeItEasy.Configuration;
using NUnit.Framework;

namespace Tests
{
    public class SharedReferenceTests
    {
        [Test]
        public void Dispose_DisposeActionInvoked()
        {
            var (sr, validation) = CreateSharedRef(1);
            sr.Dispose();
            validation.MustHaveHappened();
        }

        [Test]
        public void Dispose_WithNullDisposeAction_NoException()
        {
            var sr = new SharedReference<int>(1, null);    
            Assert.DoesNotThrow(sr.Dispose);
        }

        [Test]
        public void Dispose_CalledMultiTimes_DisposeActionInvokedOnlyOnce()
        {
            var (sr, validation) = CreateSharedRef(1);
            sr.Dispose(); 
            sr.Dispose();
            validation.MustHaveHappenedOnceExactly();
        }
        
        [TestCase(1)]
        [TestCase(true)]
        [TestCase("test")]
        public void Dispose_WithParameter_DisposeActionInvoked(object disposeArg)
        {
            var (sr, validation) = CreateSharedRef(1, disposeArg);
            sr.Dispose(disposeArg);
            validation.MustHaveHappened();
        }
        
        [TestCase(1)]
        [TestCase(true)]
        [TestCase("test")]
        public void Dispose_CalledWithParameterMultiTimes_DisposeActionInvokedOnlyOnce(object disposeArg)
        {
            var (sr, validation) = CreateSharedRef(1, disposeArg);
            sr.Dispose(disposeArg);
            sr.Dispose(disposeArg);
            validation.MustHaveHappened();
        }

        [Test]
        public void Dispose_ThenGetValue_ThrowException()
        {
            var sr = new SharedReference<int>(1, null);
            sr.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sr.GetValue());
        }
        
        [Test]
        public void GetValue_ThenDispose_DisposeActionInvoked()
        {
            var (sr, validation) = CreateSharedRef(1);
            sr.GetValue();
            sr.Dispose();
            validation.MustHaveHappened();
        }

        [Test]
        public void GetValue_MultiTimesThenDispose_DisposeActionNotHaveInvoked()
        {
            var (sr, validation) = CreateSharedRef(1);
            sr.GetValue();
            sr.GetValue();
            sr.Dispose();
            validation.MustNotHaveHappened();
        }

        [Test]
        public void GetValue_MultiTimesThenDisposeSameTimes_DisposeActionInvoked()
        {
            var (sr, validation) = CreateSharedRef(1);
            sr.GetValue();
            sr.GetValue();
            sr.Dispose();
            sr.Dispose();
            validation.MustHaveHappened();
        }

        [Test]
        public void GetValue_CalledLessThanDispose_DisposeActionInvokedOnlyOnce()
        {
            var (sr, validation) = CreateSharedRef(1);
            sr.GetValue();
            sr.GetValue();
            sr.Dispose();
            sr.Dispose();
            sr.Dispose();
            validation.MustHaveHappenedOnceExactly();
        }

        [Test]
        public void PlusPlusOperator_ThenDispose_DisposeActionInvoked()
        {
            var (sr, validation) = CreateSharedRef(1);
            ++sr;
            sr.Dispose();
            validation.MustHaveHappened();
        }
        
        [Test]
        public void PlusPlusOperator_MultiTimesThenDispose_DisposeActionNotHaveInvoked()
        {
            var (sr, validation) = CreateSharedRef(1);
            ++sr;
            ++sr;
            sr.Dispose();
            validation.MustNotHaveHappened();
        }
        
        [Test]
        public void PlusPlusOperator_MultiTimesThenDisposeSameTimes_DisposeActionInvoked()
        {
            var (sr, validation) = CreateSharedRef(1);
            ++sr;
            ++sr;
            sr.Dispose();
            sr.Dispose();
            validation.MustHaveHappened();
        }
        
        [Test]
        public void PlusPlusOperator_CalledLessThanDispose_DisposeActionInvokedOnlyOnce()
        {
            var (sr, validation) = CreateSharedRef(1);
            ++sr;
            ++sr;
            sr.Dispose();
            sr.Dispose();
            sr.Dispose();
            validation.MustHaveHappenedOnceExactly();
        }

        (SharedReference<int>, IVoidArgumentValidationConfiguration) CreateSharedRef(int value, object disposeArg = null)
        {
            var disposeAction = A.Fake<Action<int, object>>();
            var sr = new SharedReference<int>(value, disposeAction);
            return (sr, A.CallTo(() => disposeAction.Invoke(value, disposeArg)));
        }
    }
}