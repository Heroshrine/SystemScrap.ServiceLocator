using System;
using System.Collections.Generic;
using SystemScrap.ServiceLocator.Framework;

namespace SystemScrap.ServiceLocator.Core.ScopedResolvers
{
    /// <summary>
    /// Resolves a managed service registration and exposes its <see cref="ServiceRegistrationToken" />.
    /// </summary>
    /// <remarks>
    /// Instances are created by <see cref="ServiceLocator.ForManaged{T}" />. The resolver remains in scope while the
    /// corresponding registration handle is valid.
    /// </remarks>
    public class ManagedScopeResolver<TService> where TService : class
    {
        private readonly ManagedScopeResolverImplementation _resolver;

        /// <summary>
        /// Gets the token associated with the managed registration.
        /// </summary>
        public ServiceRegistrationToken Token { get; }


        internal ManagedScopeResolver(Dictionary<Type, (object, RegistrationHandle)> services,
            ServiceRegistrationToken token)
        {
            Token = token;
            _resolver = new ManagedScopeResolverImplementation(services);
        }

        /// <summary>
        /// Returns whether the managed registration is still present.
        /// </summary>
        public bool IsInScope() => _resolver.IsInScope();

        /// <summary>
        /// Attempts to resolve the service and returns the registration token alongside it.
        /// </summary>
        /// <param name="found">When successful, the resolved service instance.</param>
        /// <param name="registrationToken">When successful, the token associated with the registration.</param>
        /// <returns><c>true</c> if the service is in scope and available; otherwise <c>false</c>.</returns>
        public bool TryGet(out TService found, out ServiceRegistrationToken registrationToken)
        {
            found = null;
            registrationToken = null;
            if (!_resolver.TryGet(out found)) return false;
            registrationToken = Token;
            return true;
        }

        /// <summary>
        /// Resolves the service and returns the registration token alongside it.
        /// </summary>
        /// <param name="registrationToken">The token associated with the registration.</param>
        /// <returns>The resolved service instance.</returns>
        public TService Get(out ServiceRegistrationToken registrationToken)
        {
            var result = _resolver.Get<TService>();
            registrationToken = Token;
            return result;
        }

        /// <summary>
        /// Implementation detail that provides scope checks and dictionary access for managed registrations.
        /// </summary>
        private class ManagedScopeResolverImplementation : IScopedResolver
        {
            private readonly Dictionary<Type, (object instance, RegistrationHandle resolver)> _services;


            /// <summary>
            /// Creates a resolver over the managed services dictionary.
            /// </summary>
            public ManagedScopeResolverImplementation(Dictionary<Type, (object, RegistrationHandle)> services)
            {
                _services = services;
            }

            /// <summary>
            /// Gets the managed service instance, cast to <typeparamref name="T" />.
            /// </summary>
            public T Get<T>() where T : class
            {
                if (!IsInScope())
                    throw new InvalidOperationException("Scope is expired.");

                return (T)_services[typeof(TService)].instance;
            }

            /// <summary>
            /// Attempts to get the managed service instance, cast to <typeparamref name="T" />.
            /// </summary>
            public bool TryGet<T>(out T found) where T : class
            {
                found = null;
                if (!IsInScope()) return false;
                found = _services[typeof(TService)].instance as T;
                return found != null;
            }

            /// <summary>
            /// Returns whether the managed service is still registered.
            /// </summary>
            public bool IsInScope() => _services.ContainsKey(typeof(TService));
        }
    }
}
