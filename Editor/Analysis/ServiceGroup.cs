using System;
using System.Collections.Generic;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// Groups registration information for a single service instance or provider.
    /// </summary>
    public sealed class ServiceGroup
    {
        public ServiceKind kind;
        public object instance;
        public Type descriptorType;
        public Type originalType;
        public readonly List<Type> registeredTypes = new();
    }
}