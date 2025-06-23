using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 支持多父节点的红点节点
    /// </summary>
    public class RedDotNode
    {
        public RedDotKey Key { get; private set; }
        public int Count { get; private set; }
        public int BaseCount { get; private set; } // 节点自身设置值
        public int ChildrenSum { get; private set; } // 子节点聚合值
        public bool IsShowCount { get; private set; }

        // 所属树集合（一个节点可以属于多棵树）
        public HashSet<RedDotTree> Trees { get; } = new HashSet<RedDotTree>();

        // 父节点集合（支持多父节点）
        private HashSet<RedDotNode> parents = new HashSet<RedDotNode>();
        public IReadOnlyCollection<RedDotNode> Parents => parents;

        // 子节点集合
        private HashSet<RedDotNode> children = new HashSet<RedDotNode>();
        public IReadOnlyCollection<RedDotNode> Children => children;

        public event Action<RedDotNode> OnStateChanged;

        public RedDotNode(RedDotKey key)
        {
            Key = key;
            Count = 0;
            IsShowCount = true;
        }

        // 添加父节点
        public void AddParent(RedDotNode parent)
        {
            if (parents.Add(parent))
            {
                parent.children.Add(this);
                UpdateTrees();
            }
        }

        // 移除父节点
        public void RemoveParent(RedDotNode parent)
        {
            if (parents.Remove(parent))
            {
                parent.children.Remove(this);
                UpdateTrees();
            }
        }

        // 更新所属树
        private void UpdateTrees()
        {
            // 收集所有父节点所在的树
            var newTrees = new HashSet<RedDotTree>();
            foreach (var parent in parents)
            {
                foreach (var tree in parent.Trees)
                {
                    newTrees.Add(tree);
                }
            }

            // 从旧树中移除
            foreach (var oldTree in Trees)
            {
                if (!newTrees.Contains(oldTree))
                {
                    oldTree.RemoveNode(this);
                }
            }

            // 添加到新树
            foreach (var newTree in newTrees)
            {
                if (!Trees.Contains(newTree))
                {
                    newTree.AddNode(this);
                }
            }

            // 更新树集合
            Trees.Clear();
            foreach (var tree in newTrees)
            {
                Trees.Add(tree);
            }

            // 递归更新子节点的树
            foreach (var child in children)
            {
                child.UpdateTrees();
            }
        }

        public void SetCount(int count)
        {
            if (BaseCount != count)
            {
                BaseCount = count;
                UpdateTotalCount();
                OnStateChanged?.Invoke(this);
                UpdateParents();
            }
        }

        private void UpdateTotalCount()
        {
            Count = BaseCount + ChildrenSum;
        }

        public void SetDisplayMode(bool isShowCount)
        {
            if (IsShowCount != isShowCount)
            {
                IsShowCount = isShowCount;
                OnStateChanged?.Invoke(this);
            }
        }

        // 更新所有父节点
        private void UpdateParents()
        {
            foreach (var parent in parents)
            {
                parent.UpdateFromChildren();
            }
        }

        internal void UpdateFromChildren()
        {
            int newChildrenSum = 0;
            Debug.Log($"[RedDotNode] Updating children sum for {Key}:");

            foreach (var child in children)
            {
                Debug.Log($"- Child {child.Key}: Count={child.Count}");
                newChildrenSum += child.Count;
            }

            Debug.Log($"[RedDotNode] New children sum for {Key}: {newChildrenSum} (was {ChildrenSum})");

            if (ChildrenSum != newChildrenSum)
            {
                ChildrenSum = newChildrenSum;
                UpdateTotalCount();
                Debug.Log($"[RedDotNode] Updated {Key}: BaseCount={BaseCount} + ChildrenSum={ChildrenSum} = Count={Count}");
                OnStateChanged?.Invoke(this);
                UpdateParents();
            }
        }

        // 检查是否是指定节点的祖先
        public bool IsAncestorOf(RedDotNode node)
        {
            var nodesToCheck = new Queue<RedDotNode>();
            var visited = new HashSet<RedDotNode>();

            nodesToCheck.Enqueue(node);
            visited.Add(node);

            while (nodesToCheck.Count > 0)
            {
                var current = nodesToCheck.Dequeue();

                foreach (var parent in current.parents)
                {
                    if (parent == this)
                        return true;

                    if (!visited.Contains(parent))
                    {
                        visited.Add(parent);
                        nodesToCheck.Enqueue(parent);
                    }
                }
            }

            return false;
        }

        // 获取所有后代节点
        public HashSet<RedDotNode> GetAllDescendants()
        {
            var descendants = new HashSet<RedDotNode>();
            var nodesToCheck = new Queue<RedDotNode>();

            foreach (var child in children)
            {
                nodesToCheck.Enqueue(child);
                descendants.Add(child);
            }

            while (nodesToCheck.Count > 0)
            {
                var current = nodesToCheck.Dequeue();

                foreach (var child in current.children)
                {
                    if (!descendants.Contains(child))
                    {
                        descendants.Add(child);
                        nodesToCheck.Enqueue(child);
                    }
                }
            }

            return descendants;
        }
    }
}
