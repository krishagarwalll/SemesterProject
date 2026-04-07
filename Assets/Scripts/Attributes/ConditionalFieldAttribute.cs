using UnityEngine;

public sealed class ConditionalFieldAttribute : PropertyAttribute
{
    public string FieldName { get; }
    public bool ShowWhenTrue { get; }
    public string Header { get; }
    public bool UsesEnum { get; }
    public int ExpectedEnumValue { get; }
    public bool InvertEnumMatch { get; }

    public ConditionalFieldAttribute(string fieldName, bool showWhenTrue = true, string header = null)
    {
        FieldName = fieldName;
        ShowWhenTrue = showWhenTrue;
        Header = header;
    }

    public ConditionalFieldAttribute(string fieldName, int expectedEnumValue, string header = null)
        : this(fieldName, expectedEnumValue, false, header)
    {
    }

    public ConditionalFieldAttribute(string fieldName, int expectedEnumValue, bool invertEnumMatch, string header = null)
    {
        FieldName = fieldName;
        Header = header;
        UsesEnum = true;
        ExpectedEnumValue = expectedEnumValue;
        InvertEnumMatch = invertEnumMatch;
    }
}
