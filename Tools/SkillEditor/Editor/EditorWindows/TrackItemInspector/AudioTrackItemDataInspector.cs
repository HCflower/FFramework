using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(AudioTrackItemData))]
    public class AudioTrackItemDataInspector : TrackItemDataInspectorBase
    {
        protected override string TrackItemTypeName => "Audio";
        protected override string TrackItemDisplayTitle => "音频轨道项信息";
        protected override string DeleteButtonText => "删除音频轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as AudioTrackItemData;
            lastTrackItemName = targetData?.trackItemName; // 初始化保存的名称
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 音频片段字段
            CreateObjectField<AudioClip>("音频片段:", "audioClip", OnAudioClipChanged);

            // 音量字段
            CreateFloatField("音量:", "volume", OnVolumeChanged);

            // 音调字段
            CreateFloatField("音调:", "pitch", OnPitchChanged);

            // 循环状态
            CreateSliderField("空间混合:", "spatialBlend", 0.0f, 1.0f, OnSpatialBlendChanged);
            CreateSliderField("混响区混音:", "reverbZoneMix", 0.0f, 1.0f, OnReverbZoneMixChanged);
        }

        #region 事件处理方法

        private void OnAudioClipChanged(AudioClip newClip)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.clip = newClip, "音频片段更新");
            }, "音频片段更新");
        }

        private void OnVolumeChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.volume = newValue, "音量更新");
            }, "音量更新");
        }

        private void OnPitchChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.pitch = newValue, "音调更新");
            }, "音调更新");
        }

        private void OnSpatialBlendChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.spatialBlend = newValue, "空间混合更新");
            }, "空间混合更新");
        }

        private void OnReverbZoneMixChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.reverbZoneMix = newValue, "混响区混音更新");
            }, "混响区混音更新");
        }

        protected override void OnTrackItemNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                // 使用保存的旧名称
                string oldName = lastTrackItemName ?? targetData.trackItemName;
                UpdateTrackConfigByName(oldName, configClip => configClip.clipName = newValue, "轨道项名称更新");
                // 更新保存的名称
                lastTrackItemName = newValue;
            }, "轨道项名称更新");
        }

        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
            }, "起始帧更新");
        }

        protected override void OnDurationFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfig(System.Action<FFramework.Kit.AudioTrack.AudioClip> updateAction, string operationName = "更新配置")
        {
            UpdateTrackConfigByName(targetData.trackItemName, updateAction, operationName);
        }

        /// <summary>
        /// 根据指定名称查找并更新攻击配置数据
        /// </summary>
        /// <param name="clipName">要查找的片段名称</param>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfigByName(string clipName, System.Action<FFramework.Kit.AudioTrack.AudioClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.audioTrack == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或音频轨道为空");
                return;
            }

            // 只通过名称唯一查找
            FFramework.Kit.AudioTrack.AudioClip targetConfigClip = null;
            var audioTrackSO = skillConfig.trackContainer.audioTrack;
            if (audioTrackSO.audioTracks != null)
            {
                targetConfigClip = audioTrackSO.audioTracks
                    .SelectMany(track => track.audioClips)
                    .FirstOrDefault(clip => clip.clipName == clipName);
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的音频片段配置 (片段名: {clipName})");
            }
        }

        /// <summary>
        /// 删除音频轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        protected override void DeleteTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.audioTrack == null || targetData == null)
            {
                Debug.LogWarning("无法删除轨道项：技能配置或音频轨道为空");
                return;
            }

            // 标记要删除的音频片段配置
            FFramework.Kit.AudioTrack.AudioClip targetConfigClip = null;
            FFramework.Kit.AudioTrack parentAudioTrack = null;

            // 查找对应的音频片段配置
            var audioTrackSO = skillConfig.trackContainer.audioTrack;
            if (audioTrackSO?.audioTracks != null && targetData.trackIndex < audioTrackSO.audioTracks.Count)
            {
                // 直接通过 trackIndex 定位到对应的音频轨道
                var targetAudioTrack = audioTrackSO.audioTracks[targetData.trackIndex];
                if (targetAudioTrack.audioClips != null)
                {
                    // 直接通过唯一名称查找目标配置
                    targetConfigClip = targetAudioTrack.audioClips
                        .FirstOrDefault(clip => clip.clipName == targetData.trackItemName);
                    parentAudioTrack = targetAudioTrack;
                }
            }

            if (targetConfigClip != null && parentAudioTrack != null)
            {
                // 从配置中移除音频片段数据
                parentAudioTrack.audioClips.Remove(targetConfigClip);

                // 标记技能配置为已修改
                MarkSkillConfigDirty();

                // 删除轨道项的ScriptableObject数据文件
                if (targetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(targetData));
                }

                // 清空Inspector选择
                UnityEditor.Selection.activeObject = null;

                // 触发界面刷新以移除UI元素
                var window = UnityEditor.EditorWindow.GetWindow<SkillEditor>();
                if (window != null)
                {
                    // 使用EditorApplication.delayCall确保在下一帧执行刷新
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        window.Repaint();
                        // 直接调用静态事件方法
                        SkillEditorEvent.TriggerRefreshRequested();
                    };
                }

                Debug.Log($"音频轨道项 \"{targetData.trackItemName}\" 删除成功");
            }
            else
            {
                Debug.LogWarning($"无法删除轨道项：找不到对应的音频片段配置 \"{targetData.trackItemName}\"");
            }
        }


        #endregion
    }
}