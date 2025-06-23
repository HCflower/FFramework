using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 红点树
    /// </summary>
    public class RedDotTree
    {
        public string TreeName { get; private set; }
        public RedDotNode Root { get; private set; }
        private Dictionary<RedDotKey, RedDotNode> nodes = new Dictionary<RedDotKey, RedDotNode>();

        // 只读访问节点集合
        public IReadOnlyDictionary<RedDotKey, RedDotNode> Nodes => nodes;

        // 节点数量统计
        public int NodeCount => nodes.Count;

        public RedDotTree(string treeName, RedDotKey rootKey)
        {
            if (string.IsNullOrEmpty(treeName))
                throw new ArgumentException("Tree name cannot be null or empty", nameof(treeName));

            if (rootKey == RedDotKey.None)
                throw new ArgumentException("Root key cannot be None", nameof(rootKey));

            TreeName = treeName;
            Root = new RedDotNode(rootKey);
            AddNode(Root);
            Root.Trees.Add(this);

            Debug.Log($"[RedDotTree] Created tree '{treeName}' with root key '{rootKey}'");
        }

        public void AddNode(RedDotNode node)
        {
            if (node == null)
            {
                Debug.LogWarning("[RedDotTree] Cannot add null node to tree");
                return;
            }

            if (!nodes.ContainsKey(node.Key))
            {
                nodes.Add(node.Key, node);
                node.Trees.Add(this);
                Debug.Log($"[RedDotTree] Added node '{node.Key}' to tree '{TreeName}'");
            }
            else
            {
                Debug.LogWarning($"[RedDotTree] Node '{node.Key}' already exists in tree '{TreeName}'");
            }
        }

        public void RemoveNode(RedDotNode node)
        {
            if (node == null)
            {
                Debug.LogWarning("[RedDotTree] Cannot remove null node from tree");
                return;
            }

            if (node == Root)
            {
                Debug.LogWarning($"[RedDotTree] Cannot remove root node from tree '{TreeName}'");
                return;
            }

            if (nodes.Remove(node.Key))
            {
                node.Trees.Remove(this);
                Debug.Log($"[RedDotTree] Removed node '{node.Key}' from tree '{TreeName}'");
            }
        }

        public RedDotNode GetNode(RedDotKey key)
        {
            nodes.TryGetValue(key, out var node);
            return node;
        }

        public void ChangeRedDotCount(RedDotKey key, int count)
        {
            if (nodes.TryGetValue(key, out var node))
            {
                node.SetCount(count);
            }
        }

        public void SetRedDotDisplayMode(RedDotKey key, bool isShowCount)
        {
            if (nodes.TryGetValue(key, out var node))
            {
                node.SetDisplayMode(isShowCount);
            }
        }

        // 检查节点是否存在
        public bool HasNode(RedDotKey key)
        {
            return nodes.ContainsKey(key);
        }

        // 获取所有节点Key
        public IEnumerable<RedDotKey> GetAllNodeKeys()
        {
            return nodes.Keys;
        }

        // 获取所有节点
        public IEnumerable<RedDotNode> GetAllNodes()
        {
            return nodes.Values;
        }

        // 批量操作 - 设置多个节点的数量
        public void BatchChangeRedDotCount(Dictionary<RedDotKey, int> keyCountPairs)
        {
            if (keyCountPairs == null) return;

            foreach (var kvp in keyCountPairs)
            {
                ChangeRedDotCount(kvp.Key, kvp.Value);
            }
        }

        // 调试功能 - 打印树结构
        public void PrintTreeStructure()
        {
            Debug.Log($"=== Tree Structure: {TreeName} ===");
            Debug.Log($"Total Nodes: {NodeCount}");
            Debug.Log($"Root: {Root.Key} (Count: {Root.Count})");

            PrintNodeHierarchy(Root, 0);
        }

        private void PrintNodeHierarchy(RedDotNode node, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}- {node.Key}: Count={node.Count} (Base:{node.BaseCount}, Children:{node.ChildrenSum})");

            foreach (var child in node.Children)
            {
                PrintNodeHierarchy(child, depth + 1);
            }
        }

        // 获取树的统计信息
        public TreeStatistics GetStatistics()
        {
            var stats = new TreeStatistics
            {
                TreeName = TreeName,
                TotalNodes = NodeCount,
                TotalRedDotCount = nodes.Values.Sum(n => n.Count),
                MaxDepth = GetMaxDepth(Root, 0)
            };

            return stats;
        }

        private int GetMaxDepth(RedDotNode node, int currentDepth)
        {
            if (node.Children.Count == 0)
                return currentDepth;

            int maxChildDepth = currentDepth;
            foreach (var child in node.Children)
            {
                maxChildDepth = Math.Max(maxChildDepth, GetMaxDepth(child, currentDepth + 1));
            }

            return maxChildDepth;
        }

        // 清理树（保留根节点）
        public void Clear()
        {
            var nodesToRemove = nodes.Values.Where(n => n != Root).ToList();
            foreach (var node in nodesToRemove)
            {
                RemoveNode(node);
            }

            Root.SetCount(0);
            Debug.Log($"[RedDotTree] Cleared tree '{TreeName}', kept root node");
        }

        // 树统计信息结构
        public struct TreeStatistics
        {
            public string TreeName;
            public int TotalNodes;
            public int TotalRedDotCount;
            public int MaxDepth;

            public override string ToString()
            {
                return $"Tree: {TreeName}, Nodes: {TotalNodes}, Total Count: {TotalRedDotCount}, Max Depth: {MaxDepth}";
            }
        }
    }
}
