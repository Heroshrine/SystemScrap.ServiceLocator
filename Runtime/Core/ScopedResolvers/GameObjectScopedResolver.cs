using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SystemScrap.ServiceLocator.Core.ServiceAliasers;
using SystemScrap.ServiceLocator.Framework;
using SystemScrap.ServiceLocator.Framework.LifetimeCallbacks;
using UnityEngine;

namespace SystemScrap.ServiceLocator.Core.ScopedResolvers
{
    /// <summary>
    /// Resolves services from a specific GameObject scope.
    /// </summary>
    /// <remarks>
    /// Instances are created by <see cref="ServiceLocator.ForGameObject" /> (or <see cref="Services.For(UnityEngine.GameObject, bool)" />).
    /// If a service is not registered in the GameObject scope, resolution falls back to the scene scope
    /// (and then to global, via the scene resolver).
    /// </remarks>
    public sealed class GameObjectScopedResolver : IScopedResolver
    {
        private readonly GameObject _gameObject;
        private readonly ServiceLocator _locator;
        private readonly Dictionary<Type, LazyProvider> _providers;
        private readonly NewScope<GameObject> _newScope;
        private readonly bool _searchHierarchy;

        [CanBeNull] private Dictionary<Type, object> _services;


        internal GameObjectScopedResolver(ServiceLocator locator,
            GameObject gameObject,
            bool searchHierarchy,
            [CanBeNull] Dictionary<Type, object> services,
            Dictionary<Type, LazyProvider> providers,
            NewScope<GameObject> newScope)
        {
            if (!gameObject)
                throw new ArgumentNullException(nameof(gameObject));
            _gameObject = gameObject;

            _services = services;
            _services = services;
            _providers = providers;
            _locator = locator;
            _newScope = newScope;
            _searchHierarchy = searchHierarchy;
        }

        /// <inheritdoc />
        public T Get<T>() where T : class
        {
            // check if we're in scope
            if (!IsInScope())
                throw new InvalidOperationException("Scope is expired.");
            var requestedType = typeof(T);

            // if services is null, try to grab it from the locator.
            // don't create a new scope here to avoid garbage scopes
            _services ??= _locator.Grab(_gameObject);

            // check if services contains the key
            if (_services is null || !_services.ContainsKey(requestedType))
            {
                // gameobjects only, walk up the hierarchy
                if (_searchHierarchy)
                {
                    var grabbing = _gameObject.transform.parent;
                    while (grabbing)
                    {
                        var parentServices = _locator.Grab(grabbing.gameObject);
                        if (parentServices is not null && parentServices.TryGetValue(requestedType, out var grabbed))
                        {
                            if (grabbed is AliasTo a)
                                grabbed = a.AliasTarget;

                            if (grabbed is IOnResolved r)
                                r.OnResolved();

                            return (T)grabbed;
                        }

                        grabbing = grabbing.parent;
                    }
                }

                // check if we have the type in the providers
                if (!_providers.TryGetValue(requestedType, out var provider))
                    return _locator.ForScene(_gameObject.scene).Get<T>();

                // we do this here to avoid polluting disposer helpers with garbage values
                // (and in this case, avoid creating components)
                _services ??= _newScope(_gameObject)
                              ?? throw new InvalidOperationException("Failed to create service dictionary.");

                // check if the provider's original type was in the service (this allows for provider aliasing)
                if (_services.TryGetValue(provider.OriginalType, out var v) && v is T t)
                    return t;

                // use the provider if we've gotten this far
                var service = provider.Provider();
                requestedType = provider.OriginalType;
                _services!.Add(requestedType, service); // only ever add by original type

                if (service is IOnProviderCreated factoryCreated)
                    factoryCreated.OnProviderCreated();
            }

            // services should contain valid key here

            var aCheck = _services[requestedType];
            if (aCheck is AliasTo aliaser) // if aliaser, use the aliased target
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

            _services ??= _locator.Grab(_gameObject);

            var requestedType = typeof(T);
            if (_services is null || !_services.TryGetValue(requestedType, out var f))
            {
                if (_searchHierarchy)
                {
                    var grabbing = _gameObject.transform.parent;
                    while (grabbing)
                    {
                        var parentServices = _locator.Grab(grabbing.gameObject);
                        if (parentServices is not null && parentServices.TryGetValue(requestedType, out var grabbed))
                        {
                            if (grabbed is AliasTo a)
                                grabbed = a.AliasTarget;
                            found = grabbed as T;

                            if (grabbed is IOnResolved r)
                                r.OnResolved();

                            return true;
                        }

                        grabbing = grabbing.parent;
                    }
                }

                if (!_providers.TryGetValue(requestedType, out var provider))
                    return _locator.ForScene(_gameObject.scene).TryGet(out found);

                _services ??= _newScope(_gameObject)
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
        public bool IsInScope() => _gameObject;
    }
}