using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(EventTrackItemData))]
    public class EventTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private EventTrackItemData eventTargetData;

        protected override string TrackItemTypeName => "Event";
        protected override string TrackItemDisplayTitle => "事件轨道项信息";
        protected override string DeleteButtonText => "删除事件轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            eventTargetData = target as EventTrackItemData;
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 事件参数设置
            CreateTextField("事件类型:", "eventType", OnEventTypeChanged);
            CreateTextField("事件参数:", "eventParameters", OnEventParametersChanged);
        }

        #region 事件处理方法

        private void OnEventTypeChanged(string newValue)
        {
            SafeExecute(() =>
            {
                UpdateEventTrackConfig(configClip =>
                {
                    configClip.eventType = newValue;
                }, "事件类型更新");
            }, "事件类型更新");
        }

        private void OnEventParametersChanged(string newValue)
        {
            SafeExecute(() =>
            {
                UpdateEventTrackConfig(configClip =>
                {
                    configClip.eventParameters = newValue;
                }, "事件参数更新");
            }, "事件参数更新");
        }

        protected override void OnTrackItemNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                UpdateEventTrackConfig(configClip => configClip.clipName = newValue, "轨道项名称更新");
            }, "轨道项名称更新");
        }

        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateEventTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
            }, "起始帧更新");
        }

        protected override void OnDurationFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateEventTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        protected override void PerformDelete()
        {
            if (EditorUtility.DisplayDialog("删除确认",
                $"确定要删除事件轨道项 \"{eventTargetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteEventTrackItem();
            }
        }

        #region 数据同步方法

        /// <summary>
        /// 统一的事件配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateEventTrackConfig(System.Action<FFramework.Kit.EventTrack.EventClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.eventTrack == null || eventTargetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或事件轨道为空");
                return;
            }

            // 获取轨道索引
            int trackIndex = eventTargetData.trackIndex;

            // 验证轨道索引的有效性
            if (skillConfig.trackContainer.eventTrack.eventTracks == null ||
                trackIndex < 0 ||
                trackIndex >= skillConfig.trackContainer.eventTrack.eventTracks.Count)
            {
                Debug.LogWarning($"无法执行 {operationName}：轨道索引 {trackIndex} 超出范围或事件轨道列表为空");
                return;
            }

            // 获取指定轨道
            var targetTrack = skillConfig.trackContainer.eventTrack.eventTracks[trackIndex];
            if (targetTrack?.eventClips == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：轨道 {trackIndex} 的事件片段列表为空");
                return;
            }

            // 只通过名称唯一查找
            FFramework.Kit.EventTrack.EventClip targetConfigClip = targetTrack.eventClips
                .FirstOrDefault(clip => clip.clipName == eventTargetData.trackItemName);

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：在轨道 {trackIndex} 中找不到对应的事件片段配置 \"{eventTargetData.trackItemName}\"");
            }
        }

        /// <summary>
        /// 删除事件轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteEventTrackItem()
        {
            SafeExecute(() =>
            {
                var skillConfig = SkillEditorData.CurrentSkillConfig;
                if (skillConfig?.trackContainer?.eventTrack == null || eventTargetData == null)
                {
                    Debug.LogWarning("无法删除轨道项：技能配置或事件轨道为空");
                    return;
                }

                // 获取轨道索引
                int trackIndex = eventTargetData.trackIndex;

                // 验证轨道索引的有效性
                if (skillConfig.trackContainer.eventTrack.eventTracks == null ||
                    trackIndex < 0 ||
                    trackIndex >= skillConfig.trackContainer.eventTrack.eventTracks.Count)
                {
                    Debug.LogWarning($"无法删除轨道项：轨道索引 {trackIndex} 超出范围或事件轨道列表为空");
                    return;
                }

                // 获取指定轨道
                var targetTrack = skillConfig.trackContainer.eventTrack.eventTracks[trackIndex];
                if (targetTrack?.eventClips == null)
                {
                    Debug.LogWarning($"无法删除轨道项：轨道 {trackIndex} 的事件片段列表为空");
                    return;
                }

                // 查找要删除的事件片段
                FFramework.Kit.EventTrack.EventClip targetClip = null;
                int clipIndex = -1;

                for (int i = 0; i < targetTrack.eventClips.Count; i++)
                {
                    var clip = targetTrack.eventClips[i];
                    if (clip.clipName == eventTargetData.trackItemName)
                    {
                        // 如果有多个同名片段，通过起始帧精确匹配
                        if (clip.startFrame == eventTargetData.startFrame)
                        {
                            targetClip = clip;
                            clipIndex = i;
                            break;
                        }
                        // 如果只有名称匹配但没有找到起始帧匹配的，使用第一个匹配项
                        else if (targetClip == null)
                        {
                            targetClip = clip;
                            clipIndex = i;
                        }
                    }
                }

                if (targetClip != null && clipIndex >= 0)
                {
                    // 从配置中移除事件片段
                    targetTrack.eventClips.RemoveAt(clipIndex);

                    Debug.Log($"从轨道 {trackIndex} 移除事件片段 \"{eventTargetData.trackItemName}\"");
                }
                else
                {
                    Debug.LogWarning($"无法在轨道 {trackIndex} 中找到要删除的事件片段 \"{eventTargetData.trackItemName}\"");
                }

                // 删除UI数据的ScriptableObject资产
                if (eventTargetData != null)
                {
                    var dataAssetPath = UnityEditor.AssetDatabase.GetAssetPath(eventTargetData);
                    if (!string.IsNullOrEmpty(dataAssetPath))
                    {
                        UnityEditor.AssetDatabase.RemoveObjectFromAsset(eventTargetData);
                    }
                }

                // 保存资产变更
                UnityEditor.AssetDatabase.SaveAssets();

                // 标记配置为脏数据
                MarkSkillConfigDirty();

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

                Debug.Log($"事件轨道项 \"{eventTargetData.trackItemName}\" 删除成功");
            }, "删除事件轨道项");
        }

        #endregion
    }
}