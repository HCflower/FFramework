#if DOTWEEN_ENABLED
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public sealed class LookAt2DTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Transform);
        public override string[] ExcludedFields => new string[] { "relative" };
        public override string DisplayName => "LookAt 2D";

        [SerializeField]
        private DataInputType toInputType = DataInputType.Object;
        public DataInputType ToInputType
        {
            get => toInputType;
            set => toInputType = value;
        }

        [Tooltip("Position to point towards.")]
        [ShowIf("toInputType", DataInputType.Vector)]
        [SerializeField]
        private Vector2 toPosition;
        public Vector2 ToPosition
        {
            get => toPosition;
            set => toPosition = value;
        }

        [Tooltip("Object to point towards.")]
        [ShowIf("toInputType", DataInputType.Object)]
        [SerializeField]
        private Transform toTarget;
        public Transform ToTarget
        {
            get => toTarget;
            set => toTarget = value;
        }

        private Transform targetTransform;
        private Quaternion originalRotation;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            targetTransform = target.transform;

            if (toInputType == DataInputType.Object && this.toTarget == null)
            {
                Debug.LogWarning($"The <b>\"{DisplayName}\"</b> Action does not have a <b>\"Target\"</b>. Please consider assigning a <b>\"Target\"</b>, " +
                    $"selecting another <b>\"Input Type\"</b> or removing the action.");
                return null;
            }

            originalRotation = targetTransform.rotation;
            TweenerCore<Quaternion, Vector3, QuaternionOptions> tween = targetTransform.DORotate(CalculateRotation(), duration);

            return tween;
        }

        private Vector3 CalculateRotation()
        {
            // Calculates the direction towards the target.
            Vector2 direction = GetPosition() - (Vector2)targetTransform.position;

            // Gets the angle in radians and converts it to degrees.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Applies the rotation on the Z axis (since it's a 2D space).
            return new Vector3(0, 0, angle);
        }

        private Vector2 GetPosition()
        {
            switch (toInputType)
            {
                case DataInputType.Vector:
                    return toPosition;
                case DataInputType.Object:
                    return toTarget.position;
            }

            return Vector3.zero;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTransform == null)
                return;

            targetTransform.rotation = originalRotation;
        }
    }
}
#endif