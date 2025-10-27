using UnityEngine.EventSystems;
using UnityEngine;
using System;

namespace FFramework
{
    /// <summary>
    /// 高级拖拽工具，提供各种拖拽功能的封装
    /// </summary>
    public class UIDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region 配置选项

        [Header("拖拽设置")]
        [SerializeField] private bool enableDrag = true;
        [SerializeField] private bool returnToOriginalPosition = false;
        [SerializeField] private float returnSpeed = 5f;

        [Header("限制设置")]
        [SerializeField] private bool constrainToParent = false;
        [SerializeField] private bool constrainToScreen = true;
        [SerializeField] private Vector2 dragBounds = Vector2.zero; // 0表示无限制

        [Header("视觉效果")]
        [SerializeField] private bool scaleOnDrag = false;
        [SerializeField] private Vector3 dragScale = Vector3.one * 1.1f;
        [SerializeField] private bool fadeOnDrag = false;
        [SerializeField] private float dragAlpha = 0.7f;

        #endregion

        #region 私有字段

        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private float originalAlpha;
        private bool isDragging = false;
        private bool isReturning = false;

        #endregion

        #region 事件

        /// <summary>开始拖拽事件</summary>
        public Action<PointerEventData> OnBeginDragEvent;

        /// <summary>拖拽中事件</summary>
        public Action<PointerEventData> OnDragEvent;

        /// <summary>结束拖拽事件</summary>
        public Action<PointerEventData> OnEndDragEvent;

        /// <summary>返回原位完成事件</summary>
        public Action OnReturnCompleted;

        #endregion

        #region 公共属性

        /// <summary>是否正在拖拽</summary>
        public bool IsDragging => isDragging;

        /// <summary>是否正在返回原位</summary>
        public bool IsReturning => isReturning;

        /// <summary>启用/禁用拖拽</summary>
        public bool EnableDrag
        {
            get => enableDrag;
            set => enableDrag = value;
        }

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();

            // 如果没有CanvasGroup且需要透明度效果，则添加一个
            if (canvasGroup == null && fadeOnDrag)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 记录初始状态
            originalPosition = rectTransform.anchoredPosition;
            originalScale = rectTransform.localScale;
            originalAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        }

        private void Update()
        {
            // 处理返回原位动画
            if (isReturning && returnToOriginalPosition)
            {
                UpdateReturnAnimation();
            }
        }

        #endregion

        #region 静态便捷方法

        /// <summary>
        /// 为GameObject添加DragKit组件
        /// </summary>
        public static UIDrag Get(GameObject target)
        {
            var dragKit = target.GetComponent<UIDrag>();
            if (dragKit == null)
            {
                dragKit = target.AddComponent<UIDrag>();
            }
            return dragKit;
        }

        /// <summary>
        /// 为Component添加DragKit组件
        /// </summary>
        public static UIDrag Get(Component target)
        {
            return Get(target.gameObject);
        }

        #endregion

        #region 配置方法

        /// <summary>设置拖拽配置</summary>
        public UIDrag SetDragConfig(bool enableDrag = true, bool returnToOriginal = false, float returnSpeed = 5f)
        {
            this.enableDrag = enableDrag;
            this.returnToOriginalPosition = returnToOriginal;
            this.returnSpeed = returnSpeed;
            return this;
        }

        /// <summary>设置视觉效果</summary>
        public UIDrag SetVisualEffects(bool scaleOnDrag = false, Vector3 dragScale = default, bool fadeOnDrag = false, float dragAlpha = 0.7f)
        {
            this.scaleOnDrag = scaleOnDrag;
            this.dragScale = dragScale == default ? Vector3.one * 1.1f : dragScale;
            this.fadeOnDrag = fadeOnDrag;
            this.dragAlpha = dragAlpha;
            return this;
        }

        /// <summary>设置约束条件</summary>
        public UIDrag SetConstraints(bool constrainToParent = false, bool constrainToScreen = true, Vector2 dragBounds = default)
        {
            this.constrainToParent = constrainToParent;
            this.constrainToScreen = constrainToScreen;
            this.dragBounds = dragBounds;
            return this;
        }

        /// <summary>设置事件回调</summary>
        public UIDrag SetCallbacks(Action<PointerEventData> onBeginDrag = null, Action<PointerEventData> onDrag = null, Action<PointerEventData> onEndDrag = null)
        {
            OnBeginDragEvent = onBeginDrag;
            OnDragEvent = onDrag;
            OnEndDragEvent = onEndDrag;
            return this;
        }

        #endregion

        #region IBeginDragHandler, IDragHandler, IEndDragHandler 实现

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!enableDrag) return;

            isDragging = true;
            isReturning = false;

            // 应用拖拽开始的视觉效果
            ApplyDragStartEffects();

            // 触发事件
            OnBeginDragEvent?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!enableDrag || !isDragging) return;

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition))
            {
                // 应用位置约束
                Vector2 constrainedPosition = ApplyConstraints(localPointerPosition);
                rectTransform.anchoredPosition = constrainedPosition;
            }

            // 触发事件
            OnDragEvent?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!enableDrag) return;

            isDragging = false;

            // 应用拖拽结束的视觉效果
            ApplyDragEndEffects();

            // 如果需要返回原位
            if (returnToOriginalPosition)
            {
                StartReturnAnimation();
            }

            // 触发事件
            OnEndDragEvent?.Invoke(eventData);
        }

        #endregion

        #region 公共方法

        /// <summary>重置到原始位置</summary>
        public void ResetToOriginalPosition(bool immediate = false)
        {
            if (immediate)
            {
                rectTransform.anchoredPosition = originalPosition;
                rectTransform.localScale = originalScale;
                if (canvasGroup != null) canvasGroup.alpha = originalAlpha;
                isReturning = false;
            }
            else
            {
                StartReturnAnimation();
            }
        }

        /// <summary>更新原始位置（用于动态UI）</summary>
        public void UpdateOriginalPosition()
        {
            originalPosition = rectTransform.anchoredPosition;
        }

        #endregion

        #region 私有方法

        /// <summary>应用拖拽开始的视觉效果</summary>
        private void ApplyDragStartEffects()
        {
            if (scaleOnDrag)
            {
                rectTransform.localScale = dragScale;
            }

            if (fadeOnDrag && canvasGroup != null)
            {
                canvasGroup.alpha = dragAlpha;
            }
        }

        /// <summary>应用拖拽结束的视觉效果</summary>
        private void ApplyDragEndEffects()
        {
            if (!returnToOriginalPosition)
            {
                // 如果不返回原位，立即恢复视觉效果
                if (scaleOnDrag)
                {
                    rectTransform.localScale = originalScale;
                }

                if (fadeOnDrag && canvasGroup != null)
                {
                    canvasGroup.alpha = originalAlpha;
                }
            }
        }

        /// <summary>应用位置约束</summary>
        private Vector2 ApplyConstraints(Vector2 position)
        {
            Vector2 constrainedPosition = position;

            // 约束到父对象范围内
            if (constrainToParent && rectTransform.parent != null)
            {
                RectTransform parentRect = rectTransform.parent as RectTransform;
                if (parentRect != null)
                {
                    Vector2 parentSize = parentRect.rect.size;
                    Vector2 objectSize = rectTransform.rect.size;

                    constrainedPosition.x = Mathf.Clamp(constrainedPosition.x,
                        -parentSize.x / 2 + objectSize.x / 2,
                        parentSize.x / 2 - objectSize.x / 2);
                    constrainedPosition.y = Mathf.Clamp(constrainedPosition.y,
                        -parentSize.y / 2 + objectSize.y / 2,
                        parentSize.y / 2 - objectSize.y / 2);
                }
            }

            // 约束到屏幕范围内
            if (constrainToScreen)
            {
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                Vector2 objectSize = rectTransform.rect.size;

                constrainedPosition.x = Mathf.Clamp(constrainedPosition.x,
                    -screenSize.x / 2 + objectSize.x / 2,
                    screenSize.x / 2 - objectSize.x / 2);
                constrainedPosition.y = Mathf.Clamp(constrainedPosition.y,
                    -screenSize.y / 2 + objectSize.y / 2,
                    screenSize.y / 2 - objectSize.y / 2);
            }

            // 自定义边界约束
            if (dragBounds != Vector2.zero)
            {
                constrainedPosition.x = Mathf.Clamp(constrainedPosition.x, -dragBounds.x, dragBounds.x);
                constrainedPosition.y = Mathf.Clamp(constrainedPosition.y, -dragBounds.y, dragBounds.y);
            }

            return constrainedPosition;
        }

        /// <summary>开始返回动画</summary>
        private void StartReturnAnimation()
        {
            isReturning = true;
        }

        /// <summary>更新返回动画</summary>
        private void UpdateReturnAnimation()
        {
            float speed = returnSpeed * Time.deltaTime;

            // 位置插值
            Vector3 currentPos = rectTransform.anchoredPosition;
            Vector3 targetPos = originalPosition;
            rectTransform.anchoredPosition = Vector3.MoveTowards(currentPos, targetPos, speed * 100f);

            // 缩放插值
            if (scaleOnDrag)
            {
                Vector3 currentScale = rectTransform.localScale;
                rectTransform.localScale = Vector3.MoveTowards(currentScale, originalScale, speed);
            }

            // 透明度插值
            if (fadeOnDrag && canvasGroup != null)
            {
                float currentAlpha = canvasGroup.alpha;
                canvasGroup.alpha = Mathf.MoveTowards(currentAlpha, originalAlpha, speed);
            }

            // 检查是否完成返回
            bool positionReached = Vector3.Distance(rectTransform.anchoredPosition, originalPosition) < 0.1f;
            bool scaleReached = !scaleOnDrag || Vector3.Distance(rectTransform.localScale, originalScale) < 0.01f;
            bool alphaReached = !fadeOnDrag || canvasGroup == null || Mathf.Abs(canvasGroup.alpha - originalAlpha) < 0.01f;

            if (positionReached && scaleReached && alphaReached)
            {
                // 确保精确到达目标值
                rectTransform.anchoredPosition = originalPosition;
                if (scaleOnDrag) rectTransform.localScale = originalScale;
                if (fadeOnDrag && canvasGroup != null) canvasGroup.alpha = originalAlpha;

                isReturning = false;
                OnReturnCompleted?.Invoke();
            }
        }

        #endregion
    }
}
