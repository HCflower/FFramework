#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public sealed class ShakeRotationTweenAction : ShakeBaseTweenAction
    {
        public override string DisplayName => "Shake Rotation";

        public ShakeRotationTweenAction()
        {
            strength = new Vector3 (90, 90, 90);
        }

        private Transform targetTransform;
        private Quaternion originalRotation;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            targetTransform = target.transform;
            originalRotation = targetTransform.localRotation;

            Tweener tween = targetTransform.DOShakeRotation(duration, strength, vibrato, randomness, fadeout);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTransform == null)
                return;

            targetTransform.localRotation = originalRotation;
        }
    }
}
#endif