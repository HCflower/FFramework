#if DOTWEEN_ENABLED
using UnityEditor;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [CustomPropertyDrawer(typeof(RT_AnchoredPositionTweenAction), true)]
    public class RT_AnchoredPositionTweenActionDrawer : TweenActionBaseDrawer
    {
        protected override bool ShouldShowProperty(SerializedProperty currentProperty, SerializedProperty property)
        {
            if (currentProperty.name == "toAnchorPosition")
            {
                SerializedProperty inputTypeSerializedProperty = property.FindPropertyRelative("toInputType");
                DataInputTypeWithAnchor inputType = (DataInputTypeWithAnchor)inputTypeSerializedProperty.enumValueIndex;

                return inputType == DataInputTypeWithAnchor.Anchor;
            }

            return base.ShouldShowProperty(currentProperty, property);
        }
    }
}
#endif