using System.Collections.Generic;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 本地化编辑器处理类
/// </summary>
public static class LocalizationEditorHandler
{
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
    public static LocalizationData CreateLocalizationDataAndExcel(string dataName, string dataSavePath, string excelSavePath, bool isCreateExcel)
    {
        //创建本地化数据SO文件
        LocalizationData data = ScriptableObject.CreateInstance<LocalizationData>();
        data.name = dataName;
        string assetPath = $"{dataSavePath}/{dataName}.asset";
        AssetDatabase.CreateAsset(data, assetPath);
        //创建Excel文件
        if (isCreateExcel)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Localization");
                string[] languageTypes = System.Enum.GetNames(typeof(LanguageType));
                // 设置表头
                worksheet.Cells[1, 1].Value = "Key";
                for (int i = 0; i < languageTypes.Length; i++)
                {
                    worksheet.Cells[1, i + 2].Value = languageTypes[i];
                }
                // 确保目录存在
                if (!Directory.Exists(excelSavePath))
                {
                    Directory.CreateDirectory(excelSavePath);
                }

                // 保存Excel文件
                var fileInfo = new FileInfo($"{excelSavePath}/{dataName}.xlsx");
                package.SaveAs(fileInfo);
                // 设置 ExcelPath
                data.ExcelPath = $"{excelSavePath}/{dataName}.xlsx";
            }
        }
        else
        {
            data.ExcelPath = string.Empty;
        }
        AssetDatabase.SaveAssetIfDirty(data);
        AssetDatabase.Refresh();
        return data;
    }

    /// <summary>
    /// 更新本地化数据SO
    /// </summary>
    public static void ImportOrUpdateExcelToSO(LocalizationData data)
    {
        using (ExcelPackage package = new ExcelPackage(new FileInfo(data.ExcelPath)))
        {
            data.localizationList.Clear();
            //获取第一个工作表
            ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
            //获取枚举类型
            string[] languageTypes = System.Enum.GetNames(typeof(LanguageType));
            for (int col = 2; col < languageTypes.Length; col++)
            {
                LanguageType languageType = (LanguageType)System.Enum.Parse(typeof(LanguageType), worksheet.Cells[1, col].Text);
                List<LocalizationItem.LocalizationContent> contentList = new List<LocalizationItem.LocalizationContent>();
                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    string key = worksheet.Cells[row, 1].Text;
                    string content = worksheet.Cells[row, col].Text;

                    // 如果 key 存在且内容不为空，将其添加到内容列表中
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(content))
                    {
                        contentList.Add(new LocalizationItem.LocalizationContent
                        {
                            key = key,
                            content = content
                        });
                    }
                }
                // 当内容列表不为空时，才将该语言类型创建到 localizationList 中
                if (contentList.Count > 0)
                {
                    data.localizationList.Add(new LocalizationItem
                    {
                        languageType = languageType,
                        content = contentList
                    });
                }
            }
        }
        AssetDatabase.SaveAssetIfDirty(data);
        Debug.Log($"<color=yellow>{data.name}</color>数据载入成功.");
    }
}
