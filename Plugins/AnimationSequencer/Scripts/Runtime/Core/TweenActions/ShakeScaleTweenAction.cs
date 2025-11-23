#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public sealed class ShakeScaleTweenAction : ShakeBaseTweenAction
    {
        public override string DisplayName => "Shake Scale";

        private Transform targetTransform;
        private Vector3 originalScale;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            targetTransform = target.transform;
            originalScale = targetTransform.localScale;

            Tweener tween = targetTransform.DOShakeScale(duration, strength, vibrato, randomness, fadeout);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTransform == null)
                return;

            targetTransform.localScale = originalScale;
        }
    }
}
#endif