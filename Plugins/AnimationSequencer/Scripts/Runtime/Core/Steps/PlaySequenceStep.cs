#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [Serializable]
    public sealed class PlaySequenceStep : AnimationStepBase
    {
        public override string DisplayName => "Play Sequence";

        [SerializeField]
        private AnimationSequencer target;
        public AnimationSequencer TargetAnimSequencer
        {
            get => target;
            set => target = value;
        }

        public override Sequence GenerateTweenSequence()
        {
            if (TargetAnimSequencer == null)
            {
                Debug.LogWarning($"The <b>\"{DisplayName}\"</b> Step does not have a <b>\"Target\"</b>. Please consider assigning a <b>\"Target\"</b> or removing the step.");
                return null;
            }

            if (!TargetAnimSequencer.IsActiveAndEnabled)
                return null;

            //Sequence sequence = TargetAnimSequencer.GenerateSequence();
            TargetAnimSequencer.PlayForward(true);
            TargetAnimSequencer.PlayingSequence.Pause();
            Sequence sequence = TargetAnimSequencer.PlayingSequence;
            sequence.SetDelay(delay);

            return sequence;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (TargetAnimSequencer == null)
                return;

            TargetAnimSequencer.ResetToInitialState();
        }

        public override string GetDisplayNameForEditor(int index)
        {
            string display = "NULL";
            if (TargetAnimSequencer != null)
                display = TargetAnimSequencer.name;

            return $"{index}. Play \"{display}\" Sequence";
        }

        public override float GetDuration()
        {
            return createdSequence == null ? -1 : createdSequence.Duration() - TargetAnimSequencer.ExtraIntervalAdded;
        }

        public override float GetExtraIntervalAdded()
        {
            return createdSequence == null ? 0 : TargetAnimSequencer.ExtraIntervalAdded;
        }
    }
}
#endif