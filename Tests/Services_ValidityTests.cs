using System;
using System.Collections;
using NUnit.Framework;
using SystemScrap.ServiceLocator.Core;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using static SystemScrap.ServiceLocator.Tests.TestHelper;

namespace SystemScrap.ServiceLocator.Tests
{
    public class Services_ValidityTests
    {
        [UnityTest]
        public IEnumerator ThrowOnGetMissing()
        {
            Reset();

            var globalResolver = Services.For();
            var sceneResolver = Services.For(SceneManager.GetActiveScene());
            var objResolver = Services.For(new GameObject());

            yield return null;

            Assert.Throws<InvalidOperationException>(() => globalResolver.Get<DummyDataService>());
            Assert.Throws<InvalidOperationException>(() => sceneResolver.Get<DummyDataService>());
            Assert.Throws<InvalidOperationException>(() => objResolver.Get<DummyDataService>());
        }

        [UnityTest]
        public IEnumerator NoTrashScopes()
        {
            Reset();

            var scene = SceneManager.GetActiveScene();
            var obj = new GameObject();
            Services.For(scene);
            Services.For(obj);

            yield return null;

            Assert.IsFalse(SceneServiceDisposer.TryGetHandle(scene, out _));
            Assert.IsFalse(GameObjectServiceDisposer.TryGetHandle(obj, out _));
        }

        [Test]
        public void ResolversAreUpdated()
        {
            Reset();

            var scene = SceneManager.GetActiveScene();
            var obj = new GameObject();

            var globalResolver = Services.For();
            var sceneResolver = Services.For(scene);
            var objResolver = Services.For(obj);

            Assert.Throws<InvalidOperationException>(() => globalResolver.Get<DummyDataService>());
            Assert.Throws<InvalidOperationException>(() => sceneResolver.Get<DummyDataService>());
            Assert.Throws<InvalidOperationException>(() => objResolver.Get<DummyDataService>());

            Services.Bind(new DummyDataService { Obj = new object() });

            Assert.DoesNotThrow(() => globalResolver.Get<DummyDataService>());
            Assert.DoesNotThrow(() => sceneResolver.Get<DummyDataService>());
            Assert.DoesNotThrow(() => objResolver.Get<DummyDataService>());

            Services.Bind(new DummyDataService { Obj = new object() }, scene);

            var globalService = globalResolver.Get<DummyDataService>();
            var sceneService = sceneResolver.Get<DummyDataService>();
            var objService = objResolver.Get<DummyDataService>();

            Assert.AreNotSame(globalService, sceneService);
            Assert.AreNotSame(globalService, objService);

            Services.Bind(new DummyDataService { Obj = new object() }, obj);

            globalService = globalResolver.Get<DummyDataService>();
            sceneService = sceneResolver.Get<DummyDataService>();
            objService = objResolver.Get<DummyDataService>();

            Assert.AreNotSame(globalService, sceneService);
            Assert.AreNotSame(globalService, objService);
            Assert.AreNotSame(sceneService, objService);
        }

        [UnityTest]
        public IEnumerator ScopeIsGated()
        {
            Reset();

            var currentScene = SceneManager.GetActiveScene();
            var otherScene = SceneManager.CreateScene("OtherScene");

            yield return null;

            Services.Bind(new DummyDataService { Obj = new object() }).As<object>();
            var currentResolver = Services.For(currentScene);
            var otherResolver = Services.For(otherScene);

            Assert.IsTrue(currentResolver.TryGet<DummyDataService>(out var dummy1));
            Assert.IsTrue(currentResolver.TryGet<object>(out var dummy3));
            Assert.IsTrue(otherResolver.TryGet<DummyDataService>(out var dummy2));
            Assert.IsTrue(otherResolver.TryGet<object>(out var dummy4));
            AssertSameAs(dummy1, dummy2, dummy3, dummy4);

            Services.Bind(new DummyDataService { Obj = new object() }, currentScene).As<object>();
            Services.Bind(new DummyDataService { Obj = new object() }, otherScene).As<object>();

            Assert.IsTrue(currentResolver.TryGet(out dummy1));
            Assert.IsTrue(currentResolver.TryGet(out dummy3));
            Assert.IsTrue(otherResolver.TryGet(out dummy2));
            Assert.IsTrue(otherResolver.TryGet(out dummy4));
            Assert.AreNotSame(dummy1, dummy2);
            Assert.AreNotSame(dummy1, dummy4);
            AssertSameAs(dummy1, dummy3);

            var obj = new GameObject();
            var otherObj = new GameObject();
            SceneManager.MoveGameObjectToScene(otherObj, otherScene);

            yield return null;

            var objectResolver = Services.For(obj);
            var otherObjectResolver = Services.For(otherObj);

            Assert.IsTrue(objectResolver.TryGet(out dummy1));
            Assert.IsTrue(objectResolver.TryGet(out dummy3));
            Assert.IsTrue(otherObjectResolver.TryGet(out dummy2));
            Assert.IsTrue(otherObjectResolver.TryGet(out dummy4));
            Assert.AreNotSame(dummy1, dummy2);
            Assert.AreNotSame(dummy1, dummy4);
            AssertSameAs(dummy1, dummy3);

            Services.Bind(new DummyDataService { Obj = new object() }, obj).As<object>();
            Services.Bind(new DummyDataService { Obj = new object() }, otherObj).As<object>();

            Assert.IsTrue(objectResolver.TryGet(out dummy1));
            Assert.IsTrue(objectResolver.TryGet(out dummy3));
            Assert.IsTrue(otherObjectResolver.TryGet(out dummy2));
            Assert.IsTrue(otherObjectResolver.TryGet(out dummy4));
            Assert.AreNotSame(dummy1, dummy2);
            Assert.AreNotSame(dummy1, dummy4);
            AssertSameAs(dummy1, dummy3);
        }
    }
}