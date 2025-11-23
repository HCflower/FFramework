#if DOTWEEN_ENABLED
using DG.Tweening;
using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [CustomPropertyDrawer(typeof(TweenStep))]
    public class TweenStepDrawer : AnimationStepBaseDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawBaseGUI(position, property, label, "actions", "loopCount", "loopType");

            float originHeight = position.y;
            if (property.isExpanded)
            {
                if (EditorGUI.indentLevel > 0)
                    position = EditorGUI.IndentedRect(position);

                SerializedProperty flowTypeSerializedProperty = property.FindPropertyRelative("flowType");
                FlowType flowType = (FlowType)flowTypeSerializedProperty.enumValueIndex;
                if (flowType == FlowType.Join)
                {
                    EditorGUI.indentLevel++;
                    position = EditorGUI.IndentedRect(position);
                    EditorGUI.indentLevel--;
                }

                position.y += base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.BeginChangeCheck();
                SerializedProperty actionsSerializedProperty = property.FindPropertyRelative("actions");
                SerializedProperty targetSerializedProperty = property.FindPropertyRelative("target");
                SerializedProperty loopCountSerializedProperty = property.FindPropertyRelative("loopCount");
                EditorGUI.PropertyField(position, loopCountSerializedProperty);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                loopCountSerializedProperty.intValue = Mathf.Clamp(loopCountSerializedProperty.intValue, -1, int.MaxValue);
                if (loopCountSerializedProperty.intValue != 0)
                {
                    if (loopCountSerializedProperty.intValue == -1)
                    {
                        Debug.LogWarning("Infinity Loops doesn't work well with sequence, the best way of doing " +
                                         "that is setting to the int.MaxValue, will end eventually, but will take a really " +
                                         "long time, more info here: https://github.com/Demigiant/dotween/issues/92");
                        loopCountSerializedProperty.intValue = int.MaxValue;
                    }
                    SerializedProperty loopTypeSerializedProperty = property.FindPropertyRelative("loopType");
                    EditorGUI.PropertyField(position, loopTypeSerializedProperty);
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                
                position.y += EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight * 1.15f;
                float originalWidth = position.width;
                Rect actionsFoldoutPosition = position;
                actionsFoldoutPosition.x += 10;
                actionsFoldoutPosition.width = EditorGUIUtility.labelWidth - 10;
                actionsSerializedProperty.isExpanded = EditorGUI.Foldout(actionsFoldoutPosition, actionsSerializedProperty.isExpanded, "Actions", true, EditorStyles.foldout);

                position.x += EditorGUIUtility.labelWidth;
                position.width = originalWidth - EditorGUIUtility.labelWidth;
                if (GUI.Button(position, "+"))
                {
                    try
                    {
                        AnimationSequencerEditorGUIUtility.TweenActionsDropdown.Show(position, actionsSerializedProperty, targetSerializedProperty.objectReferenceValue,
                        item =>
                        {
                            if (AnimationSequencerEditorGUIUtility.TweenActionsDropdown.IsTypeAlreadyInUse(actionsSerializedProperty, item.BaseTweenActionType))
                                Debug.Log($"The '{item.name}' action already exists in this step.");
                            else
                                AddNewActionOfType(actionsSerializedProperty, item.BaseTweenActionType);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Unexpected error: {ex}");
                    }
                }
                position.x -= EditorGUIUtility.labelWidth;
                position.width = originalWidth;
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

                float normalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 112;
                if (actionsSerializedProperty.isExpanded)
                {
                    int arraySize = actionsSerializedProperty.arraySize;
                    for (int i = 0; i < arraySize; i++)
                    {
                        if (DrawDeleteActionButton(position, property, i))
                        {
                            SerializedProperty actionSerializedProperty = actionsSerializedProperty.GetArrayElementAtIndex(i);

                            bool guiEnabled = GUI.enabled;

                            if (GUI.enabled)
                            {
                                bool isValidTargetForRequiredComponent = IsValidTargetForRequiredComponent(targetSerializedProperty, actionSerializedProperty);
                                GUI.enabled = isValidTargetForRequiredComponent;
                            }

                            bool wasExpanded = actionSerializedProperty.isExpanded;
                            float heightToRest = 0;
                            EditorGUI.PropertyField(position, actionSerializedProperty);

                            // Verify only one action is expanded.
                            if (AnimationSequencerPreferences.GetInstance().OnlyOneActionExpandedWhileEditing)
                            {
                                if (actionSerializedProperty.isExpanded && !wasExpanded)
                                {
                                    for (int actionIndex = 0; actionIndex < arraySize; actionIndex++)
                                    {
                                        if (actionIndex != i)
                                        {
                                            SerializedProperty actionProperty = actionsSerializedProperty.GetArrayElementAtIndex(actionIndex);
                                            if (actionProperty.isExpanded)
                                            {
                                                if (i > actionIndex)
                                                    heightToRest = actionProperty.GetPropertyDrawerHeight() - 26;

                                                actionProperty.isExpanded = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            position.y += actionSerializedProperty.GetPropertyDrawerHeight() - heightToRest;

                            if (i < arraySize - 1)
                                position.y += EditorGUIUtility.standardVerticalSpacing;

                            GUI.enabled = guiEnabled;
                        }
                        else
                        {
                            i--;
                            arraySize--;
                        }
                    }
                }
                EditorGUIUtility.labelWidth = normalLabelWidth;

                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }
            property.SetPropertyDrawerHeight(position.y - originHeight + (property.isExpanded ? 0 : EditorGUIUtility.singleLineHeight));
        }

        private void AddNewActionOfType(SerializedProperty actionsSerializedProperty, Type targetType)
        {
            actionsSerializedProperty.arraySize++;
            SerializedProperty newElement = actionsSerializedProperty.GetArrayElementAtIndex(actionsSerializedProperty.arraySize - 1);
            newElement.managedReferenceValue = Activator.CreateInstance(targetType);

            SerializedProperty SetDirection(SerializedProperty element, SerializedProperty previousElement = null)
            {
                SerializedProperty direction = element.FindPropertyRelative("direction");
                if (direction == null) return null;

                direction.enumValueIndex = previousElement != null && AnimationSequencerDefaults.Instance.UsePreviousDirection
                    ? previousElement.FindPropertyRelative("direction").enumValueIndex
                    : (int)AnimationSequencerDefaults.Instance.Direction;

                return direction;
            }

            SerializedProperty SetEase(SerializedProperty element, SerializedProperty previousElement = null)
            {
                SerializedProperty ease = element.FindPropertyRelative("ease").FindPropertyRelative("ease");
                if (ease == null) return null;

                if (previousElement != null && AnimationSequencerDefaults.Instance.UsePreviousEase)
                {
                    SerializedProperty previousEase = previousElement.FindPropertyRelative("ease").FindPropertyRelative("ease");
                    ease.enumValueIndex = previousEase.enumValueIndex;

                    if (ease.enumValueIndex == (int)Ease.INTERNAL_Custom)
                    {
                        SerializedProperty previousCurve = previousElement.FindPropertyRelative("ease").FindPropertyRelative("curve");
                        element.FindPropertyRelative("ease").FindPropertyRelative("curve").animationCurveValue = previousCurve.animationCurveValue;
                    }
                }
                else
                {
                    ease.enumValueIndex = (int)AnimationSequencerDefaults.Instance.Ease.Ease;
                }

                return ease;
            }

            if (actionsSerializedProperty.arraySize > 1)
            {
                SerializedProperty previousElement = actionsSerializedProperty.GetArrayElementAtIndex(actionsSerializedProperty.arraySize - 2);
                SetDirection(newElement, previousElement);
                SetEase(newElement, previousElement);
            }
            else
            {
                SetDirection(newElement);
                SetEase(newElement);
            }

            actionsSerializedProperty.isExpanded = true;
            if (AnimationSequencerPreferences.GetInstance().OnlyOneActionExpandedWhileEditing)
            {
                int actionsCount = actionsSerializedProperty.arraySize;
                for (int i = 0; i < actionsCount - 1; i++)
                {
                    actionsSerializedProperty.GetArrayElementAtIndex(i).isExpanded = false;
                }
            }
            newElement.isExpanded = true;
            actionsSerializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private static bool IsValidTargetForRequiredComponent(SerializedProperty targetSerializedProperty, SerializedProperty actionSerializedProperty)
        {
            if (targetSerializedProperty.objectReferenceValue == null)
                return false;

            Type type = actionSerializedProperty.GetTypeFromManagedFullTypeName();
            return AnimationSequencerEditorGUIUtility.CanActionBeAppliedToTarget(type, targetSerializedProperty.objectReferenceValue as GameObject);
        }

        private bool DrawDeleteActionButton(Rect position, SerializedProperty property, int targetIndex)
        {
            Rect buttonPosition = position;
            buttonPosition.width = 24;
            buttonPosition.x += position.width - 34;
            buttonPosition.y += 4;

            if (GUI.Button(buttonPosition, "X", EditorStyles.miniButton))
            {
                DeleteElementAtIndex(property, targetIndex);
                return false;
            }

            return true;
        }

        private void DeleteElementAtIndex(SerializedProperty serializedProperty, int targetIndex)
        {
            SerializedProperty actionsPropertyPath = serializedProperty.FindPropertyRelative("actions");
            actionsPropertyPath.DeleteArrayElementAtIndex(targetIndex);
            SerializedPropertyExtensions.ClearPropertyCache(actionsPropertyPath.propertyPath);
            //actionsPropertyPath.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetPropertyDrawerHeight();
        }
    }
}
#endif