using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 轨道项
    /// </summary>
    public class SkillEditorTrackItem : VisualElement
    {
        private VisualElement trackItem;
        private VisualElement itemContent;
        private float frameCount;             // 帧数
        private TrackType trackType;          // 轨道类型
        // 轨道项构造函数
        public SkillEditorTrackItem(VisualElement visual, string title, TrackType trackType, float frameCount)
        {
            this.frameCount = frameCount;
            this.trackType = trackType;
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
            itemContent = TrackItemContent(title);
            trackItem.Add(itemContent);
            SetPixelsPerFrame();
            visual.Add(trackItem);
        }

        // 可由SkillEditorData统一管理
        public void SetPixelsPerFrame()
        {
            itemContent.style.width = frameCount * SkillEditorData.FrameUnitWidth;
        }

        // 设置轨道项的内容
        public VisualElement TrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    itemContent.AddToClassList("TrackItem-Animation");
                    break;
                case TrackType.AudioTrack:
                    itemContent.AddToClassList("TrackItem-Audio");
                    break;
                case TrackType.EffectTrack:
                    itemContent.AddToClassList("TrackItem-Effect");
                    break;
                case TrackType.EventTrack:
                    itemContent.AddToClassList("TrackItem-Event");
                    break;
                case TrackType.AttackTrack:
                    itemContent.AddToClassList("TrackItem-Attack");
                    break;
                default:
                    break;
            }
            // 创建Title区域
            Label titleLabel = new Label();
            titleLabel.AddToClassList("TrackItemTitle");
            titleLabel.text = title;
            itemContent.Add(titleLabel);

            trackItem.Add(itemContent);
            return itemContent;
        }

        // 获取轨道项内容
        public VisualElement GetContent()
        {
            return itemContent;
        }
    }
}
