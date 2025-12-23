namespace SystemScrap.ServiceLocator.Framework.LifetimeCallbacks
{
    /// <summary>
    /// Optional callback invoked when a service is resolved from a scope.
    /// </summary>
    /// <remarks>
    /// This callback is invoked by implementations of <see cref="ServiceLocator.Framework.IScopedResolver" /> after a
    /// service instance has been retrieved (including when resolved via an alias).
    /// </remarks>
    public interface IOnResolved
    {
        /// <summary>
        /// Called after the service instance has been successfully resolved.
        /// </summary>
        void OnResolved();
    }
}
