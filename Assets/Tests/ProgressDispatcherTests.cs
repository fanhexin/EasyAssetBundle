using System;
using EasyAssetBundle;
using FakeItEasy;
using NUnit.Framework;

namespace Tests
{
    public class ProgressDispatcherTests
    {
        [Test]
        public void HandlerCreateProgress_ConstructWithANullIProgress_ReturnNull()
        {
            var dispatcher = ProgressDispatcher.instance;
            using (var handler = dispatcher.Create(null))
            {
                IProgress<float> progress = handler.CreateProgress();
                Assert.IsNull(progress);
            }
        }

        [TestCase(0.0f, 0.0f, 0.0f)]
        [TestCase(0.0f, 1.0f, 0.5f)]
        [TestCase(1.0f, 0.0f, 0.5f)]
        [TestCase(1.0f, 1.0f, 1.0f)]
        [TestCase(0.0f, 0.5f, 0.25f)]
        [TestCase(0.5f, 0.0f, 0.25f)]
        [TestCase(0.5f, 0.5f, 0.5f)]
        [TestCase(0.0f, 1.5f, 0.5f)]
        [TestCase(1.5f, 0.0f, 0.5f)]
        [TestCase(1.5f, 1.5f, 1.0f)]
        [TestCase(0.5f, 1.5f, 0.75f)]
        [TestCase(1.5f, 0.5f, 0.75f)]
        [TestCase(1.0f, 1.5f, 1.0f)]
        [TestCase(1.5f, 1.0f, 1.0f)]
        public void FinalValueOfOriginalProgress_EqualToAverageOfAllSubProgresses(float p1, float p2, float targetProgress)
        {
            var dispatcher = ProgressDispatcher.instance;
            var progress = A.Fake<IProgress<float>>();
            
            using (var handler = dispatcher.Create(progress))
            {
                handler.CreateProgress().Report(p1);
                handler.CreateProgress().Report(p2);
            }
            
            A.CallTo(() => progress.Report(targetProgress)).MustHaveHappened();
        }
    }
}
