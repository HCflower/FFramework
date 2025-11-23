#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public sealed class TextCounterTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Text);
        public override string DisplayName => "Counter";

        [SerializeField]
        private int toCounter;
        public int ToCounter
        {
            get => toCounter;
            set => toCounter = value;
        }

        [Tooltip("Enable this to add a thousands separator to the counter display (e.g., 1,000 instead of 1000).")]
        [SerializeField]
        private bool thousandsSeparator = true;
        public bool AddThousandsSeparator
        {
            get => thousandsSeparator;
            set => thousandsSeparator = value;
        }

        private Text targetText;
        private string originalText;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetText == null || targetText.gameObject != target)
            {
                targetText = target.GetComponent<Text>();
                if (targetText == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalText = targetText.text;

            int startCounter = 0;
            if (int.TryParse(targetText.text, out int result))
                startCounter = result;

            TweenerCore<int, int, NoOptions> tween = targetText.DOCounter(startCounter, toCounter, duration, thousandsSeparator);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetText == null)
                return;

            targetText.text = originalText;
        }
    }
}
#endif