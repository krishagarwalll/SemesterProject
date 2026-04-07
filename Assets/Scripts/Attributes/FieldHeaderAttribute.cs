using UnityEngine;

public sealed class FieldHeaderAttribute : PropertyAttribute
{
    public string Title { get; }
    public string ConditionField { get; }
    public int ExpectedEnumValue { get; }
    public bool UsesCondition { get; }

    public FieldHeaderAttribute(string title = null)
    {
        Title = title;
    }

    public FieldHeaderAttribute(string title, string conditionField, int expectedEnumValue)
    {
        Title = title;
        ConditionField = conditionField;
        ExpectedEnumValue = expectedEnumValue;
        UsesCondition = true;
    }
}
