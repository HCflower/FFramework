using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    [CustomEditor(typeof(GameObjectTrackItemData))]
    public class GameObjectTrackItemDataInspector : BaseTrackItemDataInspector
    {
        private GameObjectTrackItemData gameObjectTargetData;

        protected override string TrackItemTypeName => "GameObject";
        protected override string TrackItemDisplayTitle => "游戏物体轨道项信息";
        protected override string DeleteButtonText => "删除游戏物体轨道项";

        public override VisualElement CreateInspectorGUI()
        {
            gameObjectTargetData = target as GameObjectTrackItemData;
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

        protected override void PerformDelete()
        {
            SafeExecute(() =>
            {
                DeleteGameObjectTrackItem();
            }, "删除游戏物体轨道项");
        }

        /// <summary>
        /// 删除游戏物体轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteGameObjectTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.gameObjectTrack == null || gameObjectTargetData == null)
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
                        // 查找要删除的片段
                        var clipToRemove = track.gameObjectClips.FirstOrDefault(clip =>
                            clip.clipName == gameObjectTargetData.trackItemName &&
                            clip.startFrame == gameObjectTargetData.startFrame);

                        if (clipToRemove != null)
                        {
                            track.gameObjectClips.Remove(clipToRemove);
                            deleted = true;
                            Debug.Log($"从配置中删除游戏物体片段: {clipToRemove.clipName} (轨道索引: {gameObjectTargetData.trackIndex})");
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
                Debug.LogWarning($"未找到要删除的游戏物体片段: {gameObjectTargetData.trackItemName} (轨道索引: {gameObjectTargetData.trackIndex})");
            }

            // 删除ScriptableObject资产
            if (gameObjectTargetData != null)
            {
                UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(gameObjectTargetData));
            }

            // 清空Inspector选择
            UnityEditor.Selection.activeObject = null;

            Debug.Log($"删除游戏物体轨道项: {gameObjectTargetData.trackItemName}");
        }

        #region 字段变化回调

        private void OnPrefabChanged(GameObject newPrefab)
        {
            SafeExecute(() =>
           {
               UpdateGameObjectTrackConfig(configClip => configClip.prefab = newPrefab, "预制体更新");
               // 预制体更改可能影响轨道项显示，刷新UI
           }, "预制体改变");
        }

        private void OnAutoDestroyChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.autoDestroy = newValue, "自动销毁更新");
            }, "自动销毁");
        }

        private void OnPositionOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.positionOffset = newValue, "位置偏移更新");
            }, "位置偏移更新");
        }

        private void OnRotationOffsetChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.rotationOffset = newValue, "旋转偏移更新");
            }, "旋转偏移更新");
        }

        private void OnScaleChanged(Vector3 newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.scale = newValue, "缩放更新");
            }, "缩放更新");
        }

        private void OnUseParentChanged(bool newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.useParent = newValue, "使用父对象更新");
            }, "使用父对象更新");
        }

        private void OnParentNameChanged(string newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.parentName = newValue, "父对象名称更新");
            }, "父对象名称更新");
        }

        private void OnDestroyDelayChanged(float newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.destroyDelay = newValue, "延迟销毁更新");
            }, "延迟销毁更新");
        }

        /// <summary>
        /// 起始帧变化事件处理
        /// </summary>
        /// <param name="newValue">新的起始帧值</param>
        protected override void OnStartFrameChanged(int newValue)
        {
            SafeExecute(() =>
            {
                UpdateGameObjectTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
                // 调用基类方法进行UI刷新
                base.OnStartFrameChanged(newValue);
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
                UpdateGameObjectTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧数更新");
            }, "持续帧数更新");
        }

        #endregion

        #region 数据同步方法

        /// <summary>
        /// 统一的游戏物体配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateGameObjectTrackConfig(System.Action<FFramework.Kit.GameObjectTrack.GameObjectClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.gameObjectTrack == null || gameObjectTargetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或游戏物体轨道为空");
                return;
            }

            // 查找对应的游戏物体片段配置
            FFramework.Kit.GameObjectTrack.GameObjectClip targetConfigClip = null;

            if (skillConfig.trackContainer.gameObjectTrack.gameObjectTracks != null)
            {
                // 遍历所有游戏物体轨道查找匹配的片段
                foreach (var track in skillConfig.trackContainer.gameObjectTrack.gameObjectTracks)
                {
                    if (track.gameObjectClips != null)
                    {
                        var candidateClips = track.gameObjectClips
                            .Where(clip => clip.clipName == gameObjectTargetData.trackItemName).ToList();

                        if (candidateClips.Count > 0)
                        {
                            if (candidateClips.Count == 1)
                            {
                                targetConfigClip = candidateClips[0];
                                break;
                            }
                            else
                            {
                                // 如果有多个同名片段，尝试通过起始帧匹配
                                var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == gameObjectTargetData.startFrame);
                                if (exactMatch != null)
                                {
                                    targetConfigClip = exactMatch;
                                    break;
                                }
                                else
                                {
                                    targetConfigClip = candidateClips[0];
                                    break;
                                }
                            }
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
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的游戏物体片段配置");
            }
        }

        #endregion
    }
}
