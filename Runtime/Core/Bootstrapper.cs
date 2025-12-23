using UnityEngine;

namespace SystemScrap.ServiceLocator.Core
{
    internal static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            SceneServiceDisposer.Clear();
            GameObjectServiceDisposer.Clear();
            Services.SetLocator(new ServiceLocator());
        }
    }
}