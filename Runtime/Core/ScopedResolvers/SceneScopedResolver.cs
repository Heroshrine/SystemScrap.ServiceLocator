using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SystemScrap.ServiceLocator.Core.ServiceAliasers;
using SystemScrap.ServiceLocator.Framework;
using SystemScrap.ServiceLocator.Framework.LifetimeCallbacks;
using UnityEngine.SceneManagement;

namespace SystemScrap.ServiceLocator.Core.ScopedResolvers
{
    /// <summary>
    /// Resolves services from a specific scene scope.
    /// </summary>
    /// <remarks>
    /// Instances are created by <see cref="ServiceLocator.ForScene" /> (or <see cref="Services.For(UnityEngine.SceneManagement.Scene)" />).
    /// If a service is not registered in the scene scope, resolution falls back to the global scope.
    /// </remarks>
    public sealed class SceneScopedResolver : IScopedResolver
    {
        private readonly Scene _scene;
        private readonly ServiceLocator _locator;
        private readonly Dictionary<Type, LazyProvider> _providers;
        private readonly NewScope<Scene> _newScope;

        [CanBeNull] private Dictionary<Type, object> _services;


        internal SceneScopedResolver(ServiceLocator locator,
            Scene scene,
            [CanBeNull] Dictionary<Type, object> services,
            Dictionary<Type, LazyProvider> providers,
            NewScope<Scene> newScope)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Scene is not valid.", nameof(scene));

            if (!scene.isLoaded)
                throw new ArgumentException("Scene is not loaded.", nameof(scene));

            _scene = scene;
            _services = services;
            _providers = providers;
            _locator = locator;
            _newScope = newScope;
        }

        /// <inheritdoc />
        public T Get<T>() where T : class
        {
            if (!IsInScope())
                throw new InvalidOperationException("Scope is expired.");

            _services ??= _locator.Grab(_scene);

            var requestedType = typeof(T);
            if (_services is null || !_services.ContainsKey(requestedType))
            {
                if (!_providers.TryGetValue(requestedType, out var provider))
                    return _locator.ForGlobal().Get<T>();

                _services ??= _newScope(_scene)
                            ?? throw new InvalidOperationException("Failed to create service dictionary.");

                if (_services.TryGetValue(provider.OriginalType, out var v) && v is T t)
                    return t;

                var service = provider.Provider();
                requestedType = provider.OriginalType;
                _services!.Add(requestedType, service);

                if (service is IOnProviderCreated factoryCreated)
                    factoryCreated.OnProviderCreated();
            }

            var aCheck = _services[requestedType];
            if (aCheck is AliasTo aliaser)
                aCheck = aliaser.AliasTarget;
            var result = (T)aCheck;

            if (result is IOnResolved resolved)
                resolved.OnResolved();

            return result;
        }

        /// <inheritdoc />
        public bool TryGet<T>(out T found) where T : class
        {
            if (!IsInScope())
            {
                found = null;
                return false;
            }

            _services ??= _locator.Grab(_scene);

            var requestedType = typeof(T);
            if (_services is null || !_services.TryGetValue(requestedType, out var f))
            {
                if (!_providers.TryGetValue(requestedType, out var provider))
                    return _locator.ForGlobal().TryGet(out found);

                _services ??= _newScope(_scene)
                            ?? throw new InvalidOperationException("Failed to create service dictionary.");

                if (_services.TryGetValue(provider.OriginalType, out var v) && v is T t)
                {
                    found = t;
                    return true;
                }

                f = provider.Provider();
                requestedType = provider.OriginalType;
                _services!.Add(requestedType, f);

                if (f is IOnProviderCreated factoryCreated)
                    factoryCreated.OnProviderCreated();
            }

            if (f is AliasTo aliaser)
                f = aliaser.AliasTarget;
            found = f as T;

            if (found is IOnResolved resolved)
                resolved.OnResolved();

            return found != null;
        }

        /// <inheritdoc />
        public bool IsInScope() => _scene.isLoaded;
    }
}