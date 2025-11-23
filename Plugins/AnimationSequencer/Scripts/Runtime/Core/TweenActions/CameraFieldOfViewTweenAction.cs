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
    public sealed class CameraFieldOfViewTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Camera);
        public override string DisplayName => "Field Of View";

        [SerializeField, Range(0, 179)]
        private float toFieldOfView = 120f;
        public float ToFieldOfView
        {
            get => toFieldOfView;
            set => toFieldOfView = Mathf.Clamp(value, 0, 179);
        }

        private Camera targetCamera;
        private float originalFieldOfView;

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

            if (targetCamera.orthographic)
            {
                Debug.Log($"The <b>\"{target.name}\"</b> GameObject with a <b>{TargetComponentType.Name}</b> component must be of type \"Perspective\" projection to work with <b>\"{DisplayName}\"</b> action.", target);
                return null;
            }

            originalFieldOfView = targetCamera.fieldOfView;

            TweenerCore<float, float, FloatOptions> tween = targetCamera.DOFieldOfView(toFieldOfView, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetCamera == null)
                return;

            targetCamera.fieldOfView = originalFieldOfView;
        }
    }
}
#endif