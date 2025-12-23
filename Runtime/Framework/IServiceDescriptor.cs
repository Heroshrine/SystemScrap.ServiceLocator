namespace SystemScrap.ServiceLocator.Framework
{
    /// <summary>
    /// Describes how to retrieve a service instance (for example, singleton vs transient).
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    public interface IServiceDescriptor<out T> where T : class
    {
        /// <summary>
        /// Gets an instance of the described service.
        /// </summary>
        /// <returns>A service instance.</returns>
        T GetService();
    }
}
