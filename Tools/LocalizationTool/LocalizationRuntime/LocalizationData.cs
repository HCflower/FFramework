using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 本地化数据
/// </summary>
public class LocalizationData : ScriptableObject
{
    public TextAsset localizationDataFile;
    public List<LocalizationItem> localizationList = new();
    private Dictionary<LanguageType, Dictionary<string, string>> localizationDic = new();

    /// <summary>
    /// 获取本地化数据
    /// </summary>
    /// <param name="languageType">语言类型</param>
    /// <param name="index">内容索引</param>
    /// <returns></returns> 
    public string GetTypeLanguageContent(LanguageType languageType, string key)
    {
        //若字典中存在则直读取
        if (localizationDic.TryGetValue(languageType, out Dictionary<string, string> contentDic))
        {
            if (contentDic.TryGetValue(key, out string content))
            {
                return content;
            }
            Debug.LogError($"[Dict Cache] Key '{key}' does not exist in language {languageType}");
            return string.Empty;
        }
        else
        {
            //将对应语言的数据存入字典
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
            else
            {
                Debug.LogError($"Key '{key}' does not exist in language {languageType}");
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// 判断是否存在该语言
    /// </summary>
    /// <param name="languageType">语言类型</param>
    /// <returns></returns>
    public bool TryGetLanguageType(LanguageType languageType)
    {
        foreach (LocalizationItem item in localizationList)
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
