using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 轨道
    /// </summary>
    public class SkillEditorTrack : VisualElement
    {
        private VisualElement trackArea;

        public SkillEditorTrack(VisualElement visual, TrackType trackType, float width)
        {
            trackArea = new VisualElement();
            trackArea.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
            trackArea.AddToClassList("TrackArea");
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    trackArea.AddToClassList("TrackArea-Animation");
                    break;
                case TrackType.AudioTrack:
                    trackArea.AddToClassList("TrackArea-Audio");
                    break;
                case TrackType.EffectTrack:
                    trackArea.AddToClassList("TrackArea-Effect");
                    break;
                case TrackType.EventTrack:
                    trackArea.AddToClassList("TrackArea-Event");
                    break;
                case TrackType.AttackTrack:
                    trackArea.AddToClassList("TrackArea-Attack");
                    break;
                default:
                    break;
            }
            trackArea.style.width = width;
            visual.Add(trackArea);
        }

        public void SetWidth(float width)
        {
            if (trackArea != null)
            {
                trackArea.style.width = width;
            }
        }
    }
}
