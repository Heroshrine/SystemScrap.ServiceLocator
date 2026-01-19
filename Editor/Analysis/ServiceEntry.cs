using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// Represents a single service registration entry for display in the analysis tool.
    /// </summary>
    public sealed class ServiceEntry
    {
        public ServiceEntry(string displayName,
            ServiceKind kind,
            IReadOnlyList<Type> registeredTypes,
            object instance,
            Type descriptorType,
            Type originalType,
            Object tiedObject,
            string tiedObjectLabel,
            string scopeLabel,
            string ownerLabel)
        {
            DisplayName = displayName;
            Kind = kind;
            RegisteredTypes = registeredTypes;
            Instance = instance;
            DescriptorType = descriptorType;
            OriginalType = originalType;
            TiedObject = tiedObject;
            TiedObjectLabel = tiedObjectLabel;
            ScopeLabel = scopeLabel;
            OwnerLabel = ownerLabel;
        }

        public string DisplayName { get; }
        public ServiceKind Kind { get; }
        public IReadOnlyList<Type> RegisteredTypes { get; }
        public object Instance { get; }
        public Type DescriptorType { get; }
        public Type OriginalType { get; }
        public Object TiedObject { get; }
        public string TiedObjectLabel { get; }
        public string ScopeLabel { get; }
        public string OwnerLabel { get; }
        public Type InstanceType => Instance?.GetType();

        /// <summary>
        /// Human-readable label for the service kind.
        /// </summary>
        public string KindLabel => Kind switch
        {
            ServiceKind.Transient => "Transient",
            ServiceKind.LazyProvider => "Factory",
            _ => "Instance"
        };
    }
}