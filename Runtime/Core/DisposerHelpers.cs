using System;
using System.Collections.Generic;
using SystemScrap.ServiceLocator.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SystemScrap.ServiceLocator.Core
{
    /// <summary>
    /// Tracks scene-scoped registration handles and disposes them when scenes unload.
    /// </summary>
    /// <remarks>
    /// This type is used by <see cref="ServiceLocator" /> to ensure scene-scoped services are cleaned up when a
    /// <see cref="Scene" /> ends, which in turn drives <see cref="ServiceLocator.Framework.IScopedResolver.OnScopeExit" /> callbacks.
    /// </remarks>
    public static class SceneServiceDisposer
    {
        /// <summary>
        /// Raised after a scene's scope has been disposed.
        /// </summary>
        public static event Action<Scene> OnSceneDisposed;

        private static readonly Dictionary<Scene, RegistrationHandle> s_Handles = new();


        /// <summary>
        /// Attempts to retrieve the registered handle for a scene scope.
        /// </summary>
        public static bool TryGetHandle(Scene scene, out RegistrationHandle handle) =>
            s_Handles.TryGetValue(scene, out handle);

        static SceneServiceDisposer()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        internal static void RegisterHandle(Scene scene, RegistrationHandle handle) => s_Handles.Add(scene, handle);

        private static void OnSceneUnloaded(Scene scene)
        {
            if (!s_Handles.Remove(scene, out var handle))
                return;

            handle.Dispose();
            OnSceneDisposed?.Invoke(scene);
        }

        /// <summary>
        /// Very unsafe to call, so don't
        /// </summary>
        internal static void Clear() => s_Handles.Clear();
    }

    /// <summary>
    /// Tracks GameObject-scoped registration handles and disposes them when GameObjects are destroyed.
    /// </summary>
    /// <remarks>
    /// <see cref="ServiceLocator" /> attaches this component to GameObjects that own scoped services so their
    /// registrations are automatically cleaned up on destroy, which in turn drives
    /// <see cref="ServiceLocator.Framework.IScopedResolver.OnScopeExit" /> callbacks.
    /// </remarks>
    public sealed class GameObjectServiceDisposer : MonoBehaviour
    {
        /// <summary>
        /// Raised after a GameObject's scope has been disposed.
        /// </summary>
        public static event Action<GameObject> OnGameObjectDisposed;

        private static readonly Dictionary<GameObject, RegistrationHandle> s_Handles = new();


        /// <summary>
        /// Attempts to retrieve the registered handle for a GameObject scope.
        /// </summary>
        public static bool TryGetHandle(GameObject gameObject, out RegistrationHandle handle) =>
            s_Handles.TryGetValue(gameObject, out handle);

        internal static void RegisterHandle(GameObject gameObject, RegistrationHandle handle)
        {
            s_Handles.Add(gameObject, handle);
            gameObject.AddComponent<GameObjectServiceDisposer>()._handle = handle;
        }

        private RegistrationHandle _handle;

        private void OnDestroy()
        {
            _handle.Dispose();
            s_Handles.Remove(gameObject);
            OnGameObjectDisposed?.Invoke(gameObject);
        }

        /// <summary>
        /// Very unsafe to call, so don't
        /// </summary>
        internal static void Clear() => s_Handles.Clear();
    }
}