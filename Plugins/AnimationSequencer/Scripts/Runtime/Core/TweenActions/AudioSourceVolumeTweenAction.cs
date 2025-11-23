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
    public sealed class AudioSourceVolumeTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(AudioSource);
        public override string DisplayName => "Volume";

        [SerializeField, Range(0, 1)]
        private float toVolume;
        public float ToVolume
        {
            get => toVolume;
            set => toVolume = Mathf.Clamp01(value);
        }

        private AudioSource targetAudioSource;
        private float originalVolume;

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

            originalVolume = targetAudioSource.volume;

            TweenerCore<float, float, FloatOptions> tween = targetAudioSource.DOFade(toVolume, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetAudioSource == null)
                return;

            targetAudioSource.volume = originalVolume;
        }
    }
}
#endif