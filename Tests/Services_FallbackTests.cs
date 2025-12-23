using NUnit.Framework;
using SystemScrap.ServiceLocator.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SystemScrap.ServiceLocator.Tests.TestHelper;

namespace SystemScrap.ServiceLocator.Tests
{
    public class Services_FallbackTests
    {
        [Test]
        public void SceneFallback_ToGlobal()
        {
            Reset();

            var scene = SceneManager.GetActiveScene();
            Services.Bind(new DummyDataService { Obj = new object() }).As<object>();

            var pass = Services.For(scene).TryGet<DummyDataService>(out _);
            var pass2 = Services.For(scene).TryGet<object>(out _);

            Assert.DoesNotThrow(() => Services.For(scene).Get<DummyDataService>());
            Assert.DoesNotThrow(() => Services.For(scene).Get<object>());
            Assert.IsTrue(pass);
            Assert.IsTrue(pass2);
        }

        [Test]
        public void ObjectFallback_ToGlobal()
        {
            Reset();

            var obj = new GameObject();
            Services.Bind(new DummyDataService { Obj = new object() }).As<object>();

            var pass = Services.For(obj).TryGet<DummyDataService>(out _);
            var pass2 = Services.For(obj).TryGet<object>(out _);

            Assert.DoesNotThrow(() => Services.For(obj).Get<DummyDataService>());
            Assert.DoesNotThrow(() => Services.For(obj).Get<object>());
            Assert.IsTrue(pass);
            Assert.IsTrue(pass2);
        }

        [Test]
        public void ObjectFallback_ToScene()
        {
            Reset();

            var obj = new GameObject();
            var scene = SceneManager.GetActiveScene();
            Services.Bind(new DummyDataService { Obj = new object() }, scene).As<object>();

            var pass = Services.For(obj).TryGet<DummyDataService>(out _);
            var pass2 = Services.For(obj).TryGet<object>(out _);

            Assert.DoesNotThrow(() => Services.For(obj).Get<DummyDataService>());
            Assert.DoesNotThrow(() => Services.For(obj).Get<object>());
            Assert.IsTrue(pass);
            Assert.IsTrue(pass2);
        }
    }
}