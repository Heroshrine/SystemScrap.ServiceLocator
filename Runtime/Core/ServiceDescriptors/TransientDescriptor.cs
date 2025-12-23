using System;
using SystemScrap.ServiceLocator.Framework;

namespace SystemScrap.ServiceLocator.Core.ServiceDescriptors
{
    /// <summary>
    /// A descriptor that creates a new instance each time it is resolved.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    public class TransientDescriptor<T> : IServiceDescriptor<T> where T : class
    {
        /// <inheritdoc />
        public T GetService() => _transientFactory();
        private readonly Func<T> _transientFactory;

        /// <summary>
        /// Creates a descriptor backed by the provided factory.
        /// </summary>
        /// <param name="transientFactory">Factory used to create a new instance for each call to <see cref="GetService" />.</param>
        public TransientDescriptor(Func<T> transientFactory) => _transientFactory = transientFactory;
    }
}
