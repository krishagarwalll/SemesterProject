using System;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class ButtonAttribute : Attribute
{
    public string Label { get; }
    public float Height { get; }
    public bool PlayModeOnly { get; }
    public bool EditModeOnly { get; }

    public ButtonAttribute(
        string label = null,
        float height = 24f,
        bool playModeOnly = false,
        bool editModeOnly = false)
    {
        Label = label;
        Height = height;
        PlayModeOnly = playModeOnly;
        EditModeOnly = editModeOnly;
    }
}
