#if DOTWEEN_ENABLED
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    public static class EditorGUIExtra
    {
        /// <summary>
        /// Makes a progress bar with a custom fill value.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="progressStart"></param>
        /// <param name="progressEnd"></param>
        /// <param name="text"></param>
        public static void ProgressBar(Rect position, float progressStart, float progressEnd, string text)
        {
            //Clamp values.
            progressStart = Mathf.Clamp01(progressStart);
            progressEnd = Mathf.Clamp01(progressEnd);

            //Define styles.
            GUIStyle backgroundStyle = "ProgressBarBack";
            GUIStyle fillStyle = "ProgressBarBar";
            GUIStyle textStyle = "ProgressBarText";

            //Draw the background of the progress bar.
            GUI.Box(position, GUIContent.none, backgroundStyle);

            //Draw the fill of the progress bar.
            float fillWidth = position.width * (progressEnd - progressStart);
            Rect fillRect = new Rect(position.x + position.width * progressStart, position.y, fillWidth, position.height);
            GUI.Box(fillRect, GUIContent.none, fillStyle);

            //Draw the text in the center.
            EditorGUI.LabelField(position, text, textStyle);
        }

        /// <summary>
        /// Makes a progress bar with a custom fill value and labels.
        /// </summary>
        /// <param name="position">The minimum height depends on the labels, if both top and bottom labels are provided, the minimum size should be 36 otherwise 18.</param>
        /// <param name="progressStart"></param>
        /// <param name="progressEnd"></param>
        /// <param name="text"></param>
        /// <param name="labelStart"></param>
        /// <param name="labelEnd"></param>
        /// <param name="labelProgressStart"></param>
        /// <param name="labelProgressEnd"></param>
        public static void ProgressBar(Rect position, float progressStart, float progressEnd, string text, 
            string labelStart = null, string labelEnd = null, string labelProgressStart = null, string labelProgressEnd = null)
        {
            if (labelStart == null && labelEnd == null && labelProgressStart == null && labelProgressEnd == null)
            {
                ProgressBar(position, progressStart, progressEnd, text);
                return;
            }

            //Define label´s size.
            Vector2 labelSize = new Vector2(100, EditorGUIUtility.singleLineHeight);

            //Clamp values.
            bool hasTopAndBottomLabels = (labelStart != null || labelEnd != null) && (labelProgressStart != null || labelProgressEnd != null);
            Rect tempPos = position;
            tempPos.height = Mathf.Clamp(tempPos.height, labelSize.y * (hasTopAndBottomLabels ? 2 : 1), Mathf.Infinity);
            position = tempPos;
            float barHeight = tempPos.height - labelSize.y * (hasTopAndBottomLabels ? 2 : 1);

            //Define styles.
            GUIStyle textStyle = "ProgressBarText";

            ////Draw top labels.
            float middleLabelSize = labelSize.x / 2;
            float drawPosition_Y = position.y;

            //Draw top labels.
            if (labelProgressStart != null)
                EditorGUI.LabelField(new Rect(new Vector2(position.x + position.width * progressStart - middleLabelSize, drawPosition_Y), labelSize), labelProgressStart, textStyle);
            if (labelProgressEnd != null)
            {
                float fillWidth = position.width * (progressEnd - progressStart);
                EditorGUI.LabelField(new Rect(new Vector2(position.x + position.width * progressStart + fillWidth - middleLabelSize, drawPosition_Y), labelSize),
                    labelProgressEnd, textStyle);
            }
            if (labelStart != null || labelEnd != null)
                drawPosition_Y += labelSize.y;

            //Draw progress bar.
            ProgressBar(new Rect(position.x, drawPosition_Y, position.width, barHeight), progressStart, progressEnd, text);
            drawPosition_Y += barHeight;

            //Draw bottom labels.
            if (labelStart != null)
                EditorGUI.LabelField(new Rect(new Vector2(position.x - middleLabelSize, drawPosition_Y), labelSize), labelStart, textStyle);
            if (labelEnd != null)
                EditorGUI.LabelField(new Rect(new Vector2(position.x + position.width - middleLabelSize, drawPosition_Y), labelSize), labelEnd, textStyle);
        }
    }
}
#endif