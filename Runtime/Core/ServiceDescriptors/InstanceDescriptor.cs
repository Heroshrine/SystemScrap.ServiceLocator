using SystemScrap.ServiceLocator.Framework;

namespace SystemScrap.ServiceLocator.Core.ServiceDescriptors
{
    /// <summary>
    /// A descriptor that always returns the same pre-constructed instance.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    public class InstanceDescriptor<T> : IServiceDescriptor<T> where T : class
    {
        private readonly T _instance;

        /// <inheritdoc />
        public T GetService() => _instance;

        /// <summary>
        /// Creates a descriptor for the provided instance.
        /// </summary>
        /// <param name="instance">The instance to return from <see cref="GetService" />.</param>
        public InstanceDescriptor(T instance) => _instance = instance;
    }
}
