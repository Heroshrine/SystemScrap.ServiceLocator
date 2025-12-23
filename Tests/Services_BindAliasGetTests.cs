using System.Collections;
using NUnit.Framework;
using SystemScrap.ServiceLocator.Core;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using static SystemScrap.ServiceLocator.Tests.TestHelper;

namespace SystemScrap.ServiceLocator.Tests
{
    public class Services_BindAliasGetTests
    {
        [SetUp]
        public void Setup()
        {
            Reset();
        }

        [Test]
        public void Bind_Alias_Get_GlobalService()
        {
            var service = new DummyDataService { Obj = new object() };
            Services.Bind(service).As<object>();

            var got = Services.For().Get<DummyDataService>();
            Services.For().TryGet(out DummyDataService got2);

            AssertSameAs(got, got2);

            var ogot = Services.For().Get<object>();
            Services.For().TryGet(out object ogot2);

            AssertSameAs(got, ogot, ogot2);
            Assert.AreEqual(got.Obj, (ogot as DummyDataService)!.Obj);
        }

        [UnityTest]
        public IEnumerator Bind_Alias_Get_SceneService()
        {
            var service = new DummyDataService { Obj = new object() };
            var scene = SceneManager.CreateScene("TestScene");
            Services.Bind(service, scene).As<object>();

            var resolver = Services.For(scene);

            yield return null;

            var got = resolver.Get<DummyDataService>();
            resolver.TryGet(out DummyDataService got2);
            var got3 = Services.For(scene).Get<DummyDataService>();
            Services.For(scene).TryGet(out DummyDataService got4);

            AssertSameAs(got, got2, got3, got4);
            yield return null;

            var ogot = resolver.Get<object>();
            resolver.TryGet(out object ogot2);
            var ogot3 = Services.For(scene).Get<object>();
            Services.For(scene).TryGet(out object ogot4);

            AssertSameAs(got, ogot, ogot2, ogot3, ogot4);
            Assert.AreEqual(got.Obj, (ogot as DummyDataService)!.Obj);

            yield return null;

            SceneManager.UnloadSceneAsync(scene);

            yield return null;

            Assert.IsFalse(resolver.IsInScope());
        }

        [UnityTest]
        public IEnumerator Bind_Alias_Get_GameObjectService()
        {
            var obj = new GameObject();
            var service = new DummyDataService { Obj = new object() };
            Services.Bind(service, obj).As<object>();

            var resolver = Services.For(obj);

            yield return null;

            var got = resolver.Get<DummyDataService>();
            resolver.TryGet(out DummyDataService got2);
            var got3 = Services.For(obj).Get<DummyDataService>();
            Services.For(obj).TryGet(out DummyDataService got4);

            AssertSameAs(got, got2, got3, got4);
            yield return null;

            var ogot = resolver.Get<object>();
            resolver.TryGet(out object ogot2);
            var ogot3 = Services.For(obj).Get<object>();
            Services.For(obj).TryGet(out object ogot4);

            AssertSameAs(got, ogot, ogot2, ogot3, ogot4);
            Assert.AreEqual(got.Obj, (ogot as DummyDataService)!.Obj);
            Object.Destroy(obj);

            yield return null;

            Assert.IsFalse(resolver.IsInScope());
        }
    }
}