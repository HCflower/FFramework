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
        // 拖拽相关
        private bool isDragging = false;
        private Vector2 dragStartPos;
        private float originalLeft;

        // 轨道项构造函数
        public SkillEditorTrackItem(VisualElement visual, string title, TrackType trackType, float frameCount)
        {
            this.frameCount = frameCount;
            this.trackType = trackType;
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
            itemContent = TrackItemContent(title);
            trackItem.Add(itemContent);
            SetWidth();
            visual.Add(trackItem);

            // 添加拖拽事件
            trackItem.RegisterCallback<PointerDownEvent>(OnPointerDown);
            trackItem.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            trackItem.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        // 设置轨道项起始位置
        public void SetLeft(float left)
        {
            trackItem.style.left = left;
        }

        public void SetWidth()
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
            return itemContent;
        }

        // 拖拽事件处理
        private void OnPointerDown(PointerDownEvent evt)
        {
            isDragging = true;
            dragStartPos = evt.position;
            originalLeft = trackItem.style.left.value.value;
            trackItem.CapturePointer(evt.pointerId);

            // 根据轨道类型创建并选中不同类型的数据对象，显示自定义Inspector
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    var animData = ScriptableObject.CreateInstance<AnimationTrackItemData>();
                    animData.trackName = (itemContent.Q<Label>("TrackItemTitle")?.text) ?? "";
                    animData.frameCount = frameCount;
                    // 补充动画相关参数
                    // 假设你有动画片段和循环参数可用
                    // animData.animationClip = ...;
                    // animData.isLoop = ...;
                    UnityEditor.Selection.activeObject = animData;
                    break;
                // 可扩展其它类型轨道项
                default:
                    var baseData = ScriptableObject.CreateInstance<AnimationTrackItemData>();
                    baseData.trackName = (itemContent.Q<Label>("TrackItemTitle")?.text) ?? "";
                    baseData.frameCount = frameCount;
                    UnityEditor.Selection.activeObject = baseData;
                    break;
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging) return;
            float deltaX = evt.position.x - dragStartPos.x;
            float newLeft = originalLeft + deltaX;
            // 对齐刻度
            float unit = SkillEditorData.FrameUnitWidth;
            newLeft = Mathf.Round(newLeft / unit) * unit;

            // 限制拖拽范围不超过轨道长度
            if (trackItem.parent != null)
            {
                float trackWidth = trackItem.parent.resolvedStyle.width;
                float itemWidth = trackItem.resolvedStyle.width;
                newLeft = Mathf.Clamp(newLeft, 0, trackWidth - itemWidth);
            }
            trackItem.style.left = newLeft;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging) return;
            isDragging = false;
            trackItem.ReleasePointer(evt.pointerId);
        }
    }
}
