using System;
using SystemScrap.ServiceLocator.Framework;

namespace SystemScrap.ServiceLocator.Core.ServiceAliasers
{
    /// <summary>
    /// Provides a fluent API for aliasing an instance registration to additional base/interface types.
    /// </summary>
    /// <remarks>
    /// Returned by instance registration methods on <see cref="ServiceLocator" /> and <see cref="Services" />.
    /// </remarks>
    public class InstanceAliaser<T> : IServiceAliaser<T> where T : class
    {
        private readonly Action<Type> _registerAlias;

        internal InstanceAliaser(Action<Type> registerAlias)
        {
            _registerAlias = registerAlias;
        }

        /// <inheritdoc />
        public IServiceAliaser<T> As<TBase>() where TBase : class
        {
            if (!typeof(TBase).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException($"Cannot bind {typeof(T)} to {typeof(TBase)}");
            _registerAlias(typeof(TBase));
            return this;
        }
    }
}
