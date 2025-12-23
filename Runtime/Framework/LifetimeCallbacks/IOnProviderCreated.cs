namespace SystemScrap.ServiceLocator.Framework.LifetimeCallbacks
{
    /// <summary>
    /// Optional callback invoked once when a lazy provider creates the service instance.
    /// </summary>
    /// <remarks>
    /// This callback is triggered by scoped resolvers when a service is first created from a
    /// <see cref="ServiceLocator.Core.LazyProvider" /> registered via <c>ServiceLocator.RegisterLazyScopedProvider</c>.
    /// </remarks>
    public interface IOnProviderCreated
    {
        /// <summary>
        /// Called immediately after the service instance is created by a lazy provider.
        /// </summary>
        void OnProviderCreated();
    }
}
