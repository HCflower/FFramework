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
    public class GameObjectSkillEditorTrack : BaseSkillEditorTrack
    {
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
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            if (!(resource is GameObject gameObject))
                return null;

            float frameRate = GetFrameRate();
            int frameCount = Mathf.RoundToInt(1f * frameRate); // 默认1秒持续时间
            string itemName = gameObject.name;

            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame, trackIndex);

            // 设置游戏物体轨道项的数据
            if (newItem.ItemData is GameObjectTrackItemData gameObjectData)
            {
                gameObjectData.prefab = gameObject;
                gameObjectData.autoDestroy = true;
                gameObjectData.positionOffset = Vector3.zero;
                gameObjectData.rotationOffset = Vector3.zero;
                gameObjectData.scale = Vector3.one;
                gameObjectData.useParent = false;
                gameObjectData.parentName = "";
                gameObjectData.destroyDelay = -1f;

#if UNITY_EDITOR
                EditorUtility.SetDirty(gameObjectData);
#endif
            }

            // 添加到技能配置
            if (addToConfig)
            {
                AddGameObjectToConfig(gameObject, itemName, startFrame, frameCount);
            }

            return newItem;
        }

        /// <summary>
        /// 应用游戏物体轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 游戏物体轨道特有的样式设置
            // 可以在这里添加游戏物体轨道特有的视觉效果
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 支持自定义名称的游戏物体轨道项添加
        /// </summary>
        /// <param name="resource">游戏物体资源</param>
        /// <param name="itemName">自定义名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        public override SkillEditorTrackItem AddTrackItem(object resource, string itemName, int startFrame, bool addToConfig)
        {
            if (!(resource is GameObject gameObject))
                return null;

            float frameRate = GetFrameRate();
            int frameCount = Mathf.RoundToInt(1f * frameRate); // 默认1秒持续时间

            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame, trackIndex);

            // 设置游戏物体轨道项的数据
            if (newItem.ItemData is GameObjectTrackItemData gameObjectData)
            {
                gameObjectData.prefab = gameObject;
                gameObjectData.autoDestroy = true;
                gameObjectData.positionOffset = Vector3.zero;
                gameObjectData.rotationOffset = Vector3.zero;
                gameObjectData.scale = Vector3.one;
                gameObjectData.useParent = false;
                gameObjectData.parentName = "";
                gameObjectData.destroyDelay = -1f;

#if UNITY_EDITOR
                EditorUtility.SetDirty(gameObjectData);
#endif
            }

            // 添加到技能配置
            if (addToConfig)
            {
                AddGameObjectToConfig(gameObject, itemName, startFrame, frameCount);
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
        /// 将游戏物体添加到技能配置的游戏物体轨道中
        /// </summary>
        /// <param name="gameObject">游戏物体</param>
        /// <param name="itemName">轨道项名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddGameObjectToConfig(GameObject gameObject, string itemName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保游戏物体轨道存在
            if (skillConfig.trackContainer.gameObjectTrack == null)
            {
                // 创建游戏物体轨道ScriptableObject
                var newGameObjectTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.GameObjectTrackSO>();
                skillConfig.trackContainer.gameObjectTrack = newGameObjectTrackSO;

#if UNITY_EDITOR
                // 将ScriptableObject作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(newGameObjectTrackSO, skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }

            // 获取游戏物体轨道SO
            var gameObjectTrackSO = skillConfig.trackContainer.gameObjectTrack;

            // 确保至少有一个轨道存在
            gameObjectTrackSO.EnsureTrackExists();

            // 确保指定索引的轨道存在
            while (gameObjectTrackSO.gameObjectTracks.Count <= trackIndex)
            {
                gameObjectTrackSO.AddTrack($"GameObject Track {gameObjectTrackSO.gameObjectTracks.Count}");
            }

            // 获取指定索引的游戏物体轨道
            var gameObjectTrack = gameObjectTrackSO.gameObjectTracks[trackIndex];

            // 确保游戏物体片段列表存在
            if (gameObjectTrack.gameObjectClips == null)
            {
                gameObjectTrack.gameObjectClips = new System.Collections.Generic.List<FFramework.Kit.GameObjectTrack.GameObjectClip>();
            }

            // 创建技能配置中的游戏物体片段数据
            var configGameObjectClip = new FFramework.Kit.GameObjectTrack.GameObjectClip
            {
                clipName = itemName,
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

            // 添加到对应索引的游戏物体轨道
            gameObjectTrack.gameObjectClips.Add(configGameObjectClip);

            Debug.Log($"AddGameObjectToConfig: 添加游戏物体 '{itemName}' 到轨道索引 {trackIndex}");

#if UNITY_EDITOR
            // 标记轨道数据和技能配置为已修改
            if (gameObjectTrackSO != null)
            {
                EditorUtility.SetDirty(gameObjectTrackSO);
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
        /// 根据索引从配置创建游戏物体轨道项
        /// </summary>
        /// <param name="track">游戏物体轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(GameObjectSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var gameObjectTrack = skillConfig.trackContainer.gameObjectTrack;
            if (gameObjectTrack == null)
            {
                Debug.Log($"CreateGameObjectTrackItemsFromConfig: 没有找到游戏物体轨道数据");
                return;
            }

            // 根据索引获取对应的轨道数据
            var targetTrack = gameObjectTrack.gameObjectTracks?.FirstOrDefault(t => t.trackIndex == trackIndex);
            if (targetTrack?.gameObjectClips == null)
            {
                Debug.Log($"CreateGameObjectTrackItemsFromConfig: 没有找到索引为{trackIndex}的游戏物体轨道数据");
                return;
            }

            foreach (var clip in targetTrack.gameObjectClips)
            {
                if (clip.prefab != null)
                {
                    // 从配置加载时，使用配置中的名称，并设置addToConfig为false，避免重复添加到配置文件
                    var trackItem = track.AddTrackItem(clip.prefab, clip.clipName, clip.startFrame, false);

                    // 从配置中恢复完整的游戏物体属性
                    if (trackItem?.ItemData is GameObjectTrackItemData gameObjectData)
                    {
                        gameObjectData.durationFrame = clip.durationFrame;
                        gameObjectData.autoDestroy = clip.autoDestroy;
                        gameObjectData.positionOffset = clip.positionOffset;
                        gameObjectData.rotationOffset = clip.rotationOffset;
                        gameObjectData.scale = clip.scale;
                        gameObjectData.useParent = clip.useParent;
                        gameObjectData.parentName = clip.parentName;
                        gameObjectData.destroyDelay = clip.destroyDelay;

#if UNITY_EDITOR
                        // 标记数据已修改
                        UnityEditor.EditorUtility.SetDirty(gameObjectData);
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
