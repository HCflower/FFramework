#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 此类包含 ReadOnly 属性的自定义抽屉。
/// </summary>
[CustomPropertyDrawer(typeof(ShowOnlyAttribute))]
public class ShowOnlyDrawer : PropertyDrawer
{
    /// <summary>
    /// 在 Editor 中绘制 GUI 的 Unity 方法
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="property">Property.</param>
    /// <param name="label">Label.</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif