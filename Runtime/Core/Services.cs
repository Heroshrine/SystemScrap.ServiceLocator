using System.Runtime.CompilerServices;
using SystemScrap.ServiceLocator.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: InternalsVisibleTo("SystemScrap.ServiceLocator.Tests")]

namespace SystemScrap.ServiceLocator.Core
{
    /// <summary>
    /// Convenience API for registering and resolving services through a shared <see cref="ServiceLocator" /> instance.
    /// </summary>
    /// <remarks>
    /// <see cref="GetLocator" />, <see cref="For()" />, and <see cref="Bind{T}(T)" /> all operate on the same underlying
    /// locator instance.
    /// </remarks>
    public static class Services
    {
        private static ServiceLocator s_locator;


        /// <summary>
        /// Gets the underlying <see cref="ServiceLocator" /> instance used by this API.
        /// </summary>
        /// <remarks>
        /// The locator is initialized during Unity subsystem registration by the internal bootstrapper.
        /// </remarks>
        public static ServiceLocator GetLocator() => s_locator;


        // scoped resolvers
        /// <summary>
        /// Gets a resolver for the provided component's <see cref="GameObject" /> scope.
        /// </summary>
        public static IScopedResolver For(Component component, bool searchHierarchy = true) => s_locator.ForGameObject(component.gameObject, searchHierarchy);

        /// <summary>
        /// Gets a resolver for the provided <see cref="GameObject" /> scope.
        /// </summary>
        public static IScopedResolver For(GameObject gameObject, bool searchHierarchy = true) => s_locator.ForGameObject(gameObject, searchHierarchy);

        /// <summary>
        /// Gets a resolver for the provided <see cref="Scene" /> scope.
        /// </summary>
        public static IScopedResolver For(Scene scene) => s_locator.ForScene(scene);

        /// <summary>
        /// Gets a resolver for the global scope.
        /// </summary>
        public static IScopedResolver For() => s_locator.ForGlobal();

        // registration
        /// <summary>
        /// Registers a global instance.
        /// </summary>
        /// <remarks>
        /// Use the returned aliaser to register additional keys that should resolve to the same instance.
        /// </remarks>
        public static IServiceAliaser<T> Bind<T>(T instance) where T : class =>
            s_locator.RegisterGlobalInstance(instance);

        /// <summary>
        /// Registers an instance scoped to a specific <see cref="Scene" />.
        /// </summary>
        /// <remarks>
        /// Use the returned aliaser to register additional keys that should resolve to the same instance.
        /// </remarks>
        public static IServiceAliaser<T> Bind<T>(T instance, Scene scene) where T : class =>
            s_locator.RegisterSceneInstance(instance, scene);

        /// <summary>
        /// Registers an instance scoped to a specific <see cref="GameObject" />.
        /// </summary>
        /// <remarks>
        /// Use the returned aliaser to register additional keys that should resolve to the same instance.
        /// </remarks>
        public static IServiceAliaser<T> Bind<T>(T instance, GameObject gameObject) where T : class =>
            s_locator.RegisterGameObjectInstance(instance, gameObject);


        internal static void SetLocator(ServiceLocator locator)
        {
            s_locator = locator;
        }
    }
}