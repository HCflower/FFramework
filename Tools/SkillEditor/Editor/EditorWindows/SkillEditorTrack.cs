using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 轨道
    /// </summary>
    public class SkillEditorTrack : VisualElement
    {
        private VisualElement trackArea;
        private TrackType trackType;
        private List<SkillEditorTrackItem> trackItems = new List<SkillEditorTrackItem>(); // 添加轨道项列表
        private FFramework.Kit.SkillConfig skillConfig; // 当前技能配置

        public SkillEditorTrack(VisualElement visual, TrackType trackType, float width, FFramework.Kit.SkillConfig skillConfig)
        {
            this.trackType = trackType;
            this.skillConfig = skillConfig;
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

            // 注册拖拽事件
            trackArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            trackArea.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

#if UNITY_EDITOR
        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (CanAcceptDrag())
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!CanAcceptDrag()) return;
            DragAndDrop.AcceptDrag();

            // 1. 获取鼠标在轨道区域的X坐标
            float mouseX = evt.localMousePosition.x;
            float unit = SkillEditorData.FrameUnitWidth;
            int frameIndex = Mathf.RoundToInt(mouseX / unit);

            foreach (var obj in DragAndDrop.objectReferences)
            {
                var newItem = AddTrackItem(obj);
                if (newItem != null)
                {
                    // 2. 设置轨道项起始位置并帧对齐
                    newItem.SetLeft(frameIndex * unit);
                }
            }
            evt.StopPropagation();
        }

        /// <summary>
        /// 添加轨道项，支持动画、音频、特效、事件、攻击轨道
        /// </summary>
        /// <param name="resource">资源对象或名称（AnimationClip/AudioClip/GameObject/string等）</param>
        /// <returns>新建的轨道项对象，失败返回null</returns>
        public SkillEditorTrackItem AddTrackItem(object resource)
        {
            SkillEditorTrackItem newItem = null;
            float frameRate = 30f;
            float duration = 0f;

            // 获取帧率（优先SkillConfig配置，其次默认30）
            if (skillConfig != null && skillConfig.frameRate > 0)
                frameRate = skillConfig.frameRate;

            // 动画轨道
            if (trackType == TrackType.AnimationTrack && resource is AnimationClip clip)
            {
                duration = clip.length;
                int frameCount = Mathf.RoundToInt(duration * frameRate);
                newItem = new SkillEditorTrackItem(trackArea, clip.name, trackType, frameCount);
            }
            // 音频轨道
            else if (trackType == TrackType.AudioTrack && resource is AudioClip audio)
            {
                duration = audio.length;
                int frameCount = Mathf.RoundToInt(duration * frameRate);
                newItem = new SkillEditorTrackItem(trackArea, audio.name, trackType, frameCount);
            }
            // 特效轨道（假定GameObject有Animation或ParticleSystem组件）
            else if (trackType == TrackType.EffectTrack && resource is GameObject go)
            {
                // 优先AnimationClip
                var anim = go.GetComponent<Animation>();
                if (anim != null && anim.clip != null)
                {
                    duration = anim.clip.length;
                }
                else
                {
                    var ps = go.GetComponent<ParticleSystem>();
                    if (ps != null)
                        duration = ps.main.duration;
                }
                int frameCount = Mathf.RoundToInt(duration * frameRate);
                newItem = new SkillEditorTrackItem(trackArea, go.name, trackType, frameCount);
            }
            // 事件轨道和攻击轨道支持字符串名称
            else if ((trackType == TrackType.EventTrack || trackType == TrackType.AttackTrack) && resource is string)
            {
                newItem = new SkillEditorTrackItem(trackArea, resource.ToString(), trackType, 5);
            }

            if (newItem != null)
            {
                trackItems.Add(newItem);
                return newItem;
            }
            return null;
        }

        // 更新所有轨道项的宽度
        public void UpdateTrackItemsWidth()
        {
            foreach (var item in trackItems)
            {
                item.SetWidth();
            }
        }

        private bool CanAcceptDrag()
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if ((trackType == TrackType.AnimationTrack && obj is AnimationClip) ||
                    (trackType == TrackType.AudioTrack && obj is AudioClip) ||
                    (trackType == TrackType.EffectTrack && obj is GameObject))
                    return true;
            }
            return false;
        }
#endif

        public void SetWidth(float width)
        {
            if (trackArea != null)
            {
                trackArea.style.width = width;
            }
        }
    }
}
