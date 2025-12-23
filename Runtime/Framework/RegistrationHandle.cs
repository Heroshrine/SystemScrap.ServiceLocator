using System;
using UnityEngine.Assertions;

namespace SystemScrap.ServiceLocator.Framework
{
    /// <summary>
    /// Represents a service registration that can be disposed to unregister it.
    /// </summary>
    /// <remarks>
    /// Use <see cref="GetToken" /> to observe disposal without holding a direct reference to this handle.
    /// </remarks>
    public sealed class RegistrationHandle : IDisposable, IEquatable<RegistrationHandle>
    {
        private readonly int _hash;
        private Action _disposeAction;
        private event Action OnDisposed;

        /// <summary>
        /// Gets whether this handle has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        internal RegistrationHandle(Action disposeAction)
        {
            _disposeAction =
                disposeAction
                ?? throw new ArgumentNullException(nameof(disposeAction));

            _hash = disposeAction.GetHashCode();
        }

        /// <summary>
        /// Creates a token that reflects this handle's disposed state and can notify when it is disposed.
        /// </summary>
        /// <remarks>
        /// This is commonly used by managed registrations (see <see cref="ServiceLocator.Core.ServiceLocator.ForManaged{T}" />).
        /// </remarks>
        public ServiceRegistrationToken GetToken() => new(this);

        internal void RegisterTokenCallback(Action callback)
        {
            if (IsDisposed)
            {
                callback?.Invoke();
                return;
            }

            OnDisposed += callback;
        }

        private void NotifyTokens() => OnDisposed?.Invoke();

        /// <summary>
        /// Disposes this handle, invoking its unregister/cleanup action exactly once.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            _disposeAction?.Invoke();

            NotifyTokens();
            _disposeAction = null;
            OnDisposed = null;
        }

        /// <inheritdoc />
        public bool Equals(RegistrationHandle other) => ReferenceEquals(this, other);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is RegistrationHandle other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _hash;

        /// <summary>
        /// Determines whether two handles refer to the same instance.
        /// </summary>
        public static bool operator ==(RegistrationHandle lh, RegistrationHandle rh) => ReferenceEquals(lh, rh);

        /// <summary>
        /// Determines whether two handles refer to different instances.
        /// </summary>
        public static bool operator !=(RegistrationHandle lh, RegistrationHandle rh) => !(lh == rh);

        /// <summary>
        /// Combines two handles into a single handle that is disposed when either input handle is disposed.
        /// </summary>
        /// <param name="first">The first handle.</param>
        /// <param name="second">The second handle.</param>
        /// <returns>A combined handle.</returns>
        public static RegistrationHandle Combine(RegistrationHandle first, RegistrationHandle second)
        {
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);

            var combined = new RegistrationHandle(() =>
            {
                first.Dispose();
                second.Dispose();
            });

            if (first.IsDisposed || second.IsDisposed)
                combined.Dispose();

            return combined;
        }
    }
}