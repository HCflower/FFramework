using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 特效轨道实现
    /// 专门处理特效GameObject的拖拽、显示和配置管理
    /// </summary>
    public class EffectSkillEditorTrack : BaseSkillEditorTrack
    {
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
        protected override BaseTrackItemView CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is GameObject gameObject))
            {
                Debug.LogWarning($"EffectTrack: 无法识别的资源类型: {resource?.GetType()?.Name}");
                return null;
            }

            float frameRate = GetFrameRate();
            float duration = GetEffectDuration(gameObject);
            int frameCount = Mathf.RoundToInt(duration * frameRate);
            string itemName = gameObject.name;

            var newItem = new EffectTrackItem(trackArea, itemName, frameCount, startFrame, trackIndex);

            // 添加到基类的轨道项列表，确保在时间轴缩放时能够被刷新
            trackItems.Add(newItem);

            // 设置特效轨道项的数据
            var effectData = newItem.EffectData;
            effectData.effectPrefab = gameObject;
            effectData.effectPlaySpeed = 1.0f;
            effectData.position = Vector3.zero;
            effectData.rotation = Vector3.zero;
            effectData.scale = Vector3.one;

#if UNITY_EDITOR
            EditorUtility.SetDirty(effectData);
#endif

            // 添加到技能配置
            if (addToConfig)
            {
                AddEffectToConfig(gameObject, itemName, startFrame, frameCount);
            }
            return newItem;
        }

        /// <summary>
        /// 应用特效轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 特效轨道特有的样式设置
            // 可以在这里添加特效轨道特有的视觉效果
        }

        #endregion

        #region 私有方法

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
        private void AddEffectToConfig(GameObject effectGameObject, string itemName, int startFrame, int frameCount)
        {
            Debug.Log($"AddEffectToConfig: 开始添加特效 '{itemName}' (GameObject: {effectGameObject?.name})");

            if (skillConfig?.trackContainer == null)
            {
                Debug.LogError("AddEffectToConfig: skillConfig 或 trackContainer 为空");
                return;
            }

            // 确保特效轨道存在
            if (skillConfig.trackContainer.effectTrack == null)
            {
                Debug.Log("AddEffectToConfig: 创建新的 EffectTrackSO");

                // 创建特效轨道ScriptableObject
                var newEffectTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.EffectTrackSO>();
                newEffectTrackSO.effectTracks = new System.Collections.Generic.List<FFramework.Kit.EffectTrack>();
                newEffectTrackSO.name = "EffectTracks";
                skillConfig.trackContainer.effectTrack = newEffectTrackSO;

#if UNITY_EDITOR
                // 将轨道SO作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(newEffectTrackSO, skillConfig);
                UnityEditor.EditorUtility.SetDirty(skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"创建特效轨道数据作为子资产嵌套到 {skillConfig.name}");
#endif
            }

            // 获取特效轨道SO
            var effectTrackSO = skillConfig.trackContainer.effectTrack;

            // 确保对应索引的轨道存在
            while (effectTrackSO.effectTracks.Count <= trackIndex)
            {
                var newTrack = new FFramework.Kit.EffectTrack
                {
                    trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.EffectTrack, effectTrackSO.effectTracks.Count),
                    isEnabled = true,
                    trackIndex = effectTrackSO.effectTracks.Count,
                    effectClips = new System.Collections.Generic.List<FFramework.Kit.EffectTrack.EffectClip>()
                };
                effectTrackSO.effectTracks.Add(newTrack);
            }

            // 获取指定索引的特效轨道
            var effectTrack = effectTrackSO.effectTracks[trackIndex];

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
                effectPlaySpeed = 1.0f,
                scale = Vector3.one,
                rotation = Vector3.zero,
                position = Vector3.zero
            };

            // 添加到对应索引的特效轨道
            effectTrack.effectClips.Add(configEffectClip);

#if UNITY_EDITOR
            // 标记轨道数据和技能配置为已修改
            if (effectTrackSO != null)
            {
                EditorUtility.SetDirty(effectTrackSO);
            }
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }

            // 强制保存资产
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        #endregion

        #region 配置恢复方法

        /// <summary>
        /// 根据索引从配置创建特效轨道项
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
                    // 从配置加载时，设置addToConfig为false，避免重复添加到配置文件
                    var trackItem = track.AddTrackItem(clip.effectPrefab, clip.startFrame, false);

                    // 从配置中恢复完整的特效属性
                    if (trackItem is EffectTrackItem effectTrackItem)
                    {
                        var effectData = effectTrackItem.EffectData;
                        effectData.durationFrame = clip.durationFrame;
                        effectData.effectPlaySpeed = clip.effectPlaySpeed;
                        effectData.position = clip.position;
                        effectData.rotation = clip.rotation;
                        effectData.scale = clip.scale;

#if UNITY_EDITOR
                        // 标记数据已修改
                        UnityEditor.EditorUtility.SetDirty(effectData);
#endif
                    }

                    // 更新轨道项的帧数和宽度显示
                    if (clip.durationFrame > 0)
                    {
                        trackItem?.UpdateFrameCount(clip.durationFrame);
                    }
                }
            }
        }

        #endregion
    }
}