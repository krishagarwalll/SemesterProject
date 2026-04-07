#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FieldHeaderAttribute))]
public class FieldHeaderDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        FieldHeaderAttribute data = (FieldHeaderAttribute)attribute;
        Rect fieldRect = position;
        if (ShouldDrawHeader(property, data))
        {
            Rect headerRect = position;
            headerRect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(headerRect, string.IsNullOrWhiteSpace(data.Title) ? label.text : data.Title, EditorStyles.boldLabel);
            fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        EditorGUI.PropertyField(fieldRect, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUI.GetPropertyHeight(property, label, true);
        FieldHeaderAttribute data = (FieldHeaderAttribute)attribute;
        if (ShouldDrawHeader(property, data))
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    private static bool ShouldDrawHeader(SerializedProperty property, FieldHeaderAttribute data)
    {
        if (!data.UsesCondition)
        {
            return true;
        }

        SerializedProperty source = property.serializedObject.FindProperty(data.ConditionField);
        return source != null
            && source.propertyType == SerializedPropertyType.Enum
            && source.enumValueIndex == data.ExpectedEnumValue;
    }
}
#endif
