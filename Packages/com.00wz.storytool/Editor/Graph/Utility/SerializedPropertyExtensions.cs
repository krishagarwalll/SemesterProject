using UnityEngine;
using UnityEditor;
using System.Collections;

namespace StoryTool.Editor
{
    /// <summary>
    /// Helper extension methods for working with <see cref="SerializedProperty"/> instances.
    /// </summary>
    public static class SerializedPropertyExtensions
    {
        /// <summary>
        /// Adds a new element to the end of an array (or list) <see cref="SerializedProperty"/>.
        /// Returns a reference to the newly added element.
        /// </summary>
        /// <param name="arrayProperty">SerializedProperty that points to an array or list.</param>
        /// <returns>The newly added element as a <see cref="SerializedProperty"/>.</returns>
        public static SerializedProperty AddArrayElementToEnd(this SerializedProperty arrayProperty)
        {
            if (!arrayProperty.isArray)
            {
                Debug.LogError($"[StoryTool] SerializedProperty '{arrayProperty.name}' is not an array or list.");
                return null;
            }

            // Insert a new element at the end of the array.
            int newIndex = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(newIndex);

            // Return the newly added element.
            SerializedProperty newElement = arrayProperty.GetArrayElementAtIndex(newIndex);

            return newElement;
        }

        /// <summary>
        /// Removes the first element from a <see cref="SerializedProperty"/> array
        /// whose <see cref="SerializedProperty.managedReferenceValue"/> is equal to the specified object.
        /// </summary>
        /// <param name="arrayProperty">SerializedProperty that points to a [SerializeReference] array.</param>
        /// <param name="targetObject">The object instance to remove from the array.</param>
        /// <returns><c>true</c> if an element was found and removed; otherwise <c>false</c>.</returns>
        public static bool RemoveManagedReferenceElement(this SerializedProperty arrayProperty, object targetObject)
        {
            if (!arrayProperty.isArray)
            {
                Debug.LogError($"[StoryTool] SerializedProperty '{arrayProperty.name}' is not an array or list.");
                return false;
            }

            if (targetObject == null)
            {
                Debug.LogWarning("[StoryTool] Attempt to remove a null object from a managed reference array.");
                return false;
            }

            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);

                // Check managedReferenceValue to find the exact instance.
                if (element.propertyType == SerializedPropertyType.ManagedReference &&
                    ReferenceEquals(element.managedReferenceValue, targetObject))
                {
                    arrayProperty.DeleteArrayElementAtIndex(i);
                    return true;
                }
            }

            // Element with the specified reference was not found.
            return false;
        }

        /// <summary>
        /// Checks that the <see cref="SerializedProperty"/> points to a non-collection field/reference
        /// whose declared type is exactly <typeparamref name="T"/>.
        ///
        /// Supports:
        /// - [SerializeField] T (non-array, non-list fields)
        /// - [SerializeReference] T (single managed reference field)
        ///
        /// IMPORTANT:
        /// This method does NOT support properties that reference collections (arrays or lists).
        /// If <paramref name="property"/> represents a collection (property.isArray == true),
        /// this method always returns <c>false</c>.
        /// </summary>
        public static bool IsOfType<T>(this SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            // Collections (arrays/lists) are intentionally not supported.
            if (property.isArray)
            {
                return false;
            }

            var targetType = typeof(T);

            // [SerializeField] T (regular non-collection field)
            if (property.propertyType == SerializedPropertyType.Generic)
            {
                // Unity usually stores just the type name (without namespace) for generic fields.
                return property.type == targetType.Name;
            }

            // [SerializeReference] T (non-collection)
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                // Use the declared field type name so behavior matches Generic fields.
                var fieldTypeName = property.managedReferenceFieldTypename;
                if (string.IsNullOrEmpty(fieldTypeName))
                {
                    return false;
                }

                // Format: "AssemblyName TypeFullName"
                var lastSpaceIndex = fieldTypeName.LastIndexOf(' ');
                var typeName = lastSpaceIndex >= 0
                    ? fieldTypeName.Substring(lastSpaceIndex + 1)
                    : fieldTypeName;

                // Strictly compare the declared type with T (full name or short name).
                return typeName == targetType.FullName || typeName == targetType.Name;
            }

            return false;
        }

        /// <summary>
        /// Checks that the value referenced by the given <see cref="SerializedProperty"/> is not null.
        /// Supports both legacy Unity versions (without <c>boxedValue</c>) and Unity 2022.1+.
        /// For value types (structs, primitives) this method returns true as long as the property exists.
        /// </summary>
        public static bool IsValueNotNull(this SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

#if UNITY_2022_1_OR_NEWER
            // In Unity 2022.1+ boxedValue delegates to the appropriate *Value properties
            // (objectReferenceValue, managedReferenceValue, etc.), so a single check is enough.
            try
            {
                return property.boxedValue != null;
            }
            catch
            {
                // If boxedValue is not available for this propertyType, treat the value as missing.
                return false;
            }
#else
            // In Unity versions prior to 2022.1 boxedValue is not available,
            // so we explicitly handle only types where null has semantic meaning.
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    // UnityEngine.Object fields (including [SerializeField]) in all Unity versions.
                    return property.objectReferenceValue != null;

                case SerializedPropertyType.ManagedReference:
                    // [SerializeReference] is supported starting from Unity 2019.3.
                    return property.managedReferenceValue != null;

                default:
                    // For most serializable types (primitives, structs, Serializable classes)
                    // Unity creates a default value; we cannot distinguish null from default without boxedValue.
                    return true;
            }
#endif
        }
    }
}