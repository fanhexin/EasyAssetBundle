using System;
using System.Collections;
using System.Linq;
using System.Threading;
using EasyAssetBundle;
using EasyAssetBundle.Common;
using FakeItEasy;
using NUnit.Framework;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Tests
{
    public class RealAssetBundleLoaderTests
    {
        const string TEST_CDN_URL = "https://unittest.com";

        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionLessThanLocalVersion_ShouldNotSaveRemoteVersion() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_ShouldNotSaveRemoteVersion(2, 1, 1));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionEqualToLocalVersion_ShouldNotSaveRemoteVersion() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_ShouldNotSaveRemoteVersion(2, 1, 2));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionLessThanCurrentVersion_ShouldNotSaveRemoteVersion() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_ShouldNotSaveRemoteVersion(1, 2, 1));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionEqualToCurrentVersion_ShouldNotSaveRemoteVersion() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_ShouldNotSaveRemoteVersion(1, 2, 2));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionGreaterThanCurrentAndLocalVersion_SaveRemoteVersion() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_SaveRemoteVersion(1, 2, 3));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionLessThanLocalVersion_OnlyLoadLocalManifest() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_OnlyLoadLocalManifest(2, 1, 1));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionEqualToLocalVersion_OnlyLoadLocalManifest() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_OnlyLoadLocalManifest(2, 1, 2));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionGreaterThanLocalVersion_LoadLocalAndRemoteManifest() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_LoadLocalAndRemoteManifest(2, 1, 3));
        
        [UnityTest]
        public IEnumerator InitAsync_WithRemoteVersionGreaterThanLocalVersionLessThanCurrentVersion_LoadLocalAndRemoteManifest() =>
            UniTask.ToCoroutine(() => Loader_InitAsync_LoadLocalAndRemoteManifest(2, 4, 3));
        
        [UnityTest]
        public IEnumerator InitAsync_WithLoadRemoteVersionThrow_NoException() =>
            UniTask.ToCoroutine(async () =>
            {
                async UniTask WrapperFn()
                {
                    await Loader_InitAsync_WithLoadRemoteVersionThrow(1, 1, 1);
                }

                Assert.DoesNotThrow(() => WrapperFn().GetResult());
            });
        
        [UnityTest]
        public IEnumerator InitAsync_WithLoadRemoteVersionThrow_ShouldNotSaveRemoteVersion() =>
            UniTask.ToCoroutine(async () =>
            {
                var loader = await Loader_InitAsync_WithLoadRemoteVersionThrow(1, 2, 3);
                A.CallTo(() => loader.saveVersionFake(A<int>._)).MustNotHaveHappened();
            });
        
        [UnityTest]
        public IEnumerator InitAsync_WithLoadRemoteVersionThrow_LoadLocalAndRemoteManifest() =>
            UniTask.ToCoroutine(async () =>
            {
                var loader = await Loader_InitAsync_WithLoadRemoteVersionThrow(1, 2, 3);
                AssertLoadLocalAndRemoteManifest(1, 2, 2, loader);
            });

        [UnityTest]
        public IEnumerator InitAsync_WithLoadLocalManifestThrow_ThrowException() =>
            UniTask.ToCoroutine(async () =>
            {
                async UniTask WrapperFn()
                {
                    await Loader_InitAsync(1, 2, 3, stub => 
                        A.CallTo(() => stub.loadManifestAsyncFake(A<string>.That.StartsWith("file:"), 1))
                            .Throws<Exception>());
                }

                Assert.Throws<Exception>(() => WrapperFn().GetResult());
            });
        
        [UnityTest]
        public IEnumerator InitAsync_WithLoadRemoteManifestThrow_NoException() =>
            UniTask.ToCoroutine(async () =>
            {
                async UniTask WrapperFn()
                {
                    await Loader_InitAsync_WithLoadRemoteManifestThrow(1, 2, 3);
                }

                Assert.DoesNotThrow(() => WrapperFn().GetResult());
            });
        
        [UnityTest]
        public IEnumerator InitAsync_WithLoadRemoteManifestThrow_ShouldNotSaveRemoteVersion() =>
            UniTask.ToCoroutine(async () =>
            {
                var loader = await Loader_InitAsync_WithLoadRemoteManifestThrow(1, 2, 3);
                A.CallTo(() => loader.saveVersionFake(A<int>._)).MustNotHaveHappened();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithStaticAb_LoadFromLocal() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var localHash = Hash128.Compute(abName);
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Static));
                A.CallTo(() => loader.localManifest.GetAssetBundleHash(abName)).Returns(localHash);
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => x.Contains("file:") && x.Contains(abName)), localHash))
                    .MustHaveHappened();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithRemoteAb_LoadFromRemote() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var remoteHash = Hash128.Compute(abName);
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Remote));
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => !x.Contains("file:") && x.Contains(abName)), remoteHash))
                    .MustHaveHappened();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithNotCachedAndNoUpdatePatchableAb_LoadFromLocal() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var hash = Hash128.Compute(abName);
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Patchable));
                A.CallTo(() => loader.localManifest.GetAssetBundleHash(abName)).Returns(hash);
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(hash);
                A.CallTo(() => loader.isVersionCachedFake(A<string>.That.Contains(abName), hash))
                    .Returns(false);
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => x.Contains("file:") && x.Contains(abName)), hash))
                    .MustHaveHappened();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithNotCachedAndHaveUpdatePatchableAb_LoadFromRemote() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var localHash = Hash128.Compute(abName);
                var remoteHash = Hash128.Compute(abName + "new");
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Patchable));
                A.CallTo(() => loader.localManifest.GetAssetBundleHash(abName)).Returns(localHash);
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                A.CallTo(() => loader.isVersionCachedFake(A<string>.That.Contains(abName), localHash))
                    .Returns(false);
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => !x.Contains("file:") && x.Contains(abName)), remoteHash))
                    .MustHaveHappened();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithCachedPatchableAb_LoadFromRemote() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var localHash = Hash128.Compute(abName);
                var remoteHash = Hash128.Compute(abName + "new");
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Patchable));
                A.CallTo(() => loader.localManifest.GetAssetBundleHash(abName)).Returns(localHash);
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                A.CallTo(() => loader.isVersionCachedFake(A<string>.That.Contains(abName), localHash))
                    .Returns(true);
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => !x.Contains("file:") && x.Contains(abName)), remoteHash))
                    .MustHaveHappened();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithNotFoundedAb_LoadFromRemote() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var remoteHash = Hash128.Compute(abName);
                var loader = CreateLoader(1, 2, 3, (abName + "new", BundleType.Patchable));
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => !x.Contains("file:") && x.Contains(abName)), remoteHash))
                    .MustHaveHappened();
            });

        [UnityTest]
        public IEnumerator LoadAsync_WithStaticAb_GetDependenciesFromLocalManifest() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Static));
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() => loader.localManifest.GetAllDependencies(abName)).MustHaveHappenedOnceExactly();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithPatchableAb_GetDependenciesFromRemoteManifest() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Patchable));
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() => loader.remoteManifest.GetAllDependencies(abName)).MustHaveHappenedOnceExactly();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithRemoteAb_GetDependenciesFromRemoteManifest() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Remote));
                await loader.LoadAsync(abName, null, default);
                A.CallTo(() => loader.remoteManifest.GetAllDependencies(abName)).MustHaveHappenedOnceExactly();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithLoadNotCachedRemoteAbException_ThrowSameException() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var remoteHash = Hash128.Compute(abName);
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Remote));
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                A.CallTo(() => loader.loadAssetBundleAsyncFake(A<string>.That.Contains(abName), remoteHash)).Throws<Exception>();

                async UniTask WrapperFn()
                {
                    await loader.LoadAsync(abName, null, default);
                }
                Assert.Throws<Exception>(() => WrapperFn().GetResult());
            });
            
        [UnityTest]
        public IEnumerator LoadAsync_WithLoadCachedRemoteAbException_FallbackToCache() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var remoteHash = Hash128.Compute(abName);
                var cachedHash = Hash128.Compute(abName + "cache");
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Remote));
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                A.CallTo(() => loader.getCachedVersionRecentlyFake(abName)).Returns(cachedHash);
                A.CallTo(() => loader.loadAssetBundleAsyncFake(A<string>.That.Contains(abName), remoteHash)).Throws<Exception>();

                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => !x.Contains("file:") && x.Contains(abName)), cachedHash))
                    .MustHaveHappenedOnceExactly();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithLoadNotCachedPatchableAbException_FallbackToLocal() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var remoteHash = Hash128.Compute(abName);
                var localHash = Hash128.Compute(abName + "local");
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Patchable));
                A.CallTo(() => loader.localManifest.GetAssetBundleHash(abName)).Returns(localHash);
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                A.CallTo(() => loader.loadAssetBundleAsyncFake(A<string>.That.Contains(abName), remoteHash)).Throws<Exception>();

                await loader.LoadAsync(abName, null, default);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => x.Contains("file:") && x.Contains(abName)), localHash))
                    .MustHaveHappenedOnceExactly();
            });

        [UnityTest]
        public IEnumerator LoadAsync_WithSameRemoteAbMultiTimesAtSameTime_OnlyLoadAssetBundleOnce() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var remoteHash = Hash128.Compute(abName);
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Remote));
                A.CallTo(() => loader.remoteManifest.GetAssetBundleHash(abName)).Returns(remoteHash);
                var t1 = loader.LoadAsync(abName, null, default);
                var t2 = loader.LoadAsync(abName, null, default);
                await UniTask.WhenAll(t1, t2);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => !x.Contains("file:") && x.Contains(abName)), remoteHash))
                    .MustHaveHappenedOnceExactly();
            });
        
        [UnityTest]
        public IEnumerator LoadAsync_WithDiffRemoteAbHaveOneSameDependencies_OnlyLoadAssetBundleOnce() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb_1";
                string abName2 = "testAb_2";
                string sameDependency = "common";
                
                var loader = CreateLoader(1, 2, 3, 
                    (abName, BundleType.Remote),
                    (abName2, BundleType.Remote),
                    (sameDependency, BundleType.Remote));

                A.CallTo(() => loader.remoteManifest.GetAllDependencies(A<string>.That.Matches(x => x == abName || x == abName2)))
                    .Returns(new[] {sameDependency});
                
                var t1 = loader.LoadAsync(abName, null, default);
                var t2 = loader.LoadAsync(abName2, null, default);
                await UniTask.WhenAll(t1, t2);
                A.CallTo(() =>
                        loader.loadAssetBundleAsyncFake(
                            A<string>.That.Matches(x => !x.Contains("file:") && x.Contains(sameDependency)), A<Hash128>._))
                    .MustHaveHappenedOnceExactly();
            });

        [UnityTest]
        public IEnumerator LoadAsync_WithLoadAssetBundleOperationCanceledException_ThrowSameException() =>
            UniTask.ToCoroutine(async () =>
            {
                string abName = "testAb";
                var loader = CreateLoader(1, 2, 3, (abName, BundleType.Remote));
                A.CallTo(() => loader.loadAssetBundleAsyncFake(A<string>._, A<Hash128>._)).Throws<OperationCanceledException>();
                async UniTask WrapperFn()
                {
                    await loader.LoadAsync(abName, null, default);
                }

                Assert.Throws<OperationCanceledException>(() => WrapperFn().GetResult());
            });
        
        async UniTask<RealAssetBundleLoaderStub> Loader_InitAsync(int localVersion, int currentVersion,
            int remoteVersion, Action<RealAssetBundleLoaderStub> initStubFn = null)
        {
            RealAssetBundleLoaderStub loader = CreateLoader(localVersion, currentVersion, remoteVersion);
            initStubFn?.Invoke(loader);
            await loader.InitAsync();
            return loader;
        }

        UniTask<RealAssetBundleLoaderStub> Loader_InitAsync_WithLoadRemoteVersionThrow(int localVersion,
            int currentVersion, int remoteVersion)
        {
            return Loader_InitAsync(localVersion, currentVersion, remoteVersion, stub => 
                A.CallTo(() => stub.loadRemoteVersionAsyncFake()).Throws<Exception>());
        }
        
        UniTask<RealAssetBundleLoaderStub> Loader_InitAsync_WithLoadRemoteManifestThrow(int localVersion,
            int currentVersion, int remoteVersion)
        {
            return Loader_InitAsync(localVersion, currentVersion, remoteVersion, stub => 
                A.CallTo(() => stub.loadManifestAsyncFake(A<string>.That.Not.StartsWith("file:"), remoteVersion))
                    .Throws<Exception>());
        }

        async UniTask Loader_InitAsync_ShouldNotSaveRemoteVersion(int localVersion, int currentVersion, int remoteVersion)
        {
            var loader = await Loader_InitAsync(localVersion, currentVersion, remoteVersion);
            A.CallTo(() => loader.saveVersionFake(A<int>._)).MustNotHaveHappened();
        }
        
        async UniTask Loader_InitAsync_SaveRemoteVersion(int localVersion, int currentVersion, int remoteVersion)
        {
            var loader = await Loader_InitAsync(localVersion, currentVersion, remoteVersion);
            A.CallTo(() => loader.saveVersionFake(remoteVersion)).MustHaveHappened();
        }

        async UniTask Loader_InitAsync_OnlyLoadLocalManifest(int localVersion, int currentVersion, int remoteVersion)
        {
            var loader = await Loader_InitAsync(localVersion, currentVersion, remoteVersion);
            AssertOnlyLoadLocalManifest(localVersion, loader);
        }

        static void AssertOnlyLoadLocalManifest(int localVersion, RealAssetBundleLoaderStub loader)
        {
            A.CallTo(() => loader.loadManifestAsyncFake(A<string>._, A<int>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => loader.loadManifestAsyncFake(A<string>.That.StartsWith("file:"), localVersion))
                .MustHaveHappenedOnceExactly();
        }

        async UniTask Loader_InitAsync_LoadLocalAndRemoteManifest(int localVersion, int currentVersion, int remoteVersion)
        {
            var loader = await Loader_InitAsync(localVersion, currentVersion, remoteVersion);
            AssertLoadLocalAndRemoteManifest(localVersion, currentVersion, remoteVersion, loader);
        }

        static void AssertLoadLocalAndRemoteManifest(int localVersion, int currentVersion, int remoteVersion,
            RealAssetBundleLoaderStub loader)
        {
            A.CallTo(() => loader.loadManifestAsyncFake(A<string>._, A<int>._))
                .MustHaveHappenedTwiceExactly();
            A.CallTo(() => loader.loadManifestAsyncFake(A<string>.That.StartsWith("file:"), localVersion))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() =>
                    loader.loadManifestAsyncFake(A<string>.That.Not.StartsWith("file:"),
                        Mathf.Max(currentVersion, remoteVersion)))
                .MustHaveHappenedOnceExactly();
        }

        RealAssetBundleLoaderStub CreateLoader(int localVersion, int currentVersion, int remoteVersion,
            params (string, BundleType)[] bundleArr)
        {
            var loader = new RealAssetBundleLoaderStub(string.Empty,
                new RuntimeSettings
                {
                    version = localVersion,
                    cdnUrl = TEST_CDN_URL,
                    name2BundleDic = bundleArr.ToDictionary(x => x.Item1,
                        x => new Bundle {name = x.Item1, type = x.Item2})
                }) {currentVersion = currentVersion};

            A.CallTo(() => loader.loadRemoteVersionAsyncFake()).Returns(remoteVersion);
            return loader;
        }

        class RealAssetBundleLoaderStub : RealAssetBundleLoader 
        {
            public int currentVersion { get; set; }
            public BaseAbManifest localManifest { get; }
            public BaseAbManifest remoteManifest { get; }
            public Action<int> saveVersionFake { get; }
            public Func<string, int, BaseAbManifest> loadManifestAsyncFake { get; }
            public Func<int> loadRemoteVersionAsyncFake { get; }
            public Action<string, Hash128> loadAssetBundleAsyncFake { get; }
            public Func<string, Hash128, bool> isVersionCachedFake { get; }
            public Func<string, Hash128?> getCachedVersionRecentlyFake { get; }

            public RealAssetBundleLoaderStub(string basePath, RuntimeSettings runtimeSettings) 
                : base(basePath, runtimeSettings)
            {
                saveVersionFake = A.Fake<Action<int>>();
                localManifest = A.Fake<BaseAbManifest>();
                remoteManifest = A.Fake<BaseAbManifest>();
                loadManifestAsyncFake = A.Fake<Func<string, int, BaseAbManifest>>();
                loadRemoteVersionAsyncFake = A.Fake<Func<int>>();
                loadAssetBundleAsyncFake = A.Fake<Action<string, Hash128>>();
                isVersionCachedFake = A.Fake<Func<string, Hash128, bool>>();
                getCachedVersionRecentlyFake = A.Fake<Func<string, Hash128?>>();
                
                A.CallTo(() => loadManifestAsyncFake(A<string>.That.StartsWith("file:"), A<int>._))
                    .Returns(localManifest);
                
                A.CallTo(() => loadManifestAsyncFake(A<string>.That.Not.StartsWith("file:"), A<int>._))
                    .Returns(remoteManifest);
            }

            public override int version => currentVersion;

            protected override UniTask<BaseAbManifest> LoadManifestAsync(string url, int version, Func<string, UnityWebRequest> createFn)
            {
                BaseAbManifest manifest = loadManifestAsyncFake(url, version);
                A.CallTo(() => manifest.version).Returns(version);
                return UniTask.FromResult(manifest);
            }

            protected override UniTask<int> LoadRemoteVersionAsync()
            {
                int version = loadRemoteVersionAsyncFake();
                return UniTask.FromResult(version);
            }

            protected override UniTask<AssetBundle> LoadAssetBundleAsync(string url, Hash128 hash, IProgress<float> progress, CancellationToken token)
            {
                loadAssetBundleAsyncFake(url, hash);
                return UniTask.FromResult<AssetBundle>(null);
            }

            protected override void SaveVersion(int v)
            {
                saveVersionFake(v);
                currentVersion = v;
            }

            protected override bool IsVersionCached(string url, Hash128 hash)
            {
                return isVersionCachedFake(url, hash);
            }

            public override Hash128? GetCachedVersionRecently(string abName)
            {
                return getCachedVersionRecentlyFake(abName);
            }
        }
    }
}