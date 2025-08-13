using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(GameObjectTrackItemData))]
    public class GameObjectTrackItemDataInspector : TrackItemDataInspectorBase
    {
        protected override string TrackItemTypeName => "GameObject";
        protected override string TrackItemDisplayTitle => "游戏物体轨道项信息";
        protected override string DeleteButtonText => "删除游戏物体轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as GameObjectTrackItemData;
            lastTrackItemName = targetData?.trackItemName; // 初始化保存的名称
            return base.CreateInspectorGUI();
        }

        protected override void CreateSpecificFields()
        {
            // 预制体字段
            CreateObjectField<GameObject>("预制体:", "prefab", OnPrefabChanged);

            // 自动销毁字段
            CreateToggleField("自动销毁:", "autoDestroy", OnAutoDestroyChanged);

            // 位置偏移字段
            CreateVector3Field("位置偏移:", "positionOffset", OnPositionOffsetChanged);

            // 旋转偏移字段
            CreateVector3Field("旋转偏移:", "rotationOffset", OnRotationOffsetChanged);

            // 缩放字段
            CreateVector3Field("缩放:", "scale", OnScaleChanged);

            // 父对象设置
            CreateToggleField("使用父对象:", "useParent", OnUseParentChanged);

            // 父对象名称字段
            CreateTextField("父对象名称:", "parentName", OnParentNameChanged);

            // 销毁延迟字段
            CreateFloatField("销毁延迟(秒):", "destroyDelay", OnDestroyDelayChanged);
        }

        #region 字段变化回调

        private void OnPrefabChanged(GameObject newPrefab)
        {
            SafeExecute(() =>
           {
               UpdateTrackConfig(configClip => configClip.prefab = newPrefab, "预制体更新");
               // 预制体更改可能影响轨道项显示，刷新UI
           }, "预制体改变");
        }

        private void OnAutoDestroyChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.autoDestroy = newValue, "自动销毁更新");
            }, "自动销毁");
        }

        private void OnPositionOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.positionOffset = newValue, "位置偏移更新");
            }, "位置偏移更新");
        }

        private void OnRotationOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.rotationOffset = newValue, "旋转偏移更新");
            }, "旋转偏移更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.scale = newValue, "缩放更新");
            }, "缩放更新");
        }

        private void OnUseParentChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.useParent = newValue, "使用父对象更新");
            }, "使用父对象更新");
        }

        private void OnParentNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                // 使用保存的旧名称
                string oldName = lastTrackItemName ?? targetData.trackItemName;

                // 先更新配置中的名称（使用旧名称查找）
                UpdateTrackConfigByName(oldName, configClip => configClip.clipName = newValue, "轨道项名称更新");

                // 更新保存的名称
                lastTrackItemName = newValue;
            }, "父对象名称更新");
        }

        private void OnDestroyDelayChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.destroyDelay = newValue, "延迟销毁更新");
            }, "延迟销毁更新");
        }

        protected override void OnTrackItemNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                UpdateTrackConfig(configClip => configClip.clipName = newValue, "轨道项名称更新");
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
        private void UpdateTrackConfig(System.Action<FFramework.Kit.GameObjectTrack.GameObjectClip> updateAction, string operationName = "更新配置")
        {
            UpdateTrackConfigByName(targetData.trackItemName, updateAction, operationName);
        }

        /// <summary>
        /// 根据指定名称查找并更新攻击配置数据
        /// </summary>
        /// <param name="clipName">要查找的片段名称</param>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateTrackConfigByName(string clipName, System.Action<FFramework.Kit.GameObjectTrack.GameObjectClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.gameObjectTrack == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或游戏物体轨道为空");
                return;
            }


            // 只通过名称唯一查找
            FFramework.Kit.GameObjectTrack.GameObjectClip targetConfigClip = null;
            if (skillConfig.trackContainer.gameObjectTrack.gameObjectTracks != null)
            {
                foreach (var track in skillConfig.trackContainer.gameObjectTrack.gameObjectTracks)
                {
                    if (track.gameObjectClips != null)
                    {
                        targetConfigClip = track.gameObjectClips
                            .FirstOrDefault(clip => clip.clipName == clipName);
                        if (targetConfigClip != null)
                            break;
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
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的游戏物体片段配置");
            }
        }

        /// <summary>
        /// 删除游戏物体轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        protected override void DeleteTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.gameObjectTrack == null || targetData == null)
            {
                Debug.LogWarning("无法删除游戏物体轨道项：技能配置或游戏物体轨道为空");
                return;
            }

            bool deleted = false;

            // 从游戏物体轨道中查找并删除对应的片段
            if (skillConfig.trackContainer.gameObjectTrack.gameObjectTracks != null)
            {
                foreach (var track in skillConfig.trackContainer.gameObjectTrack.gameObjectTracks)
                {
                    if (track.gameObjectClips != null)
                    {
                        // 直接通过唯一名称查找要删除的片段
                        var clipToRemove = track.gameObjectClips
                            .FirstOrDefault(clip => clip.clipName == targetData.trackItemName);

                        if (clipToRemove != null)
                        {
                            track.gameObjectClips.Remove(clipToRemove);
                            deleted = true;
                            Debug.Log($"从配置中删除游戏物体片段: {clipToRemove.clipName} (轨道索引: {targetData.trackIndex})");
                            break;
                        }
                    }
                }
            }

            if (deleted)
            {
                // 标记配置文件为已修改
                MarkSkillConfigDirty();
                UnityEditor.AssetDatabase.SaveAssets();

                // 触发UI刷新
                SkillEditorEvent.TriggerRefreshRequested();
                SkillEditorEvent.TriggerCurrentFrameChanged(SkillEditorData.CurrentFrame);
            }
            else
            {
                Debug.LogWarning($"未找到要删除的游戏物体片段: {targetData.trackItemName} (轨道索引: {targetData.trackIndex})");
            }

            // 删除ScriptableObject资产
            if (targetData != null)
            {
                UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(targetData));
            }

            // 清空Inspector选择
            UnityEditor.Selection.activeObject = null;

            Debug.Log($"删除游戏物体轨道项: {targetData.trackItemName}");
        }


        #endregion
    }
}
