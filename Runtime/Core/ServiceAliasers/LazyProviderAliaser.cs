using System;
using SystemScrap.ServiceLocator.Framework;

namespace SystemScrap.ServiceLocator.Core.ServiceAliasers
{
    /// <summary>
    /// Provides a fluent API for aliasing a lazy provider registration to additional base/interface types.
    /// </summary>
    /// <remarks>
    /// Returned by <see cref="ServiceLocator" /> when registering lazy providers to allow registering additional keys
    /// that resolve through the same underlying provider.
    /// </remarks>
    public class LazyProviderAliaser<T> : IServiceAliaser<T> where T : class
    {
        private readonly LazyProvider _provider;
        private readonly ServiceLocator _locator;
        
        internal LazyProviderAliaser(ServiceLocator locator, LazyProvider provider)
        {
            _provider = provider;
            _locator = locator;
        }

        /// <inheritdoc />
        public IServiceAliaser<T> As<TBase>() where TBase : class
        {
            if (!typeof(TBase).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException($"Cannot bind {typeof(T)} to {typeof(TBase)}");
            _locator.RegisterLazyProvider<TBase>(_provider);
            return this;
        }
    }
}