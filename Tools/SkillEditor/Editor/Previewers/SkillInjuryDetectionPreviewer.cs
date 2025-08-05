using System.Collections.Generic;
using UnityEngine;
using FFramework.Kit;

namespace SkillEditor
{
    /// <summary>
    /// 伤害检测预览器类，负责在编辑器模式下预览伤害检测
    /// </summary>
    public class SkillInjuryDetectionPreviewer : System.IDisposable
    {
        #region 私有字段

        /// <summary>技能拥有者</summary>
        private SkillRuntimeController skillOwner;

        /// <summary>技能配置</summary>
        private SkillConfig skillConfig;

        /// <summary>是否处于预览激活状态</summary>
        private bool isPreviewActive;

        /// <summary>当前激活的伤害检测组信息</summary>
        private Dictionary<int, List<CustomCollider>> activeCollisionGroups = new Dictionary<int, List<CustomCollider>>();

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否正在预览
        /// </summary>
        public bool IsPreviewActive => isPreviewActive;

        /// <summary>
        /// 技能拥有者
        /// </summary>
        public SkillRuntimeController SkillOwner => skillOwner;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner">技能拥有者</param>
        /// <param name="config">技能配置</param>
        public SkillInjuryDetectionPreviewer(SkillRuntimeController owner, SkillConfig config)
        {
            skillOwner = owner;
            skillConfig = config;
            isPreviewActive = false;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始伤害检测预览
        /// </summary>
        public void StartPreview()
        {
            if (skillOwner == null || skillConfig == null)
            {
                Debug.LogWarning("无法启动伤害检测预览：技能拥有者或技能配置为空");
                return;
            }

            isPreviewActive = true;
            Debug.Log($"伤害检测预览已启动 - 技能拥有者: {skillOwner.name}");
        }

        /// <summary>
        /// 停止伤害检测预览
        /// </summary>
        public void StopPreview()
        {
            // 停止预览时，确保所有碰撞组都被设置为非激活状态
            DeactivateAllCollisionGroups();
            isPreviewActive = false;
            Debug.Log("伤害检测预览已停止");
        }

        /// <summary>
        /// 预览指定帧的伤害检测
        /// </summary>
        /// <param name="frame">当前帧数</param>
        public void PreviewFrame(int frame)
        {
            if (!isPreviewActive || skillConfig?.trackContainer?.injuryDetectionTrack?.injuryDetectionTracks == null)
                return;

            // 遍历所有伤害检测轨道
            foreach (var injuryTrack in skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks)
            {
                // 检查轨道是否激活
                if (injuryTrack == null || !injuryTrack.isEnabled)
                    continue;

                if (injuryTrack.injuryDetectionClips != null)
                {
                    // 遍历轨道中的伤害检测片段
                    foreach (var injuryClip in injuryTrack.injuryDetectionClips)
                    {
                        ProcessInjuryDetectionClip(injuryClip, frame);
                    }
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 处理伤害检测片段
        /// </summary>
        /// <param name="injuryClip">伤害检测片段</param>
        /// <param name="currentFrame">当前帧</param>
        private void ProcessInjuryDetectionClip(FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip injuryClip, int currentFrame)
        {
            int startFrame = injuryClip.startFrame;
            int endFrame = injuryClip.startFrame + injuryClip.durationFrame;

            // 检查当前帧是否在伤害检测片段范围内
            if (currentFrame >= startFrame && currentFrame <= endFrame)
            {
                // 在范围内，激活对应的碰撞组
                ActivateCollisionGroup(injuryClip);
            }
            else
            {
                // 不在范围内，停用对应的碰撞组
                DeactivateCollisionGroup(injuryClip);
            }
        }

        /// <summary>
        /// 激活碰撞组
        /// </summary>
        /// <param name="injuryClip">伤害检测片段</param>
        private void ActivateCollisionGroup(FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip injuryClip)
        {
            if (skillOwner.collisionGroup == null) return;

            if (injuryClip.enableAllCollisionGroups)
            {
                // 启用所有碰撞组
                foreach (var collisionGroup in skillOwner.collisionGroup)
                {
                    ActivateCollidersInGroup(collisionGroup.collisionGroupId, collisionGroup.colliders);
                }
            }
            else
            {
                // 启用指定的碰撞组
                var targetGroup = skillOwner.collisionGroup.Find(g => g.collisionGroupId == injuryClip.collisionGroupId);
                if (targetGroup != null)
                {
                    ActivateCollidersInGroup(targetGroup.collisionGroupId, targetGroup.colliders);
                }
            }
        }

        /// <summary>
        /// 停用碰撞组
        /// </summary>
        /// <param name="injuryClip">伤害检测片段</param>
        private void DeactivateCollisionGroup(FFramework.Kit.InjuryDetectionTrack.InjuryDetectionClip injuryClip)
        {
            if (skillOwner.collisionGroup == null) return;

            if (injuryClip.enableAllCollisionGroups)
            {
                // 停用所有碰撞组
                foreach (var collisionGroup in skillOwner.collisionGroup)
                {
                    DeactivateCollidersInGroup(collisionGroup.collisionGroupId, collisionGroup.colliders);
                }
            }
            else
            {
                // 停用指定的碰撞组
                var targetGroup = skillOwner.collisionGroup.Find(g => g.collisionGroupId == injuryClip.collisionGroupId);
                if (targetGroup != null)
                {
                    DeactivateCollidersInGroup(targetGroup.collisionGroupId, targetGroup.colliders);
                }
            }
        }

        /// <summary>
        /// 激活指定组的碰撞器
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <param name="colliders">碰撞器列表</param>
        private void ActivateCollidersInGroup(int groupId, List<CustomCollider> colliders)
        {
            if (colliders == null) return;

            // 记录当前激活的碰撞组
            if (!activeCollisionGroups.ContainsKey(groupId))
            {
                activeCollisionGroups[groupId] = new List<CustomCollider>();
            }

            foreach (var collider in colliders)
            {
                if (collider != null && !collider.enabled)
                {
                    collider.enabled = true;
                    activeCollisionGroups[groupId].Add(collider);
                    Debug.Log($"激活碰撞器: {collider.name} (组ID: {groupId})");
                }
            }
        }

        /// <summary>
        /// 停用指定组的碰撞器
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <param name="colliders">碰撞器列表</param>
        private void DeactivateCollidersInGroup(int groupId, List<CustomCollider> colliders)
        {
            if (colliders == null) return;

            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled)
                {
                    collider.enabled = false;
                    Debug.Log($"停用碰撞器: {collider.name} (组ID: {groupId})");
                }
            }

            // 清理激活记录
            if (activeCollisionGroups.ContainsKey(groupId))
            {
                activeCollisionGroups[groupId].Clear();
            }
        }

        /// <summary>
        /// 停用所有激活的碰撞组
        /// </summary>
        private void DeactivateAllCollisionGroups()
        {
            foreach (var kvp in activeCollisionGroups)
            {
                foreach (var collider in kvp.Value)
                {
                    if (collider != null && collider.enabled)
                    {
                        collider.enabled = false;
                        Debug.Log($"停用碰撞器: {collider.name} (组ID: {kvp.Key})");
                    }
                }
            }
            activeCollisionGroups.Clear();
        }

        #endregion

        #region IDisposable实现

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            StopPreview();
        }

        #endregion
    }
}
