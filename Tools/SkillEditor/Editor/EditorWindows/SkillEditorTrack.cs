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
        public SkillEditorTrack(VisualElement visual, TrackType trackType, float width)
        {
            this.trackType = trackType;
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
            foreach (var obj in DragAndDrop.objectReferences)
            {
                AddTrackItem(obj);
            }
            evt.StopPropagation();
        }
        /// <summary>
        /// 添加轨道项，外部和内部均可调用
        /// </summary>
        /// <param name="resource">资源对象（AnimationClip/AudioClip/GameObject等）</param>
        /// <returns>成功创建返回true，否则false</returns>
        /// <summary>
        /// 添加轨道项，支持动画、音频、特效、事件、攻击轨道
        /// </summary>
        /// <param name="resource">资源对象或名称（AnimationClip/AudioClip/GameObject/string等）</param>
        /// <returns>成功创建返回true，否则false</returns>
        public bool AddTrackItem(object resource)
        {
            SkillEditorTrackItem newItem = null;

            // 动画、音频、特效
            if ((trackType == TrackType.AnimationTrack && resource is AnimationClip)
                || (trackType == TrackType.AudioTrack && resource is AudioClip)
                || (trackType == TrackType.EffectTrack && resource is GameObject))
            {
                newItem = new SkillEditorTrackItem(trackArea, resource is Object unityObj ? unityObj.name : resource.ToString(), trackType);
            }
            // 事件轨道和攻击轨道支持字符串名称
            else if ((trackType == TrackType.EventTrack || trackType == TrackType.AttackTrack) && resource is string)
            {
                newItem = new SkillEditorTrackItem(trackArea, resource.ToString(), trackType);
            }

            if (newItem != null)
            {
                trackItems.Add(newItem);
                return true;
            }
            return false;
        }

        // 更新所有轨道项的宽度
        public void UpdateTrackItemsWidth(float itemWidth)
        {
            foreach (var item in trackItems)
            {
                item.SetWidth(itemWidth);
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
