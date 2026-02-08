using System;
using System.Collections.Generic;
using SystemScrap.ServiceLocator.Core.ScopedResolvers;
using SystemScrap.ServiceLocator.Core.ServiceAliasers;
using SystemScrap.ServiceLocator.Core.ServiceDescriptors;
using SystemScrap.ServiceLocator.Framework;
using SystemScrap.ServiceLocator.Framework.LifetimeCallbacks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SystemScrap.ServiceLocator.Core
{
    // TODO: it may be necessary to gate aliasers based on their scope 
    /// <summary>
    /// Registers services and creates resolvers across global, scene, GameObject, and managed scopes.
    /// </summary>
    /// <remarks>
    /// Most consumer code uses the static <see cref="Services" /> API, which delegates to a shared instance of this
    /// class.
    /// </remarks>
    public class ServiceLocator
    {
        private readonly Dictionary<Type, (object instance, RegistrationHandle handle)> _managedServices = new();
        private readonly Dictionary<Type, IServiceDescriptor<object>> _globalServices = new();
        private readonly Dictionary<Scene, Dictionary<Type, object>> _sceneServices = new();
        private readonly Dictionary<GameObject, Dictionary<Type, object>> _gameObjectServices = new();

        private readonly Dictionary<Scope, Dictionary<Type, LazyProvider>> _lazyProviders = new()
        {
            { Scope.Global, new Dictionary<Type, LazyProvider>() },
            { Scope.Scene, new Dictionary<Type, LazyProvider>() },
            { Scope.GameObject, new Dictionary<Type, LazyProvider>() }
        };

        internal LazyProviderAliaser<T> RegisterLazyProvider<T>(LazyProvider provider)
            where T : class
        {
            if (!_lazyProviders[provider.OriginalScope].TryAdd(typeof(T), provider))
                throw new InvalidOperationException(
                    $"Factory for type {typeof(T)} already registered in scope {provider.OriginalScope}.");
            return new LazyProviderAliaser<T>(this, provider);
        }

        // alias
        internal void RegisterAlias(Type type, AliasTo aliaser, Scope scope, object scopeObject = null)
        {
            switch (scope)
            {
                case Scope.GameObject:
                    _gameObjectServices[(GameObject)scopeObject!].Add(type, aliaser);
                    break;
                case Scope.Scene:
                    _sceneServices[(Scene)scopeObject!].Add(type, aliaser);
                    break;
                case Scope.Global:
                    _globalServices.Add(type, new InstanceDescriptor<object>(aliaser));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Invalid scope");
            }
        }

        // factory registration, for ease of use
        /// <summary>
        /// Registers a lazy factory for a service type within a given scope.
        /// </summary>
        /// <remarks>
        /// The factory is invoked the first time the service is resolved from the corresponding scoped resolver
        /// (see <see cref="ForGlobal" />, <see cref="ForScene" />, and <see cref="ForGameObject" />).
        /// <para />
        /// If the created service implements <see cref="IOnProviderCreated" />, it is notified once after creation.
        /// If it implements <see cref="IOnResolved" />, it is notified each time it is successfully resolved.
        /// </remarks>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="provider">Factory used to create the service.</param>
        /// <param name="scope">The scope the factory is registered under.</param>
        /// <returns>
        /// An aliaser that can be used to register additional keys (via <see cref="IServiceAliaser{T}.As{TBase}" />)
        /// that should resolve to the same provider.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a factory for <typeparamref name="T" /> is already registered in the given scope.
        /// </exception>
        public LazyProviderAliaser<T> RegisterLazyScopedProvider<T>(Func<T> provider, Scope scope) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            var lazyProvider = new LazyProvider(provider, typeof(T), scope);
            return RegisterLazyProvider<T>(lazyProvider);
        }

        // instance registration, traditional use
        /// <summary>
        /// Registers a global instance that can be resolved from any scope.
        /// </summary>
        /// <remarks>
        /// If <paramref name="instance" /> is a <see cref="MonoBehaviour" />, its GameObject is marked as
        /// <see cref="UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object)" />.
        /// </remarks>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>
        /// An aliaser that can be used to register additional keys (via <see cref="IServiceAliaser{T}.As{TBase}" />)
        /// that should resolve to the same instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a global instance for <typeparamref name="T" /> is already registered.
        /// </exception>
        public IServiceAliaser<T> RegisterGlobalInstance<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!_globalServices.TryAdd(typeof(T), new InstanceDescriptor<object>(instance)))
                throw new InvalidOperationException($"Instance for type {typeof(T)} already registered globally.");

            if (instance is MonoBehaviour mb)
                Object.DontDestroyOnLoad(mb.gameObject);
            return new InstanceAliaser<T>(type => RegisterAlias(type, new AliasTo(instance), Scope.Global));
        }

        /// <summary>
        /// Registers an instance scoped to a specific <see cref="Scene" />.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <param name="scene">The scene whose scope the instance is tied to.</param>
        /// <returns>
        /// An aliaser that can be used to register additional keys (via <see cref="IServiceAliaser{T}.As{TBase}" />)
        /// that should resolve to the same instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an instance for <typeparamref name="T" /> is already registered in <paramref name="scene" />.
        /// </exception>
        public IServiceAliaser<T> RegisterSceneInstance<T>(T instance, Scene scene) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!_sceneServices.ContainsKey(scene))
                NewSceneScope(scene);

            if (!_sceneServices[scene].TryAdd(typeof(T), instance))
                throw new InvalidOperationException(
                    $"Instance for type {typeof(T)} already registered in scene {scene}.");

            return new InstanceAliaser<T>(type => RegisterAlias(type, new AliasTo(instance), Scope.Scene, scene));
        }

        /// <summary>
        /// Registers an instance scoped to a specific <see cref="GameObject" />.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <param name="gameObject">The GameObject whose scope the instance is tied to.</param>
        /// <returns>
        /// An aliaser that can be used to register additional keys (via <see cref="IServiceAliaser{T}.As{TBase}" />)
        /// that should resolve to the same instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an instance for <typeparamref name="T" /> is already registered for <paramref name="gameObject" />.
        /// </exception>
        public IServiceAliaser<T> RegisterGameObjectInstance<T>(T instance, GameObject gameObject) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!_gameObjectServices.ContainsKey(gameObject))
                NewGameObjectScope(gameObject);

            if (!_gameObjectServices[gameObject].TryAdd(typeof(T), instance))
                throw new InvalidOperationException(
                    $"Instance for type {typeof(T)} already registered for gameobject {gameObject}.");

            return new InstanceAliaser<T>(type =>
                RegisterAlias(type, new AliasTo(instance), Scope.GameObject, gameObject));
        }

        // special registrations, rare use
        /// <summary>
        /// Registers a global transient service.
        /// </summary>
        /// <remarks>
        /// Each resolution results in a new instance produced by <paramref name="factory" />.
        /// </remarks>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="factory">Factory used to create a new instance each time.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a service for <typeparamref name="T" /> is already registered globally.
        /// </exception>
        public void RegisterTransientService<T>(Func<T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (!_globalServices.TryAdd(typeof(T), new TransientDescriptor<object>(factory)))
                throw new InvalidOperationException($"Service of type {typeof(T)} already registered globally.");
        }

        /// <summary>
        /// Registers an instance in the managed scope and returns a handle that can be disposed to unregister it.
        /// </summary>
        /// <remarks>
        /// Managed services are resolved via <see cref="ForManaged{T}" />. Disposing the returned handle removes the
        /// service and performs scope cleanup (see <see cref="IOnScopeEnd" /> and <see cref="IDisposable" />).
        /// </remarks>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="instance">The instance to register.</param>
        /// <returns>A handle that can be disposed to unregister the service.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an instance for <typeparamref name="T" /> is already registered in the managed scope.
        /// </exception>
        public RegistrationHandle RegisterManagedService<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var registrationHandle = new RegistrationHandle(() =>
            {
                _managedServices.Remove(typeof(T), out var service);
                CleanupService(service.instance);
            });
            if (!_managedServices.TryAdd(typeof(T), (instance, registrationHandle)))
                throw new InvalidOperationException(
                    $"Instance for type {typeof(T)} already registered in managed services.");

            return registrationHandle;
        }

        // scoped resolvers
        /// <summary>
        /// Creates a resolver for the global scope.
        /// </summary>
        public IScopedResolver ForGlobal() =>
            new GlobalScopedResolver(_globalServices, _lazyProviders[Scope.Global]);

        /// <summary>
        /// Creates a resolver for a specific scene scope.
        /// </summary>
        /// <param name="scene">The loaded scene to resolve services from.</param>
        /// <returns>A scene-scoped resolver.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="scene" /> is invalid or not loaded.</exception>
        public IScopedResolver ForScene(Scene scene)
        {
            _sceneServices.TryGetValue(scene, out var services);
            return new SceneScopedResolver(this, scene, services, _lazyProviders[Scope.Scene], NewSceneScope);
        }

        /// <summary>
        /// Creates a resolver for a specific GameObject scope.
        /// </summary>
        /// <param name="gameObject">The GameObject to resolve services from.</param>
        /// <param name="searchHierarchy">If true, search the hierarchy for services.</param>
        /// <returns>A GameObject-scoped resolver.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="gameObject" /> is <c>null</c>.</exception>
        public IScopedResolver ForGameObject(GameObject gameObject, bool searchHierarchy)
        {
            _gameObjectServices.TryGetValue(gameObject, out var services);
            return new GameObjectScopedResolver(this, gameObject, searchHierarchy, services,
                _lazyProviders[Scope.GameObject],
                NewGameObjectScope);
        }

        /// <summary>
        /// Creates a resolver for a managed service registration.
        /// </summary>
        /// <typeparam name="T">The managed service type to resolve.</typeparam>
        /// <returns>A managed resolver that can return the service along with its registration token.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no managed service of type <typeparamref name="T" /> is registered.</exception>
        public ManagedScopeResolver<T> ForManaged<T>() where T : class
        {
            if (!_managedServices.TryGetValue(typeof(T), out var service))
                throw new InvalidOperationException($"No service of type {typeof(T)} registered.");
            return new ManagedScopeResolver<T>(_managedServices, service.handle.GetToken());
        }

        // helper methods
        private Dictionary<Type, object> NewGameObjectScope(GameObject obj)
        {
            _gameObjectServices[obj] = new Dictionary<Type, object>();
            var handle = new RegistrationHandle(() =>
            {
                _gameObjectServices.Remove(obj, out var services);
                CleanupServices(services);
            });
            GameObjectServiceDisposer.RegisterHandle(obj, handle);
            return _gameObjectServices[obj];
        }

        private Dictionary<Type, object> NewSceneScope(Scene scene)
        {
            _sceneServices[scene] = new Dictionary<Type, object>();
            var handle = new RegistrationHandle(() =>
            {
                _sceneServices.Remove(scene, out var services);
                CleanupServices(services);
            });
            SceneServiceDisposer.RegisterHandle(scene, handle);
            return _sceneServices[scene];
        }

        internal Dictionary<Type, object> Grab(Scene scene)
        {
            _sceneServices.TryGetValue(scene, out var services);
            return services;
        }

        internal Dictionary<Type, object> Grab(GameObject obj)
        {
            _gameObjectServices.TryGetValue(obj, out var services);
            return services;
        }

        private static void CleanupServices(Dictionary<Type, object> services)
        {
            foreach (var service in services.Values)
                CleanupService(service);
        }

        private static void CleanupService(object service)
        {
            if (service is IOnScopeEnd scopeEnd)
                scopeEnd.OnScopeEnded();
            if (service is IDisposable disposable)
                disposable.Dispose();
        }
    }
}