using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道类
    /// 负责管理单个轨道的可视化显示、拖拽交互和轨道项管理
    /// 支持动画、音频、特效、事件和攻击等多种轨道类型
    /// </summary>
    public class SkillEditorTrack : VisualElement
    {
        #region 私有字段

        /// <summary>轨道显示区域的可视元素</summary>
        private VisualElement trackArea;

        /// <summary>当前轨道的类型</summary>
        private TrackType trackType;

        /// <summary>轨道中的所有轨道项列表</summary>
        private List<SkillEditorTrackItem> trackItems = new List<SkillEditorTrackItem>();

        /// <summary>当前关联的技能配置</summary>
        private FFramework.Kit.SkillConfig skillConfig;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，创建技能编辑器轨道实例
        /// 初始化轨道区域、应用样式并注册拖拽事件
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="trackType">轨道类型</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        public SkillEditorTrack(VisualElement visual, TrackType trackType, float width, FFramework.Kit.SkillConfig skillConfig)
        {
            this.trackType = trackType;
            this.skillConfig = skillConfig;

            // 创建轨道区域并应用基础样式
            trackArea = new VisualElement();
            trackArea.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
            trackArea.AddToClassList("TrackArea");

            // 根据轨道类型应用特定样式
            ApplyTrackTypeStyle();

            // 设置宽度并添加到父容器
            trackArea.style.width = width;
            visual.Add(trackArea);

            // 注册拖拽事件处理
            RegisterDragEvents();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 根据轨道类型应用对应的CSS样式类
        /// 为不同类型的轨道设置不同的视觉外观
        /// </summary>
        private void ApplyTrackTypeStyle()
        {
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
            }
        }

        /// <summary>
        /// 注册拖拽相关事件
        /// 启用轨道区域的拖拽接收功能
        /// </summary>
        private void RegisterDragEvents()
        {
            trackArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            trackArea.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        #endregion

#if UNITY_EDITOR

        #region 编辑器拖拽处理

        /// <summary>
        /// 处理拖拽更新事件
        /// 检查当前拖拽的对象是否可被当前轨道接受，并设置相应的拖拽视觉反馈
        /// </summary>
        /// <param name="evt">拖拽更新事件参数</param>
        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (CanAcceptDrag())
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        /// <summary>
        /// 处理拖拽执行事件
        /// 当用户完成拖拽操作时，在指定位置创建新的轨道项
        /// </summary>
        /// <param name="evt">拖拽执行事件参数</param>
        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!CanAcceptDrag()) return;
            DragAndDrop.AcceptDrag();

            // 计算拖拽位置对应的帧数
            float mouseX = evt.localMousePosition.x;
            float unit = SkillEditorData.FrameUnitWidth;
            int frameIndex = Mathf.RoundToInt(mouseX / unit);

            // 为每个拖拽的对象创建轨道项，直接在正确位置创建
            foreach (var obj in DragAndDrop.objectReferences)
            {
                var newItem = AddTrackItem(obj, frameIndex);
                // 轨道项已经在构造时设置了正确的位置，无需再次调用SetLeft
            }
            evt.StopPropagation();
        }

        /// <summary>
        /// 检查当前拖拽的对象是否可被轨道接受
        /// 根据轨道类型验证拖拽对象的兼容性
        /// </summary>
        /// <returns>如果可以接受拖拽返回true，否则返回false</returns>
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

        #endregion

#endif

        #region 公共方法

        /// <summary>
        /// 添加轨道项到当前轨道
        /// 支持多种资源类型：AnimationClip、AudioClip、GameObject、字符串等
        /// 根据资源类型和轨道类型自动计算持续帧数
        /// </summary>
        /// <param name="resource">要添加的资源对象</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <returns>成功创建的轨道项，失败时返回null</returns>
        public SkillEditorTrackItem AddTrackItem(object resource, int startFrame = 0)
        {
            return AddTrackItem(resource, startFrame, true);
        }

        /// <summary>
        /// 添加轨道项到当前轨道
        /// 支持多种资源类型：AnimationClip、AudioClip、GameObject、字符串等
        /// 根据资源类型和轨道类型自动计算持续帧数
        /// </summary>
        /// <param name="resource">要添加的资源对象</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="addToConfig">是否将数据添加到技能配置中，从配置加载时应设为false</param>
        /// <returns>成功创建的轨道项，失败时返回null</returns>
        public SkillEditorTrackItem AddTrackItem(object resource, int startFrame, bool addToConfig)
        {
            SkillEditorTrackItem newItem = null;
            float frameRate = GetFrameRate();

            // 动画轨道
            if (trackType == TrackType.AnimationTrack && resource is AnimationClip clip)
            {
                int frameCount = Mathf.RoundToInt(clip.length * frameRate);
                newItem = new SkillEditorTrackItem(trackArea, clip.name, trackType, frameCount, startFrame);

                // 仅在需要时添加动画数据到技能配置
                if (addToConfig)
                {
                    AddAnimationClipToConfig(clip, startFrame, frameCount, frameRate);
                }
            }
            // 音频轨道
            else if (trackType == TrackType.AudioTrack && resource is AudioClip audio)
            {
                int frameCount = Mathf.RoundToInt(audio.length * frameRate);
                newItem = new SkillEditorTrackItem(trackArea, audio.name, trackType, frameCount, startFrame);
            }
            // 特效轨道
            else if (trackType == TrackType.EffectTrack && resource is GameObject go)
            {
                float duration = GetEffectDuration(go);
                int frameCount = Mathf.RoundToInt(duration * frameRate);
                newItem = new SkillEditorTrackItem(trackArea, go.name, trackType, frameCount, startFrame);
            }
            // 事件轨道和攻击轨道
            else if ((trackType == TrackType.EventTrack || trackType == TrackType.AttackTrack) && resource is string name)
            {
                // 事件和攻击轨道项默认5帧长度
                newItem = new SkillEditorTrackItem(trackArea, name, trackType, 5, startFrame);
            }

            // 将创建的轨道项添加到列表中
            if (newItem != null)
            {
                trackItems.Add(newItem);
            }

            return newItem;
        }

        /// <summary>
        /// 更新所有轨道项的宽度
        /// 当时间轴缩放变化时，同步更新轨道中所有项的显示宽度
        /// </summary>
        public void UpdateTrackItemsWidth()
        {
            foreach (var item in trackItems)
            {
                item.SetWidth();
            }
        }

        /// <summary>
        /// 刷新所有轨道项的显示
        /// 当时间轴缩放变化时，同步更新轨道中所有项的位置和宽度显示
        /// </summary>
        public void RefreshTrackItems()
        {
            foreach (var item in trackItems)
            {
                item.RefreshDisplay();
            }
        }

        /// <summary>
        /// 设置轨道宽度
        /// 更新轨道区域的显示宽度以适应时间轴的缩放变化
        /// </summary>
        /// <param name="width">新的轨道宽度（像素）</param>
        public void SetWidth(float width)
        {
            if (trackArea != null)
            {
                trackArea.style.width = width;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 将动画片段添加到技能配置的动画轨道中
        /// </summary>
        /// <param name="animationClip">Unity动画片段</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        /// <param name="frameRate">帧率</param>
        private void AddAnimationClipToConfig(AnimationClip animationClip, int startFrame, int frameCount, float frameRate)
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

            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                UnityEditor.EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 获取当前技能配置的帧率
        /// 优先使用SkillConfig中配置的帧率，否则使用默认30fps
        /// </summary>
        /// <returns>帧率值</returns>
        private float GetFrameRate()
        {
            return (skillConfig != null && skillConfig.frameRate > 0) ? skillConfig.frameRate : 30f;
        }

        /// <summary>
        /// 获取特效GameObject的持续时间
        /// 优先检查Animation组件，其次检查ParticleSystem组件
        /// </summary>
        /// <param name="go">特效GameObject</param>
        /// <returns>特效持续时间（秒）</returns>
        private float GetEffectDuration(GameObject go)
        {
            // 优先检查Animation组件
            var animation = go.GetComponent<Animation>();
            if (animation != null && animation.clip != null)
            {
                return animation.clip.length;
            }

            // 检查ParticleSystem组件
            var particleSystem = go.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                return particleSystem.main.duration;
            }

            // 默认1秒
            return 1.0f;
        }

        #endregion
    }
}
