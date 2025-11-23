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
    public sealed class LightIntensityTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Light);
        public override string DisplayName => "Intensity";

        [SerializeField, Min(0)]
        private float toIntensity;
        public float ToIntensity
        {
            get => toIntensity;
            set => toIntensity = Mathf.Clamp(value, 0, Mathf.Infinity);
        }

        private Light targetLight;
        private float originalIntensity;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetLight == null || targetLight.gameObject != target)
            {
                targetLight = target.GetComponent<Light>();
                if (targetLight == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalIntensity = targetLight.intensity;

            TweenerCore<float, float, FloatOptions> tween = targetLight.DOIntensity(toIntensity, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetLight == null)
                return;

            targetLight.intensity = originalIntensity;
        }
    }
}
#endif