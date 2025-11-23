#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public sealed class CameraOrthographicSizeTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Camera);
        public override string DisplayName => "Orthographic Size";

        [SerializeField]
        private float toOrthographicSize = 10f;
        public float ToOrthographicSize
        {
            get => toOrthographicSize;
            set => toOrthographicSize = value;
        }

        [SerializeField]
        [Tooltip("Enable this to interpret the input value as a percentage. Examples: 50% scales the size to half, 100% keeps it unchanged, and 200% doubles it.")]
        private bool toPercentageMode;
        public bool ToPercentageMode
        {
            get => toPercentageMode;
            set => toPercentageMode = value;
        }

        private Camera targetCamera;
        private float originalOrthographicSize;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetCamera == null || targetCamera.gameObject != target)
            {
                targetCamera = target.GetComponent<Camera>();
                if (targetCamera == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            if (!targetCamera.orthographic)
            {
                Debug.Log($"The <b>\"{target.name}\"</b> GameObject with a <b>{TargetComponentType.Name}</b> component must be of type \"Orthographic\" projection to work with <b>\"{DisplayName}\"</b> action.", target);
                return null;
            }

            originalOrthographicSize = targetCamera.orthographicSize;

            float endValue = toPercentageMode ? originalOrthographicSize * (toOrthographicSize / 100) : toOrthographicSize;
            TweenerCore<float, float, FloatOptions> tween = targetCamera.DOOrthoSize(endValue, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetCamera == null)
                return;

            targetCamera.orthographicSize = originalOrthographicSize;
        }
    }
}
#endif