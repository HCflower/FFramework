using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 本地化管理器
    /// </summary>
    public static class LocalizationKit
    {
        private static event Action<LanguageType> OnLanguageChanged;
        private static LanguageType languageType
        {
            get
            {
                if (GlobalSetting.Instance != null)
                    return GlobalSetting.Instance.LanguageType;
                else
                    return LanguageType.English;
            }
        }

        /// <summary>
        /// 触发语言类型修改事件
        /// </summary>
        public static void Trigger()
        {
            OnLanguageChanged?.Invoke(languageType);
        }

        /// <summary>
        /// 注册语言类型修改事件
        /// </summary>
        public static void Register(Action<LanguageType> action)
        {
            OnLanguageChanged += action;
        }

        /// <summary>
        /// 取消注册语言类型修改事件
        /// </summary>  
        public static void UnRegister(Action<LanguageType> action)
        {
            OnLanguageChanged -= action;
        }

        /// <summary>
        /// 取消注册所有语言类型修改事件
        /// </summary>
        public static void UnAllRegister()
        {
            OnLanguageChanged = null;
        }

        /// <summary>
        /// 尝试获取指定语言类型的数据
        /// </summary>
        public static void TryGetLanguageType(LocalizationData localizationData)
        {
            if (localizationData != null)
            {
                if (!localizationData.TryGetLanguageType(languageType))
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
        public static string GetTypeLanguageContent(LocalizationData localizationData, string key)
        {
            if (localizationData != null)
            {
                return localizationData.GetTypeLanguageContent(languageType, key);
            }
            else return string.Empty;
        }
    }
}