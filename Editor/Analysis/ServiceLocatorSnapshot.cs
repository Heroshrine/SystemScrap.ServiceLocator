using System;
using System.Collections.Generic;
using System.Linq;
using SystemScrap.ServiceLocator.Core;
using SystemScrap.ServiceLocator.Core.ServiceDescriptors;
using SystemScrap.ServiceLocator.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using SLocator = SystemScrap.ServiceLocator.Core.ServiceLocator;
using static SystemScrap.ServiceLocator.Analysis.AnalysisUtilities;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// Captures a snapshot of the current state of the Service Locator for analysis.
    /// </summary>
    public sealed class ServiceLocatorSnapshot
    {
        public static readonly List<ServiceEntry> EmptyServices = new();

        public static readonly ServiceLocatorSnapshot Empty = new(false,
            new List<ServiceEntry>(),
            new List<ServiceEntry>(),
            new List<EntryCategory<Scene>>(),
            new List<EntryCategory<GameObject>>(),
            new List<EntryCategory<Scope>>());

        private ServiceLocatorSnapshot(bool hasLocator,
            List<ServiceEntry> globalServices,
            List<ServiceEntry> managedServices,
            List<EntryCategory<Scene>> sceneScopes,
            List<EntryCategory<GameObject>> gameObjectScopes,
            List<EntryCategory<Scope>> factoryScopes)
        {
            HasLocator = hasLocator;
            GlobalServices = globalServices;
            ManagedServices = managedServices;
            SceneScopes = sceneScopes;
            GameObjectScopes = gameObjectScopes;
            FactoryScopes = factoryScopes;
        }

        public bool HasLocator { get; }
        public List<ServiceEntry> GlobalServices { get; }
        public List<ServiceEntry> ManagedServices { get; }
        public List<EntryCategory<Scene>> SceneScopes { get; }
        public List<EntryCategory<GameObject>> GameObjectScopes { get; }
        public List<EntryCategory<Scope>> FactoryScopes { get; }

        /// <summary>
        /// Captures the current state of the global Service Locator.
        /// </summary>
        public static ServiceLocatorSnapshot Capture()
        {
            var locator = Services.GetLocator();
            if (locator == null)
                return Empty;

            // Gather all services from the locator via reflection
            var globalServices = BuildGlobalServices(locator);
            var managedServices = BuildManagedServices(locator);
            var sceneScopes = BuildSceneScopes(locator);
            var gameObjectScopes = BuildGameObjectScopes(locator);
            var factoryScopes = BuildFactoryScopes(locator);

            return new ServiceLocatorSnapshot(true, globalServices, managedServices, sceneScopes,
                gameObjectScopes, factoryScopes);
        }

        private static List<ServiceEntry> BuildGlobalServices(SLocator locator)
        {
            var services = ReflectionCache.GetGlobalServices(locator);
            if (services == null || services.Count == 0)
                return new List<ServiceEntry>();

            var groups = new Dictionary<object, ServiceGroup>(ReferenceEqualityComparer.Instance);
            foreach (var (registrationType, descriptor) in services)
            {
                if (descriptor == null)
                    continue;

                var descriptorType = descriptor.GetType();

                // Handle Transient registrations
                if (descriptorType.IsGenericType &&
                    descriptorType.GetGenericTypeDefinition() ==
                    typeof(TransientDescriptor<>))
                {
                    var group = GetOrCreateGroup(groups, descriptor, ServiceKind.Transient);
                    group.descriptorType = descriptorType;
                    group.registeredTypes.Add(registrationType);
                    continue;
                }

                // Handle Instance registrations (Singleton/Scoped)
                object instance = null;
                if (TryGetInstanceFromDescriptor(descriptor, out var rawInstance))
                    instance = UnwrapAlias(rawInstance);

                var identity = instance ?? descriptor;
                var instanceGroup = GetOrCreateGroup(groups, identity, ServiceKind.Instance);
                instanceGroup.instance = instance;
                instanceGroup.descriptorType = descriptorType;
                instanceGroup.registeredTypes.Add(registrationType);
            }

            return BuildEntriesFromGroups(groups.Values, "Global", null, null);
        }

        private static List<ServiceEntry> BuildManagedServices(SLocator locator)
        {
            var services = ReflectionCache.GetManagedServices(locator);
            if (services == null || services.Count == 0)
                return new List<ServiceEntry>();

            var entries = new List<ServiceEntry>();
            foreach (var (registrationType, value) in services)
            {
                var instance = value.instance;
                var registeredTypes = new List<Type> { registrationType };

                // Determine display name: use implementation type if available, otherwise registration type
                var displayName = instance != null
                    ? FormatTypeName(instance.GetType())
                    : FormatTypeName(registrationType);

                var tiedObject = ResolveTiedObject(instance, null, out var label);
                entries.Add(new ServiceEntry(displayName, ServiceKind.Instance, registeredTypes, instance, null,
                    null, tiedObject, label, "Managed", null));
            }

            // Sort alphabetically by display name
            entries.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        private static List<EntryCategory<Scene>> BuildSceneScopes(SLocator locator)
        {
            var sceneServices = ReflectionCache.GetSceneServices(locator);
            if (sceneServices == null || sceneServices.Count == 0)
                return new List<EntryCategory<Scene>>();

            var entries = new List<EntryCategory<Scene>>();
            foreach (var pair in sceneServices)
            {
                var scene = pair.Key;

                // Only include valid and loaded scenes
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                var services = pair.Value;
                if (services == null || services.Count == 0)
                    continue;

                var displayName = FormatSceneLabel(scene);
                var serviceEntries = BuildInstanceEntries(services, "Scene", displayName, null);
                if (serviceEntries.Count == 0)
                    continue;

                entries.Add(new EntryCategory<Scene>(scene, displayName, serviceEntries));
            }

            // Sort scenes by name
            entries.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        private static List<EntryCategory<GameObject>> BuildGameObjectScopes(SLocator locator)
        {
            var gameObjectServices = ReflectionCache.GetGameObjectServices(locator);
            if (gameObjectServices == null || gameObjectServices.Count == 0)
                return new List<EntryCategory<GameObject>>();

            var entries = new List<EntryCategory<GameObject>>();
            foreach (var (gameObject, services) in gameObjectServices)
            {
                // Validate GameObject and its services
                if (!gameObject)
                    continue;

                if (services == null || services.Count == 0)
                    continue;

                var displayName = GetGameObjectPath(gameObject);
                var serviceEntries = BuildInstanceEntries(services, "GameObject", displayName, gameObject);
                if (serviceEntries.Count == 0)
                    continue;

                entries.Add(new EntryCategory<GameObject>(gameObject, displayName, serviceEntries));
            }

            // Sort GameObjects by hierarchy path
            entries.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        private static List<EntryCategory<Scope>> BuildFactoryScopes(SLocator locator)
        {
            var providers = ReflectionCache.GetLazyProviders(locator);
            if (providers == null || providers.Count == 0)
                return new List<EntryCategory<Scope>>();

            var entries = new List<EntryCategory<Scope>>();
            foreach (var (scope, scopeProviders) in providers)
            {
                if (scopeProviders == null || scopeProviders.Count == 0)
                    continue;

                // Group providers by implementation to handle multiple interface registrations
                var groups = new Dictionary<object, ServiceGroup>(ReferenceEqualityComparer.Instance);
                foreach (var (registrationType, provider) in scopeProviders)
                {
                    if (provider == null)
                        continue;

                    var group = GetOrCreateGroup(groups, provider, ServiceKind.LazyProvider);
                    group.instance = provider;
                    group.originalType = provider.OriginalType;
                    group.registeredTypes.Add(registrationType);
                }

                var scopeLabel = FormatScopeLabel(scope);
                var serviceEntries = BuildEntriesFromGroups(groups.Values, scopeLabel, null, null);
                if (serviceEntries.Count == 0)
                    continue;

                entries.Add(new EntryCategory<Scope>(scope, scopeLabel, serviceEntries));
            }

            // Sort factory scopes by name
            entries.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        private static List<ServiceEntry> BuildInstanceEntries(Dictionary<Type, object> services,
            string scopeLabel,
            string ownerLabel,
            GameObject scopeObject)
        {
            var groups = new Dictionary<object, ServiceGroup>(ReferenceEqualityComparer.Instance);
            foreach (var pair in services)
            {
                var registrationType = pair.Key;
                var rawValue = pair.Value;
                if (rawValue == null)
                    continue;

                var instance = UnwrapAlias(rawValue);
                var identity = instance ?? rawValue;
                var group = GetOrCreateGroup(groups, identity, ServiceKind.Instance);
                group.instance = instance;
                group.registeredTypes.Add(registrationType);
            }

            return BuildEntriesFromGroups(groups.Values, scopeLabel, ownerLabel, scopeObject);
        }

        private static List<ServiceEntry> BuildEntriesFromGroups(IEnumerable<ServiceGroup> groups,
            string scopeLabel,
            string ownerLabel,
            GameObject scopeObject)
        {
            var entries = new List<ServiceEntry>();
            foreach (var group in groups)
            {
                // Clean and sort registered types for display
                var registeredTypes = group.registeredTypes
                    .Distinct()
                    .OrderBy(FormatTypeName)
                    .ToList();

                var displayName = BuildDisplayName(group, registeredTypes);

                // Try to find a Unity object to link to for pinging
                var tiedObject = ResolveTiedObject(group.instance, scopeObject, out var tiedLabel);

                entries.Add(new ServiceEntry(displayName, group.kind, registeredTypes, group.instance,
                    group.descriptorType, group.originalType, tiedObject, tiedLabel, scopeLabel, ownerLabel));
            }

            // Final sort of entries within the category
            entries.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        private static string BuildDisplayName(ServiceGroup group, List<Type> registeredTypes)
        {
            return group.kind switch
            {
                ServiceKind.LazyProvider =>
                    group.originalType != null
                        ? $"{FormatTypeName(group.originalType)} (Factory)"
                        : "Factory",
                ServiceKind.Transient => registeredTypes.Count > 0
                    ? $"{FormatTypeName(registeredTypes[0])} (Transient)"
                    : "Transient Service",
                _ => group.instance != null
                    ? FormatTypeName(group.instance.GetType())
                    : registeredTypes.Count > 0
                        ? FormatTypeName(registeredTypes[0])
                        : "Service"
            };
        }

        private static ServiceGroup GetOrCreateGroup(Dictionary<object, ServiceGroup> groups, object key,
            ServiceKind kind)
        {
            if (groups.TryGetValue(key, out var group)) return group;

            group = new ServiceGroup
            {
                kind = kind
            };
            groups[key] = group;

            return group;
        }
    }
}