using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace FFramework.Tools
{
    /// <summary>
    /// 一键替换场景中所有 Text 和 TextMeshPro 字体的工具窗口
    /// </summary>
    public class OneClickReplacementAllFontInScene : EditorWindow
    {
        // 目标字体（用于Unity原生Text组件）
        private Font targetFont;
        // 目标TMP字体（用于TextMeshPro组件）
        private TMP_FontAsset targetTMPFont;
        // 是否包含未激活的对象
        private bool includeInactiveObjects = true;
        // 滚动视图位置
        private Vector2 scrollPosition;

        /// <summary>
        /// 打开工具窗口菜单
        /// </summary>
        [MenuItem("FFramework/Tools/一键替换场景中的字体资产")]
        public static void ShowWindow()
        {
            GetWindow<OneClickReplacementAllFontInScene>("一键字体替换工具");
        }

        /// <summary>
        /// 绘制窗口界面
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.LabelField("场景字体一键替换工具", EditorStyles.boldLabel);

            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 设置目标字体
            EditorGUILayout.LabelField("Text组件字体设置", EditorStyles.boldLabel);
            targetFont = (Font)EditorGUILayout.ObjectField("目标字体", targetFont, typeof(Font), false);

            GUILayout.Space(5);

            // 设置目标TMP字体
            EditorGUILayout.LabelField("TextMeshPro组件字体设置", EditorStyles.boldLabel);
            targetTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("目标TMP字体", targetTMPFont, typeof(TMP_FontAsset), false);

            GUILayout.Space(10);

            // 是否包含未激活对象
            includeInactiveObjects = EditorGUILayout.Toggle("包含未激活对象", includeInactiveObjects);

            GUILayout.Space(5);

            // 提示信息
            EditorGUILayout.HelpBox("注意：本工具会替换场景中所有Text和TextMeshPro组件的字体。请确保已保存场景。", MessageType.Info);

            GUILayout.Space(10);

            // 替换按钮
            EditorGUI.BeginDisabledGroup(targetFont == null && targetTMPFont == null);
            if (GUILayout.Button("一键替换字体", GUILayout.Height(30)))
            {
                ReplaceAllFontsInScene();
            }
            EditorGUI.EndDisabledGroup();

            // 状态提示
            if (targetFont == null && targetTMPFont == null)
            {
                EditorGUILayout.HelpBox("请至少选择一个目标字体", MessageType.Warning);
            }

            GUILayout.Space(10);

            // 字体使用统计
            if (GUILayout.Button("统计场景字体使用情况"))
            {
                ShowFontUsageStatistics();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 替换场景中所有Text和TextMeshPro组件的字体
        /// </summary>
        private void ReplaceAllFontsInScene()
        {
            int textReplaceCount = 0;
            int tmpReplaceCount = 0;

            // 查找所有GameObject
            GameObject[] allObjects = FindObjectsOfType<GameObject>(includeInactiveObjects);

            foreach (GameObject obj in allObjects)
            {
                // 替换Text组件字体
                if (targetFont != null)
                {
                    Text textComponent = obj.GetComponent<Text>();
                    if (textComponent != null && textComponent.font != targetFont)
                    {
                        textComponent.font = targetFont;
                        textReplaceCount++;
                        EditorUtility.SetDirty(textComponent);
                    }
                }

                // 替换TextMeshPro组件字体
                if (targetTMPFont != null)
                {
                    TextMeshProUGUI tmpComponent = obj.GetComponent<TextMeshProUGUI>();
                    if (tmpComponent != null && tmpComponent.font != targetTMPFont)
                    {
                        tmpComponent.font = targetTMPFont;
                        tmpReplaceCount++;
                        EditorUtility.SetDirty(tmpComponent);
                    }
                }
            }

            // 显示结果
            string resultMessage = $"字体替换完成：\n";
            if (targetFont != null)
                resultMessage += $"Text组件替换: {textReplaceCount} 个\n";
            if (targetTMPFont != null)
                resultMessage += $"TextMeshPro组件替换: {tmpReplaceCount} 个";

            EditorUtility.DisplayDialog("替换结果", resultMessage, "确定");

            // 标记场景已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        /// <summary>
        /// 统计场景中所有Text和TextMeshPro组件的字体使用情况
        /// </summary>
        private void ShowFontUsageStatistics()
        {
            Dictionary<Font, int> fontUsage = new Dictionary<Font, int>();
            Dictionary<TMP_FontAsset, int> tmpFontUsage = new Dictionary<TMP_FontAsset, int>();

            GameObject[] allObjects = FindObjectsOfType<GameObject>(includeInactiveObjects);

            foreach (GameObject obj in allObjects)
            {
                // 统计Text组件字体
                Text textComponent = obj.GetComponent<Text>();
                if (textComponent != null && textComponent.font != null)
                {
                    if (fontUsage.ContainsKey(textComponent.font))
                        fontUsage[textComponent.font]++;
                    else
                        fontUsage[textComponent.font] = 1;
                }

                // 统计TextMeshPro组件字体
                TextMeshProUGUI tmpComponent = obj.GetComponent<TextMeshProUGUI>();
                if (tmpComponent != null && tmpComponent.font != null)
                {
                    if (tmpFontUsage.ContainsKey(tmpComponent.font))
                        tmpFontUsage[tmpComponent.font]++;
                    else
                        tmpFontUsage[tmpComponent.font] = 1;
                }
            }

            // 显示统计结果
            string statsMessage = "场景字体使用统计:\n\n";
            statsMessage += "Text字体:\n";
            if (fontUsage.Count > 0)
            {
                foreach (var pair in fontUsage)
                {
                    statsMessage += $"- {pair.Key.name}: {pair.Value} 个\n";
                }
            }
            else
            {
                statsMessage += "- 无Text字体\n";
            }

            statsMessage += "\nTextMeshPro字体:\n";
            if (tmpFontUsage.Count > 0)
            {
                foreach (var pair in tmpFontUsage)
                {
                    statsMessage += $"- {pair.Key.name}: {pair.Value} 个\n";
                }
            }
            else
            {
                statsMessage += "- 无TextMeshPro字体\n";
            }

            EditorUtility.DisplayDialog("字体使用统计", statsMessage, "确定");
        }
    }
}