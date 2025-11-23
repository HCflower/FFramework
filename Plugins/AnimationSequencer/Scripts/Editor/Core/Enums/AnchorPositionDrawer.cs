#if DOTWEEN_ENABLED
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [CustomPropertyDrawer(typeof(AnchorPosition))]
    public class AnchorPositionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            int enumValueIndex = property.enumValueIndex;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Calculate the size of the grid (limited to the available width).
            float gridSize = Mathf.Min(position.width - EditorGUIUtility.labelWidth, lineHeight * 3f);
            float buttonSize = gridSize / 3f;

            // Draw field name.
            Rect labeRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
            EditorGUI.LabelField(labeRect, label);

            // Draw enum dropdown.
            Rect enumRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y + lineHeight * 3,
                position.width - EditorGUIUtility.labelWidth, lineHeight);
            EditorGUI.PropertyField(enumRect, property, new GUIContent());

            // Draw grid.
            Rect gridRect = new Rect(position.x + EditorGUIUtility.labelWidth + (enumRect.width - gridSize) * 0.5f, position.y, gridSize, gridSize);

            for (int i = 0; i < System.Enum.GetValues(typeof(AnchorPosition)).Length; i++)
            {
                int row = i / 3;
                int col = i % 3;

                int index = i;
                bool isSelected = index == enumValueIndex;

                if (isSelected)
                    GUI.backgroundColor = Color.green;

                Rect buttonRect = new Rect(gridRect.x + col * buttonSize, gridRect.y + row * buttonSize, buttonSize - 2, buttonSize - 2);
                if (GUI.Button(buttonRect, GUIContent.none))
                {
                    property.enumValueIndex = index;
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Enough height for the enum and the grid.
            return EditorGUIUtility.singleLineHeight * 4f;
        }
    }
}
#endif