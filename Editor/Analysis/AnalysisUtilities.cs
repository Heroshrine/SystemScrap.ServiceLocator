using System;
using System.Linq;
using JetBrains.Annotations;
using SystemScrap.ServiceLocator.Core.ServiceDescriptors;
using SystemScrap.ServiceLocator.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SystemScrap.ServiceLocator.Analysis
{
    /// <summary>
    /// Utility methods for the Service Locator analysis tools.
    /// </summary>
    public static class AnalysisUtilities
    {
        /// <summary>
        /// Formats a type name into a human-readable string, including generic arguments.
        /// </summary>
        public static string FormatTypeName([NotNull] Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type.IsGenericType)
                return type.Name;

            var name = type.Name;
            var tickIndex = name.IndexOf('`');
            if (tickIndex >= 0)
                name = name[..tickIndex];

            var args = type.GetGenericArguments().Select(FormatTypeName);
            return $"{name}<{string.Join(", ", args)}>";
        }

        /// <summary>
        /// Formats a scene's name for display.
        /// </summary>
        public static string FormatSceneLabel(Scene scene) =>
            string.IsNullOrWhiteSpace(scene.name) ? "Untitled Scene" : scene.name;

        /// <summary>
        /// Formats a <see cref="Scope"/> into a display-friendly string.
        /// </summary>
        public static string FormatScopeLabel(Scope scope) =>
            scope switch
            {
                Scope.Global => "Global",
                Scope.Scene => "Scene",
                Scope.GameObject => "GameObject",
                _ => scope.ToString()
            };

        /// <summary>
        /// Gets the full hierarchy path of a GameObject.
        /// </summary>
        public static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null)
                return "Missing GameObject";

            var path = obj.name;
            var current = obj.transform.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            if (obj.scene.IsValid() && !string.IsNullOrWhiteSpace(obj.scene.name))
                path = $"{obj.scene.name}/{path}";

            return path;
        }

        /// <summary>
        /// Attempts to find a Unity Object related to the service instance for editor pinging.
        /// </summary>
        public static Object ResolveTiedObject(object instance, GameObject scopeObject, out string label)
        {
            if (instance is Object unityObject)
            {
                label = "Service Object";
                return unityObject;
            }

            if (scopeObject != null)
            {
                label = "Registered GameObject";
                return scopeObject;
            }

            label = null;
            return null;
        }

        /// <summary>
        /// Attempts to extract an instance from an <see cref="InstanceDescriptor{T}"/>.
        /// </summary>
        public static bool TryGetInstanceFromDescriptor(IServiceDescriptor<object> descriptor, out object instance)
        {
            instance = null;
            if (descriptor == null)
                return false;

            var descriptorType = descriptor.GetType();
            if (!descriptorType.IsGenericType ||
                descriptorType.GetGenericTypeDefinition() != typeof(InstanceDescriptor<>))
            {
                return false;
            }

            var field = ReflectionCache.GetInstanceField(descriptorType);
            if (field == null)
                return false;

            instance = field.GetValue(descriptor);
            return true;
        }

        /// <summary>
        /// Unwraps an Alias object to its target value.
        /// </summary>
        public static object UnwrapAlias(object value)
        {
            if (value == null)
                return null;

            if (ReflectionCache.AliasToType != null && ReflectionCache.AliasToType.IsInstanceOfType(value))
                return ReflectionCache.AliasTargetProperty?.GetValue(value);

            return value;
        }
    }
}