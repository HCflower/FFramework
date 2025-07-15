using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 轨道
    /// </summary>
    public class SkillEditorTrack : VisualElement
    {
        public SkillEditorTrack(VisualElement visual, float width)
        {
            VisualElement trackArea = new VisualElement();
            trackArea.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
            trackArea.AddToClassList("TrackArea");
            trackArea.style.width = width;
            visual.Add(trackArea);
        }
    }
}
