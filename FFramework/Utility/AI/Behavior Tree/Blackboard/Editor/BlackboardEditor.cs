// =============================================================
// 描述：黑板数据编辑器
// 作者：HCFlower
// 创建时间：2025-11-16 00:44:00
// 版本：1.0.0
// =============================================================
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using static BlackboardData;

[CustomEditor(typeof(BlackboardData))]
public class BlackboardEditor : Editor
{
    ReorderableList entryList;

    void OnEnable()
    {
        entryList = new ReorderableList(serializedObject, serializedObject.FindProperty("entries"), true, true, true, true)
        {
            drawHeaderCallback = rect =>
            {
                float col1 = rect.width * 0.3f;
                float col2 = rect.width * 0.3f;
                float col3 = rect.width * 0.4f;
                float padding = 5f;

                float x0 = rect.x + padding;
                float x1 = x0 + col1;
                float x2 = x1 + col2;

                EditorGUI.LabelField(new Rect(x0, rect.y, col1 - padding, EditorGUIUtility.singleLineHeight), "Key");
                EditorGUI.LabelField(new Rect(x1, rect.y, col2 - padding, EditorGUIUtility.singleLineHeight), "Type");
                EditorGUI.LabelField(new Rect(x2, rect.y, col3 - padding, EditorGUIUtility.singleLineHeight), "Value");
            }
        };
        entryList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = entryList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            float col1 = rect.width * 0.3f;
            float col2 = rect.width * 0.3f;
            float col3 = rect.width * 0.4f;
            float padding = 5f;

            float x0 = rect.x + padding;
            float x1 = x0 + col1;
            float x2 = x1 + col2;

            var keyProp = element.FindPropertyRelative("keyName");
            var valueType = element.FindPropertyRelative("valueType");
            var valueProp = element.FindPropertyRelative("value");

            var keyNameRect = new Rect(x0, rect.y, col1 - padding, EditorGUIUtility.singleLineHeight);
            var valueTypeRect = new Rect(x1, rect.y, col2 - padding, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(x2, rect.y, col3 - padding, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(keyNameRect, keyProp, GUIContent.none);
            EditorGUI.PropertyField(valueTypeRect, valueType, GUIContent.none);
            switch ((AnyValue.ValueType)valueType.enumValueIndex)
            {
                case AnyValue.ValueType.Bool:
                    EditorGUI.PropertyField(valueRect, valueProp.FindPropertyRelative("boolValue"), GUIContent.none);
                    break;
                case AnyValue.ValueType.Int:
                    EditorGUI.PropertyField(valueRect, valueProp.FindPropertyRelative("intValue"), GUIContent.none);
                    break;
                case AnyValue.ValueType.Float:
                    EditorGUI.PropertyField(valueRect, valueProp.FindPropertyRelative("floatValue"), GUIContent.none);
                    break;
                case AnyValue.ValueType.String:
                    EditorGUI.PropertyField(valueRect, valueProp.FindPropertyRelative("stringValue"), GUIContent.none);
                    break;
                case AnyValue.ValueType.Vector3:
                    EditorGUI.PropertyField(valueRect, valueProp.FindPropertyRelative("vector3Value"), GUIContent.none);
                    break;
                case AnyValue.ValueType.GameObject:
                    EditorGUI.PropertyField(valueRect, valueProp.FindPropertyRelative("gameObjectValue"), GUIContent.none);
                    break;
                default:
                    EditorGUI.LabelField(valueRect, "Unsupported Type");
                    break;
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        entryList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
