using System;

namespace SystemScrap.ServiceLocator.Framework
{
    /// <summary>
    /// Resolves services within a particular scope (global, scene, or GameObject).
    /// </summary>
    /// <remarks>
    /// Instances are created by <see cref="ServiceLocator.Core.ServiceLocator" /> (or the
    /// convenience API in <see cref="ServiceLocator.Core.Services" />), and may fall back to
    /// broader scopes depending on the implementation.
    /// </remarks>
    public interface IScopedResolver
    {
        /// <summary>
        /// Returns whether this resolver can still resolve services for its scope.
        /// </summary>
        bool IsInScope();

        /// <summary>
        /// Attempts to resolve a service of type <typeparamref name="T" />.
        /// </summary>
        /// <param name="found">When this method returns <c>true</c>, the resolved service instance.</param>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns><c>true</c> if a service instance is available; otherwise <c>false</c>.</returns>
        bool TryGet<T>(out T found) where T : class;

        /// <summary>
        /// Resolves a service of type <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the scope has ended, or when no service is registered in this scope chain.
        /// </exception>
        T Get<T>() where T : class;
    }
}
