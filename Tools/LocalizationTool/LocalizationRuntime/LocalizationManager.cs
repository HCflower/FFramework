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
        public LocalizationData localizationData;
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


        protected override void OnDestroy()
        {
            if (OnLanguageChanged == null)
                base.OnDestroy();
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
            if (localizationData != null)
            {
                if (!localizationData.TryGetLanguageType(languageType))
                {
                    Debug.Log($"<color=yellow>没有{languageType}语言的数据!</color>");
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
            if (localizationData != null)
            {
                return localizationData.GetTypeLanguageContent(LanguageType, key);
            }
            else return string.Empty;
        }
    }
}