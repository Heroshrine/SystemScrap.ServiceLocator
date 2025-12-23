namespace SystemScrap.ServiceLocator.Framework
{
    /// <summary>
    /// Provides a fluent API for registering additional alias types for a service registration.
    /// </summary>
    /// <typeparam name="T">The concrete registered service type.</typeparam>
    public interface IServiceAliaser<T> where T : class
    {
        /// <summary>
        /// Registers <typeparamref name="TBase" /> as an additional key that resolves to the same service as
        /// <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="TBase">A base class or interface implemented by <typeparamref name="T" />.</typeparam>
        /// <returns>This aliaser instance, for chaining.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when <typeparamref name="T" /> is not assignable to <typeparamref name="TBase" />.
        /// </exception>
        IServiceAliaser<T> As<TBase>() where TBase : class;
    }
}
