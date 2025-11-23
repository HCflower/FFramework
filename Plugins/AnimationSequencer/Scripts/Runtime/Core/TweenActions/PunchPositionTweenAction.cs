#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public sealed class PunchPositionTweenAction : PunchBaseTweenAction
    {
        public override string DisplayName => "Punch Position";

        [Tooltip("If true, the animated position values will snap to integer values, creating a more grid-like movement. " +
            "Useful for animations that require precise, whole number positioning.")]
        [SerializeField]
        private bool snapping;
        public bool Snapping
        {
            get => snapping;
            set => snapping = value;
        }

        private Transform targetTransform;
        private Vector3 originalPosition;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            targetTransform = target.transform;
            originalPosition = targetTransform.localPosition;

            Tweener tween = targetTransform.DOPunchPosition(punch, duration, vibrato, elasticity, snapping);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTransform == null)
                return;

            targetTransform.localPosition = originalPosition;
        }
    }
}
#endif