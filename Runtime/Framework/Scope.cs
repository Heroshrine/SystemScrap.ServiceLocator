namespace SystemScrap.ServiceLocator.Framework
{
    /// <summary>
    /// Identifies the lifetime scope a service is registered in and resolved from.
    /// </summary>
    public enum Scope : byte
    {
        /// <summary>
        /// Services shared application-wide.
        /// </summary>
        Global = 0,

        /// <summary>
        /// Services tied to a specific loaded <see cref="UnityEngine.SceneManagement.Scene" />.
        /// </summary>
        Scene = 1,

        /// <summary>
        /// Services tied to a specific <see cref="UnityEngine.GameObject" />.
        /// </summary>
        GameObject = 2
    }
}
