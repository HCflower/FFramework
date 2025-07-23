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

        /// <summary>轨道索引，用于多轨道类型的数据映射</summary>
        private int trackIndex;

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
        /// <param name="trackIndex">轨道索引，用于多轨道数据映射</param>
        public SkillEditorTrack(VisualElement visual, TrackType trackType, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
        {
            this.trackType = trackType;
            this.skillConfig = skillConfig;
            this.trackIndex = trackIndex;

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
        /// 添加轨道项到当前轨道（支持指定名称）
        /// </summary>
        /// <param name="resource">要添加的资源对象</param>
        /// <param name="itemName">自定义轨道项名称</param>
        /// <param name="startFrame">轨道项的起始帧位置</param>
        /// <param name="addToConfig">是否将数据添加到技能配置中</param>
        /// <returns>成功创建的轨道项，失败时返回null</returns>
        public SkillEditorTrackItem AddTrackItem(object resource, string itemName, int startFrame, bool addToConfig)
        {
            SkillEditorTrackItem newItem = null;
            float frameRate = GetFrameRate();

            // 音频轨道
            if (trackType == TrackType.AudioTrack && resource is AudioClip audio)
            {
                int frameCount = Mathf.RoundToInt(audio.length * frameRate);
                newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame);

                // 获取轨道项数据并设置音频属性
                if (newItem.ItemData is AudioTrackItemData audioData)
                {
                    audioData.audioClip = audio;
                    audioData.volume = 1.0f;
                    audioData.pitch = 1.0f;
                    audioData.isLoop = false;

                    // 标记数据已修改
                    UnityEditor.EditorUtility.SetDirty(audioData);
                }

                // 仅在需要时添加音频数据到技能配置
                if (addToConfig)
                {
                    AddAudioClipToConfig(audio, itemName, startFrame, frameCount);
                }
            }

            // 将轨道项添加到轨道容器
            if (newItem != null)
            {
                trackItems.Add(newItem);
            }

            return newItem;
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
                    AddAnimationClipToConfig(clip, startFrame, frameCount);
                }
            }
            // 音频轨道
            else if (trackType == TrackType.AudioTrack && resource is AudioClip audio)
            {
                int frameCount = Mathf.RoundToInt(audio.length * frameRate);
                string itemName = audio.name;
                newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame);

                // 获取轨道项数据并设置音频属性
                if (newItem.ItemData is AudioTrackItemData audioData)
                {
                    audioData.audioClip = audio;
                    audioData.volume = 1.0f;
                    audioData.pitch = 1.0f;
                    audioData.isLoop = false;

                    // 标记数据已修改
                    UnityEditor.EditorUtility.SetDirty(audioData);
                }

                // 仅在需要时添加音频数据到技能配置
                if (addToConfig)
                {
                    AddAudioClipToConfig(audio, itemName, startFrame, frameCount);
                }
            }
            // 特效轨道
            else if (trackType == TrackType.EffectTrack && resource is GameObject go)
            {
                float duration = GetEffectDuration(go);
                int frameCount = Mathf.RoundToInt(duration * frameRate);
                string itemName = go.name;
                newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame);

                // 仅在需要时添加特效数据到技能配置
                if (addToConfig)
                {
                    AddEffectToConfig(go, itemName, startFrame, frameCount);
                }
            }
            // 事件轨道和攻击轨道
            else if ((trackType == TrackType.EventTrack || trackType == TrackType.AttackTrack) && resource is string name)
            {
                // 事件和攻击轨道项默认5帧长度
                newItem = new SkillEditorTrackItem(trackArea, name, trackType, 5, startFrame);

                // 仅在需要时添加数据到技能配置
                if (addToConfig)
                {
                    if (trackType == TrackType.EventTrack)
                    {
                        AddEventToConfig(name, startFrame, 5);
                    }
                    else if (trackType == TrackType.AttackTrack)
                    {
                        AddInjuryDetectionToConfig(name, startFrame, 5);
                    }
                }
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

            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                UnityEditor.EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 将音频片段添加到技能配置的音频轨道中
        /// </summary>
        /// <param name="audioClip">Unity音频片段</param>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        /// <param name="frameRate">帧率</param>
        private void AddAudioClipToConfig(AudioClip audioClip, string itemName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保音频轨道列表存在
            if (skillConfig.trackContainer.audioTracks == null)
            {
                skillConfig.trackContainer.audioTracks = new System.Collections.Generic.List<FFramework.Kit.AudioTrack>();
            }

            // 确保有足够的轨道数据，如果不足则创建
            while (skillConfig.trackContainer.audioTracks.Count <= trackIndex)
            {
                var newAudioTrack = new FFramework.Kit.AudioTrack();
                newAudioTrack.trackName = $"Audio Track {skillConfig.trackContainer.audioTracks.Count + 1}";
                newAudioTrack.audioClips = new System.Collections.Generic.List<FFramework.Kit.AudioTrack.AudioClip>();
                skillConfig.trackContainer.audioTracks.Add(newAudioTrack);
            }

            // 获取对应索引的音频轨道
            var audioTrack = skillConfig.trackContainer.audioTracks[trackIndex];

            // 确保音频片段列表存在
            if (audioTrack.audioClips == null)
            {
                audioTrack.audioClips = new System.Collections.Generic.List<FFramework.Kit.AudioTrack.AudioClip>();
            }

            // 创建技能配置中的音频片段数据
            var configAudioClip = new FFramework.Kit.AudioTrack.AudioClip
            {
                clipName = itemName,
                startFrame = startFrame,
                durationFrame = frameCount,
                clip = audioClip,
                volume = 1.0f,
                pitch = 1.0f,
                isLoop = false
            };

            // 添加到对应索引的音频轨道
            audioTrack.audioClips.Add(configAudioClip);

            Debug.Log($"AddAudioClipToConfig: 添加音频片段 '{itemName}' 到轨道索引 {trackIndex}");

            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                UnityEditor.EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 将特效对象添加到技能配置的特效轨道中
        /// </summary>
        /// <param name="effectGameObject">特效GameObject</param>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddEffectToConfig(GameObject effectGameObject, string itemName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保特效轨道列表存在
            if (skillConfig.trackContainer.effectTracks == null)
            {
                skillConfig.trackContainer.effectTracks = new System.Collections.Generic.List<FFramework.Kit.EffectTrack>();
            }

            // 确保有足够的轨道数据，如果不足则创建
            while (skillConfig.trackContainer.effectTracks.Count <= trackIndex)
            {
                var newEffectTrack = new FFramework.Kit.EffectTrack();
                newEffectTrack.trackName = $"Effect Track {skillConfig.trackContainer.effectTracks.Count + 1}";
                newEffectTrack.effectClips = new System.Collections.Generic.List<FFramework.Kit.EffectTrack.EffectClip>();
                skillConfig.trackContainer.effectTracks.Add(newEffectTrack);
            }

            // 获取对应索引的特效轨道
            var effectTrack = skillConfig.trackContainer.effectTracks[trackIndex];

            // 确保特效片段列表存在
            if (effectTrack.effectClips == null)
            {
                effectTrack.effectClips = new System.Collections.Generic.List<FFramework.Kit.EffectTrack.EffectClip>();
            }

            // 创建技能配置中的特效片段数据
            var configEffectClip = new FFramework.Kit.EffectTrack.EffectClip
            {
                clipName = itemName,
                startFrame = startFrame,
                durationFrame = frameCount,
                effectPrefab = effectGameObject,
                scale = UnityEngine.Vector3.one,
                rotation = UnityEngine.Vector3.zero,
                position = UnityEngine.Vector3.zero
            };

            // 添加到对应索引的特效轨道
            effectTrack.effectClips.Add(configEffectClip);

            Debug.Log($"AddEffectToConfig: 添加特效 '{itemName}' 到轨道索引 {trackIndex}");

            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                UnityEditor.EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 将事件添加到技能配置的事件轨道中
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddEventToConfig(string eventName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保事件轨道列表存在
            if (skillConfig.trackContainer.eventTracks == null)
            {
                skillConfig.trackContainer.eventTracks = new System.Collections.Generic.List<FFramework.Kit.EventTrack>();
            }

            // 确保有足够的轨道数据，如果不足则创建
            while (skillConfig.trackContainer.eventTracks.Count <= trackIndex)
            {
                var newEventTrack = new FFramework.Kit.EventTrack();
                newEventTrack.trackName = $"Event Track {skillConfig.trackContainer.eventTracks.Count + 1}";
                newEventTrack.eventClips = new System.Collections.Generic.List<FFramework.Kit.EventTrack.EventClip>();
                skillConfig.trackContainer.eventTracks.Add(newEventTrack);
            }

            // 获取对应索引的事件轨道
            var eventTrack = skillConfig.trackContainer.eventTracks[trackIndex];

            // 确保事件片段列表存在
            if (eventTrack.eventClips == null)
            {
                eventTrack.eventClips = new System.Collections.Generic.List<FFramework.Kit.EventTrack.EventClip>();
            }

            // 创建技能配置中的事件片段数据
            var configEventClip = new FFramework.Kit.EventTrack.EventClip
            {
                clipName = eventName,
                startFrame = startFrame,
                durationFrame = frameCount,
                eventType = eventName,
                eventParameters = ""
            };

            // 添加到对应索引的事件轨道
            eventTrack.eventClips.Add(configEventClip);

            Debug.Log($"AddEventToConfig: 添加事件 '{eventName}' 到轨道索引 {trackIndex}");

            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                UnityEditor.EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 将伤害检测添加到技能配置的伤害检测轨道中
        /// </summary>
        /// <param name="injuryDetectionName">伤害检测名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddInjuryDetectionToConfig(string injuryDetectionName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保伤害检测轨道列表存在
            if (skillConfig.trackContainer.injuryDetectionTracks == null)
            {
                skillConfig.trackContainer.injuryDetectionTracks = new System.Collections.Generic.List<FFramework.Kit.InjuryDetectionTrack>();
            }

            // 确保有足够的轨道数据，如果不足则创建
            while (skillConfig.trackContainer.injuryDetectionTracks.Count <= trackIndex)
            {
                var newInjuryDetectionTrack = new FFramework.Kit.InjuryDetectionTrack();
                newInjuryDetectionTrack.trackName = $"Injury Detection Track {skillConfig.trackContainer.injuryDetectionTracks.Count + 1}";
                newInjuryDetectionTrack.injuryDetectionClips = new System.Collections.Generic.List<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip>();
                skillConfig.trackContainer.injuryDetectionTracks.Add(newInjuryDetectionTrack);
            }

            // 获取对应索引的伤害检测轨道
            var injuryDetectionTrack = skillConfig.trackContainer.injuryDetectionTracks[trackIndex];

            // 确保伤害检测片段列表存在
            if (injuryDetectionTrack.injuryDetectionClips == null)
            {
                injuryDetectionTrack.injuryDetectionClips = new System.Collections.Generic.List<FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip>();
            }

            // 创建技能配置中的伤害检测片段数据
            var configInjuryDetectionClip = new FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip
            {
                clipName = injuryDetectionName,
                startFrame = startFrame,
                durationFrame = frameCount,
                targetLayers = -1,
                isMultiInjuryDetection = false,
                multiInjuryDetectionInterval = 0.1f,
                colliderType = FFramework.Kit.ColliderType.Box,
                innerCircleRadius = 0f,
                outerCircleRadius = 1f,
                sectorAngle = 0f,
                sectorThickness = 0.1f,
                position = UnityEngine.Vector3.zero,
                rotation = UnityEngine.Vector3.zero,
                scale = UnityEngine.Vector3.one
            };

            // 添加到对应索引的伤害检测轨道
            injuryDetectionTrack.injuryDetectionClips.Add(configInjuryDetectionClip);

            Debug.Log($"AddInjuryDetectionToConfig: 添加伤害检测 '{injuryDetectionName}' 到轨道索引 {trackIndex}");

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
