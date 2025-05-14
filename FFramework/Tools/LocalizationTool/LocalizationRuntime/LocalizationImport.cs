using System.Collections.Generic;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace FFramework
{
    /// <summary>
    /// 导入Excel文件数据到SO文件
    /// </summary>
    public static class LocalizationImport
    {
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
}