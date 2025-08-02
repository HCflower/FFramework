using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FFramework.Kit;

namespace SkillEditor
{
    /// <summary>
    /// 技能Transform预览器
    /// 负责在编辑器模式下预览Transform变换，包括位置、旋转和缩放动画
    /// 不影响其他轨道的预览效果，独立管理Transform状态
    /// </summary>
    public class SkillTransformPreviewer
    {
        #region 私有字段

        /// <summary>技能所有者游戏对象</summary>
        private SkillRuntimeController skillOwner;

        /// <summary>技能配置数据</summary>
        private SkillConfig skillConfig;

        /// <summary>是否正在预览中</summary>
        private bool isPreviewActive = false;

        /// <summary>是否在预览结束后保持状态</summary>
        private bool keepStateOnEnd = true;

        /// <summary>当前预览帧</summary>
        private int currentFrame = 0;

        /// <summary>上一次预览的帧（用于检测循环重新开始）</summary>
        private int previousFrame = -1;

        /// <summary>存储原始Transform状态（用于恢复）</summary>
        private struct OriginalTransformState
        {
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }

        /// <summary>原始Transform状态</summary>
        private OriginalTransformState originalTransform;

        /// <summary>Transform组件引用</summary>
        private Transform targetTransform;

        #endregion

        #region 公共属性

        /// <summary>是否正在预览中</summary>
        public bool IsPreviewActive => isPreviewActive;

        /// <summary>当前预览帧</summary>
        public int CurrentFrame => currentFrame;

        /// <summary>技能所有者</summary>
        public SkillRuntimeController SkillOwner => skillOwner;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造Transform预览器
        /// </summary>
        /// <param name="owner">技能所有者</param>
        /// <param name="config">技能配置</param>
        public SkillTransformPreviewer(SkillRuntimeController owner, SkillConfig config)
        {
            skillOwner = owner;
            skillConfig = config;

            if (skillOwner != null)
            {
                targetTransform = skillOwner.transform;
                StoreOriginalTransform();
            }
        }

        #endregion

        #region 预览控制方法

        /// <summary>
        /// 开始Transform预览
        /// </summary>
        public void StartPreview()
        {
            if (skillOwner == null || skillConfig == null || targetTransform == null)
            {
                Debug.LogWarning("SkillTransformPreviewer: 无法开始预览，技能所有者或配置为空");
                return;
            }

            if (isPreviewActive)
            {
                return;
            }

            isPreviewActive = true;
            currentFrame = 0;
            previousFrame = -1; // 重置上一帧记录

            // 存储原始Transform状态
            StoreOriginalTransform();

            // 预览第一帧
            PreviewFrame(0);
        }

        /// <summary>
        /// 停止Transform预览
        /// </summary>
        public void StopPreview()
        {
            if (!isPreviewActive) return;

            isPreviewActive = false;
            currentFrame = 0;
            previousFrame = -1; // 重置上一帧记录

            // 如果不保持状态，则恢复原始Transform
            if (!keepStateOnEnd)
            {
                RestoreOriginalTransform();
            }
        }

        /// <summary>
        /// 预览指定帧的Transform变换
        /// </summary>
        /// <param name="frame">目标帧</param>
        public void PreviewFrame(int frame)
        {
            if (!isPreviewActive || skillConfig == null || targetTransform == null) return;

            int targetFrame = Mathf.Clamp(frame, 0, skillConfig.maxFrames);

            // 检查是否是循环重新开始（从高帧数突然跳到第0帧）
            bool isLoopRestart = (targetFrame == 0 && previousFrame >= skillConfig.maxFrames - 1);

            // 如果是循环重新开始，更新所有clips的初始Transform状态为当前状态
            if (isLoopRestart)
            {
                UpdateClipsInitialTransform();
            }

            currentFrame = targetFrame;

            // 获取当前帧的Transform数据并应用
            ApplyTransformAtFrame(currentFrame);

            // 当预览到最后一帧时，将当前Transform状态保存为新的原始状态（为下次循环做准备）
            if (currentFrame == skillConfig.maxFrames - 1)
            {
                StoreOriginalTransform();
            }

            // 更新上一帧记录
            previousFrame = currentFrame;
        }
        /// <summary>  
        /// 刷新Transform数据 - 当轨道项发生变化时调用
        /// </summary>
        public void RefreshTransformData()
        {
            if (!isPreviewActive) return;

            // 重新预览当前帧
            PreviewFrame(currentFrame);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 存储原始Transform状态
        /// </summary>
        private void StoreOriginalTransform()
        {
            if (targetTransform == null) return;

            originalTransform = new OriginalTransformState
            {
                position = targetTransform.position,
                rotation = targetTransform.rotation.eulerAngles,
                scale = targetTransform.localScale
            };
        }

        /// <summary>
        /// 更新所有clips的初始Transform状态为当前Transform状态（用于循环累加）
        /// </summary>
        private void UpdateClipsInitialTransform()
        {
            if (skillConfig?.trackContainer?.transformTrack == null || targetTransform == null)
                return;

            var transformTrack = skillConfig.trackContainer.transformTrack;

            // 更新轨道中所有clips的初始状态为当前Transform状态
            transformTrack.InitializeTransforms(targetTransform);
        }

        /// <summary>
        /// 恢复原始Transform状态
        /// </summary>
        private void RestoreOriginalTransform()
        {
            if (targetTransform == null) return;

            targetTransform.position = originalTransform.position;
            targetTransform.rotation = Quaternion.Euler(originalTransform.rotation);
            targetTransform.localScale = originalTransform.scale;
        }

        /// <summary>
        /// 应用指定帧的Transform变换
        /// </summary>
        /// <param name="frame">目标帧</param>
        private void ApplyTransformAtFrame(int frame)
        {
            if (skillConfig?.trackContainer?.transformTrack == null || targetTransform == null)
                return;

            var transformTrack = skillConfig.trackContainer.transformTrack;

            // 使用累加方式应用Transform变换，不重置到原始状态
            // 这样可以保持循环播放时的状态连续性
            bool hasTransform = transformTrack.ExecuteAtFrame(targetTransform, frame);

            if (hasTransform)
            {
                // 标记场景需要重绘
                UnityEditor.SceneView.RepaintAll();
            }
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            // 强制恢复原始状态
            keepStateOnEnd = false;
            StopPreview();
            skillOwner = null;
            skillConfig = null;
            targetTransform = null;
        }

        #endregion
    }
}