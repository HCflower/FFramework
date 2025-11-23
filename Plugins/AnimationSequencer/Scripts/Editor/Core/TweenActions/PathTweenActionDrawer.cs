#if DOTWEEN_ENABLED
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [CustomPropertyDrawer(typeof(PathTweenAction), true)]
    public class PathTweenActionDrawer : TweenActionBaseDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawBaseGUI(position, property, label, "direction");
        }

        protected override bool ShouldShowProperty(SerializedProperty currentProperty, SerializedProperty property)
        {
            if (currentProperty.name == "positions")
                return IsInputTypeSelected(property, DataInputType.Vector);

            if (currentProperty.name == "targets")
                return IsInputTypeSelected(property, DataInputType.Object);

            return base.ShouldShowProperty(currentProperty, property);
        }

        private bool IsInputTypeSelected(SerializedProperty property, DataInputType inputType)
        {
            SerializedProperty inputTypeSerializedProperty = property.FindPropertyRelative("inputType");
            DataInputType selectedInputType = (DataInputType)inputTypeSerializedProperty.enumValueIndex;

            return selectedInputType == inputType;
        }
    }
}
#endif