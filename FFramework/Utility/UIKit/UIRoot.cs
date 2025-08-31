using UnityEngine;
using UnityEngine.UI;

namespace FFramework.Kit
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
            if (!TryGetComponent<Canvas>(out _)) gameObject.AddComponent<Canvas>();
            if (!TryGetComponent<CanvasScaler>(out _)) gameObject.AddComponent<CanvasScaler>();
            if (!TryGetComponent<GraphicRaycaster>(out _)) gameObject.AddComponent<GraphicRaycaster>();

            // 添加UI层
            BackgroundLayer = CreateAndAddUIlayerInGameObject("BackgroundLayer", this.transform, false, false, false);
            PostProcessingLayer = CreateAndAddUIlayerInGameObject("PostProcessingLayer", this.transform, true, true, false);
            ContentLayer = CreateAndAddUIlayerInGameObject("ContentLayer", this.transform, true, true, false);
            PopupLayer = CreateAndAddUIlayerInGameObject("PopupLayer", this.transform, false, true, false);
            GuideLayer = CreateAndAddUIlayerInGameObject("GuideLayer", this.transform, true, true, false);
            DebugLayer = CreateAndAddUIlayerInGameObject("DebugLayer", this.transform, true, true, false);

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
        /// <param name="interactable">是否可交互</param>
        /// <param name="blocksRaycasts">是否阻挡射线</param>
        /// <param name="ignoreParentGroups">是否忽略父级组</param>
        private Transform CreateAndAddUIlayerInGameObject(string uiLayerName, Transform parent, bool interactable, bool blocksRaycasts, bool ignoreParentGroups)
        {
            // 优先查找已存在的层级
            Transform exist = parent.Find(uiLayerName);
            if (exist != null)
            {
                // 查找或添加CanvasGroup，并设置属性
                var canvasGroup = exist.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = exist.gameObject.AddComponent<CanvasGroup>();
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = blocksRaycasts;
                canvasGroup.ignoreParentGroups = ignoreParentGroups;
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

            // 添加CanvasGroup并设置属性
            var newCanvasGroup = uiLayer.GetComponent<CanvasGroup>();
            if (newCanvasGroup == null) newCanvasGroup = uiLayer.AddComponent<CanvasGroup>();
            newCanvasGroup.interactable = interactable;
            newCanvasGroup.blocksRaycasts = blocksRaycasts;
            newCanvasGroup.ignoreParentGroups = ignoreParentGroups;

            return rectTransform;
        }
    }

    /// <summary>
    /// UI层级枚举
    /// </summary>
    public enum UILayer
    {
        BackgroundLayer,
        PostProcessingLayer,
        ContentLayer,
        PopupLayer,
        GuideLayer,
        DebugLayer
    }
}