using NUnit.Framework;
using SystemScrap.ServiceLocator.Core;

namespace SystemScrap.ServiceLocator.Tests
{
    public static class TestHelper
    {
        public static void Reset()
        {
            SceneServiceDisposer.Clear();
            GameObjectServiceDisposer.Clear();
            Services.SetLocator(new Core.ServiceLocator());
        }

        public static void AssertSameAs(object comparing, params object[] others)
        {
            foreach (var other in others)
                Assert.AreSame(comparing, other);
        }
    }
}