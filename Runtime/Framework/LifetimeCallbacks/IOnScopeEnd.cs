namespace SystemScrap.ServiceLocator.Framework.LifetimeCallbacks
{
    /// <summary>
    /// Optional callback invoked when the owning scope ends and services are being cleaned up.
    /// </summary>
    /// <remarks>
    /// Scope cleanup is performed by <see cref="ServiceLocator.Core.ServiceLocator" /> when a scene unloads,
    /// a GameObject is destroyed, or a managed registration is disposed. If a service also implements
    /// <see cref="System.IDisposable" />, the callback is invoked before <see cref="System.IDisposable.Dispose" />.
    /// </remarks>
    public interface IOnScopeEnd
    {
        /// <summary>
        /// Called when the scope ends and the service is being removed.
        /// </summary>
        void OnScopeEnded();
    }
}
