using System.Collections.Generic;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// Represents a category of service entries, grouped by a context.
    /// </summary>
    /// <typeparam name="T">The type of the context.</typeparam>
    public sealed class EntryCategory<T>
    {
        public EntryCategory(T ctx, string displayName, List<ServiceEntry> services)
        {
            Ctx = ctx;
            DisplayName = displayName;
            Services = services;
        }

        /// <summary>
        /// The context object for this category.
        /// </summary>
        public T Ctx { get; }

        /// <summary>
        /// The name to display for this category.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The list of services in this category.
        /// </summary>
        public List<ServiceEntry> Services { get; }
    }
}