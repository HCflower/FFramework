using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(AudioTrackItemData))]
    public class AudioTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private AudioTrackItemData audioTargetData;

        protected override string TrackItemTypeName => "Audio";
        protected override string TrackItemDisplayTitle => "音频轨道项信息";
        protected override string DeleteButtonText => "删除音频轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            audioTargetData = target as AudioTrackItemData;
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
            CreateToggleField("是否启用循环:", "isLoop", OnLoopChanged);
        }

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除音频轨道项 \"{audioTargetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteAudioTrackItem();
            }
        }

        #region 事件处理方法

        private void OnAudioClipChanged(AudioClip newClip)
        {
            SafeExecute(() =>
            {
                UpdateAudioTrackConfig(configClip => configClip.clip = newClip, "音频片段更新");
            }, "音频片段更新");
        }

        private void OnVolumeChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateAudioTrackConfig(configClip => configClip.volume = newValue, "音量更新");
            }, "音量更新");
        }

        private void OnPitchChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateAudioTrackConfig(configClip => configClip.pitch = newValue, "音调更新");
            }, "音调更新");
        }

        private void OnLoopChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateAudioTrackConfig(configClip => configClip.isLoop = newValue, "循环状态更新");
            }, "循环状态更新");
        }

        /// <summary>
        /// 起始帧变化事件处理
        /// </summary>
        /// <param name="newValue">新的起始帧值</param>
        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateAudioTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
            }, "起始帧更新");
        }

        /// <summary>
        /// 持续帧数变化事件处理
        /// </summary>
        /// <param name="newValue">新的持续帧数值</param>
        protected override void OnDurationFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateAudioTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的音频配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateAudioTrackConfig(System.Action<FFramework.Kit.AudioTrack.AudioClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.audioTrack == null || audioTargetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或音频轨道为空");
                return;
            }

            // 查找对应的音频片段配置
            FFramework.Kit.AudioTrack.AudioClip targetConfigClip = null;

            var audioTrackSO = skillConfig.trackContainer.audioTrack;
            if (audioTrackSO.audioTracks != null && audioTargetData.trackIndex < audioTrackSO.audioTracks.Count)
            {
                // 直接通过 trackIndex 定位到对应的音频轨道
                var targetAudioTrack = audioTrackSO.audioTracks[audioTargetData.trackIndex];
                if (targetAudioTrack.audioClips != null)
                {
                    var candidateClips = targetAudioTrack.audioClips
                        .Where(clip => clip.clipName == audioTargetData.trackItemName).ToList();

                    if (candidateClips.Count > 0)
                    {
                        if (candidateClips.Count == 1)
                        {
                            targetConfigClip = candidateClips[0];
                        }
                        else
                        {
                            // 如果有多个同名片段，尝试通过起始帧匹配
                            var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == audioTargetData.startFrame);
                            targetConfigClip = exactMatch ?? candidateClips[0]; // 使用精确匹配或第一个匹配项
                        }
                    }
                }
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的音频片段配置");
            }
        }

        /// <summary>
        /// 删除音频轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteAudioTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.audioTrack == null || audioTargetData == null)
            {
                Debug.LogWarning("无法删除轨道项：技能配置或音频轨道为空");
                return;
            }

            // 标记要删除的音频片段配置
            FFramework.Kit.AudioTrack.AudioClip targetConfigClip = null;
            FFramework.Kit.AudioTrack parentAudioTrack = null;

            // 查找对应的音频片段配置
            var audioTrackSO = skillConfig.trackContainer.audioTrack;
            if (audioTrackSO?.audioTracks != null && audioTargetData.trackIndex < audioTrackSO.audioTracks.Count)
            {
                // 直接通过 trackIndex 定位到对应的音频轨道
                var targetAudioTrack = audioTrackSO.audioTracks[audioTargetData.trackIndex];
                if (targetAudioTrack.audioClips != null)
                {
                    var candidateClips = targetAudioTrack.audioClips
                        .Where(clip => clip.clipName == audioTargetData.trackItemName).ToList();

                    if (candidateClips.Count > 0)
                    {
                        if (candidateClips.Count == 1)
                        {
                            targetConfigClip = candidateClips[0];
                            parentAudioTrack = targetAudioTrack;
                        }
                        else
                        {
                            // 如果有多个同名片段，尝试通过起始帧匹配
                            var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == audioTargetData.startFrame);
                            targetConfigClip = exactMatch ?? candidateClips[0]; // 使用精确匹配或第一个匹配项
                            parentAudioTrack = targetAudioTrack;
                        }
                    }
                }
            }

            if (targetConfigClip != null && parentAudioTrack != null)
            {
                // 从配置中移除音频片段数据
                parentAudioTrack.audioClips.Remove(targetConfigClip);

                // 标记技能配置为已修改
                MarkSkillConfigDirty();

                // 删除轨道项的ScriptableObject数据文件
                if (audioTargetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(audioTargetData));
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

                        // 通过反射获取skillEditorEvent实例并触发刷新
                        var skillEditorEventField = typeof(SkillEditor).GetField("skillEditorEvent",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (skillEditorEventField != null)
                        {
                            var skillEditorEvent = skillEditorEventField.GetValue(window) as SkillEditorEvent;
                            skillEditorEvent?.TriggerRefreshRequested();
                        }
                    };
                }

                Debug.Log($"音频轨道项 \"{audioTargetData.trackItemName}\" 删除成功");
            }
            else
            {
                Debug.LogWarning($"无法删除轨道项：找不到对应的音频片段配置 \"{audioTargetData.trackItemName}\"");
            }
        }

        #endregion
    }
}