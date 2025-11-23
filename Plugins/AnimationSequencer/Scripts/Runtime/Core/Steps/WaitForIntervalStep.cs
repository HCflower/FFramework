#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [Serializable]
    public sealed class WaitForIntervalStep : AnimationStepBase
    {
        public override string DisplayName => "Wait for Interval";

        [SerializeField, Min(0)]
        private float interval;
        public float Interval
        {
            get => interval;
            set => interval = Mathf.Clamp(value, 0, Mathf.Infinity);
        }

        private float duration;

        public override Sequence GenerateTweenSequence()
        {
            duration = delay + interval;
            if (duration == 0)
            {
                Debug.LogWarning($"The duration of the <b>\"{DisplayName}\"</b> Step is <b>\"Zero\"</b>. Please consider assigning a <b>\"Greater\"</b> value or removing the step.");
                return null;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delay);
            sequence.AppendInterval(interval);

            return sequence;
        }

        protected override void ResetToInitialState_Internal() { }

        public override string GetDisplayNameForEditor(int index)
        {
            float seconds = delay + interval;
            return $"{index}. Wait {seconds} second{(seconds != 1 ? "s" : "")}";
        }

        public override float GetDuration()
        {
            //Manual calculation is performed here due to a "sequence.Duration()" error when called in the "Backwards" direction, as it always returns zero.
            return createdSequence == null ? -1 : duration;
        }
    }
}
#endif