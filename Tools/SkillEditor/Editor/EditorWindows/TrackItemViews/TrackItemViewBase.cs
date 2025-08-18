using UnityEngine.UIElements;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 轨道项视图基类
    /// 提供轨道项的基础视图设置功能，包括拖拽交互
    /// </summary>
    public abstract class TrackItemViewBase : VisualElement
    {
        #region 受保护字段

        /// <summary>轨道项容器</summary>
        protected VisualElement trackItem;

        /// <summary>轨道项内容容器</summary>
        protected VisualElement itemContent;

        /// <summary>起始帧位置</summary>
        protected int startFrame;

        /// <summary>轨道项持续帧数</summary>
        protected int trackItemDurationFrame;

        /// <summary>是否正在拖拽中</summary>
        protected bool isDragging = false;

        /// <summary>拖拽开始位置</summary>
        protected Vector2 dragStartPos;

        /// <summary>拖拽前的原始左边距</summary>
        protected float originalLeft;

        #endregion

        #region 公共方法
        public int GetStartFrame()
        {
            return startFrame;
        }

        public int GetDurationFrame()
        {
            return trackItemDurationFrame; // 假设有一个字段或属性表示持续帧数
        }

        /// <summary>
        /// 设置轨道项的起始帧位置
        /// 根据帧位置和当前帧单位宽度计算实际的像素位置
        /// </summary>
        /// <param name="frame">起始帧位置</param>
        public virtual void SetStartFrame(int frame)
        {
            startFrame = frame;
            UpdatePosition();
        }

        /// <summary>
        /// 更新轨道项的位置
        /// 根据起始帧位置和当前帧单位宽度重新计算像素位置
        /// </summary>
        public virtual void UpdatePosition()
        {
            if (trackItem != null)
            {
                float pixelPosition = startFrame * SkillEditorData.FrameUnitWidth;
                trackItem.style.left = pixelPosition;
            }
        }

        /// <summary>
        /// 设置轨道项宽度（虚方法，子类可重写）
        /// </summary>
        public virtual void SetWidth()
        {
            // 默认实现为空，子类可以重写
        }

        /// <summary>
        /// 刷新轨道项的显示（虚方法，子类可重写）
        /// 更新宽度和位置以适应缩放变化
        /// </summary>
        public virtual void RefreshDisplay()
        {
            SetWidth();
            UpdatePosition();
        }

        /// <summary>
        /// 更新轨道项的帧数（虚方法，子类可重写）
        /// </summary>
        /// <param name="newFrameCount">新的帧数</param>
        public virtual void UpdateFrameCount(int newFrameCount)
        {
            // 默认实现为空，子类可以重写
        }

        /// <summary>
        /// 注册拖拽相关的鼠标事件
        /// </summary>
        protected virtual void RegisterDragEvents()
        {
            if (trackItem != null)
            {
                trackItem.RegisterCallback<PointerDownEvent>(OnPointerDown);
                trackItem.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                trackItem.RegisterCallback<PointerUpEvent>(OnPointerUp);
            }
        }

        #endregion

        #region 受保护方法

        /// <summary>
        /// 鼠标按下事件处理
        /// 启动拖拽操作并可选择性地处理其他逻辑
        /// </summary>
        /// <param name="evt">鼠标按下事件参数</param>
        protected virtual void OnPointerDown(PointerDownEvent evt)
        {
            StartDrag(evt);
            OnTrackItemSelected();
        }

        /// <summary>
        /// 开始拖拽操作
        /// 记录拖拽开始位置和原始帧位置，并捕获指针
        /// </summary>
        /// <param name="evt">鼠标按下事件参数</param>
        protected virtual void StartDrag(PointerDownEvent evt)
        {
            isDragging = true;
            dragStartPos = evt.position;
            originalLeft = startFrame * SkillEditorData.FrameUnitWidth;
            trackItem?.CapturePointer(evt.pointerId);
        }

        /// <summary>
        /// 轨道项被选中时的处理（子类可重写）
        /// </summary>
        protected virtual void OnTrackItemSelected()
        {
            // 默认实现为空，子类可以重写
        }

        /// <summary>
        /// 鼠标移动事件处理
        /// 当正在拖拽时更新轨道项位置并对齐到刻度
        /// </summary>
        /// <param name="evt">鼠标移动事件参数</param>
        protected virtual void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging) return;

            float newLeft = CalculateNewPosition(evt);
            newLeft = ClampToTrackBounds(newLeft);

            // 更新像素位置和对应的帧位置
            if (trackItem != null)
            {
                trackItem.style.left = newLeft;
                int newStartFrame = (int)(newLeft / SkillEditorData.FrameUnitWidth);

                // 如果起始帧发生变化，更新数据
                if (newStartFrame != startFrame)
                {
                    SetStartFrame(newStartFrame);
                    OnStartFrameChanged(startFrame);
                }
            }
        }

        /// <summary>
        /// 起始帧发生变化时的处理（子类可重写）
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected virtual void OnStartFrameChanged(int newStartFrame)
        {
            // 默认实现为空，子类可以重写
        }

        /// <summary>
        /// 鼠标释放事件处理
        /// 结束拖拽操作并释放指针捕获
        /// </summary>
        /// <param name="evt">鼠标释放事件参数</param>
        protected virtual void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging) return;
            isDragging = false;
            trackItem?.ReleasePointer(evt.pointerId);

            if (trackItem != null)
            {
                int finalStartFrame = (int)(trackItem.style.left.value.value / SkillEditorData.FrameUnitWidth);
                SetStartFrame(finalStartFrame);
                OnDragCompleted();
            }
        }

        /// <summary>
        /// 拖拽完成时的处理（子类可重写）
        /// </summary>
        protected virtual void OnDragCompleted()
        {
            // 默认实现为空，子类可以重写
        }

        /// <summary>
        /// 计算拖拽时的新位置
        /// 根据鼠标移动距离计算新位置并对齐到帧刻度
        /// </summary>
        /// <param name="evt">鼠标移动事件参数</param>
        /// <returns>对齐到刻度的新左边距位置</returns>
        protected virtual float CalculateNewPosition(PointerMoveEvent evt)
        {
            float deltaX = evt.position.x - dragStartPos.x;
            float newLeft = originalLeft + deltaX;

            // 对齐到帧刻度
            float unit = SkillEditorData.FrameUnitWidth;
            return Mathf.Round(newLeft / unit) * unit;
        }

        /// <summary>
        /// 确保轨道项不会拖拽到轨道区域之外
        /// </summary>
        /// <param name="newLeft">计算得到的新左边距位置</param>
        /// <returns>限制在边界内的左边距位置</returns>
        protected virtual float ClampToTrackBounds(float newLeft)
        {
            if (trackItem?.parent == null) return newLeft;

            float trackWidth = trackItem.parent.resolvedStyle.width;
            float itemWidth = trackItem.resolvedStyle.width;

            // 如果宽度为0，使用计算出的宽度
            if (itemWidth <= 0)
            {
                itemWidth = trackItemDurationFrame * SkillEditorData.FrameUnitWidth;
            }

            if (trackWidth <= 0)
            {
                return Mathf.Max(0, newLeft);
            }

            // 让左边界最大值对齐到帧刻度
            float unit = SkillEditorData.FrameUnitWidth;
            // 计算最大允许的帧数，要考虑第0帧宽度+1
            int maxFrame = Mathf.FloorToInt((trackWidth + 1 - itemWidth) / unit);
            float maxLeft = maxFrame * unit;

            return Mathf.Clamp(newLeft, 0, maxLeft);
        }

        #endregion
    }
}
