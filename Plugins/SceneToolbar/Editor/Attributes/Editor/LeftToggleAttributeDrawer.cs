using UnityEditor;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar.Attributes
{
    [CustomPropertyDrawer(typeof(LeftToggleAttribute))]
    sealed class LeftToggleAttributeDrawer : PropertyDrawer
    {

        #region Overrides of PropertyDrawer

        /// <inheritdoc />
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent lbl)
        {
            if (prop.propertyType != SerializedPropertyType.Boolean)
            {
                EditorGUI.HelpBox(pos, "[LeftToggle] is only supported on booleans.", MessageType.Error);
                return;
            }

            prop.boolValue = EditorGUI.ToggleLeft(pos, lbl, prop.boolValue);
        }

        #endregion

    }
}
