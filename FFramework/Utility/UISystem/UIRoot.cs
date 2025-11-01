using FFramework.Architecture;
using UnityEngine.UI;
using UnityEngine;

namespace FFramework.Utility
{
    ///<summary>
    /// UI根节点
    /// </summary>
    public class UIRoot : SingletonMono<UIRoot>
    {
        public Transform BackgroundLayer;       //背景层 - 静态背景
        public Transform PostProcessingLayer;   //后期处理层 - 后期处理效果
        public Transform ContentLayer;          //内容层 - 主要UI功能
        public Transform PopupLayer;            //弹窗层 - 消息弹窗
        public Transform GuideLayer;            //引导层 - 引导玩家操作    
        public Transform DebugLayer;            //调试层 - 创建和调试UI   

        [ContextMenu("创建UI层级")]
        private void CreateUILayer()
        {
            this.name = "UIRoot";
            // 必要组件
            if (!TryGetComponent<Canvas>(out _))
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            if (!TryGetComponent<CanvasScaler>(out _))
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); // 设置参考分辨率
            }
            if (!TryGetComponent<GraphicRaycaster>(out _)) gameObject.AddComponent<GraphicRaycaster>();

            // 添加UI层
            BackgroundLayer = CreateAndAddUIlayerInGameObject("BackgroundLayer", this.transform);
            PostProcessingLayer = CreateAndAddUIlayerInGameObject("PostProcessingLayer", this.transform);
            ContentLayer = CreateAndAddUIlayerInGameObject("ContentLayer", this.transform);
            PopupLayer = CreateAndAddUIlayerInGameObject("PopupLayer", this.transform);
            GuideLayer = CreateAndAddUIlayerInGameObject("GuideLayer", this.transform);
            DebugLayer = CreateAndAddUIlayerInGameObject("DebugLayer", this.transform);

            // EventSystem
            if (transform.Find("EventSystem") == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        /// <summary>
        /// 创建并添加UI层
        /// </summary>
        /// <param name="uiLayerName">ui层名称</param>
        /// <param name="parent">父级变换</param>
        private Transform CreateAndAddUIlayerInGameObject(string uiLayerName, Transform parent)
        {
            // 优先查找已存在的层级
            Transform exist = parent.Find(uiLayerName);
            if (exist != null)
            {
                // 如果不是RectTransform则替换
                var rect = exist as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                }
                return exist;
            }

            // 创建新层级（使用RectTransform）
            GameObject uiLayer = new GameObject(uiLayerName, typeof(RectTransform));
            var rectTransform = uiLayer.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false); // 保持局部坐标
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            return rectTransform;
        }
    }

    /// <summary>
    /// UI层级枚举
    /// </summary>
    public enum UILayer
    {
        /// <summary> 
        /// 背景层 - 静态背景
        /// </summary>
        BackgroundLayer,

        /// <summary> 
        /// 后期处理层 - UI后期处理效果
        /// </summary>
        PostProcessingLayer,

        /// <summary>
        /// 内容层 - 主要UI功能
        /// </summary>
        ContentLayer,

        /// <summary> 
        /// 弹窗层 - 消息弹窗
        /// </summary>
        PopupLayer,

        /// <summary>
        /// 引导层 - 引导玩家操作
        /// </summary>
        GuideLayer,

        /// <summary> 
        /// 调试层 - 创建和调试UI
        /// </summary>
        DebugLayer
    }
}