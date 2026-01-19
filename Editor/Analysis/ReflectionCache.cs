using System;
using System.Collections.Generic;
using System.Reflection;
using SystemScrap.ServiceLocator.Core;
using SystemScrap.ServiceLocator.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using SLocator = SystemScrap.ServiceLocator.Core.ServiceLocator;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// Caches reflection data to access internal members of <see cref="SLocator"/>.
    /// </summary>
    public static class ReflectionCache
    {
        public static readonly Type AliasToType =
            typeof(SLocator).Assembly.GetType(RegisteredServicesWindow.ALIAS_TYPE_NAME);

        public static readonly PropertyInfo AliasTargetProperty =
            AliasToType?.GetProperty("AliasTarget",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo s_GlobalServicesField =
            typeof(SLocator).GetField("_globalServices", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_ManagedServicesField =
            typeof(SLocator).GetField("_managedServices", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_SceneServicesField =
            typeof(SLocator).GetField("_sceneServices", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_GameObjectServicesField =
            typeof(SLocator).GetField("_gameObjectServices", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_LazyProvidersField =
            typeof(SLocator).GetField("_lazyProviders", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Dictionary<Type, FieldInfo> s_InstanceFields = new();

        /// <summary>
        /// Gets the '_instance' field of an InstanceDescriptor via reflection.
        /// </summary>
        public static FieldInfo GetInstanceField(Type descriptorType)
        {
            if (descriptorType == null)
                return null;

            if (s_InstanceFields.TryGetValue(descriptorType, out var field))
                return field;

            field = descriptorType.GetField("_instance", BindingFlags.Instance | BindingFlags.NonPublic);
            s_InstanceFields[descriptorType] = field;
            return field;
        }

        /// <summary>
        /// Gets the global services dictionary from the locator.
        /// </summary>
        public static Dictionary<Type, IServiceDescriptor<object>> GetGlobalServices(SLocator locator) =>
            s_GlobalServicesField?.GetValue(locator) as Dictionary<Type, IServiceDescriptor<object>>;

        /// <summary>
        /// Gets the managed services dictionary from the locator.
        /// </summary>
        public static Dictionary<Type, (object instance, RegistrationHandle handle)> GetManagedServices(
            SLocator locator) =>
            s_ManagedServicesField?.GetValue(locator)
                as Dictionary<Type, (object instance, RegistrationHandle handle)>;

        /// <summary>
        /// Gets the scene-scoped services dictionary from the locator.
        /// </summary>
        public static Dictionary<Scene, Dictionary<Type, object>> GetSceneServices(SLocator locator) =>
            s_SceneServicesField?.GetValue(locator) as Dictionary<Scene, Dictionary<Type, object>>;

        /// <summary>
        /// Gets the GameObject-scoped services dictionary from the locator.
        /// </summary>
        public static Dictionary<GameObject, Dictionary<Type, object>> GetGameObjectServices(SLocator locator) =>
            s_GameObjectServicesField?.GetValue(locator)
                as Dictionary<GameObject, Dictionary<Type, object>>;

        /// <summary>
        /// Gets the lazy providers dictionary from the locator.
        /// </summary>
        public static Dictionary<Scope, Dictionary<Type, LazyProvider>> GetLazyProviders(SLocator locator) =>
            s_LazyProvidersField?.GetValue(locator) as Dictionary<Scope, Dictionary<Type, LazyProvider>>;
    }
}