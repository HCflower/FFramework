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
    /// 游戏物体轨道实现
    /// 专门处理游戏物体（预制体）的拖拽、生成和配置管理
    /// 支持多轨道并行操作
    /// </summary>
    public class GameObjectSkillEditorTrack : SkillEditorTrackBase
    {
        #region 构造函数

        /// <summary>
        /// 游戏物体轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public GameObjectSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.GameObjectTrack, width, skillConfig, trackIndex)
        { }

        #endregion

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的游戏物体
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否为游戏物体或预制体</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return obj is GameObject;
        }

        /// <summary>
        /// 从游戏物体创建轨道项
        /// </summary>
        /// <param name="resource">游戏物体资源</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override TrackItemViewBase CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is GameObject gameObject))
                return null;

            var frameCount = CalculateFrameCount();
            var newItem = CreateGameObjectTrackItem(gameObject.name, startFrame, frameCount, addToConfig);

            if (addToConfig)
            {
                AddTrackItemDataToConfig(gameObject, gameObject.name, startFrame, frameCount);
                SkillEditorEvent.OnRefreshRequested();
            }

            return newItem;
        }

        /// <summary>
        /// 应用游戏物体轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 游戏物体轨道特有的样式设置
            trackArea.AddToClassList("TrackArea-GameObject");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 创建游戏物体轨道项
        /// </summary>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的游戏物体轨道项</returns>
        public GameObjectTrackItem CreateGameObjectTrackItem(string itemName, int startFrame, int frameCount, bool addToConfig = true)
        {
            var gameObjectItem = new GameObjectTrackItem(trackArea, itemName, frameCount, startFrame, trackIndex);

            trackItems.Add(gameObjectItem);

            if (addToConfig)
            {
                AddTrackItemDataToConfig(null, itemName, startFrame, frameCount);
                SkillEditorEvent.OnRefreshRequested();
            }

            return gameObjectItem;
        }

        /// <summary>
        /// 刷新轨道项
        /// </summary>
        public override void RefreshTrackItems()
        {
            base.RefreshTrackItems();
        }

        /// <summary>
        /// 从配置创建游戏物体轨道项
        /// </summary>
        /// <param name="track">游戏物体轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(GameObjectSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var gameObjectTrack = skillConfig.trackContainer.gameObjectTrack;
            if (gameObjectTrack == null)
            {
                Debug.Log("CreateGameObjectTrackItemsFromConfig: 没有找到游戏物体轨道数据");
                return;
            }

            var targetTrack = gameObjectTrack.gameObjectTracks?.FirstOrDefault(t => t.trackIndex == trackIndex);
            if (targetTrack?.gameObjectClips == null)
            {
                Debug.Log($"CreateGameObjectTrackItemsFromConfig: 没有找到索引为{trackIndex}的游戏物体轨道数据");
                return;
            }

            foreach (var clip in targetTrack.gameObjectClips)
            {
                var trackItem = track.CreateGameObjectTrackItem(clip.clipName, clip.startFrame, clip.durationFrame, false);

                if (trackItem is GameObjectTrackItem gameObjectTrackItem)
                {
                    RestoreGameObjectData(gameObjectTrackItem, clip);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计算帧数
        /// </summary>
        /// <returns>计算得到的帧数</returns>
        private int CalculateFrameCount()
        {
            return Mathf.RoundToInt(1f * GetFrameRate()); // 默认1秒持续时间
        }

        /// <summary>
        /// 将游戏物体添加到技能配置的游戏物体轨道中
        /// </summary>
        /// <param name="gameObject">游戏物体</param>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddTrackItemDataToConfig(GameObject gameObject, string itemName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            EnsureGameObjectTrackExists();

            var gameObjectTrackSO = skillConfig.trackContainer.gameObjectTrack;

            while (gameObjectTrackSO.gameObjectTracks.Count <= trackIndex)
            {
                gameObjectTrackSO.AddTrack($"GameObject Track {gameObjectTrackSO.gameObjectTracks.Count}");
            }

            var gameObjectTrack = gameObjectTrackSO.gameObjectTracks[trackIndex];

            if (gameObjectTrack.gameObjectClips == null)
            {
                gameObjectTrack.gameObjectClips = new List<FFramework.Kit.GameObjectTrack.GameObjectClip>();
            }

            string finalName = itemName;
            int suffix = 1;
            while (gameObjectTrack.gameObjectClips.Any(c => c.clipName == finalName))
            {
                finalName = $"{itemName}_{suffix++}";
            }

            var configGameObjectClip = new FFramework.Kit.GameObjectTrack.GameObjectClip
            {
                clipName = finalName,
                startFrame = startFrame,
                durationFrame = frameCount,
                prefab = gameObject,
                autoDestroy = true,
                positionOffset = Vector3.zero,
                rotationOffset = Vector3.zero,
                scale = Vector3.one,
                useParent = false,
                parentName = "",
                destroyDelay = -1f
            };

            gameObjectTrack.gameObjectClips.Add(configGameObjectClip);

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObjectTrackSO);
            EditorUtility.SetDirty(skillConfig);
#endif
        }

        /// <summary>
        /// 确保游戏物体轨道存在
        /// </summary>
        private void EnsureGameObjectTrackExists()
        {
            if (skillConfig.trackContainer.gameObjectTrack != null) return;

            var newGameObjectTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.GameObjectTrackSO>();
            skillConfig.trackContainer.gameObjectTrack = newGameObjectTrackSO;

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(newGameObjectTrackSO, skillConfig);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// 恢复游戏物体数据
        /// </summary>
        /// <param name="trackItem">轨道项</param>
        /// <param name="clip">游戏物体片段</param>
        private static void RestoreGameObjectData(GameObjectTrackItem trackItem, FFramework.Kit.GameObjectTrack.GameObjectClip clip)
        {
            if (trackItem?.GameObjectData == null) return;

            var gameObjectData = trackItem.GameObjectData;
            gameObjectData.trackItemName = clip.clipName;
            gameObjectData.durationFrame = clip.durationFrame;
            gameObjectData.autoDestroy = clip.autoDestroy;
            gameObjectData.positionOffset = clip.positionOffset;
            gameObjectData.rotationOffset = clip.rotationOffset;
            gameObjectData.scale = clip.scale;
            gameObjectData.useParent = clip.useParent;
            gameObjectData.parentName = clip.parentName;
            gameObjectData.destroyDelay = clip.destroyDelay;

#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObjectData);
#endif
        }

        #endregion
    }
}
