using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;

namespace FFramework
{
    /// <summary>
    /// 本地化控制器
    /// </summary>
    public class LocalizationController : MonoBehaviour
    {
        [Tooltip("本地化数据(如果没有则获取全局本地化数据文件)")][SerializeField] private LocalizationData localizationData;
        [SerializeField] private List<LocalizationControllerItem> localizationItemList;

        private void Awake()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.Register(OnLanguageChanged);
        }
        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.UnRegister(OnLanguageChanged);
        }

        private void Start()
        {
            InitializeLanguage();
        }

        // 初始化语言
        private void InitializeLanguage()
        {
            OnLanguageChanged(LocalizationManager.Instance.LanguageType);
        }

        //接收语言改变事件
        private void OnLanguageChanged(LanguageType type)
        {
            foreach (var item in localizationItemList)
            {
                //尝试使用多态方式处理不同类型的文本组件
                string localizedText = localizationData == null
                    ? LocalizationManager.Instance.GetLocalizedContent(item.key)
                    : localizationData.GetTypeLanguageContent(type, item.key);

                //先尝试获取 Text 组件
                if (item.textComponent.gameObject.TryGetComponent<Text>(out var textComponent))
                {
                    textComponent.text = localizedText;
                }
                //再尝试获取 TextMeshProUGUI 组件
                else if (item.textComponent.gameObject.TryGetComponent<TextMeshProUGUI>(out var tmpComponent))
                {
                    tmpComponent.text = localizedText;
                }
            }
        }
    }

    [Serializable]
    public class LocalizationControllerItem
    {
        public Component textComponent;
        public string key;
    }
}