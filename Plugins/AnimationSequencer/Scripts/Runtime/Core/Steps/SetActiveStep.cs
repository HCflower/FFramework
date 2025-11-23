#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [Serializable]
    public sealed class SetActiveStep : AnimationStepBase
    {
        public override string DisplayName => "Set Active";

        [SerializeField]
        private GameObject target;
        public GameObject Target
        {
            get => target;
            set => target = value;
        }

        [SerializeField]
        private bool toSetActive;
        public bool ToSetActive
        {
            get => toSetActive;
            set => toSetActive = value;
        }

        private bool originalActiveSelf;

        public override Sequence GenerateTweenSequence()
        {
            if (target == null)
            {
                Debug.LogWarning($"The <b>\"{DisplayName}\"</b> Step does not have a <b>\"Target\"</b>. Please consider assigning a <b>\"Target\"</b> or removing the step.");
                return null;
            }

            originalActiveSelf = target.activeSelf;

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delay);

            float duration = GetExtraInterval();
            var tween = DOTween.To(() => target.activeSelf ? 1f : 0f, x =>
            {
                if (x == 0f)
                    target.SetActive(false);
                else if (x == 1f)
                    target.SetActive(true);
            }
            , toSetActive ? 1f : 0f, duration);

            sequence.Append(tween);

            return sequence;
        }

        private float GetExtraInterval()
        {
            return extraInterval;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (target == null)
                return;

            target.SetActive(originalActiveSelf);
        }

        public override string GetDisplayNameForEditor(int index)
        {
            string display = "NULL";
            if (target != null)
                display = target.name;
            
            return $"{index}. Set \"{display}\" Active: {toSetActive}";
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