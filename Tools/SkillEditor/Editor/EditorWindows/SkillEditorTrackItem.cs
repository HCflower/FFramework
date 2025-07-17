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

        // 轨道项构造函数
        public SkillEditorTrackItem(VisualElement visual, string title, TrackType trackType)
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
            itemContent = TrackItemContent(title, trackType);
            trackItem.Add(itemContent);
            visual.Add(trackItem);
        }

        // 设置轨道项的宽度
        public void SetWidth(float width)
        {
            itemContent.style.width = width;
        }

        // 设置轨道项的内容
        public VisualElement TrackItemContent(string title, TrackType trackType)
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
