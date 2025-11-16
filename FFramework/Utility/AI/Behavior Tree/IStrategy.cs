// =============================================================
// 描述：行为树策略接口
// 作者：HCFlower
// 创建时间：2025-11-16 16:30:00
// 版本：1.0.0
// =============================================================
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;
using System;

namespace BehaviourTree
{
    // 策略接口
    public interface IStrategy
    {
        Node.Status Process();  // 处理节点逻辑
        void Reset() { }        // 重置节点状态(默认空实现)
    }

    /// <summary>
    /// 巡逻策略
    /// </summary>
    /// <remarks>
    /// PatrolStrategy（巡逻策略）：实体沿预设路径点巡逻，使用NavMeshAgent进行路径计算和移动。
    /// </remarks>
    public class PatrolStrategy : IStrategy
    {
        readonly Transform entity;
        readonly NavMeshAgent agent;
        readonly List<Transform> patrolPoints;
        readonly float speed;
        int currentPointIndex = 0;
        bool isPathCalculated = false;
        public PatrolStrategy(Transform entity, NavMeshAgent agent, List<Transform> patrolPoints, float speed)
        {
            this.entity = entity;
            this.agent = agent;
            this.patrolPoints = patrolPoints;
            this.speed = speed;
        }

        // 策略执行
        public Node.Status Process()
        {
            if (currentPointIndex >= patrolPoints.Count) return Node.Status.Success;

            var target = patrolPoints[currentPointIndex];

            // 仅在切换目标点时设置
            if (!isPathCalculated)
            {
                agent.SetDestination(target.position);
                agent.speed = speed;
                entity.LookAt(target.position);
                isPathCalculated = true;
            }

            // 路径计算完成且到达目标点
            if (!agent.pathPending && agent.remainingDistance <= 0.1f)
            {
                currentPointIndex++;
                isPathCalculated = false;
            }

            return Node.Status.Running;
        }

        // 重置策略状态
        public void Reset() => currentPointIndex = 0;
    }

    /// <summary>
    /// 条件策略
    /// </summary>
    /// <remarks>
    /// Condition（条件策略）：基于传入的布尔函数，判断条件是否满足，返回成功或失败状态。
    /// </remarks>
    public class Condition : IStrategy
    {
        readonly Func<bool> predicate;
        public Condition(Func<bool> predicate)
        {
            this.predicate = predicate;
        }
        public Node.Status Process() => predicate() ? Node.Status.Success : Node.Status.Failure;
    }
}