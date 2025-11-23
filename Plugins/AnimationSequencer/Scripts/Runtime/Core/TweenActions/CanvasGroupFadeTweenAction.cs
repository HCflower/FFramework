#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [Serializable]
    public sealed class CanvasGroupFadeTweenAction : FadeTweenAction
    {
        public override Type TargetComponentType => typeof(CanvasGroup);
        public override string DisplayName => "Fade (Canvas Group)";

        private CanvasGroup targetCanvasGroup;
        private float originalAlpha;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetCanvasGroup == null || targetCanvasGroup.gameObject != target)
            {
                targetCanvasGroup = target.GetComponent<CanvasGroup>();
                if (targetCanvasGroup == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalAlpha = targetCanvasGroup.alpha;

            TweenerCore<float, float, FloatOptions> tween = targetCanvasGroup.DOFade(toAlpha, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetCanvasGroup == null)
                return;

            targetCanvasGroup.alpha = originalAlpha;
        }
    }
}
#endif