using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 基于枚举的红点系统工具
    /// </summary>
    public static class RedDotKit
    {
        private static Dictionary<string, RedDotTree> trees = new Dictionary<string, RedDotTree>();
        private static Dictionary<RedDotKey, RedDotNode> allNodes = new Dictionary<RedDotKey, RedDotNode>();

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

        public static void RemoveParent(RedDotKey childKey, RedDotKey parentKey)
        {
            if (allNodes.TryGetValue(childKey, out var childNode) &&
                allNodes.TryGetValue(parentKey, out var parentNode))
            {
                childNode.RemoveParent(parentNode);
            }
        }

        public static void RemoveNode(RedDotKey key)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                // 断开与父节点的连接
                var parentsCopy = new List<RedDotNode>(node.Parents);
                foreach (var parent in parentsCopy)
                {
                    node.RemoveParent(parent);
                }

                // 断开与子节点的连接
                var childrenCopy = new List<RedDotNode>(node.Children);
                foreach (var child in childrenCopy)
                {
                    child.RemoveParent(node);
                }

                // 从所有树中移除
                var treesCopy = new List<RedDotTree>(node.Trees);
                foreach (var tree in treesCopy)
                {
                    tree.RemoveNode(node);
                }

                // 从节点字典中移除
                allNodes.Remove(key);

                Debug.Log($"[RedDotSystem] Removed node {key}");
            }
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

        public static bool HasNode(RedDotKey key)
        {
            return allNodes.ContainsKey(key);
        }

        public static int GetNodeCount(RedDotKey key)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                return node.Count;
            }
            return 0;
        }

        public static IEnumerable<RedDotKey> GetAllNodeKeys()
        {
            return allNodes.Keys;
        }

        public static IEnumerable<RedDotNode> GetAllNodes()
        {
            return allNodes.Values;
        }

        // 批量操作支持
        public static void BatchAddParents(RedDotKey childKey, params RedDotKey[] parentKeys)
        {
            var childNode = GetOrCreateNode(childKey);
            foreach (var parentKey in parentKeys)
            {
                var parentNode = GetOrCreateNode(parentKey);
                childNode.AddParent(parentNode);
            }
        }

        public static void BatchRemoveParents(RedDotKey childKey, params RedDotKey[] parentKeys)
        {
            if (allNodes.TryGetValue(childKey, out var childNode))
            {
                foreach (var parentKey in parentKeys)
                {
                    if (allNodes.TryGetValue(parentKey, out var parentNode))
                    {
                        childNode.RemoveParent(parentNode);
                    }
                }
            }
        }

        // 获取节点的所有父节点Key
        public static IEnumerable<RedDotKey> GetParentKeys(RedDotKey key)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                var parentKeys = new List<RedDotKey>();
                foreach (var parent in node.Parents)
                {
                    parentKeys.Add(parent.Key);
                }
                return parentKeys;
            }
            return new List<RedDotKey>();
        }

        // 获取节点的所有子节点Key
        public static IEnumerable<RedDotKey> GetChildrenKeys(RedDotKey key)
        {
            if (allNodes.TryGetValue(key, out var node))
            {
                var childKeys = new List<RedDotKey>();
                foreach (var child in node.Children)
                {
                    childKeys.Add(child.Key);
                }
                return childKeys;
            }
            return new List<RedDotKey>();
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
