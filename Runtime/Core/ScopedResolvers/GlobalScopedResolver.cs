using System;
using System.Collections.Generic;
using SystemScrap.ServiceLocator.Core.ServiceAliasers;
using SystemScrap.ServiceLocator.Core.ServiceDescriptors;
using SystemScrap.ServiceLocator.Framework;
using SystemScrap.ServiceLocator.Framework.LifetimeCallbacks;

namespace SystemScrap.ServiceLocator.Core.ScopedResolvers
{
    /// <summary>
    /// Resolves services from the global scope.
    /// </summary>
    /// <remarks>
    /// Instances are created by <see cref="ServiceLocator.ForGlobal" /> (or <see cref="Services.For()" />).
    /// Lazy providers registered for the global scope are invoked on first resolution and then cached globally.
    /// </remarks>
    public class GlobalScopedResolver : IScopedResolver
    {
        private readonly Dictionary<Type, IServiceDescriptor<object>> _services;
        private readonly Dictionary<Type, LazyProvider> _providers;


        internal GlobalScopedResolver(Dictionary<Type, IServiceDescriptor<object>> services,
            Dictionary<Type, LazyProvider> providers)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _providers = providers;
        }

        /// <inheritdoc />
        public T Get<T>() where T : class
        {
            if (!IsInScope())
                throw new InvalidOperationException("Scope is expired.");

            var requestedType = typeof(T);
            if (!_services.ContainsKey(requestedType))
            {
                if (!_providers.TryGetValue(requestedType, out var provider))
                    throw new InvalidOperationException($"Service of type {requestedType} is not registered.");

                if (_services.TryGetValue(provider.OriginalType, out var vd) && vd.GetService() is T t)
                    return t;

                var service = provider.Provider();
                requestedType = provider.OriginalType;
                _services.Add(requestedType, new InstanceDescriptor<object>(service));

                if (service is IOnProviderCreated factoryCreated)
                    factoryCreated.OnProviderCreated();
            }

            var aCheck = _services[requestedType].GetService();
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

            found = null;
            var requestedType = typeof(T);
            if (!_services.TryGetValue(requestedType, out var f))
            {
                if (!_providers.TryGetValue(requestedType, out var provider))
                    return false;

                if (_services.TryGetValue(provider.OriginalType, out var vd) && vd.GetService() is T t)
                {
                    found = t;
                    return true;
                }

                var service = provider.Provider();
                requestedType = provider.OriginalType;
                _services.Add(requestedType, f = new InstanceDescriptor<object>(service));

                if (service is IOnProviderCreated factoryCreated)
                    factoryCreated.OnProviderCreated();
            }

            var aCheck = f.GetService();
            if (aCheck is AliasTo aliaser)
                aCheck = aliaser.AliasTarget;
            found = aCheck as T;

            if (found is IOnResolved resolved)
                resolved.OnResolved();

            return found != null;
        }

        /// <inheritdoc />
        public bool IsInScope() => true;
    }
}