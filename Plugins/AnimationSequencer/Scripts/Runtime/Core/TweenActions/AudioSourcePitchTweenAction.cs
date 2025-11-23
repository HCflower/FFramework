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
    public sealed class AudioSourcePitchTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(AudioSource);
        public override string DisplayName => "Pitch";

        [SerializeField, Range(-3f, 3f)]
        private float toPitch = 1.5f;
        public float ToPitch
        {
            get => toPitch;
            set => toPitch = Mathf.Clamp(value, -3f, 3f);
        }

        private AudioSource targetAudioSource;
        private float originalPitch;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetAudioSource == null || targetAudioSource.gameObject != target)
            {
                targetAudioSource = target.GetComponent<AudioSource>();
                if (targetAudioSource == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have an <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning an <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalPitch = targetAudioSource.pitch;

            TweenerCore<float, float, FloatOptions> tween = targetAudioSource.DOPitch(toPitch, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetAudioSource == null)
                return;

            targetAudioSource.pitch = originalPitch;
        }
    }
}
#endif