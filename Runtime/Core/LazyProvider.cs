using System;
using SystemScrap.ServiceLocator.Framework;

namespace SystemScrap.ServiceLocator.Core
{
    /// <summary>
    /// Describes a lazily-created service registration.
    /// </summary>
    /// <param name="Provider">Factory invoked to create the service instance.</param>
    /// <param name="OriginalType">
    /// The concrete type under which the created instance is cached (used to support aliasing).
    /// </param>
    /// <param name="OriginalScope">The scope the provider was registered for.</param>
    public sealed record LazyProvider(Func<object> Provider, Type OriginalType, Scope OriginalScope)
    {
        /// <summary>
        /// Gets the factory used to create the service instance.
        /// </summary>
        public Func<object> Provider { get; } = Provider;

        /// <summary>
        /// Gets the original (concrete) service type produced by <see cref="Provider" />.
        /// </summary>
        public Type OriginalType { get; } = OriginalType;

        /// <summary>
        /// Gets the scope the provider is registered under.
        /// </summary>
        public Scope OriginalScope { get; } = OriginalScope;
    }
}
