#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [Serializable]
    public sealed class ImageFillAmountTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Image);
        public override string DisplayName => "Fill Amount";

        [SerializeField, Range(0, 1)]
        private float toFillAmount;
        public float ToFillAmount
        {
            get => toFillAmount;
            set => toFillAmount = Mathf.Clamp01(value);
        }

        private Image targetImage;
        private float originalFillAmount;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetImage == null || targetImage.gameObject != target)
            {
                targetImage = target.GetComponent<Image>();
                if (targetImage == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have an <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning an <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            if (targetImage.type != Image.Type.Filled)
            {
                Debug.Log($"The <b>\"{target.name}\"</b> GameObject with an <b>{TargetComponentType.Name}</b> component must be of type \"Filled\" to work with <b>\"{DisplayName}\"</b> action.", target);
                return null;
            }

            originalFillAmount = targetImage.fillAmount;

            TweenerCore<float, float, FloatOptions> tween = targetImage.DOFillAmount(toFillAmount, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetImage == null)
                return;

            targetImage.fillAmount = originalFillAmount;
        }
    }
}
#endif