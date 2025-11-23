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
    public sealed class PositionTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Transform);
        public override string DisplayName => "Position";

        [SerializeField]
        private DataInputType toInputType;
        public DataInputType ToInputType
        {
            get => toInputType;
            set => toInputType = value;
        }

        [ShowIf("toInputType", DataInputType.Vector)]
        [SerializeField]
        private Vector3 toPosition;
        public Vector3 ToPosition
        {
            get => toPosition;
            set => toPosition = value;
        }

        [Tooltip("If TRUE, endValue is added to the object's current local or global position, depending on LocalSpace.")]
        [ShowIf("toInputType", DataInputType.Vector)]
        [SerializeField]
        private bool toRelative;
        public bool ToRelative
        {
            get => toRelative;
            set => toRelative = value;
        }

        [Tooltip("If true, the tween will use local coordinates of the object, moving it relative to its parent's position and rotation. " +
            "If false, the tween will operate in world space coordinates.")]
        [ShowIf("toInputType", DataInputType.Vector)]
        [SerializeField]
        private bool toLocalSpace = true;
        public bool ToLocalSpace
        {
            get => toLocalSpace;
            set => toLocalSpace = value;
        }

        [ShowIf("toInputType", DataInputType.Object)]
        [SerializeField]
        private Transform toTarget;
        public Transform ToTarget
        {
            get => toTarget;
            set => toTarget = value;
        }

        [ShowIf("toInputType", DataInputType.Object)]
        [SerializeField]
        private Vector3 toOffset;
        public Vector3 ToOffset
        {
            get => toOffset;
            set => toOffset = value;
        }

        [Tooltip("Specifies the axis or combination of axes along which the animation will apply. " +
            "Use this to constrain movement to a single axis (X, Y, or Z) or a combination of them.")]
        [SerializeField]
        private AxisConstraint axisConstraint;
        public AxisConstraint AxisConstraint
        {
            get => axisConstraint;
            set => axisConstraint = value;
        }

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

            if (toInputType == DataInputType.Object && this.toTarget == null)
            {
                Debug.LogWarning($"The <b>\"{DisplayName}\"</b> Action does not have a <b>\"Target\"</b>. Please consider assigning a <b>\"Target\"</b>, " +
                    $"selecting another <b>\"Input Type\"</b> or removing the action.");
                return null;
            }

            TweenerCore<Vector3, Vector3, VectorOptions> tween;
            if (toInputType == DataInputType.Vector && toLocalSpace)
            {
                originalPosition = targetTransform.localPosition;
                tween = targetTransform.DOLocalMove(GetPosition(), duration, axisConstraint, snapping);
            }
            else
            {
                originalPosition = targetTransform.position;
                tween = targetTransform.DOMove(GetPosition(), duration, axisConstraint, snapping);
            }

            return tween;
        }

        private Vector3 GetPosition()
        {
            switch (toInputType)
            {
                case DataInputType.Vector:
                    if (toRelative)
                        return toLocalSpace ? targetTransform.localPosition + toPosition : targetTransform.position + toPosition;
                    else
                        return toPosition;
                case DataInputType.Object:
                    return toTarget.position + toOffset;
            }

            return Vector3.zero;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTransform == null)
                return;

            if (toInputType == DataInputType.Vector && toLocalSpace)
                targetTransform.localPosition = originalPosition;
            else
                targetTransform.position = originalPosition;
        }
    }
}
#endif