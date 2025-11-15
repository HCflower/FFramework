// =============================================================
// 描述：平滑震动效果
// 作者：HCFlower
// 创建时间：2025-11-16 00:44:00
// 版本：1.0.0
// =============================================================
using System.Collections;
using UnityEngine;

namespace FFramework.Utility
{
    /// <summary>
    /// 通用平滑震动组件
    /// 基于SmoothShakeFree的实现原理，专为技能编辑器设计
    /// 可用于任何GameObject，包括摄像机、UI元素、特效等
    /// </summary>
    public class SmoothShake : ShakeBase
    {
        [Header("目标设置")]
        [Tooltip("指定震动目标,如果为空则使用当前GameObject")]
        public Transform shakeTarget;

        private Coroutine shakeCoroutine;

        /// <summary>
        /// 获取实际的震动目标
        /// </summary>
        private Transform ShakeTransform => shakeTarget != null ? shakeTarget : transform;

        /// <summary>
        /// 保存原始变换状态
        /// </summary>
        public override void SaveOriginalTransform()
        {
            var target = ShakeTransform;
            originalPosition = target.localPosition;
            originalRotation = target.localEulerAngles;
        }

        /// <summary>
        /// 应用震动变换
        /// </summary>
        /// <param name="positionOffset">位置偏移</param>
        /// <param name="rotationOffset">旋转偏移</param>
        protected override void ApplyShake(Vector3 positionOffset, Vector3 rotationOffset)
        {
            var target = ShakeTransform;
            target.localPosition = originalPosition + positionOffset;
            target.localEulerAngles = originalRotation + rotationOffset;
        }

        /// <summary>
        /// 重置变换到原始状态
        /// </summary>
        public override void ResetTransform()
        {
            var target = ShakeTransform;
            target.localPosition = originalPosition;
            target.localEulerAngles = originalRotation;
        }

        /// <summary>
        /// 设置震动目标
        /// </summary>
        /// <param name="target">目标Transform</param>
        public void SetShakeTarget(Transform target)
        {
            shakeTarget = target;
            SaveOriginalTransform();
        }

        // 替换震动启动逻辑为协程
        public override void StartShake()
        {
            if (shakePreset == null)
            {
                Debug.LogWarning($"ShakeBase: 没有设置震动预设文件，无法开始震动");
                return;
            }

            if (isShaking)
            {
                StopShake();
            }

            SaveOriginalTransform();

            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }

        public override void StopShake()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }
            isShaking = false;
            ResetTransform();
        }

        private IEnumerator ShakeCoroutine()
        {
            isShaking = true;
            float totalDuration = fadeInDuration + holdDuration + fadeOutDuration;
            float elapsed = 0f;
            float lastCalculatedTime = -1f;
            float lastCalculatedIntensity = 0f;
            Vector3 cachedPositionOffset = Vector3.zero;
            Vector3 cachedRotationOffset = Vector3.zero;

            while (elapsed < totalDuration && isShaking)
            {
                if (Mathf.Abs(elapsed - lastCalculatedTime) > 0.001f)
                {
                    lastCalculatedTime = elapsed;
                    lastCalculatedIntensity = CalculateIntensity(elapsed);

                    cachedPositionOffset = positionShake.Evaluate(elapsed) * lastCalculatedIntensity;
                    cachedRotationOffset = rotationShake.Evaluate(elapsed) * lastCalculatedIntensity;
                }

                ApplyShake(cachedPositionOffset, cachedRotationOffset);

                elapsed += Time.deltaTime;
                yield return null;
            }

            ResetTransform();
            isShaking = false;
        }

        /// <summary>
        /// 快速开始震动（使用简单参数）
        /// </summary>
        /// <param name="positionIntensity">位置震动强度</param>
        /// <param name="rotationIntensity">旋转震动强度</param>
        /// <param name="duration">持续时间</param>
        public void StartQuickShake(float positionIntensity = 0.2f, float rotationIntensity = 2.0f, float duration = 0.5f)
        {
            // 创建临时预设
            var tempPreset = ScriptableObject.CreateInstance<ShakePreset>();
            tempPreset.positionShake = new ShakePreset.ShakeSettings
            {
                noiseType = ShakePreset.NoiseType.PerlinNoise,
                amplitude = Vector3.one * positionIntensity,
                frequency = Vector3.one * 2.0f
            };
            tempPreset.rotationShake = new ShakePreset.ShakeSettings
            {
                noiseType = ShakePreset.NoiseType.PerlinNoise,
                amplitude = Vector3.one * rotationIntensity,
                frequency = Vector3.one * 1.8f
            };
            tempPreset.holdDuration = duration;

            StartShake(tempPreset);
        }

        /// <summary>
        /// 震动预设类型枚举
        /// </summary>
        public enum ShakePresetType
        {
            Light,      // 轻微震动
            Medium,     // 中等震动  
            Heavy,      // 强烈震动
            Continuous  // 持续震动
        }
    }
}