using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// A comparer that uses reference equality instead of value equality.
    /// </summary>
    public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}