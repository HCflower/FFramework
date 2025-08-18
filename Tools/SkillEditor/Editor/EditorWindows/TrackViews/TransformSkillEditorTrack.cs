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
    /// 变换轨道实现
    /// 专门处理Transform变换的创建、显示和配置管理
    /// </summary>
    public class TransformSkillEditorTrack : SkillEditorTrackBase
    {
        #region 私有字段

        // 私有字段可以在这里声明

        #endregion

        #region 构造函数

        /// <summary>
        /// 变换轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public TransformSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.TransformTrack, width, skillConfig, trackIndex)
        {
        }

        #endregion

        #region 抽象方法实现
        /// <summary>
        /// 应用游戏物体轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 游戏物体轨道特有的样式设置
            trackArea.AddToClassList("TrackArea-Transform");
        }

        /// <summary>
        /// 检查是否可以接受拖拽的对象（变换轨道主要通过UI创建，不接受拖拽）
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否可以接受GameObject</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            return obj is GameObject;
        }

        /// <summary>
        /// 从对象创建变换轨道项
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override TrackItemViewBase CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            string itemName = "";
            GameObject targetObject = null;

            if (resource is GameObject gameObject)
            {
                targetObject = gameObject;
                itemName = $"Transform {gameObject.name}";
            }
            else if (resource is string name)
            {
                itemName = name;
            }
            else
            {
                return null;
            }

            int frameCount = 5;

            if (addToConfig)
            {
                AddTrackItemDataToConfig(itemName, targetObject, startFrame, frameCount);
                SkillEditorEvent.TriggerRefreshRequested();
            }

            var newItem = new TransformTrackItem(trackArea, itemName, frameCount, startFrame, trackIndex);
            trackItems.Add(newItem);
            return newItem;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 根据索引从配置创建变换轨道项
        /// </summary>
        /// <param name="track">变换轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(TransformSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var transformTrack = skillConfig.trackContainer.transformTrack;
            if (transformTrack == null)
            {
                Debug.Log("CreateTransformTrackItemsFromConfig: 没有找到变换轨道数据");
                return;
            }

            if (transformTrack.transformClips != null)
            {
                foreach (var clip in transformTrack.transformClips)
                {
                    var trackItem = track.AddTrackItem(clip.clipName, clip.startFrame, false);

                    if (trackItem is TransformTrackItem transformTrackItem)
                    {
                        var transformData = transformTrackItem.TransformData;
                        transformData.durationFrame = clip.durationFrame;
                        transformData.trackItemName = clip.clipName;
                        transformData.enablePosition = clip.enablePosition;
                        transformData.enableRotation = clip.enableRotation;
                        transformData.enableScale = clip.enableScale;
                        transformData.positionOffset = clip.positionOffset;
                        transformData.targetRotation = clip.targetRotation;
                        transformData.targetScale = clip.targetScale;
                        transformData.curveType = clip.curveType;
                        transformData.customCurve = clip.customCurve;

#if UNITY_EDITOR
                        EditorUtility.SetDirty(transformData);
#endif
                    }

                    if (clip.durationFrame > 0)
                    {
                        trackItem?.UpdateFrameCount(clip.durationFrame);
                    }
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将变换添加到技能配置的变换轨道中
        /// </summary>
        /// <param name="trackItemName">变换名称</param>
        /// <param name="targetObject">目标对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddTrackItemDataToConfig(string trackItemName, GameObject targetObject, int startFrame, int frameCount)
        {
            string finalName = trackItemName;
            if (skillConfig?.trackContainer?.transformTrack != null)
            {
                int suffix = 1;
                while (skillConfig.trackContainer.transformTrack.transformClips?
                    .Any(clip => clip.clipName == finalName) == true)
                {
                    finalName = $"{trackItemName}_{suffix++}";
                }
            }
            if (skillConfig?.trackContainer == null) return;

            if (skillConfig.trackContainer.transformTrack == null)
            {
                var transformTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.TransformTrackSO>();
                transformTrackSO.trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.TransformTrack, 0);
                transformTrackSO.transformClips = new List<FFramework.Kit.TransformTrack.TransformClip>();
                skillConfig.trackContainer.transformTrack = transformTrackSO;

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.AddObjectToAsset(transformTrackSO, skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }

            var transformTrack = skillConfig.trackContainer.transformTrack;

            if (transformTrack.transformClips == null)
            {
                transformTrack.transformClips = new List<FFramework.Kit.TransformTrack.TransformClip>();
            }

            var configTransformClip = new FFramework.Kit.TransformTrack.TransformClip
            {
                clipName = finalName,
                startFrame = startFrame,
                durationFrame = frameCount,
                enablePosition = true,
                enableRotation = false,
                enableScale = false,
                curveType = FFramework.Kit.AnimationCurveType.Linear,
            };

            transformTrack.transformClips.Add(configTransformClip);

            Debug.Log($"AddTransformToConfig: 添加变换 '{finalName}' 到轨道索引 {trackIndex}");

#if UNITY_EDITOR
            if (transformTrack != null)
            {
                EditorUtility.SetDirty(transformTrack);
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
