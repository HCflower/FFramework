using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 动画轨道实现
    /// 专门处理动画剪辑的拖拽、显示和配置管理
    /// </summary>
    public class AnimationSkillEditorTrack : BaseSkillEditorTrack
    {
        /// <summary>
        /// 动画轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public AnimationSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.AnimationTrack, width, skillConfig, trackIndex)
        { }

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的动画剪辑
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否为动画剪辑</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return obj is AnimationClip;
        }

        /// <summary>
        /// 从动画剪辑创建轨道项
        /// </summary>
        /// <param name="resource">动画剪辑资源</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is AnimationClip animationClip))
                return null;

            float frameRate = GetFrameRate();
            int frameCount = Mathf.RoundToInt(animationClip.length * frameRate);
            string itemName = animationClip.name;

            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame, trackIndex);

            // 添加到技能配置
            if (addToConfig)
            {
                AddAnimationClipToConfig(animationClip, startFrame, frameCount);
            }

            return newItem;
        }

        /// <summary>
        /// 应用动画轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 动画轨道特有的样式设置
            // 可以在这里添加动画轨道特有的视觉效果
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将动画片段添加到技能配置的动画轨道中
        /// </summary>
        /// <param name="animationClip">动画剪辑</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddAnimationClipToConfig(AnimationClip animationClip, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer?.animationTrack == null) return;

            // 创建技能配置中的动画片段数据
            var configAnimClip = new FFramework.Kit.AnimationTrack.AnimationClip
            {
                clipName = animationClip.name,
                startFrame = startFrame,
                durationFrame = frameCount,
                clip = animationClip,
                playSpeed = 1.0f,
                isLoop = false,
                applyRootMotion = false
            };

            // 添加到动画轨道
            skillConfig.trackContainer.animationTrack.animationClips.Add(configAnimClip);

            Debug.Log($"AddAnimationClipToConfig: 添加动画片段 '{animationClip.name}' 到动画轨道");

#if UNITY_EDITOR
            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
#endif
        }

        #endregion
    }
}
