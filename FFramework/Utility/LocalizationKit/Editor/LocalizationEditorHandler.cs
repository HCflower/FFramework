using System.Collections.Generic;
using FFramework.Kit;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace LocalizationEditor
{
    /// <summary>
    /// 本地化编辑器处理类
    /// </summary>
    public static class LocalizationEditorHandler
    {
        /// <summary>
        /// 获取所有启用的语言类型名称
        /// </summary>
        private static List<string> GetEnabledLanguageTypes()
        {
            List<string> languages = new List<string>();
            var fields = typeof(LanguageType).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            foreach (var field in fields)
            {
                // 跳过被注释掉的字段
                if (!field.IsDefined(typeof(System.ObsoleteAttribute), false))
                {
                    languages.Add(field.Name);
                }
            }
            return languages;
        }

        /// <summary>
        /// 获取本地化数据SO列表
        /// </summary>
        public static List<LocalizationData> GetLocalizationDataSOList()
        {
            List<LocalizationData> localizationDataList = new List<LocalizationData>();
            // 获取项目中所有 LocalizationData 类型的资产 GUID
            string[] guids = AssetDatabase.FindAssets("t:LocalizationData");

            foreach (string guid in guids)
            {
                // 通过 GUID 获取资产路径
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 加载资产
                LocalizationData data = AssetDatabase.LoadAssetAtPath<LocalizationData>(assetPath);

                if (data != null)
                {
                    localizationDataList.Add(data);
                }
            }
            return localizationDataList;
        }

        // <summary>
        /// 创建本地化数据SO
        /// </summary>
        /// <param name="dataSavePath">数据保存路径</param>
        /// <param name="excelSavePath">Excel文件保存路径</param>   
        public static LocalizationData CreateLocalizationDataAndCSV(string dataName, string dataSavePath, string excelSavePath, bool isCreateCSV)
        {
            //创建本地化数据SO文件
            LocalizationData data = ScriptableObject.CreateInstance<LocalizationData>();
            data.name = dataName;
            string assetPath = $"{dataSavePath}/{dataName}.asset";
            AssetDatabase.CreateAsset(data, assetPath);
            //创建CSV文件
            if (isCreateCSV)
            {
                string path = excelSavePath + $"{dataName}.csv";

                using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    var languages = GetEnabledLanguageTypes();
                    string header = "Key," + string.Join(",", languages);
                    writer.WriteLine(header);

                }
                AssetDatabase.Refresh();
                data.localizationDataFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) as TextAsset;
                Debug.Log($"A localized data file has been created: {excelSavePath}");
            }
            else
            {
                Debug.LogWarning($"The file already exists: {excelSavePath}");
            }
            //聚焦到文件位置
            var csvFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dataSavePath);

            Selection.activeObject = csvFile;
            EditorGUIUtility.PingObject(csvFile);
            AssetDatabase.SaveAssetIfDirty(data);
            AssetDatabase.Refresh();
            return data;
        }

        /// <summary>
        /// 删除当前的SO数据文件和绑定的CSV文件
        /// </summary>
        /// <param name="currentData">要删除的数据对象</param>
        public static void DeleteCurrentDataFile(LocalizationData currentData)
        {
            if (currentData == null)
            {
                Debug.LogWarning("没有当前数据文件可删除");
                return;
            }

            // 获取SO文件路径
            string soPath = AssetDatabase.GetAssetPath(currentData);
            if (string.IsNullOrEmpty(soPath))
            {
                Debug.LogError("无法获取当前数据文件的路径");
                return;
            }

            // 获取绑定的CSV文件路径
            string csvPath = "";
            if (currentData is LocalizationData localizationData)
            {
                csvPath = localizationData.localizationDataPath;
            }

            // 准备删除信息
            string deleteInfo = $"即将删除以下文件：\n\n";
            deleteInfo += $"SO文件: {soPath}\n";
            if (!string.IsNullOrEmpty(csvPath) && File.Exists(csvPath))
            {
                deleteInfo += $"CSV文件: {csvPath}\n";
            }
            deleteInfo += "\n此操作不可撤销,确定要删除吗?";

            // 显示确认对话框
            if (EditorUtility.DisplayDialog("确认删除", deleteInfo, "删除", "取消"))
            {
                try
                {
                    bool hasError = false;

                    // 删除SO文件
                    if (!string.IsNullOrEmpty(soPath))
                    {
                        if (AssetDatabase.DeleteAsset(soPath))
                        {
                            Debug.Log($"已删除SO文件: {soPath}");
                        }
                        else
                        {
                            Debug.LogError($"删除SO文件失败: {soPath}");
                            hasError = true;
                        }
                    }

                    // 删除CSV文件
                    if (!string.IsNullOrEmpty(csvPath) && System.IO.File.Exists(csvPath))
                    {
                        try
                        {
                            File.Delete(csvPath);
                            Debug.Log($"已删除CSV文件: {csvPath}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"删除CSV文件失败: {csvPath}, 错误: {e.Message}");
                            hasError = true;
                        }
                    }

                    // 刷新资源数据库
                    AssetDatabase.Refresh();

                    if (!hasError) Debug.Log("文件删除成功");
                }
                catch (Exception e)
                {
                    Debug.LogError($"删除文件时发生错误: {e.Message}");
                    EditorUtility.DisplayDialog("删除失败", $"删除文件时发生错误:\n{e.Message}", "确定");
                }
            }
        }

        /// <summary>
        /// 更新本地化数据SO
        /// </summary>
        public static void ImportOrUpdateCSVToSO(LocalizationData data)
        {
            if (data.localizationDataFile == null)
            {
                Debug.LogError("CSV TextAsset为空!");
                return;
            }

            try
            {
                // 直接从TextAsset读取CSV内容
                string[] csvLines = data.localizationDataFile.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                if (csvLines.Length < 2) // 至少需要表头+1行数据
                {
                    Debug.LogWarning("CSV文件没有有效数据内容，跳过导入");
                    return;
                }

                // 解析表头获取语言类型
                string[] headers = csvLines[0].Split(',');
                if (headers.Length < 2)
                {
                    Debug.LogError("CSV格式无效 - 缺少语言列");
                    return;
                }

                bool hasValidData = false;
                //清理旧数据
                data.localizationList.Clear();
                // 处理每一行数据
                for (int i = 1; i < csvLines.Length; i++)
                {
                    string[] values = csvLines[i].Split(',');
                    if (values.Length != headers.Length)
                    {
                        Debug.LogWarning($"跳过无效行 {i}：列计数不匹配");
                        continue;
                    }

                    string key = values[0].Trim();
                    if (string.IsNullOrEmpty(key))
                    {
                        continue; // 跳过空key的行
                    }

                    hasValidData = true;
                    // 为每种语言添加数据
                    for (int langIndex = 1; langIndex < headers.Length; langIndex++)
                    {
                        string content = values[langIndex].Trim();
                        if (string.IsNullOrEmpty(content))
                        {
                            continue; // 跳过空内容的语言列
                        }

                        LanguageType languageType = (LanguageType)System.Enum.Parse(typeof(LanguageType), headers[langIndex]);
                        // 添加到列表
                        var langItem = data.localizationList.Find(x => x.languageType == languageType);
                        if (langItem == null)
                        {
                            langItem = new LocalizationItem()
                            {
                                languageType = languageType,
                                content = new List<LocalizationItem.LocalizationContent>()
                            };
                            data.localizationList.Add(langItem);
                        }

                        var contentItem = langItem.content.Find(x => x.key == key);
                        if (contentItem == null)
                        {
                            contentItem = new LocalizationItem.LocalizationContent()
                            {
                                key = key,
                                content = content
                            };
                            langItem.content.Add(contentItem);
                        }
                        else
                        {
                            contentItem.content = content;
                        }
                    }
                }

                if (hasValidData)
                {
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssetIfDirty(data);
                    Debug.Log($"<color=yellow>{data.name}</color> 数据导入成功，共导入 {csvLines.Length - 1} 条记录");
                }
                else
                {
                    Debug.LogWarning("CSV中没有有效数据内容，跳过导入");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"导入CSV失败: {e.Message}");
            }
        }
    }
}