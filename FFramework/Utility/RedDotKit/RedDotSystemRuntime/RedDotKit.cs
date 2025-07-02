using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace FFramework.Kit
{
    /// <summary>
    /// 基于枚举的红点系统工具
    /// </summary>
    public static class RedDotKit
    {
        private static Dictionary<string, RedDotTree> trees = new Dictionary<string, RedDotTree>();
        private static Dictionary<RedDotKey, RedDotNode> allNodes = new Dictionary<RedDotKey, RedDotNode>();

        /// <summary>
        /// 初始化红点树
        /// </summary>
        public static void InitRedDotTree(RedDotKitConfig redDotKitConfig)
        {
            // 清空现有数据
            Clear();
            // 遍历所有树定义
            foreach (var treeDef in redDotKitConfig.RedDotTrees)
            {
                if (string.IsNullOrEmpty(treeDef.treeName))
                {
                    Debug.LogWarning("[RedDotSystem] Tree name is empty, skipped");
                    continue;
                }

                // 创建树
                var tree = CreateTree(treeDef.treeName, treeDef.rootKey);

                // 第一步：创建所有节点并建立父子关系
                foreach (var relation in treeDef.nodeRelations)
                {
                    var node = GetOrCreateNode(relation.nodeKey);
                    SetRedDotDisplayMode(relation.nodeKey, relation.isShowRedDotCount);

                    foreach (var parentKey in relation.parentKeys)
                    {
                        AddParent(relation.nodeKey, parentKey);
                    }
                }

                // 第二步：统一设置红点数量（从叶子节点开始）
                var processedNodes = new HashSet<RedDotKey>();
                foreach (var relation in treeDef.nodeRelations.OrderByDescending(r => r.parentKeys.Count))
                {
                    var node = GetNode(relation.nodeKey);
                    if (node != null && !processedNodes.Contains(relation.nodeKey))
                    {
                        node.SetCount(relation.redDotCount);
                        processedNodes.Add(relation.nodeKey);
                    }
                }

                Debug.Log($"[RedDotSystem] Initialized tree: {treeDef.treeName} with {treeDef.nodeRelations.Count} nodes");
            }
        }

        public static RedDotTree CreateTree(string treeName, RedDotKey rootKey)
        {
            if (trees.ContainsKey(treeName))
            {
                throw new ArgumentException($"Tree {treeName} already exists");
            }

            var tree = new RedDotTree(treeName, rootKey);
            trees.Add(treeName, tree);
            var rootNode = tree.Root;
            rootNode.SetCount(0);
            allNodes[rootKey] = rootNode;
            return tree;
        }

        public static RedDotNode GetOrCreateNode(RedDotKey key)
        {
            if (!allNodes.TryGetValue(key, out var node))
            {
                node = new RedDotNode(key);
                allNodes.Add(key, node);
            }
            return node;
        }

        public static void AddParent(RedDotKey childKey, RedDotKey parentKey)
        {
            var childNode = GetOrCreateNode(childKey);
            var parentNode = GetOrCreateNode(parentKey);

            childNode.AddParent(parentNode);
        }

        public static void ChangeRedDotCount(RedDotKey key, int count)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                node.SetCount(count);
                Debug.Log($"[RedDotSystem] Set {key} count to {count}, current parents: {node.Parents.Count}");
            }
            else
            {
                Debug.LogWarning($"[RedDotSystem] Node {key} not found when trying to set count to {count}");
            }
        }

        //设置是否显示红点数量
        public static void SetRedDotDisplayMode(RedDotKey key, bool isShowCount)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                node.SetDisplayMode(isShowCount);
            }
            else
            {
                Debug.LogWarning($"[RedDotSystem] Node {key} not found when trying to set display mode");
            }
        }

        /// <summary>
        /// 获取是否显示红点数量
        /// </summary>
        public static bool GetRedDotDisplayMode(RedDotKey key)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                return node.GetDisplayMode();
            }
            return true; // 默认显示数量
        }

        public static RedDotNode GetNode(RedDotKey key)
        {
            allNodes.TryGetValue(key, out var node);
            return node;
        }

        public static RedDotTree GetTree(string treeName)
        {
            trees.TryGetValue(treeName, out var tree);
            return tree;
        }

        public static int GetNodeCount(RedDotKey key)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                return node.Count;
            }
            return 0;
        }

        public static void Clear()
        {
            trees.Clear();
            allNodes.Clear();
            Debug.Log("[RedDotSystem] Cleared all data");
        }

        // 调试辅助方法
        public static void PrintNodeHierarchy(RedDotKey key)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                Debug.Log($"[RedDotSystem] Node {key} hierarchy:");
                Debug.Log($"- Count: {node.Count} (Base: {node.BaseCount}, Children: {node.ChildrenSum})");

                var parentKeys = new List<string>();
                foreach (var parent in node.Parents)
                {
                    parentKeys.Add(parent.Key.ToString());
                }
                Debug.Log($"- Parents: {string.Join(", ", parentKeys)}");

                var childKeys = new List<string>();
                foreach (var child in node.Children)
                {
                    childKeys.Add(child.Key.ToString());
                }
                Debug.Log($"- Children: {string.Join(", ", childKeys)}");
            }
            else
            {
                Debug.LogWarning($"[RedDotSystem] Node {key} not found");
            }
        }
    }
}
