#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public class BlockRaycastsStep : AnimationStepBase
    {
        public override string DisplayName => "Block Raycasts";

        [SerializeField]
        private CanvasGroup target;
        public CanvasGroup TargetCanvasGroup
        {
            get => target;
            set => target = value;
        }

        [SerializeField]
        private bool toBlockRaycasts;
        public bool ToBlockRaycasts
        {
            get => toBlockRaycasts;
            set => toBlockRaycasts = value;
        }

        private bool originalBlocksRaycasts;

        public override Sequence GenerateTweenSequence()
        {
            if (TargetCanvasGroup == null)
            {
                Debug.LogWarning($"The <b>\"{DisplayName}\"</b> Step does not have a <b>\"Target\"</b>. Please consider assigning a <b>\"Target\"</b> or removing the step.");
                return null;
            }

            originalBlocksRaycasts = TargetCanvasGroup.blocksRaycasts;

            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(delay);

            float duration = GetExtraInterval();
            var tween = DOTween.To(() => TargetCanvasGroup.blocksRaycasts ? 1f : 0f, x =>
            {
                if (x == 0f)
                    TargetCanvasGroup.blocksRaycasts = false;
                else if (x == 1f)
                    TargetCanvasGroup.blocksRaycasts = true;
            }
            , toBlockRaycasts ? 1f : 0f, duration);

            sequence.Append(tween);

            return sequence;
        }

        private float GetExtraInterval()
        {
            return extraInterval;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (TargetCanvasGroup == null)
                return;

            TargetCanvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }

        public override string GetDisplayNameForEditor(int index)
        {
            string display = "NULL";
            if (TargetCanvasGroup != null)
                display = TargetCanvasGroup.name;

            return $"{index}. Block \"{display}\" Raycasts: {toBlockRaycasts}";
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