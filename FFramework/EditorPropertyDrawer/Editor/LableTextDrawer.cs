#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

//TODO:优化
[CustomPropertyDrawer(typeof(TextLableAttribute))]
public class LableTextDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var changeNameAttribute = (TextLableAttribute)attribute;
        // 使用自定义名称作为标签
        label.text = changeNameAttribute.displayName;
        // 绘制字段
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 保持原字段高度
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

#endif