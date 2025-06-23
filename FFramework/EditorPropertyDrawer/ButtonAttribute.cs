using UnityEngine;
using System;

/// <summary>
/// Inspector 按钮属性[Button]
/// 支持颜色参数格式：
/// - 颜色名称：red, green, blue, white, black, yellow, cyan, magenta, gray
/// - 十六进制值：#RRGGBB 或 #RRGGBBAA
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public string ButtonName { get; }
    public Color ButtonColor { get; }

    public ButtonAttribute(string name = null, string colorName = "white")
    {
        ButtonName = name;
        ButtonColor = GetColorByName(colorName);
    }

    private static Color GetColorByName(string name)
    {
        switch (name.ToLower())
        {
            case "red": return Color.red;
            case "green": return Color.green;
            case "blue": return Color.blue;
            case "white": return Color.white;
            case "black": return Color.black;
            case "yellow": return Color.yellow;
            case "cyan": return Color.cyan;
            case "magenta": return Color.magenta;
            case "gray": return Color.gray;
            case "grey": return Color.grey;
            default:
                if (ColorUtility.TryParseHtmlString(name, out var color))
                    return color;
                return Color.white;
        }
    }
}
