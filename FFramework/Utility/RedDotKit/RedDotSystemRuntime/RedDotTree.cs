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

        /// <summary>
        /// 红点树构造函数
        /// </summary>
        public RedDotTree(string treeName, RedDotKey rootKey)
        {
            if (string.IsNullOrEmpty(treeName))
                throw new ArgumentException("树名称不能为null或为空.", nameof(treeName));

            if (rootKey == RedDotKey.None)
                throw new ArgumentException("根键不能为None.", nameof(rootKey));

            TreeName = treeName;
            Root = new RedDotNode(rootKey);
            AddNode(Root);
            Root.Trees.Add(this);

            Debug.Log($"[RedDotTree] 已创建根键为'{rootKey}'的红点树'{treeName}.");
        }

        /// <summary>
        /// 添加节点    
        /// </summary>
        public void AddNode(RedDotNode node)
        {
            if (node == null)
            {
                Debug.LogWarning("[RedDotTree] 无法将 null 节点添加到树中.");
                return;
            }

            if (!nodes.ContainsKey(node.Key))
            {
                nodes.Add(node.Key, node);
                node.Trees.Add(this);
                Debug.Log($"[RedDotTree] 添加节点 '{node.Key}' 到树 '{TreeName}'");
            }
            else
            {
                Debug.LogWarning($"[RedDotTree] 节点 '{node.Key}' 已存在于树中 '{TreeName}'");
            }
        }

        /// <summary>
        /// 移除节点
        /// </summary>
        public void RemoveNode(RedDotNode node)
        {
            if (node == null)
            {
                Debug.LogWarning("[RedDotTree] 无法从树中删除 null 节点.");
                return;
            }

            if (node == Root)
            {
                Debug.LogWarning($"[RedDotTree] 无法从树中删除根节点 '{TreeName}'.");
                return;
            }

            if (nodes.Remove(node.Key))
            {
                node.Trees.Remove(this);
                Debug.Log($"[RedDotTree] 从树'{TreeName}'中移除节点 '{node.Key}'.");
            }
        }

        // 获取节点
        public RedDotNode GetNode(RedDotKey key)
        {
            nodes.TryGetValue(key, out var node);
            return node;
        }

        // 更新节点的红点数量
        public void ChangeRedDotCount(RedDotKey key, int count)
        {
            if (nodes.TryGetValue(key, out var node))
            {
                node.SetCount(count);
            }
        }

        // 设置节点的显示模式
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

        // 获取树的最大深度
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
            Debug.Log($"[RedDotTree] 清除了树 '{TreeName}'，保留了根节点.");
        }

#if UNITY_EDITOR
        // 调试功能 - 打印树结构
        public void PrintTreeStructure()
        {
            Debug.Log($"=== 树结构: {TreeName} ===");
            Debug.Log($"节点总数量: {NodeCount}");
            Debug.Log($"根节点: {Root.Key} (数量: {Root.Count})");

            PrintNodeHierarchy(Root, 0);
        }

        // 调试功能 - 打印树统计信息
        private void PrintNodeHierarchy(RedDotNode node, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.Log($"{indent}- {node.Key}: Count={node.Count} (Base:{node.BaseCount}, Children:{node.ChildrenSum})");

            foreach (var child in node.Children)
            {
                PrintNodeHierarchy(child, depth + 1);
            }
        }
#endif

    }
}
