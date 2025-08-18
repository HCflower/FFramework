using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 动画轨道实现
    /// 专门处理动画剪辑的拖拽、显示和配置管理
    /// </summary>
    public class AnimationSkillEditorTrack : SkillEditorTrackBase
    {
        #region 私有字段

        private List<AnimationTrackItem> animationTrackItems = new List<AnimationTrackItem>();

        #endregion

        #region 构造函数

        public AnimationSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.AnimationTrack, width, skillConfig, trackIndex)
        { }

        #endregion

        #region 抽象方法实现

        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return obj is AnimationClip;
        }

        protected override TrackItemViewBase CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is AnimationClip animationClip))
                return null;

            var animationItem = CreateAnimationTrackItem(animationClip.name, CalculateFrameCount(animationClip), startFrame, false);

            if (addToConfig)
            {
                AddAnimationClipToConfig(animationClip, startFrame, CalculateFrameCount(animationClip));
                SkillEditorEvent.TriggerRefreshRequested();
            }

            return animationItem;
        }

        protected override void ApplySpecificTrackStyle()
        {
            // 动画轨道特有的样式设置
            trackArea.AddToClassList("TrackArea-Animation");
        }

        #endregion

        #region 公共方法

        public AnimationTrackItem CreateAnimationTrackItem(string animationName, int frameCount, int startFrame, bool addToConfig = true)
        {
            var animationItem = new AnimationTrackItem(trackArea, animationName, frameCount, startFrame, trackIndex);

            animationTrackItems.Add(animationItem);
            trackItems.Add(animationItem);

            if (addToConfig)
            {
                // 外部调用者处理配置逻辑
            }

            return animationItem;
        }

        public override void RefreshTrackItems()
        {
            base.RefreshTrackItems();
        }

        public static void CreateTrackItemsFromConfig(AnimationSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig)
        {
            var animationTrack = skillConfig.trackContainer.animationTrack;
            if (animationTrack?.animationClips == null)
            {
                Debug.Log("CreateAnimationTrackItemsFromConfig: 没有动画片段数据");
                return;
            }

            foreach (var clip in animationTrack.animationClips.ToList())
            {
                if (clip.clip != null)
                {
                    var animationTrackItem = track.CreateAnimationTrackItem(clip.clipName, clip.durationFrame, clip.startFrame, false);
                    RestoreAnimationData(animationTrackItem, clip);
                }
            }
        }

        #endregion

        #region 私有方法

        private int CalculateFrameCount(AnimationClip animationClip)
        {
            return Mathf.RoundToInt(animationClip.length * GetFrameRate());
        }

        private void AddAnimationClipToConfig(AnimationClip animationClip, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer?.animationTrack == null) return;

            var configAnimClip = new FFramework.Kit.AnimationTrack.AnimationClip
            {
                clipName = animationClip.name,
                startFrame = startFrame,
                durationFrame = frameCount,
                clip = animationClip,
                animationPlaySpeed = 1.0f,
                transitionDurationFrame = 0,
                applyRootMotion = false
            };

            skillConfig.trackContainer.animationTrack.animationClips.Add(configAnimClip);

            Debug.Log($"AddAnimationClipToConfig: 添加动画片段 '{animationClip.name}' 到动画轨道");

#if UNITY_EDITOR
            EditorUtility.SetDirty(skillConfig);
#endif
        }

        private static void RestoreAnimationData(AnimationTrackItem trackItem, FFramework.Kit.AnimationTrack.AnimationClip clip)
        {
            if (trackItem?.AnimationData == null)
            {
                Debug.LogWarning("RestoreAnimationData: 动画数据为空");
                return;
            }

            var animationData = trackItem.AnimationData;
            animationData.animationClip = clip.clip;
            animationData.durationFrame = clip.durationFrame;
            animationData.animationPlaySpeed = clip.animationPlaySpeed;
            animationData.applyRootMotion = clip.applyRootMotion;

#if UNITY_EDITOR
            EditorUtility.SetDirty(animationData);
#endif
        }

        #endregion
    }
}
