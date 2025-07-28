using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 变换轨道实现
    /// 专门处理Transform变换的创建、显示和配置管理
    /// </summary>
    public class TransformSkillEditorTrack : BaseSkillEditorTrack
    {
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

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的对象（变换轨道主要通过UI创建，不接受拖拽）
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否可以接受GameObject</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            // 变换轨道可以接受GameObject拖拽，用于设置目标对象
            return obj is GameObject;
        }

        /// <summary>
        /// 从对象创建变换轨道项
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            string itemName = "";
            GameObject targetObject = null;

            // 如果是GameObject，设置为目标对象
            if (resource is GameObject gameObject)
            {
                targetObject = gameObject;
                itemName = $"Transform {gameObject.name}";
            }
            // 如果是字符串，创建默认变换
            else if (resource is string name)
            {
                itemName = name;
            }
            else
            {
                return null;
            }

            // 变换轨道项默认30帧长度（1秒）
            int frameCount = 30;
            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame);

            // 添加到技能配置
            if (addToConfig)
            {
                AddTransformToConfig(itemName, targetObject, startFrame, frameCount);
            }

            return newItem;
        }

        /// <summary>
        /// 应用变换轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 变换轨道特有的样式设置
            // 可以在这里添加变换轨道特有的视觉效果
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 创建新的变换轨道项
        /// </summary>
        /// <param name="transformName">变换名称</param>
        /// <param name="targetObject">目标对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">持续帧数</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的变换轨道项</returns>
        public SkillEditorTrackItem CreateTransformItem(string transformName, GameObject targetObject, int startFrame, int frameCount = 30, bool addToConfig = true)
        {
            var newItem = new SkillEditorTrackItem(trackArea, transformName, trackType, frameCount, startFrame);

            if (addToConfig)
            {
                AddTransformToConfig(transformName, targetObject, startFrame, frameCount);
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
        /// 将变换添加到技能配置的变换轨道中
        /// </summary>
        /// <param name="transformName">变换名称</param>
        /// <param name="targetObject">目标对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddTransformToConfig(string transformName, GameObject targetObject, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保变换轨道存在
            if (skillConfig.trackContainer.transformTrack == null)
            {
                // 创建变换轨道ScriptableObject
                var transformTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.TransformTrackSO>();
                transformTrackSO.trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.TransformTrack, 0);
                transformTrackSO.transformClips = new System.Collections.Generic.List<FFramework.Kit.TransformTrack.TransformClip>();
                skillConfig.trackContainer.transformTrack = transformTrackSO;

#if UNITY_EDITOR
                // 将ScriptableObject作为子资产添加到技能配置文件中
                UnityEditor.AssetDatabase.AddObjectToAsset(transformTrackSO, skillConfig);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
            }

            // 获取变换轨道
            var transformTrack = skillConfig.trackContainer.transformTrack;            // 确保变换片段列表存在
            if (transformTrack.transformClips == null)
            {
                transformTrack.transformClips = new System.Collections.Generic.List<FFramework.Kit.TransformTrack.TransformClip>();
            }

            // 创建技能配置中的变换片段数据
            var configTransformClip = new FFramework.Kit.TransformTrack.TransformClip
            {
                clipName = transformName,
                startFrame = startFrame,
                durationFrame = frameCount,
                enablePosition = true,
                enableRotation = false,
                enableScale = false,
                startPosition = Vector3.zero,
                endPosition = Vector3.forward,
                startRotation = Vector3.zero,
                endRotation = Vector3.zero,
                startScale = Vector3.one,
                endScale = Vector3.one,
                curveType = FFramework.Kit.AnimationCurveType.Linear,
                isRelative = false
            };

            // 添加到对应索引的变换轨道
            transformTrack.transformClips.Add(configTransformClip);

            Debug.Log($"AddTransformToConfig: 添加变换 '{transformName}' 到轨道索引 {trackIndex}");

#if UNITY_EDITOR
            // 标记轨道数据和技能配置为已修改
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
