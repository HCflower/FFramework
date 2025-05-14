using System.Collections.Generic;
using UnityEngine;
using System;

    /// <summary>
    /// 本地化数据
    /// </summary>
    public class LocalizationData : ScriptableObject
    {
        public string ExcelPath = "";
        public List<LocalizationItem> localizationList = new();
        public Dictionary<LanguageType, Dictionary<string, string>> localizationDic = new();

        /// <summary>
        /// 获取本地化数据
        /// </summary>
        /// <param name="languageType">语言类型</param>
        /// <param name="index">内容索引</param>
        /// <returns></returns> 
        public string GetLanguageContent(LanguageType languageType, string key)
        {
            //若字典中存在则直读取
            if (localizationDic.TryGetValue(languageType, out Dictionary<string, string> contentDic))
            {
                if (contentDic.TryGetValue(key, out string content))
                {
                    return content;
                }
                return string.Empty;
            }
            else
            {
                Dictionary<string, string> newContentDic = new Dictionary<string, string>();
                foreach (var item in localizationList)
                {
                    if (item.languageType == languageType)
                    {
                        foreach (var contentItem in item.content)
                        {
                            newContentDic[contentItem.key] = contentItem.content;
                        }
                        break;
                    }
                }
                localizationDic[languageType] = newContentDic;

                if (newContentDic.TryGetValue(key, out string content))
                {
                    return content;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 判断是否存在该语言
        /// </summary>
        /// <param name="languageType">语言类型</param>
        /// <returns></returns>
        public bool TryGetLanguageType(LanguageType languageType)
        {
            foreach (var item in localizationList)
            {
                if (item.languageType == languageType) return true;
            }
            return false;
        }
    }

    [Serializable]
    public class LocalizationItem
    {
        public LanguageType languageType;
        public List<LocalizationContent> content;

        [Serializable]
        public class LocalizationContent
        {
            public string key;
            public string content;
        }
    }
