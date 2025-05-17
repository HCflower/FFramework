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
        [SerializeField] private LocalizationData localizationData;
        [SerializeField] private List<LocalizationControllerItem> localizationItemList;

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
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
                item.text.gameObject.TryGetComponent<Text>(out var text);
                if (text != null)
                {
                    if (localizationData == null)
                        text.text = LocalizationManager.Instance.GetLocalizedContent(item.key);
                    else
                        text.text = localizationData.GetLanguageContent(type, item.key);
                }
                if (text == null)
                {
                    item.text.gameObject.TryGetComponent<TextMeshProUGUI>(out var textMeshProUGUI);
                    if (textMeshProUGUI != null)
                    {
                        if (localizationData == null)
                            textMeshProUGUI.text = LocalizationManager.Instance.GetLocalizedContent(item.key);
                        else
                            textMeshProUGUI.text = localizationData.GetLanguageContent(type, item.key);
                    }
                }
            }
        }
    }

    [Serializable]
    public class LocalizationControllerItem
    {
        public Component text;
        public string key;
    }
}