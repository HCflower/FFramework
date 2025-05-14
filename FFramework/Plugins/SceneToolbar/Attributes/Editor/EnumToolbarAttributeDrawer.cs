using UnityEditor;
using UnityEngine;

namespace Plugins.Neonalig.SceneToolbar.Attributes
{
    [CustomPropertyDrawer(typeof(EnumToolbarAttribute))]
    sealed class EnumToolbarAttributeDrawer : PropertyDrawer
    {

        #region Overrides of PropertyDrawer

        /// <inheritdoc />
        public override bool CanCacheInspectorGUI(SerializedProperty property) => true;

        /// <inheritdoc />
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent lbl)
        {
            lbl = EditorGUI.BeginProperty(pos, lbl, prop);
            pos = EditorGUI.PrefixLabel(pos, lbl);
            EditorGUI.BeginChangeCheck();
            int newValue = GUI.Toolbar(pos, prop.enumValueIndex, prop.enumDisplayNames);
            if (EditorGUI.EndChangeCheck())
            {
                prop.enumValueIndex = newValue;
            }
            EditorGUI.EndProperty();
        }

        #endregion

    }
}
