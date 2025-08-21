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
        private Dictionary<string, List<Collider>> activeCollisionGroups = new Dictionary<string, List<Collider>>();

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
        }

        /// <summary>
        /// 停止伤害检测预览
        /// </summary>
        public void StopPreview()
        {
            // 停止预览时，确保所有碰撞组都被设置为非激活状态
            DeactivateAllCollisionGroups();
            isPreviewActive = false;
        }

        /// <summary>
        /// 预览指定帧的伤害检测
        /// </summary>
        /// <param name="frame">当前帧数</param>
        public void PreviewFrame(int frame)
        {
            if (!isPreviewActive || skillConfig?.trackContainer?.injuryDetectionTrack?.injuryDetectionTracks == null)
                return;

            // 记录本帧需要激活的所有碰撞体
            var collidersToActivate = new HashSet<Collider>();

            foreach (var injuryTrack in skillConfig.trackContainer.injuryDetectionTrack.injuryDetectionTracks)
            {
                if (injuryTrack == null || !injuryTrack.isEnabled)
                    continue;

                if (injuryTrack.injuryDetectionClips != null)
                {
                    foreach (var injuryClip in injuryTrack.injuryDetectionClips)
                    {
                        int startFrame = injuryClip.startFrame;
                        int endFrame = startFrame + injuryClip.durationFrame;

                        if (frame >= startFrame && frame < endFrame)
                        {
                            if (injuryClip.enableAllCollisionGroups)
                            {
                                foreach (var collisionGroup in skillOwner.collisionGroup)
                                {
                                    if (collisionGroup.colliders != null)
                                        foreach (var col in collisionGroup.colliders)
                                            collidersToActivate.Add(col);
                                }
                            }
                            else
                            {
                                var targetGroup = skillOwner.collisionGroup.Find(g => g.injuryDetectionGroupUID == injuryClip.injuryDetectionGroupUID);
                                if (targetGroup != null && targetGroup.colliders != null)
                                {
                                    foreach (var col in targetGroup.colliders)
                                        collidersToActivate.Add(col);
                                }
                            }
                        }
                    }
                }
            }

            // 激活本帧需要的所有碰撞体
            foreach (var col in collidersToActivate)
            {
                if (col != null && !col.enabled)
                    col.enabled = true;
            }

            // 禁用其它未激活的碰撞体
            foreach (var group in skillOwner.collisionGroup)
            {
                if (group.colliders == null) continue;
                foreach (var col in group.colliders)
                {
                    if (col != null && !collidersToActivate.Contains(col) && col.enabled)
                        col.enabled = false;
                }
            }
        }

        #endregion

        #region 私有方法
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
                    ActivateCollidersInGroup(collisionGroup.injuryDetectionGroupUID, collisionGroup.colliders);
                }
            }
            else
            {
                // 启用指定的碰撞组
                var targetGroup = skillOwner.collisionGroup.Find(g => g.injuryDetectionGroupUID == injuryClip.injuryDetectionGroupUID);
                if (targetGroup != null)
                {
                    ActivateCollidersInGroup(targetGroup.injuryDetectionGroupUID, targetGroup.colliders);
                }
            }
        }

        /// <summary>
        /// 激活指定组的碰撞器
        /// </summary>
        /// <param name="injuryDetectionGroupUID">组ID</param>
        /// <param name="colliders">碰撞器列表</param>
        private void ActivateCollidersInGroup(string injuryDetectionGroupUID, List<Collider> colliders)
        {
            if (colliders == null) return;

            // 记录当前激活的碰撞组
            if (!activeCollisionGroups.ContainsKey(injuryDetectionGroupUID))
            {
                activeCollisionGroups[injuryDetectionGroupUID] = new List<Collider>();
            }

            foreach (var collider in colliders)
            {
                if (collider != null && !collider.enabled)
                {
                    collider.enabled = true;
                    activeCollisionGroups[injuryDetectionGroupUID].Add(collider);
                }
            }
        }

        /// <summary>
        /// 停用指定组的碰撞器
        /// </summary>
        /// <param name="injuryDetectionGroupUID">组ID</param>
        /// <param name="colliders">碰撞器列表</param>
        private void DeactivateCollidersInGroup(string injuryDetectionGroupUID, List<Collider> colliders)
        {
            if (colliders == null) return;

            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled)
                {
                    collider.enabled = false;
                }
            }

            // 清理激活记录
            if (activeCollisionGroups.ContainsKey(injuryDetectionGroupUID))
            {
                activeCollisionGroups[injuryDetectionGroupUID].Clear();
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
