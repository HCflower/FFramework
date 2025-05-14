using UnityEditor;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar.Attributes
{
    [CustomPropertyDrawer(typeof(SceneDropdownAttribute))]
    sealed class SceneDropdownAttributeDrawer : PropertyDrawer
    {

        #region Overrides of PropertyDrawer

        /// <inheritdoc />
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent lbl)
        {
            if (prop.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(pos, lbl.text, "Use [SceneDropdown] with strings.");
                return;
            }

            string path = prop.stringValue;
            EditorUtilities.DrawSceneFieldWithDropdown(pos, lbl, path, OnSelection);

            void OnSelection(string newPath)
            {
                prop.stringValue = newPath;
                prop.serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion

    }
}
