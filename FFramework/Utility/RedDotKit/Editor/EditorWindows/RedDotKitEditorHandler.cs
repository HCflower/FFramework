using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.Linq;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace RedDotKitEditor
{
    /// <summary>
    /// 红点Key管理器，用于动态添加enum值
    /// </summary>
    public static class RedDotKitEditorHandler
    {
        /// <summary>
        /// 通过反射获取RedDotKey枚举的源文件路径
        /// </summary>
        private static string GetRedDotKeyFilePath()
        {
            // 搜索所有.cs文件，查找包含RedDotKey枚举定义的文件
            string[] allScripts = AssetDatabase.FindAssets("t:Script");

            foreach (string guid in allScripts)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 跳过非.cs文件
                if (!assetPath.EndsWith(".cs"))
                    continue;

                // 跳过当前Handler文件，避免错误匹配
                if (assetPath.Contains("RedDotKitEditorHandler"))
                    continue;

                try
                {
                    string content = File.ReadAllText(assetPath);

                    // 更精确地检查枚举定义，确保匹配的是真正的枚举定义而不是引用
                    if (Regex.IsMatch(content, @"public\s+enum\s+RedDotKey\s*\{") ||
                        Regex.IsMatch(content, @"enum\s+RedDotKey\s*\{"))
                    {
                        Debug.Log($"找到RedDotKey枚举定义文件: {assetPath}");
                        return assetPath;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"读取文件失败: {assetPath}, 错误: {e.Message}");
                }
            }

            Debug.LogError("未找到RedDotKey枚举定义文件");
            return null;
        }

        /// <summary>
        /// 创建新的红点Key
        /// </summary>
        public static RedDotKey CreateNodeKey(string name)
        {
            // 基本验证
            if (string.IsNullOrWhiteSpace(name))
                return RedDotKey.None;

            Type redDotKeyType = typeof(RedDotKey);
            if (Enum.GetNames(redDotKeyType).Contains(name))
            {
                Debug.LogError($"Key {name} 已存在");
                return RedDotKey.None;
            }

            // 获取文件路径
            string filePath = GetRedDotKeyFilePath();
            if (string.IsNullOrEmpty(filePath))
                return RedDotKey.None;

            try
            {
                // 读取文件
                string fileContent = File.ReadAllText(filePath);

                // 计算新值
                int newValue = Enum.GetValues(redDotKeyType)
                                  .Cast<int>()
                                  .DefaultIfEmpty(-1)
                                  .Max() + 1;

                // 查找枚举定义
                var regex = new Regex(@"public\s+enum\s+RedDotKey\s*{([^}]*)}", RegexOptions.Singleline);
                Match match = regex.Match(fileContent);

                if (!match.Success)
                {
                    Debug.LogError("无法找到RedDotKey枚举定义");
                    return RedDotKey.None;
                }

                // 处理枚举内容
                string enumContent = match.Groups[1].Value;

                // 寻找最后一个逗号的位置
                int lastCommaPos = enumContent.LastIndexOf(',');
                if (lastCommaPos == -1)
                {
                    // 如果没有逗号，可能是空枚举或格式异常
                    enumContent += $"\n    {name} = {newValue},";
                }
                else
                {
                    // 在最后一个逗号后添加新项
                    enumContent = enumContent.Insert(lastCommaPos + 1, $"\n    {name} = {newValue},");
                }

                // 替换并写入
                string updatedContent = regex.Replace(fileContent, $"public enum RedDotKey {{{enumContent}}}");
                File.WriteAllText(filePath, updatedContent);
                AssetDatabase.Refresh();

                // 注册回调以验证
                EditorApplication.delayCall += () => ValidateNewKey(name, newValue);

                return RedDotKey.None;
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建Key失败: {ex.Message}");
                return RedDotKey.None;
            }
        }

        // 拆分验证功能到单独方法
        private static void ValidateNewKey(string name, int value)
        {
            try
            {
                Type updatedType = typeof(RedDotKey);
                if (Enum.IsDefined(updatedType, name))
                {
                    RedDotKey newKey = (RedDotKey)Enum.Parse(updatedType, name);
                    Debug.Log($"成功添加Key: {name}，值为 {(int)newKey}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"验证新Key失败: {e.Message}");
            }
        }

        /// <summary>
        /// 删除指定的红点Key
        /// </summary>
        public static bool DeleteNodeKey(string name)
        {
            // 基本验证
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // 通过反射检查是否存在
            Type redDotKeyType = typeof(RedDotKey);
            if (!Enum.GetNames(redDotKeyType).Contains(name))
            {
                Debug.LogError($"Key {name} 不存在");
                return false;
            }

            // 动态获取文件路径
            string filePath = GetRedDotKeyFilePath();
            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                // 读取文件
                string fileContent = File.ReadAllText(filePath);

                // 查找枚举定义
                var regex = new Regex(@"public\s+enum\s+RedDotKey\s*{([^}]*)}", RegexOptions.Singleline);
                Match match = regex.Match(fileContent);

                if (!match.Success)
                {
                    Debug.LogError("无法找到RedDotKey枚举定义");
                    return false;
                }

                // 获取枚举内容
                string enumContent = match.Groups[1].Value;

                // 使用正则表达式查找要删除的枚举项
                var lineRegex = new Regex($@"[\r\n]\s*{name}\s*=\s*\d+\s*,?", RegexOptions.Multiline);
                Match lineMatch = lineRegex.Match(enumContent);

                if (!lineMatch.Success)
                {
                    Debug.LogError($"找不到Key {name} 的定义");
                    return false;
                }

                // 删除匹配到的行
                string updatedEnum = enumContent.Remove(lineMatch.Index, lineMatch.Length);

                // 处理结尾逗号
                updatedEnum = CleanupEnumContent(updatedEnum);

                // 替换并写入
                string updatedContent = regex.Replace(fileContent, $"public enum RedDotKey {{{updatedEnum}}}");
                File.WriteAllText(filePath, updatedContent);
                AssetDatabase.Refresh();

                Debug.Log($"成功删除Key: {name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除Key失败: {ex.Message}");
                return false;
            }
        }

        // 辅助方法：清理枚举内容，确保格式正确
        private static string CleanupEnumContent(string enumContent)
        {
            // 删除多余的空行
            enumContent = Regex.Replace(enumContent, @"[\r\n]{2,}", Environment.NewLine);

            // 确保最后一项有逗号
            string trimmed = enumContent.Trim();
            if (!trimmed.EndsWith(",") && trimmed.Length > 0)
            {
                int lastLineBreak = enumContent.LastIndexOf('\n');
                if (lastLineBreak != -1)
                {
                    string lastLine = enumContent.Substring(lastLineBreak).Trim();
                    if (!lastLine.EndsWith(",") && !lastLine.StartsWith("//"))
                    {
                        enumContent = enumContent.Insert(enumContent.Length, ",");
                    }
                }
            }

            return enumContent;
        }
    }
}
