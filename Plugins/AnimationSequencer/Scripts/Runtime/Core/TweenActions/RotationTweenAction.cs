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
    public sealed class RotationTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Transform);
        public override string DisplayName => "Rotation";

        [SerializeField]
        private Vector3 toEulerAngles;
        public Vector3 ToEulerAngles
        {
            get => toEulerAngles;
            set => toEulerAngles = value;
        }

        [Tooltip("If true, the tween will use local coordinates of the object, rotating it relative to its parent's rotation. " +
            "If false, the tween will operate in world space coordinates.")]
        [SerializeField]
        private bool localSpace = true;
        public bool LocalSpace
        {
            get => localSpace;
            set => localSpace = value;
        }

        [SerializeField]
        private RotateMode rotationMode = RotateMode.Fast;
        public RotateMode RotationMode
        {
            get => rotationMode;
            set => rotationMode = value;
        }

        private Transform targetTransform;
        private Quaternion originalRotation;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            targetTransform = target.transform;

            TweenerCore<Quaternion, Vector3, QuaternionOptions> tween;
            if (localSpace)
            {
                originalRotation = targetTransform.localRotation;
                tween = targetTransform.DOLocalRotate(toEulerAngles, duration, rotationMode);
            }
            else
            {
                originalRotation = targetTransform.rotation;
                tween = targetTransform.DORotate(toEulerAngles, duration, rotationMode);
            }

            return tween;
        }

        public Vector3 GetStartValue(GameObject target)
        {
            return GetValue(target, direction == AnimationDirection.To ? AnimationDirection.From : AnimationDirection.To);
        }

        public Vector3 GetEndValue(GameObject target)
        {
            return GetValue(target, direction);
        }

        private Vector3 GetValue(GameObject target, AnimationDirection direction)
        {
            //return direction == AnimationDirection.To ? toEulerAngles : target.transform.localEulerAngles;

            if (direction == AnimationDirection.To)
                return localSpace ? toEulerAngles : ConvertToLocalEulerAngles(target, toEulerAngles);

            return target.transform.localEulerAngles;
        }

        private Vector3 ConvertToLocalEulerAngles(GameObject target, Vector3 globalEulerAngles)
        {
            Transform parentTransform = target.transform.parent;
            if (parentTransform == null)
                return globalEulerAngles;

            Quaternion globalRotation = Quaternion.Euler(globalEulerAngles);
            Quaternion localRotation = Quaternion.Inverse(parentTransform.rotation) * globalRotation;

            return localRotation.eulerAngles;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTransform == null)
                return;

            if (!localSpace)
                targetTransform.rotation = originalRotation;
            else
                targetTransform.localRotation = originalRotation;
        }
    }
}
#endif