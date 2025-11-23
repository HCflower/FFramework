#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [Serializable]
    public sealed class PlayParticleSystemStep : AnimationStepBase
    {
        public override string DisplayName => "Play Particles";

        [SerializeField]
        private ParticleSystem target;
        public ParticleSystem TargetParticleSystem
        {
            get => target;
            set => target = value;
        }

        [SerializeField]
        private bool toPlayParticles = true;
        public bool ToPlayParticles
        {
            get => toPlayParticles;
            set => toPlayParticles = value;
        }

        private bool originalIsEmitting;

        public override Sequence GenerateTweenSequence()
        {
            if (TargetParticleSystem == null)
            {
                Debug.LogWarning($"The <b>\"{DisplayName}\"</b> Step does not have a <b>\"Target\"</b>. Please consider assigning a <b>\"Target\"</b> or removing the step.");
                return null;
            }

            originalIsEmitting = TargetParticleSystem.isEmitting;

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delay);

            float duration = GetExtraInterval();
            var tween = DOTween.To(() => TargetParticleSystem.isEmitting ? 1f : 0f, x =>
            {
                if (x == 0f)
                    TargetParticleSystem.Stop();
                else if (x == 1f)
                    TargetParticleSystem.Play();
            }
            , toPlayParticles ? 1f : 0f, duration);

            sequence.Append(tween);

            return sequence;
        }

        private float GetExtraInterval()
        {
            return extraInterval;
        }

        protected override void ResetToInitialState_Internal() 
        {
            if (TargetParticleSystem == null)
                return;

            if (originalIsEmitting)
                TargetParticleSystem.Play();
            else
                TargetParticleSystem.Stop();
        }

        public override string GetDisplayNameForEditor(int index)
        {
            string display = "NULL";
            if (TargetParticleSystem != null)
                display = TargetParticleSystem.name;

            return $"{index}. Play \"{display}\" Particles";
        }

        public override float GetDuration()
        {
            return createdSequence == null ? -1 : createdSequence.Duration() - GetExtraInterval();
        }

        public override float GetExtraIntervalAdded()
        {
            return createdSequence == null ? 0 : GetExtraInterval();
        }
    }
}
#endif