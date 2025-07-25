using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 红点系统配置
    /// </summary>
    [CreateAssetMenu(fileName = "RedDotSystemConfig", menuName = "FFramework/RedDotKitConfig", order = 2)]
    public class RedDotKitConfig : ScriptableObject
    {
        public TextAsset configJsonTextAsset;
        // 所有树的定义
        public List<TreeDefinition> RedDotTrees = new List<TreeDefinition>();

#if UNITY_EDITOR
        [SerializeField, ShowOnly] private string configJsonPath = "Assets/RedDotSystemConfig.json";
#endif

        [Serializable]
        public class TreeDefinition
        {
            public string treeName;
            public RedDotKey rootKey;
            [Tooltip("当前树的所有节点关系")] public List<NodeRelation> nodeRelations = new List<NodeRelation>();
        }

        [Serializable]
        public class NodeRelation
        {
            [Tooltip("当前节点Key")] public RedDotKey nodeKey;
            [Tooltip("红点数量"), Min(0)] public int redDotCount = 0;
            [Tooltip("是否显示数量")] public bool isShowRedDotCount = true;
            [Tooltip("是否激活")] public bool isActive = true;
            [Tooltip("当前节点的父节点Key")] public List<RedDotKey> parentKeys = new List<RedDotKey>();
        }

        [Serializable]
        private class SerializedData
        {
            public List<TreeDefinition> RedDotTrees;
        }

#if UNITY_EDITOR

        [Button("保存数据", "yellow")]
        public void SaveData()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        // 获取实际的保存/加载路径
        private string GetActualConfigPath()
        {
            // 如果configJsonTextAsset中有数据，则使用该文件所在的路径
            if (configJsonTextAsset != null)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(configJsonTextAsset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // 获取文件所在的文件夹路径
                    string directoryPath = System.IO.Path.GetDirectoryName(assetPath);
                    // 获取原始文件名
                    string fileName = System.IO.Path.GetFileName(configJsonPath);
                    // 如果原路径没有文件名，使用默认名称
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = "RedDotConfig.json";
                    }
                    // 组合新的保存路径
                    return System.IO.Path.Combine(directoryPath, fileName);
                }
            }

            return configJsonPath;
        }

        [Button("保存数据到Json文件", "yellow")]
        public void SaveToJson()
        {
            try
            {
                string savePath = GetActualConfigPath();

                string json = JsonUtility.ToJson(new SerializedData { RedDotTrees = this.RedDotTrees }, true);
                System.IO.File.WriteAllText(savePath, json);

                // 更新configJsonPath为实际保存的路径
                configJsonPath = savePath;

                UnityEditor.AssetDatabase.Refresh();

                // 重新加载TextAsset引用
                configJsonTextAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(configJsonPath);

                Debug.Log($"红点系统配置已保存到: {configJsonPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存红点系统配置失败: {e.Message}");
            }
        }

        [Button("从Json文件中加载数据", "yellow")]
        public void LoadFromJson()
        {
            try
            {
                string loadPath = GetActualConfigPath();

                if (System.IO.File.Exists(loadPath))
                {
                    string json = System.IO.File.ReadAllText(loadPath);
                    var data = JsonUtility.FromJson<SerializedData>(json);
                    RedDotTrees = data.RedDotTrees;

                    // 更新configJsonPath为实际加载的路径
                    configJsonPath = loadPath;

                    // 更新TextAsset引用
                    configJsonTextAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(configJsonPath);

                    Debug.Log($"红点系统配置已从 {configJsonPath} 加载");
                }
                else
                {
                    Debug.LogWarning($"找不到红点系统配置文件: {loadPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载红点系统配置失败: {e.Message}");
            }
        }

#endif

    }
}