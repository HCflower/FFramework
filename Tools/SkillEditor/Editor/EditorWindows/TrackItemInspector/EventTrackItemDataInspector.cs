using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

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
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "事件类型更新");
        }

        private void OnEventParametersChanged(string newValue)
        {
            SafeExecute(() =>
            {
                // TODO: 同步到配置文件
                MarkSkillConfigDirty();
            }, "事件参数更新");
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

                // TODO: 实现事件轨道项的配置数据删除逻辑
                // 由于事件轨道的具体数据结构需要进一步确认，这里保留删除框架

                // 删除ScriptableObject资产
                if (eventTargetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(eventTargetData));
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

                Debug.Log($"事件轨道项 \"{eventTargetData.trackItemName}\" 删除成功");
            }, "删除事件轨道项");
        }

        #endregion
    }
}