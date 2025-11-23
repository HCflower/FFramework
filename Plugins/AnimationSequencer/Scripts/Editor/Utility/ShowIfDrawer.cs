using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
                return EditorGUI.GetPropertyHeight(property, label, true);
            else
                return -EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
#if !UNITY_2021_1_OR_NEWER
                ProcessMinAttribute(property);
#endif
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private bool ShouldShow(SerializedProperty property)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;

            string[] conditionParts = showIf.Condition.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (conditionParts.Length == 0)
            {
                Debug.LogWarning("'ShowIf' attribute requires a property name or an expression to evaluate.");
            }
            else if (conditionParts.Length == 1)
            {
                try
                {
                    return EvaluateConditionWithDefaults(property, conditionParts[0], showIf.ComparisonValue);
                }
                catch (Exception)
                {
                    Debug.LogWarning($"In 'ShowIf' attribute, the condition could not be evaluated. " +
                        $"Please ensure the format is correct for interpretation.");
                }
            }
            else
            {
                try
                {
                    return EvaluateExpression(property, conditionParts);
                }
                catch (Exception)
                {
                    Debug.LogWarning($"In 'ShowIf' attribute, the expression could not be evaluated. " +
                        $"Please verify the format is correct for interpretation.");
                }
            }

            return true;
        }

        private bool EvaluateConditionWithDefaults(SerializedProperty property, string propertyName, object comparisonValue, string operatorString = "")
        {
            SerializedProperty conditionProperty = GetConditionProperty(property, propertyName);
            if (conditionProperty == null)
            {
                Debug.LogWarning($"In 'ShowIf' attribute, the property '{propertyName}' could not be found. " +
                    $"Please ensure the name is correct or check the expression format.");
                return true;
            }

            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    if (string.IsNullOrEmpty(operatorString))
                        operatorString = "==";
                    if (comparisonValue == null)
                        comparisonValue = true;
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (string.IsNullOrEmpty(operatorString))
                        operatorString = "!=";
                    break;
                case SerializedPropertyType.Enum:
                    if (string.IsNullOrEmpty(operatorString))
                        operatorString = "==";
                    if (comparisonValue == null)
                    {
                        Debug.LogWarning("'ShowIf' attribute requires a comparison value for enum types.");
                        return true;
                    }

                    //if ((comparisonValue is Enum) == false)
                    //{
                    //    Debug.LogWarning("In 'ShowIf' attribute, the comparison value must be of enum type.");
                    //    return true;
                    //}

                    if (!conditionProperty.enumNames.Contains(comparisonValue.ToString()))
                    {
                        Debug.LogWarning($"In 'ShowIf' attribute, the comparison value '{comparisonValue}' does not match the expected enum type for the condition.");
                        return true;
                    }
                    break;
            }

            return EvaluateCondition(conditionProperty, operatorString, comparisonValue);
        }

        private SerializedProperty GetConditionProperty(SerializedProperty property, string propertyName)
        {
            // Check if the property is nested
            string propertyPath = property.propertyPath;
            string fullPath = propertyPath.Replace(property.name, propertyName);

            return property.serializedObject.FindProperty(fullPath);
        }

        private bool EvaluateCondition(SerializedProperty conditionProperty, string operatorString, object comparisonValue)
        {
            if ((operatorString == "==" || operatorString == "!=") == false)
                throw new Exception("Invalid operator used.");

            bool isEqualityOperator = operatorString == "==";

            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return conditionProperty.boolValue == (bool)comparisonValue == isEqualityOperator;
                case SerializedPropertyType.ObjectReference:
                    bool isConditionPropertyNull = conditionProperty.objectReferenceValue == null;
                    return isConditionPropertyNull == isEqualityOperator;
                case SerializedPropertyType.Enum:
                    //Enum enumValue = GetEnumValue(conditionProperty);
                    //return enumValue.Equals(comparisonValue) == isEqualityOperator;
                    return conditionProperty.enumNames[conditionProperty.enumValueIndex].Equals(comparisonValue.ToString()) == isEqualityOperator;
                default:
                    return true;
            }
        }

        private Enum GetEnumValue(SerializedProperty property)
        {
            Type enumType = GetPropertyType(property);
            return (Enum)Enum.GetValues(enumType).GetValue(property.enumValueIndex);
        }

        private Type GetPropertyType(SerializedProperty property)
        {
            object targetObject = property.serializedObject.targetObject;
            Type targetType = targetObject.GetType();
            FieldInfo field = targetType.GetField(property.propertyPath, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return field.FieldType;
        }

        private bool EvaluateExpression(SerializedProperty property, string[] conditionParts)
        {
            bool result = false;
            string currentOperator = "";

            for (int i = 0; i < conditionParts.Length; i++)
            {
                // If we find a logical operator ("&&" or "||"), store it and move to the next expression.
                if (conditionParts[i] == "&&" || conditionParts[i] == "||")
                {
                    currentOperator = conditionParts[i];
                    continue;
                }

                bool currentCondition;
                // Evaluate conditions with equality operators ("==" or "!=").
                if (i + 2 < conditionParts.Length && (conditionParts[i + 1] == "==" || conditionParts[i + 1] == "!="))
                {
                    // Get the property name, operator, and comparison value.
                    string propertyName = conditionParts[i];
                    string operatorString = conditionParts[i + 1];
                    string comparisonValue = conditionParts[i + 2];

                    // Convert the comparison value if necessary (bool, enum, etc.)
                    object comparisonValueConverted = ParseComparisonValue(property, propertyName, comparisonValue);

                    // Evaluate the condition.
                    currentCondition = EvaluateConditionWithDefaults(property, propertyName, comparisonValueConverted, operatorString);

                    // If it's the first condition, store it in the result.
                    if (i == 0)
                        result = currentCondition;

                    // Skip the operator and comparison value.
                    i += 2;
                }
                else
                {
                    // Evaluate conditions without a comparison value.
                    string propertyName = conditionParts[i];
                    currentCondition = EvaluateConditionWithDefaults(property, propertyName, null);
                }

                // Apply the logical operator ("&&" or "||").
                if (i == 0)
                {
                    result = currentCondition;
                }
                else
                {
                    if (currentOperator == "&&")
                        result = result && currentCondition;
                    else if (currentOperator == "||")
                        result = result || currentCondition;
                }
            }

            return result;
        }

        private object ParseComparisonValue(SerializedProperty property, string propertyName, string comparisonValue)
        {
            // Find the property to evaluate.
            SerializedProperty conditionProperty = GetConditionProperty(property, propertyName);
            if (conditionProperty == null)
                return comparisonValue;

            // Convert the comparison value based on the property type.
            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return bool.Parse(comparisonValue);
                case SerializedPropertyType.Enum:
                    // If the comparisonValue is in format [EnumType].[EnumValue], only take [EnumValue].
                    if (comparisonValue.Contains("."))
                        comparisonValue = comparisonValue.Split('.')[1];

                    return comparisonValue;

                    //var targetObject = conditionProperty.serializedObject.targetObject;
                    //var targetType = targetObject.GetType();

                    //// Find the corresponding field for the enum property.
                    //var enumField = targetType.GetField(conditionProperty.name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    //if (enumField != null)
                    //{
                    //    var enumType = enumField.FieldType;

                    //    // Parse the comparison value to the enum type.
                    //    return Enum.Parse(enumType, comparisonValue);
                    //}
                    //else
                    //{
                    //    return comparisonValue;
                    //}
                default:
                    // Handle other types like int, float, strings, etc.
                    return comparisonValue;
            }
        }

        private void ProcessMinAttribute(SerializedProperty property)
        {
            MinAttribute minAttribute = GetMinAttribute();
            if (minAttribute != null)
            {
                if (property.propertyType == SerializedPropertyType.Float)
                    property.floatValue = Mathf.Max(property.floatValue, minAttribute.min);
                else if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = Mathf.Max(property.intValue, (int)minAttribute.min);
            }
        }

        private MinAttribute GetMinAttribute()
        {
            var minAttributes = fieldInfo.GetCustomAttributes(typeof(MinAttribute), false);
            if (minAttributes.Length > 0)
                return (MinAttribute)minAttributes[0];

            return null;
        }
    }
}
