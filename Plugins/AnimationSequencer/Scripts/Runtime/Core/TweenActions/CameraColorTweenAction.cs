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
    public sealed class CameraColorTweenAction : ColorTweenAction
    {
        public override Type TargetComponentType => typeof(Camera);

        private Camera targetCamera;
        private Color originalColor;

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

            originalColor = targetCamera.backgroundColor;

            TweenerCore<Color, Color, ColorOptions> tween = targetCamera.DOColor(toColor, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetCamera == null)
                return;

            targetCamera.backgroundColor = originalColor;
        }
    }
}
#endif