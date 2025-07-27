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
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is GameObject gameObject))
                return null;

            float frameRate = GetFrameRate();
            float duration = GetEffectDuration(gameObject);
            int frameCount = Mathf.RoundToInt(duration * frameRate);
            string itemName = gameObject.name;

            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame);

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
            if (skillConfig?.trackContainer == null) return;

            // 确保特效轨道存在
            if (skillConfig.trackContainer.effectTrack == null)
            {
                // 创建特效轨道ScriptableObject
                var effectTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.EffectTrackSO>();
                effectTrackSO.trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.EffectTrack, 0);
                effectTrackSO.effectClips = new System.Collections.Generic.List<FFramework.Kit.EffectTrack.EffectClip>();
                skillConfig.trackContainer.effectTrack = effectTrackSO;

#if UNITY_EDITOR
                // 将ScriptableObject保存为资产文件
                var skillConfigPath = UnityEditor.AssetDatabase.GetAssetPath(skillConfig);
                var configDirectory = System.IO.Path.GetDirectoryName(skillConfigPath);
                var configName = System.IO.Path.GetFileNameWithoutExtension(skillConfigPath);
                var tracksFolder = System.IO.Path.Combine(configDirectory, $"{configName}_Tracks");

                if (!System.IO.Directory.Exists(tracksFolder))
                {
                    System.IO.Directory.CreateDirectory(tracksFolder);
                }

                var assetPath = System.IO.Path.Combine(tracksFolder, $"{configName}_EffectTrack.asset");
                assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetPath);

                UnityEditor.AssetDatabase.CreateAsset(effectTrackSO, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }

            // 获取特效轨道
            var effectTrack = skillConfig.trackContainer.effectTrack;            // 确保特效片段列表存在
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
                scale = Vector3.one,
                rotation = Vector3.zero,
                position = Vector3.zero
            };

            // 添加到对应索引的特效轨道
            effectTrack.effectClips.Add(configEffectClip);

            Debug.Log($"AddEffectToConfig: 添加特效 '{itemName}' 到轨道索引 {trackIndex}");

#if UNITY_EDITOR
            // 标记轨道数据和技能配置为已修改
            if (effectTrack != null)
            {
                EditorUtility.SetDirty(effectTrack);
            }
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
#endif
        }

        #endregion
    }
}
