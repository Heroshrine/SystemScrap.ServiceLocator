using System;
using System.Collections.Generic;
using UnityEngine;

namespace SystemScrap.ServiceLocator.Framework
{
    /// <summary>
    /// A lightweight token that reflects the lifetime of a <see cref="RegistrationHandle" />.
    /// </summary>
    /// <remarks>
    /// Tokens are created via <see cref="RegistrationHandle.GetToken" /> and are commonly used by
    /// managed registrations (see <see cref="ServiceLocator.Core.ScopedResolvers.ManagedScopeResolver{TService}" />).
    /// </remarks>
    public sealed class ServiceRegistrationToken
    {
        private readonly RegistrationHandle _handle;

        private readonly Dictionary<Action, (ExceptionCatcher catcher, int count)> _callbackMap = new();

        /// <summary>
        /// Raised when the associated <see cref="RegistrationHandle" /> is disposed.
        /// </summary>
        /// <remarks>
        /// If a handler is added after disposal, it is invoked immediately. Exceptions thrown by handlers are logged
        /// and do not prevent other handlers from running.
        /// </remarks>
        public event Action OnDisposed
        {
            add
            {
                if (IsDisposed)
                {
                    try
                    {
                        value?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    return;
                }

                if (!_callbackMap.TryGetValue(value, out var v))
                    v = _callbackMap[value] = (new ExceptionCatcher(value), 0);
                v.count++;

                _onDisposed += v.catcher.Invoke;
            }
            remove
            {
                if (!_callbackMap.TryGetValue(value, out var v)) return;

                _onDisposed -= v.catcher.Invoke;
                v.count--;
                if (v.count == 0) _callbackMap.Remove(value);
            }
        }

        private Action _onDisposed;

        /// <summary>
        /// Gets whether the associated handle is disposed.
        /// </summary>
        public bool IsDisposed => _handle?.IsDisposed ?? true;

        /// <summary>
        /// Gets whether this token is valid.
        /// </summary>
        /// <remarks>
        /// Tokens created by <see cref="RegistrationHandle.GetToken" /> are always valid.
        /// </remarks>
        public bool IsValid { get; }

        internal ServiceRegistrationToken(RegistrationHandle handle)
        {
            _handle = handle;
            _onDisposed = null;
            IsValid = true;
            _handle.RegisterTokenCallback(OnHandleDisposed);
        }

        private void OnHandleDisposed()
        {
            try
            {
                _onDisposed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Wraps a callback so exceptions can be caught and logged.
        /// </summary>
        private record ExceptionCatcher(Action Action)
        {
            /// <summary>
            /// Gets the wrapped callback.
            /// </summary>
            public Action Action { get; } = Action;

            /// <summary>
            /// Invokes the wrapped callback while logging any thrown exceptions.
            /// </summary>
            [HideInCallstack]
            public void Invoke()
            {
                try
                {
                    Action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}