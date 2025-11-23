#if DOTWEEN_ENABLED
using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [CustomPropertyDrawer(typeof(TweenActionBase), true)]
    public class TweenActionBaseDrawer : PropertyDrawer
    {
        protected void DrawBaseGUI(Rect position, SerializedProperty property, GUIContent label, params string[] excludedPropertiesNames)
        {
            float originY = position.y;

            //Margin start.
            position.x += 10;
            position.width -= 20;
            position.y += 4;
            position.height = EditorGUIUtility.singleLineHeight;

            //Foldout.
            property.isExpanded = EditorGUI.Foldout(new Rect(position.x + 10, position.y, position.width, position.height), property.isExpanded, "", true);

            //Action name.
            Type type = property.GetTypeFromManagedFullTypeName();
            GUIContent displayName = AnimationSequencerEditorGUIUtility.GetTypeDisplayName(type);
            //-36 = ("X" button width size + foldout´s arrow width size).
            EditorGUI.LabelField(new Rect(position.x + 10, position.y, position.width - 36, position.height), displayName, EditorStyles.boldLabel);
            position.y += EditorGUIUtility.singleLineHeight;

            //Draw fields.
            if (property.isExpanded)
            {
                position.y += EditorGUIUtility.standardVerticalSpacing;
                //EditorGUI.BeginProperty(position, GUIContent.none, property);
                EditorGUI.BeginChangeCheck();

                if (property.TryGetTargetObjectOfProperty(out TweenActionBase tweenActionBase))
                {
                    string[] excludedFields = tweenActionBase.ExcludedFields;
                    if (excludedFields != null && excludedFields.Length > 0)
                    {
                        if (excludedPropertiesNames.Length > 0)
                        {
                            string[] tempPropertiesNames = new string[excludedPropertiesNames.Length + excludedFields.Length];
                            excludedPropertiesNames.CopyTo(tempPropertiesNames, 0);
                            excludedFields.CopyTo(tempPropertiesNames, excludedPropertiesNames.Length);
                            excludedPropertiesNames = tempPropertiesNames;
                        }
                        else
                        {
                            excludedPropertiesNames = excludedFields;
                        }
                    }
                }

                foreach (SerializedProperty serializedProperty in property.GetChildren())
                {
                    bool shouldDraw = true;
                    for (int i = 0; i < excludedPropertiesNames.Length; i++)
                    {
                        string excludedPropertyName = excludedPropertiesNames[i];
                        if (serializedProperty.name.Equals(excludedPropertyName, StringComparison.Ordinal))
                        {
                            shouldDraw = false;
                            break;
                        }
                    }

                    if (!shouldDraw)
                        continue;

                    if (!ShouldShowProperty(serializedProperty, property))
                        continue;

                    Rect propertyRect = position;
                    EditorGUI.PropertyField(propertyRect, serializedProperty, ModifyLabel(serializedProperty), true);

                    position.y += EditorGUI.GetPropertyHeight(serializedProperty, true) + EditorGUIUtility.standardVerticalSpacing;
                }

                //EditorGUI.EndProperty();
                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }

            //Margin end.
            position.x -= 10;
            position.width += 20;
            position.y += 4;

            //Background.
            Rect boxPosition = position;
            boxPosition.y = originY;
            boxPosition.height = position.y - originY;
            GUI.Box(boxPosition, GUIContent.none, EditorStyles.helpBox);

            //Property´s height.
            property.SetPropertyDrawerHeight(position.y - originY);
        }

        protected virtual bool ShouldShowProperty(SerializedProperty currentProperty, SerializedProperty property)
        {
            return true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawBaseGUI(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetPropertyDrawerHeight();
        }

        private GUIContent ModifyLabel(SerializedProperty property)
        {
            string label = property.displayName;
            if (label.Contains("To "))
                label = label.Replace("To ", "");

            return new GUIContent(label, property.tooltip);
        }
    }
}
#endif