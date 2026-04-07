#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute data = (ConditionalFieldAttribute)attribute;
        if (!ShouldShow(property, data))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(data.Header))
        {
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, data.Header, EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute data = (ConditionalFieldAttribute)attribute;
        if (!ShouldShow(property, data))
        {
            return 0f;
        }

        float height = EditorGUI.GetPropertyHeight(property, label, true);
        if (!string.IsNullOrWhiteSpace(data.Header))
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    private static bool ShouldShow(SerializedProperty property, ConditionalFieldAttribute data)
    {
        SerializedProperty source = property.serializedObject.FindProperty(data.FieldName);
        if (source == null)
        {
            return true;
        }

        if (!data.UsesEnum)
        {
            bool matches = source.propertyType == SerializedPropertyType.Boolean && source.boolValue;
            return data.ShowWhenTrue ? matches : !matches;
        }

        bool enumMatches = source.propertyType == SerializedPropertyType.Enum && source.enumValueIndex == data.ExpectedEnumValue;
        return data.InvertEnumMatch ? !enumMatches : enumMatches;
    }
}
#endif
