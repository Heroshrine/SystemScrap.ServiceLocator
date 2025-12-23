namespace SystemScrap.ServiceLocator.Core.ServiceAliasers
{
    /// <summary>
    /// Internal wrapper used to represent an alias registration to another service instance.
    /// </summary>
    /// <remarks>
    /// Alias registrations are created via <see cref="ServiceLocator.Framework.IServiceAliaser{T}.As{TBase}" /> and unwrapped by
    /// scoped resolvers before returning the underlying target instance.
    /// </remarks>
    internal sealed record AliasTo(object AliasTarget)
    {
        /// <summary>
        /// Gets the service instance this alias points to.
        /// </summary>
        public object AliasTarget { get; private set; } = AliasTarget;
    }
}
