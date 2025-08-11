using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器轨道基类
    /// 定义所有轨道类型的通用行为和接口
    /// </summary>
    public abstract class BaseSkillEditorTrack : VisualElement
    {
        #region 受保护字段

        /// <summary>轨道显示区域的可视元素</summary>
        protected VisualElement trackArea;

        /// <summary>当前轨道的类型</summary>
        protected TrackType trackType;

        /// <summary>轨道索引，用于多轨道类型的数据映射</summary>
        protected int trackIndex;

        /// <summary>轨道中的所有轨道项列表</summary>
        protected List<BaseTrackItemView> trackItems = new List<BaseTrackItemView>();

        /// <summary>当前关联的技能配置</summary>
        protected FFramework.Kit.SkillConfig skillConfig;

        #endregion

        #region 构造函数

        /// <summary>
        /// 基础轨道构造函数
        /// </summary>
        /// <param name="visual">父级可视元素容器</param>
        /// <param name="trackType">轨道类型</param>
        /// <param name="width">轨道初始宽度</param>
        /// <param name="skillConfig">技能配置对象</param>
        /// <param name="trackIndex">轨道索引</param>
        protected BaseSkillEditorTrack(VisualElement visual, TrackType trackType, float width, FFramework.Kit.SkillConfig skillConfig, int trackIndex = 0)
        {
            this.trackType = trackType;
            this.skillConfig = skillConfig;
            this.trackIndex = trackIndex;

            InitializeTrackArea(visual, width);
            ApplyTrackTypeStyle();
            RegisterDragEvents();
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 检查是否可以接受拖拽的对象
        /// 由具体轨道类型实现
        /// </summary>
        /// <param name="obj">拖拽的对象</param>
        /// <returns>是否可以接受</returns>
        protected abstract bool CanAcceptDraggedObject(Object obj);

        /// <summary>
        /// 创建轨道项并添加到配置
        /// 由具体轨道类型实现
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        protected abstract BaseTrackItemView CreateTrackItemFromResource(object resource, int startFrame, bool addToConfig);

        /// <summary>
        /// 应用轨道类型特定的样式
        /// 由具体轨道类型实现
        /// </summary>
        protected abstract void ApplySpecificTrackStyle();

        #endregion

        #region 虚方法

        /// <summary>
        /// 获取帧率，子类可以重写
        /// </summary>
        /// <returns>帧率</returns>
        protected virtual float GetFrameRate()
        {
            return (skillConfig != null && skillConfig.frameRate > 0) ? skillConfig.frameRate : 30f;
        }

        #endregion

        #region 通用实现

        /// <summary>
        /// 初始化轨道区域
        /// </summary>
        /// <param name="visual">父容器</param>
        /// <param name="width">宽度</param>
        private void InitializeTrackArea(VisualElement visual, float width)
        {
            trackArea = new VisualElement();
            trackArea.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
            trackArea.AddToClassList("TrackArea");
            trackArea.style.width = width;
            visual.Add(trackArea);
        }

        /// <summary>
        /// 应用轨道类型样式
        /// </summary>
        private void ApplyTrackTypeStyle()
        {
            // 应用通用样式
            switch (trackType)
            {
                case TrackType.AnimationTrack:
                    trackArea.AddToClassList("TrackArea-Animation");
                    break;
                case TrackType.AudioTrack:
                    trackArea.AddToClassList("TrackArea-Audio");
                    break;
                case TrackType.EffectTrack:
                    trackArea.AddToClassList("TrackArea-Effect");
                    break;
                case TrackType.EventTrack:
                    trackArea.AddToClassList("TrackArea-Event");
                    break;
                case TrackType.InjuryDetectionTrack:
                    trackArea.AddToClassList("TrackArea-Attack");
                    break;
                case TrackType.TransformTrack:
                    trackArea.AddToClassList("TrackArea-Transform");
                    break;
                case TrackType.CameraTrack:
                    trackArea.AddToClassList("TrackArea-Camera");
                    break;
                case TrackType.GameObjectTrack:
                    trackArea.AddToClassList("TrackArea-GameObject");
                    break;
            }

            // 应用特定样式
            ApplySpecificTrackStyle();
        }

        /// <summary>
        /// 注册拖拽事件
        /// </summary>
        private void RegisterDragEvents()
        {
            trackArea.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            trackArea.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        #endregion

#if UNITY_EDITOR

        #region 拖拽处理

        /// <summary>
        /// 处理拖拽更新事件
        /// </summary>
        /// <param name="evt">拖拽更新事件</param>
        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (CanAcceptDrag())
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        /// <summary>
        /// 处理拖拽执行事件
        /// </summary>
        /// <param name="evt">拖拽执行事件</param>
        private void OnDragPerform(DragPerformEvent evt)
        {
            if (!CanAcceptDrag()) return;
            DragAndDrop.AcceptDrag();

            // 计算拖拽位置对应的帧数
            float mouseX = evt.localMousePosition.x;
            float unit = SkillEditorData.FrameUnitWidth;
            int frameIndex = Mathf.RoundToInt(mouseX / unit);

            // 为每个拖拽的对象创建轨道项
            foreach (var obj in DragAndDrop.objectReferences)
            {
                Debug.Log($"BaseSkillEditorTrack.OnDragPerform: 处理对象 {obj?.name} (类型: {obj?.GetType()?.Name})");
                AddTrackItem(obj, frameIndex); // 这里默认 addToConfig = true
            }
            evt.StopPropagation();
        }

        /// <summary>
        /// 检查是否可以接受拖拽
        /// </summary>
        /// <returns>是否可以接受</returns>
        private bool CanAcceptDrag()
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (CanAcceptDraggedObject(obj))
                    return true;
            }
            return false;
        }

        #endregion

#endif

        #region 公共方法

        /// <summary>
        /// 添加轨道项
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <returns>创建的轨道项</returns>
        public BaseTrackItemView AddTrackItem(object resource, int startFrame = 0)
        {
            return AddTrackItem(resource, startFrame, true);
        }

        /// <summary>
        /// 添加轨道项
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        public BaseTrackItemView AddTrackItem(object resource, int startFrame, bool addToConfig)
        {
            var newItem = CreateTrackItemFromResource(resource, startFrame, addToConfig);
            if (newItem != null)
            {
                trackItems.Add(newItem);
            }
            return newItem;
        }

        /// <summary>
        /// 添加轨道项（带自定义名称）
        /// </summary>
        /// <param name="resource">资源对象</param>
        /// <param name="itemName">自定义名称</param>
        /// <param name="startFrame">起始帧</param>
        /// <param name="addToConfig">是否添加到配置</param>
        /// <returns>创建的轨道项</returns>
        public virtual BaseTrackItemView AddTrackItem(object resource, string itemName, int startFrame, bool addToConfig)
        {
            // 默认实现，子类可以重写以支持自定义名称
            return AddTrackItem(resource, startFrame, addToConfig);
        }

        /// <summary>
        /// 更新所有轨道项的宽度
        /// </summary>
        public void UpdateTrackItemsWidth()
        {
            foreach (var item in trackItems)
            {
                item.SetWidth();
            }
        }

        /// <summary>
        /// 刷新所有轨道项的显示
        /// </summary>
        public virtual void RefreshTrackItems()
        {
            foreach (var item in trackItems)
            {
                item.RefreshDisplay();
            }
        }

        /// <summary>
        /// 设置轨道宽度
        /// </summary>
        /// <param name="width">新宽度</param>
        public void SetWidth(float width)
        {
            if (trackArea != null)
            {
                trackArea.style.width = width;
            }
        }

        #endregion
    }
}
