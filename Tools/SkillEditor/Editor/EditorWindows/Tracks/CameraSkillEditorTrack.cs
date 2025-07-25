using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 摄像机轨道实现
    /// 专门处理摄像机的创建、显示和配置管理
    /// </summary>
    public class CameraSkillEditorTrack : BaseSkillEditorTrack
    {
        /// <summary>
        /// 摄像机轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        public CameraSkillEditorTrack(VisualElement visual, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
            : base(visual, TrackType.CameraTrack, width, skillConfig, trackIndex)
        {
        }

        #region 抽象方法实现

        /// <summary>
        /// 检查是否可以接受拖拽的对象（摄像机轨道主要通过UI创建，可以接受Camera组件）
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否可以接受Camera组件</returns>
        protected override bool CanAcceptDraggedObject(Object obj)
        {
            // 摄像机轨道可以接受Camera组件或有Camera组件的GameObject
            if (obj is Camera) return true;
            if (obj is GameObject gameObject && gameObject.GetComponent<Camera>() != null) return true;
            return false;
        }

        /// <summary>
        /// 从对象创建摄像机轨道项
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected override SkillEditorTrackItem CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            string itemName = "";
            Camera targetCamera = null;

            // 如果是Camera组件，直接使用
            if (resource is Camera camera)
            {
                targetCamera = camera;
                itemName = $"Camera {camera.name}";
            }
            // 如果是GameObject，获取Camera组件
            else if (resource is GameObject gameObject)
            {
                targetCamera = gameObject.GetComponent<Camera>();
                if (targetCamera != null)
                {
                    itemName = $"Camera {gameObject.name}";
                }
            }
            // 如果是字符串，创建默认摄像机动作
            else if (resource is string name)
            {
                itemName = name;
            }
            else
            {
                return null;
            }

            // 摄像机轨道项默认30帧长度（1秒）
            int frameCount = 30;
            var newItem = new SkillEditorTrackItem(trackArea, itemName, trackType, frameCount, startFrame);

            // 添加到技能配置
            if (addToConfig)
            {
                AddCameraToConfig(itemName, targetCamera, startFrame, frameCount);
            }

            return newItem;
        }

        /// <summary>
        /// 应用摄像机轨道特定样式
        /// </summary>
        protected override void ApplySpecificTrackStyle()
        {
            // 摄像机轨道特有的样式设置
            // 可以在这里添加摄像机轨道特有的视觉效果
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 创建新的摄像机轨道项
        /// </summary>
        /// <param name="cameraName">摄像机动作名称</param>
        /// <param name="targetCamera">目标摄像机</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">持续帧数</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的摄像机轨道项</returns>
        public SkillEditorTrackItem CreateCameraItem(string cameraName, Camera targetCamera, int startFrame, int frameCount = 60, bool addToConfig = true)
        {
            var newItem = new SkillEditorTrackItem(trackArea, cameraName, trackType, frameCount, startFrame);

            if (addToConfig)
            {
                AddCameraToConfig(cameraName, targetCamera, startFrame, frameCount);
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
        /// 将摄像机动作添加到技能配置的摄像机轨道中
        /// </summary>
        /// <param name="cameraName">摄像机动作名称</param>
        /// <param name="targetCamera">目标摄像机</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddCameraToConfig(string cameraName, Camera targetCamera, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保摄像机轨道存在
            if (skillConfig.trackContainer.cameraTrack == null)
            {
                skillConfig.trackContainer.cameraTrack = new FFramework.Kit.CameraTrack();
                skillConfig.trackContainer.cameraTrack.trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.CameraTrack, 0);
                skillConfig.trackContainer.cameraTrack.cameraClips = new System.Collections.Generic.List<FFramework.Kit.CameraTrack.CameraClip>();
            }

            // 获取摄像机轨道
            var cameraTrack = skillConfig.trackContainer.cameraTrack;

            // 确保摄像机片段列表存在
            if (cameraTrack.cameraClips == null)
            {
                cameraTrack.cameraClips = new System.Collections.Generic.List<FFramework.Kit.CameraTrack.CameraClip>();
            }

            // 创建技能配置中的摄像机片段数据
            var configCameraClip = new FFramework.Kit.CameraTrack.CameraClip
            {
                clipName = cameraName,
                startFrame = startFrame,
                durationFrame = frameCount,
                // 默认摄像机动作参数
                startPosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero,
                endPosition = targetCamera != null ? targetCamera.transform.position : Vector3.forward * 5,
                startRotation = targetCamera != null ? targetCamera.transform.rotation.eulerAngles : Vector3.zero,
                endRotation = targetCamera != null ? targetCamera.transform.rotation.eulerAngles : Vector3.zero,
                startFieldOfView = targetCamera != null ? targetCamera.fieldOfView : 60f,
                endFieldOfView = targetCamera != null ? targetCamera.fieldOfView : 60f,
                curveType = FFramework.Kit.AnimationCurveType.Linear,
                isRelative = false,
            };

            // 添加到摄像机轨道
            cameraTrack.cameraClips.Add(configCameraClip);

            Debug.Log($"AddCameraToConfig: 添加摄像机动作 '{cameraName}' 到摄像机轨道");

#if UNITY_EDITOR
            // 标记技能配置为已修改
            if (skillConfig != null)
            {
                EditorUtility.SetDirty(skillConfig);
            }
#endif
        }

        #endregion
    }
}
