using UnityEngine;
using System;

namespace FFramework
{
    /// <summary>
    /// 本地化管理器
    /// </summary>
    public class LocalizationManager : SingletonMono<LocalizationManager>
    {
        private event Action<LanguageType> OnLanguageChanged;
        public LocalizationData GlobalLocalizationData;
        [SerializeField] private LanguageType languageType;
        public LanguageType LanguageType
        {
            get => languageType;
            set
            {
                if (languageType != value)
                {
                    TryGetLanguageType();
                    languageType = value;
                    OnLanguageChanged?.Invoke(languageType);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 仅在运行模式下触发
            if (Application.isPlaying)
            {
                TryGetLanguageType();
                OnLanguageChanged?.Invoke(languageType);
            }
        }
#endif

        //注册语言类型修改事件
        public void Register(Action<LanguageType> action)
        {
            OnLanguageChanged += action;
        }

        //取消注册语言类型修改事件
        public void UnRegister(Action<LanguageType> action)
        {
            OnLanguageChanged -= action;
        }

        //尝试获取指定语言类型的数据
        private void TryGetLanguageType()
        {
            if (GlobalLocalizationData != null)
            {
                if (!GlobalLocalizationData.TryGetLanguageType(languageType))
                {
                    Debug.Log($"<color=yellow>No {languageType} language data!!</color>");
                }
            }
        }

        /// <summary>
        /// 根据索引获取本地化内容
        /// </summary>
        /// <param name="index">本地化数据索引</param>
        /// <returns></returns>
        public string GetLocalizedContent(string key)
        {
            if (GlobalLocalizationData != null)
            {
                return GlobalLocalizationData.GetTypeLanguageContent(LanguageType, key);
            }
            else return string.Empty;
        }
    }
}