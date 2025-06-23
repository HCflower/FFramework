using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 红点系统配置
    /// </summary>
    [CreateAssetMenu(fileName = "RedDotSystemConfig", menuName = "FFramework/RedDot System Config")]
    public class RedDotKitConfig : ScriptableObject
    {
        public TextAsset redDotSystemConfigJson;
        public string redDotSystemConfigJsonPath = "Assets/RedDotSystemConfig.json";
        // 所有树的定义
        public List<TreeDefinition> RedDotTrees = new List<TreeDefinition>();

        [Serializable]
        public class NodeRelation
        {
            [Tooltip("当前节点Key")] public RedDotKey nodeKey;
            [Tooltip("红点数量"), Min(0)] public int redDotCount = 1;
            [Tooltip("当前节点的父节点Key")] public List<RedDotKey> parentKeys = new List<RedDotKey>();
        }

        [Serializable]
        public class TreeDefinition
        {
            public string treeName;
            public RedDotKey rootKey;
            public List<NodeRelation> nodeRelations = new List<NodeRelation>();
        }

#if UNITY_EDITOR
        [Button("保存数据到Json文件", "yellow")]
        private void SaveToJson()
        {
            try
            {
                string json = JsonUtility.ToJson(new SerializedData { RedDotTrees = this.RedDotTrees }, true);
                System.IO.File.WriteAllText(redDotSystemConfigJsonPath, json);
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log($"红点系统配置已保存到: {redDotSystemConfigJsonPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存红点系统配置失败: {e.Message}");
            }
        }
#endif

#if UNITY_EDITOR
        [Button("从Json文件中加载数据", "yellow")]
        public void LoadFromJson()
        {
            try
            {
                if (System.IO.File.Exists(redDotSystemConfigJsonPath))
                {
                    string json = System.IO.File.ReadAllText(redDotSystemConfigJsonPath);
                    var wrapper = JsonUtility.FromJson<Wrapper>(json);
                    RedDotTrees = wrapper.RedDotTrees;
                    Debug.Log($"红点系统配置已从 {redDotSystemConfigJsonPath} 加载");
                }
                else
                {
                    Debug.LogWarning($"找不到红点系统配置文件: {redDotSystemConfigJsonPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载红点系统配置失败: {e.Message}");
            }
        }
#endif

        [Serializable]
        private class SerializedData
        {
            public List<TreeDefinition> RedDotTrees;
        }

        [Serializable]
        private class Wrapper
        {
            public List<TreeDefinition> RedDotTrees;
        }

    }
}