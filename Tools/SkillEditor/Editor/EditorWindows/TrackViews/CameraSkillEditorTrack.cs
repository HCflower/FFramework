using System.Collections.Generic;
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
        #region 私有字段

        /// <summary>摄像机轨道项列表</summary>
        private List<CameraTrackItem> cameraTrackItems = new List<CameraTrackItem>();

        #endregion
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
        protected override BaseTrackItemView CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig)
        {
            string itemName = ExtractCameraName(resource);
            if (string.IsNullOrEmpty(itemName)) return null;

            // 摄像机轨道项默认5帧长度
            int frameCount = 5;
            var cameraItem = CreateCameraTrackItem(itemName, startFrame, frameCount, addToConfig);

            return cameraItem;
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
        /// 创建专门的摄像机轨道项（推荐使用）
        /// </summary>
        /// <param name="cameraName">摄像机动作名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">持续帧数</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的专门摄像机轨道项</returns>
        public CameraTrackItem CreateCameraTrackItem(string cameraName, int startFrame, int frameCount = 5, bool addToConfig = true)
        {
            var cameraItem = new CameraTrackItem(trackArea, cameraName, frameCount, startFrame, trackIndex);

            // 添加到摄像机轨道项列表
            cameraTrackItems.Add(cameraItem);

            // 添加到基类的轨道项列表，确保在时间轴缩放时能够被刷新
            trackItems.Add(cameraItem);

            if (addToConfig)
            {
                AddCameraToConfig(cameraName, startFrame, frameCount);
            }

            return cameraItem;
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 刷新摄像机轨道项的显示
        /// 重写基类方法以处理摄像机特有的轨道项类型
        /// </summary>
        public override void RefreshTrackItems()
        {
            // 直接调用基类方法，因为摄像机轨道项已经在基类的 trackItems 列表中
            base.RefreshTrackItems();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从资源对象中提取摄像机名称
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <returns>摄像机名称，如果无法提取则返回空字符串</returns>
        private string ExtractCameraName(object resource)
        {
            return resource switch
            {
                Camera camera => $"Camera {camera.name}",
                GameObject gameObject when gameObject.GetComponent<Camera>() != null => $"Camera {gameObject.name}",
                string name => name,
                _ => string.Empty
            };
        }

        /// <summary>
        /// 将摄像机动作添加到技能配置的摄像机轨道中
        /// </summary>
        /// <param name="cameraName">摄像机动作名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="frameCount">总帧数</param>
        private void AddCameraToConfig(string cameraName, int startFrame, int frameCount)
        {
            if (skillConfig?.trackContainer == null) return;

            // 确保摄像机轨道存在
            EnsureCameraTrackExists();

            // 获取摄像机轨道
            var cameraTrack = skillConfig.trackContainer.cameraTrack;

            // 确保摄像机片段列表存在
            if (cameraTrack.cameraClips == null)
            {
                cameraTrack.cameraClips = new System.Collections.Generic.List<FFramework.Kit.CameraTrack.CameraClip>();
            }

            // 创建配置片段（最小化初始化，详细属性由CameraTrackItem处理）
            var configCameraClip = new FFramework.Kit.CameraTrack.CameraClip
            {
                clipName = cameraName,
                startFrame = startFrame,
                durationFrame = frameCount,
            };

            cameraTrack.cameraClips.Add(configCameraClip);

#if UNITY_EDITOR
            // 标记为已修改
            EditorUtility.SetDirty(cameraTrack);
            EditorUtility.SetDirty(skillConfig);
#endif
        }

        /// <summary>
        /// 确保摄像机轨道SO存在
        /// </summary>
        private void EnsureCameraTrackExists()
        {
            if (skillConfig.trackContainer.cameraTrack != null) return;

            var cameraTrackSO = ScriptableObject.CreateInstance<FFramework.Kit.CameraTrackSO>();
            cameraTrackSO.trackName = SkillEditorTrackFactory.GetDefaultTrackName(TrackType.CameraTrack, 0);
            cameraTrackSO.cameraClips = new System.Collections.Generic.List<FFramework.Kit.CameraTrack.CameraClip>();
            skillConfig.trackContainer.cameraTrack = cameraTrackSO;

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(cameraTrackSO, skillConfig);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        #endregion

        #region 配置恢复方法

        /// <summary>
        /// 根据索引从配置创建摄像机轨道项
        /// </summary>
        /// <param name="track">摄像机轨道实例</param>
        /// <param name="skillConfig">技能配置</param>
        /// <param name="trackIndex">轨道索引</param>
        public static void CreateTrackItemsFromConfig(CameraSkillEditorTrack track, FFramework.Kit.SkillConfig skillConfig, int trackIndex)
        {
            var cameraTrack = skillConfig.trackContainer.cameraTrack;
            if (cameraTrack?.cameraClips == null || trackIndex != 0) return;

            foreach (var clip in cameraTrack.cameraClips)
            {
                // 使用统一的创建方法
                var cameraTrackItem = track.CreateCameraTrackItem(clip.clipName, clip.startFrame, clip.durationFrame, false);

                // CameraTrackItem会自动同步配置数据，只需确保显示正确
                if (clip.durationFrame > 0)
                {
                    cameraTrackItem?.UpdateFrameCount(clip.durationFrame);
                }
            }
        }

        #endregion
    }
}
