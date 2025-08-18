using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 特效轨道实现
    /// 专门处理特效GameObject的拖拽、显示和配置管理
    /// </summary>
    public class EffectSkillEditorTrack : SkillEditorTrackBase
    {
        #region 构造函数

        /// <summary>
        /// 特效轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public EffectSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.EffectTrack, width, skillConfig, trackIndex)
        { }

        #endregion

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的GameObject
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否为GameObject</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return obj is GameObject;
        }

        /// <summary>
        /// 从GameObject创建轨道项
        /// </summary>
        /// <param name="resource">GameObject资源</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override TrackItemViewBase CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is GameObject gameObject))
            {
                Debug.LogWarning($"EffectTrack: 无法识别的资源类型: {resource?.GetType()?.Name}");
                return null;
            }

            var frameCount = CalculateFrameCount(gameObject);
            var newItem = CreateEffectTrackItem(gameObject.name, startFrame, frameCount, addToConfig);

            if (addToConfig)
            {
                AddTrackItemDataToConfig(gameObject, gameObject.name, startFrame, frameCount);
                SkillEditorEvent.TriggerRefreshRequested();
            }

            return newItem;
        }

        /// <summary>
        /// 应用特效轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 特效轨道特有的样式设置
            trackArea.AddToClassList("TrackArea-Effect");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 创建特效轨道项
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的特效轨道项</returns>
        public EffectTrackItem CreateEffectTrackItem(string itemName, int startFrame, int frameCount, bool addToConfig = true)
        {
            var effectItem = new EffectTrackItem(trackArea, itemName, frameCount, startFrame, trackIndex);

            if (addToConfig)
            {
                // 外部调用者处理配置逻辑
            }

            trackItems.Add(effectItem);
            return effectItem;
        }

        /// <summary>
        /// 刷新轨道项
        /// </summary>
        public override void RefreshTrackItems()
        {
            base.RefreshTrackItems();
        }

        /// <summary>
        /// 根据配置创建特效轨道项
        /// </summary>
        /// <param name="track">特效轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(EffectSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var effectTrackSO = skillConfig.trackContainer.effectTrack;
            if (effectTrackSO?.effectTracks == null || trackIndex >= effectTrackSO.effectTracks.Count) return;

            var effectTrack = effectTrackSO.effectTracks[trackIndex];
            if (effectTrack.effectClips == null) return;

            foreach (var clip in effectTrack.effectClips)
            {
                if (clip.effectPrefab != null)
                {
                    var trackItem = track.CreateEffectTrackItem(clip.clipName, clip.startFrame, clip.durationFrame, false);
                    RestoreEffectData(trackItem as EffectTrackItem, clip);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算特效的帧数
        /// </summary>
        /// <param name="gameObject">特效GameObject</param>
        /// <returns>帧数</returns>
        private int CalculateFrameCount(GameObject gameObject)
        {
            float frameRate = GetFrameRate();
            float duration = GetEffectDuration(gameObject);
            return Mathf.RoundToInt(duration * frameRate);
        }

        /// <summary>
        /// 获取特效GameObject的持续时间
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

        /// <summary>
        /// 将特效对象添加到技能配置的特效轨道中
        /// </summary>
        /// <param name="effectGameObject">特效GameObject</param>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddTrackItemDataToConfig(GameObject effectGameObject, string itemName, int startFrame, int frameCount)
        {
            Debug.Log($"EffectTrack: 添加轨道项数据 - 名称: {itemName}, 起始帧: {startFrame}, 持续帧数: {frameCount}");
            if (skillConfig?.trackContainer == null) return;

            EnsureEffectTrackExists();

            var effectTrackSO = skillConfig.trackContainer.effectTrack;

            while (effectTrackSO.effectTracks.Count <= trackIndex)
            {
                var newTrack = new FFramework.Kit.EffectTrack
                {
                    trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.EffectTrack, effectTrackSO.effectTracks.Count),
                    isEnabled = true,
                    trackIndex = effectTrackSO.effectTracks.Count,
                    effectClips = new List<FFramework.Kit.EffectTrack.EffectClip>()
                };
                effectTrackSO.effectTracks.Add(newTrack);
            }

            var effectTrack = effectTrackSO.effectTracks[trackIndex];

            if (effectTrack.effectClips == null)
            {
                effectTrack.effectClips = new List<FFramework.Kit.EffectTrack.EffectClip>();
            }

            string finalName = itemName;
            int suffix = 1;
            while (effectTrack.effectClips.Any(c => c.clipName == finalName))
            {
                finalName = $"{itemName}_{suffix++}";
            }

            var configEffectClip = new FFramework.Kit.EffectTrack.EffectClip
            {
                clipName = finalName,
                startFrame = startFrame,
                durationFrame = frameCount,
                effectPrefab = effectGameObject,
                effectPlaySpeed = 1.0f,
                scale = Vector3.one,
                rotation = Vector3.zero,
                position = Vector3.zero
            };

            effectTrack.effectClips.Add(configEffectClip);

#if UNITY_EDITOR
            EditorUtility.SetDirty(effectTrackSO);
            EditorUtility.SetDirty(skillConfig);
#endif
        }

        /// <summary>
        /// 确保特效轨道存在
        /// </summary>
        private void EnsureEffectTrackExists()
        {
            if (skillConfig.trackContainer.effectTrack != null) return;

            var effectTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.EffectTrackSO>();
            effectTrackSO.effectTracks = new List<FFramework.Kit.EffectTrack>();
            skillConfig.trackContainer.effectTrack = effectTrackSO;

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(effectTrackSO, skillConfig);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// 恢复特效数据
        /// </summary>
        /// <param name="trackItem">轨道项</param>
        /// <param name="clip">特效片段</param>
        private static void RestoreEffectData(EffectTrackItem trackItem, FFramework.Kit.EffectTrack.EffectClip clip)
        {
            if (trackItem?.EffectData == null) return;

            var effectData = trackItem.EffectData;
            effectData.trackItemName = clip.clipName;
            effectData.durationFrame = clip.durationFrame;
            effectData.effectPlaySpeed = clip.effectPlaySpeed;

            // 重要修复：根据durationFrame和effectPlaySpeed计算正确的frameCount
            // frameCount应该是特效的原始帧数（播放速度为1.0时的帧数）
            if (clip.effectPlaySpeed > 0)
            {
                effectData.frameCount = (int)(clip.durationFrame * clip.effectPlaySpeed);
            }

            effectData.position = clip.position;
            effectData.rotation = clip.rotation;
            effectData.scale = clip.scale;

#if UNITY_EDITOR
            EditorUtility.SetDirty(effectData);
#endif
        }

        #endregion
    }
}