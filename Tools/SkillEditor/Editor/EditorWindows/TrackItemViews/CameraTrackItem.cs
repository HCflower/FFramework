using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 摄像机轨道项
    /// 专门处理摄像机轨道项的显示、交互和数据管理
    /// 提供摄像机特有的事件视图和Inspector数据功能
    /// </summary>
    public class CameraTrackItem : BaseTrackItemView
    {
        #region 私有字段

        /// <summary>轨道项持续帧数</summary>
        private int frameCount;

        /// <summary>轨道索引，用于多轨道数据定位</summary>
        private int trackIndex;

        /// <summary>当前摄像机轨道项数据对象</summary>
        private CameraTrackItemData currentCameraData;

        /// <summary>摄像机事件标签</summary>
        private Label cameraEvent;

        /// <summary> 还原帧视图/// </summary>
        private Label restoreFrameView;

        #endregion

        #region 构造函数

        /// <summary>
        /// 摄像机轨道项构造函数
        /// 创建并初始化摄像机轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="frameCount">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="trackIndex">轨道索引，用于多轨道数据定位，默认为0</param>
        public CameraTrackItem(VisualElement visual, string title, int frameCount, int startFrame = 0, int trackIndex = 0)
        {
            this.frameCount = frameCount;
            this.startFrame = startFrame;
            this.trackIndex = trackIndex;

            // 创建并配置轨道项容器
            InitializeCameraTrackItem();

            // 创建轨道项内容
            itemContent = CreateCameraTrackItemContent(title);
            trackItem.Add(itemContent);

            // 设置宽度和位置
            SetWidth();
            UpdatePosition();
            visual.Add(trackItem);

            // 初始化摄像机数据
            currentCameraData = CreateCameraTrackItemData();

            // 设置摄像机事件视图
            SetEventFrameView(currentCameraData.animationStartFrameOffset, currentCameraData.animationDurationFrame, cameraEvent);
            // 设置还原帧视图
            SetRestoreFrameView(currentCameraData.restoreFrame, restoreFrameView);

            // 注册拖拽事件
            RegisterDragEvents();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取当前摄像机轨道项的数据对象
        /// </summary>
        public CameraTrackItemData CameraData
        {
            get
            {
                if (currentCameraData == null)
                {
                    currentCameraData = CreateCameraTrackItemData();
                }
                return currentCameraData;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置轨道项的起始帧位置
        /// 根据帧位置和当前帧单位宽度计算实际的像素位置
        /// </summary>
        /// <param name="frame">起始帧位置</param>
        public override void SetStartFrame(int frame)
        {
            startFrame = frame;
            UpdatePosition();
        }

        /// <summary>
        /// 更新轨道项的位置
        /// 根据起始帧位置和当前帧单位宽度重新计算像素位置
        /// </summary>
        public override void UpdatePosition()
        {
            float pixelPosition = startFrame * SkillEditorData.FrameUnitWidth;
            trackItem.style.left = pixelPosition;
        }

        /// <summary>
        /// 根据帧数和单位宽度设置轨道项宽度
        /// 宽度会根据SkillEditorData中的帧单位宽度动态计算
        /// </summary>
        public override void SetWidth()
        {
            itemContent.style.width = frameCount * SkillEditorData.FrameUnitWidth;
        }

        /// <summary>
        /// 更新轨道项的帧数，并重新计算宽度
        /// </summary>
        /// <param name="newFrameCount">新的帧数</param>
        public override void UpdateFrameCount(int newFrameCount)
        {
            frameCount = newFrameCount;
            SetWidth();
        }

        /// <summary>
        /// 获取轨道项的起始帧位置
        /// </summary>
        /// <returns>起始帧位置</returns>
        public float GetStartFrame()
        {
            return startFrame;
        }

        /// <summary>
        /// 获取轨道项的结束帧位置
        /// </summary>
        /// <returns>结束帧位置</returns>
        public float GetEndFrame()
        {
            return startFrame + frameCount;
        }

        /// <summary>
        /// 刷新轨道项的显示
        /// 更新宽度和位置以适应缩放变化
        /// </summary>
        public override void RefreshDisplay()
        {
            SetWidth();
            UpdatePosition();
            // 设置摄像机事件视图
            SetEventFrameView(currentCameraData.animationStartFrameOffset, currentCameraData.animationDurationFrame, cameraEvent);
            // 设置还原帧视图
            SetRestoreFrameView(currentCameraData.restoreFrame, restoreFrameView);
        }

        /// <summary>
        /// 设置事件视图的显示
        /// 用于在轨道项中显示特定事件的可视化表示
        /// </summary>
        /// <param name="startFrame">事件起始帧</param>
        /// <param name="durationFrame">事件持续帧数</param>
        /// <param name="eventElement">要设置的事件元素</param>
        private void SetEventFrameView(int startFrame, int durationFrame, VisualElement eventElement)
        {
            if (eventElement == null) return;
            eventElement.AddToClassList("CameraEvent");
            eventElement.style.left = startFrame * SkillEditorData.FrameUnitWidth - 2f;
            eventElement.style.width = durationFrame * SkillEditorData.FrameUnitWidth - 0.5f;
        }

        /// <summary>
        /// 设置还原帧视图的显示
        /// 用于在轨道项中显示特定事件的可视化表示
        /// </summary>
        /// <param name="startFrame">还原帧起始帧</param>
        /// <param name="durationFrame">还原帧持续帧数</param>
        /// <param name="eventElement">要设置的事件元素</param>
        private void SetRestoreFrameView(int durationFrame, VisualElement eventElement)
        {
            if (eventElement == null) return;
            eventElement.AddToClassList("RestoreFrameView");
            eventElement.style.width = durationFrame * SkillEditorData.FrameUnitWidth;
        }

        #endregion

        #region 基类重写方法

        /// <summary>
        /// 摄像机轨道项被选中时的处理
        /// 选中轨道项到Inspector面板
        /// </summary>
        protected override void OnTrackItemSelected()
        {
            SelectCameraTrackItemInInspector();
        }

        /// <summary>
        /// 起始帧发生变化时的处理
        /// 更新数据对象中的起始帧值
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected override void OnStartFrameChanged(int newStartFrame)
        {
            // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
            if (currentCameraData != null)
            {
                currentCameraData.startFrame = newStartFrame;
            }
        }

        /// <summary>
        /// 拖拽完成时的处理
        /// 更新Inspector显示
        /// </summary>
        protected override void OnDragCompleted()
        {
            if (currentCameraData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化摄像机轨道项容器和基础样式
        /// </summary>
        private void InitializeCameraTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建摄像机轨道项的内容容器
        /// 应用摄像机轨道特定的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的摄像机轨道项内容容器</returns>
        private VisualElement CreateCameraTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            itemContent.AddToClassList("TrackItem-Camera"); // 摄像机轨道特定样式
            itemContent.tooltip = title;

            // 创建摄像机事件标签
            cameraEvent = new Label();
            itemContent.Add(cameraEvent);

            // 创建恢复帧标签
            restoreFrameView = new Label();
            itemContent.Add(restoreFrameView);

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为摄像机轨道项内容添加标题标签
        /// </summary>
        /// <param name="itemContent">内容容器</param>
        /// <param name="title">标题文本</param>
        private void AddTitleLabel(VisualElement itemContent, string title)
        {
            Label titleLabel = new Label();
            titleLabel.AddToClassList("TrackItemTitle");
            titleLabel.text = title;
            itemContent.Add(titleLabel);
        }

        #endregion

        #region Inspector数据管理方法

        /// <summary>
        /// 在Inspector面板中选中当前摄像机轨道项
        /// 创建摄像机轨道项数据并设置为选中对象
        /// </summary>
        private void SelectCameraTrackItemInInspector()
        {
            if (currentCameraData == null)
            {
                currentCameraData = CreateCameraTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentCameraData;
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentCameraData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentCameraData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentCameraData);
            }
        }

        /// <summary>
        /// 创建摄像机轨道项的数据对象
        /// </summary>
        /// <returns>摄像机轨道项数据对象</returns>
        private CameraTrackItemData CreateCameraTrackItemData()
        {
            string itemName = GetCameraTrackItemName();

            var cameraData = ScriptableObject.CreateInstance<CameraTrackItemData>();
            cameraData.trackItemName = itemName;
            cameraData.frameCount = frameCount;
            cameraData.startFrame = startFrame;
            // 设置轨道索引用于多轨道数据定位
            cameraData.trackIndex = trackIndex;
            cameraData.durationFrame = frameCount;

            // 设置摄像机特有的默认属性
            SetDefaultCameraProperties(cameraData);

            // 从技能配置同步摄像机数据
            SyncWithCameraConfigData(cameraData, itemName);

            return cameraData;
        }

        /// <summary>
        /// 获取摄像机轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetCameraTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 设置摄像机数据的默认属性
        /// </summary>
        /// <param name="cameraData">要设置的摄像机数据对象</param>
        private void SetDefaultCameraProperties(CameraTrackItemData cameraData)
        {
            // 默认摄像机动作参数
            cameraData.curveType = FFramework.Kit.AnimationCurveType.Linear;
            cameraData.enablePosition = true;
            cameraData.enableRotation = true;
            cameraData.enableFieldOfView = false;
            cameraData.positionOffset = Vector3.zero;
            cameraData.targetRotation = Vector3.zero;
            cameraData.targetFieldOfView = 60f;
            cameraData.shakePreset = null;
            cameraData.customCurve = AnimationCurve.Linear(0, 0, 1, 1);
            cameraData.restoreFrame = 1;
            cameraData.enableShake = false;
            cameraData.animationStartFrameOffset = startFrame;
            cameraData.animationDurationFrame = frameCount;
        }

        /// <summary>
        /// 从技能配置同步摄像机数据
        /// </summary>
        /// <param name="cameraData">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithCameraConfigData(CameraTrackItemData cameraData, string itemName)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.cameraTrack == null)
                return;

            // 查找对应的摄像机片段配置
            var configClip = skillConfig.trackContainer.cameraTrack.cameraClips?
                .FirstOrDefault(clip => clip.clipName == itemName && clip.startFrame == startFrame);

            if (configClip != null)
            {
                // 从配置中恢复摄像机属性
                cameraData.durationFrame = configClip.durationFrame;
                cameraData.curveType = configClip.curveType;
                cameraData.enablePosition = configClip.enablePosition;
                cameraData.enableRotation = configClip.enableRotation;
                cameraData.enableFieldOfView = configClip.enableFieldOfView;
                cameraData.positionOffset = configClip.positionOffset;
                cameraData.targetRotation = configClip.targetRotation;
                cameraData.targetFieldOfView = configClip.targetFieldOfView;
                cameraData.shakePreset = configClip.shakePreset;
                cameraData.customCurve = configClip.customCurve;
                cameraData.restoreFrame = configClip.restoreFrame;
                cameraData.enableShake = configClip.enableShake;
                cameraData.animationStartFrameOffset = configClip.animationStartFrameOffset;
                cameraData.animationDurationFrame = configClip.animationDurationFrame;
            }
        }

        #endregion
    }
}
