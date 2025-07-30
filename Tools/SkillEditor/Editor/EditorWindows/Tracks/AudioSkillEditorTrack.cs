using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 音频轨道实现
    /// 专门处理音频剪辑的拖拽、显示和配置管理
    /// </summary>
    public class AudioSkillEditorTrack : BaseSkillEditorTrack
    {
        /// <summary>
        /// 音频轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public AudioSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.AudioTrack, width, skillConfig, trackIndex)
        { }

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的音频剪辑
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否为音频剪辑</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return obj is AudioClip;
        }

        /// <summary>
        /// 从音频剪辑创建轨道项
        /// </summary>
        /// <param name="resource">音频剪辑资源</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is AudioClip audioClip))
                return null;

            float frameRate = GetFrameRate();
            int frameCount = Mathf.RoundToInt(audioClip.length * frameRate);
            string itemName = audioClip.name;

            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame, trackIndex);

            // 设置音频轨道项的数据
            if (newItem.ItemData is AudioTrackItemData audioData)
            {
                audioData.audioClip = audioClip;
                audioData.volume = 1.0f;
                audioData.pitch = 1.0f;
                audioData.isLoop = false;

#if UNITY_EDITOR
                EditorUtility.SetDirty(audioData);
#endif
            }

            // 添加到技能配置
            if (addToConfig)
            {
                AddAudioClipToConfig(audioClip, itemName, startFrame, frameCount);
            }

            return newItem;
        }

        /// <summary>
        /// 应用音频轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 音频轨道特有的样式设置
            // 可以在这里添加音频轨道特有的视觉效果
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 支持自定义名称的音频轨道项添加
        /// </summary>
        /// <param name="resource">音频剪辑资源</param>
        /// <param name="itemName">自定义名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        public override SkillEditorTrackItem AddTrackItem(object resource, string itemName, int startFrame, bool addToConfig)
        {
            if (!(resource is AudioClip audioClip))
                return null;

            float frameRate = GetFrameRate();
            int frameCount = Mathf.RoundToInt(audioClip.length * frameRate);

            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame, trackIndex);

            // 设置音频轨道项的数据
            if (newItem.ItemData is AudioTrackItemData audioData)
            {
                audioData.audioClip = audioClip;
                audioData.volume = 1.0f;
                audioData.pitch = 1.0f;
                audioData.isLoop = false;

#if UNITY_EDITOR
                EditorUtility.SetDirty(audioData);
#endif
            }

            // 添加到技能配置
            if (addToConfig)
            {
                AddAudioClipToConfig(audioClip, itemName, startFrame, frameCount);
            }

            if (newItem != null)
            {
                trackItems.Add(newItem);
            }

            return newItem;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将音频片段添加到技能配置的音频轨道中
        /// </summary>
        /// <param name="audioClip">音频剪辑</param>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddAudioClipToConfig(AudioClip audioClip, string itemName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保音频轨道存在
            if (skillConfig.trackContainer.audioTrack == null)
            {
                // 创建音频轨道ScriptableObject
                var newAudioTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.AudioTrackSO>();
                newAudioTrackSO.audioTracks = new System.Collections.Generic.List<FFramework.Kit.AudioTrack>();
                newAudioTrackSO.name = "AudioTracks";
                skillConfig.trackContainer.audioTrack = newAudioTrackSO;

#if UNITY_EDITOR
                // 将轨道SO作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(newAudioTrackSO, skillConfig);
                UnityEditor.EditorUtility.SetDirty(skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"创建音频轨道数据作为子资产嵌套到 {skillConfig.name}");
#endif
            }

            // 获取音频轨道SO
            var audioTrackSO = skillConfig.trackContainer.audioTrack;

            // 确保对应索引的轨道存在
            while (audioTrackSO.audioTracks.Count <= trackIndex)
            {
                var newTrack = new FFramework.Kit.AudioTrack
                {
                    trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.AudioTrack, audioTrackSO.audioTracks.Count),
                    isEnabled = true,
                    trackIndex = audioTrackSO.audioTracks.Count,
                    audioClips = new System.Collections.Generic.List<FFramework.Kit.AudioTrack.AudioClip>()
                };
                audioTrackSO.audioTracks.Add(newTrack);
            }

            // 获取指定索引的音频轨道
            var audioTrack = audioTrackSO.audioTracks[trackIndex];

            // 确保音频片段列表存在
            if (audioTrack.audioClips == null)
            {
                audioTrack.audioClips = new System.Collections.Generic.List<FFramework.Kit.AudioTrack.AudioClip>();
            }            // 创建技能配置中的音频片段数据
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

#if UNITY_EDITOR
            // 标记轨道数据和技能配置为已修改
            if (audioTrackSO != null)
            {
                EditorUtility.SetDirty(audioTrackSO);
            }
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
#endif
        }

        #endregion

        #region 配置恢复方法

        /// <summary>
        /// 根据索引从配置创建音频轨道项
        /// </summary>
        /// <param name="track">音频轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(AudioSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var audioTrackSO = skillConfig.trackContainer.audioTrack;
            if (audioTrackSO?.audioTracks == null || trackIndex >= audioTrackSO.audioTracks.Count)
            {
                Debug.Log($"CreateAudioTrackItemsFromConfig: 没有找到索引 {trackIndex} 的音频轨道数据");
                return;
            }

            var audioTrack = audioTrackSO.audioTracks[trackIndex];
            if (audioTrack.audioClips == null) return;

            foreach (var clip in audioTrack.audioClips)
            {
                if (clip.clip != null)
                {
                    // 从配置加载时，使用配置中的名称，并设置addToConfig为false，避免重复添加到配置文件
                    var trackItem = track.AddTrackItem(clip.clip, clip.clipName, clip.startFrame, false);

                    // 从配置中恢复完整的音频属性
                    if (trackItem?.ItemData is AudioTrackItemData audioData)
                    {
                        audioData.durationFrame = clip.durationFrame;
                        audioData.volume = clip.volume;
                        audioData.pitch = clip.pitch;
                        audioData.isLoop = clip.isLoop;

#if UNITY_EDITOR
                        // 标记数据已修改
                        UnityEditor.EditorUtility.SetDirty(audioData);
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
