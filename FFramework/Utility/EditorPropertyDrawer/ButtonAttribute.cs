using System;

/// <summary>
/// Inspector 按钮属性[Button]
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public string ButtonName { get; }

    public ButtonAttribute(string name = null)
    {
        ButtonName = name;
    }
}
