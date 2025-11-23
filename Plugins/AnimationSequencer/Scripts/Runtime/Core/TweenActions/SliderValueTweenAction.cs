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
    public sealed class SliderValueTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Slider);
        public override string DisplayName => "Value";

        [SerializeField]
        private float toValue;
        public float ToValue
        {
            get => toValue;
            set => this.toValue = value;
        }

        [Tooltip("If true, the animated position values will snap to integer values, creating a more grid-like movement. " +
            "Useful for animations that require precise, whole number positioning.")]
        [SerializeField]
        private bool snapping;
        public bool Snapping
        {
            get => snapping;
            set => snapping = value;
        }

        private Slider targetSlider;
        private float originalValue;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetSlider == null || targetSlider.gameObject != target)
            {
                targetSlider = target.GetComponent<Slider>();
                if (targetSlider == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalValue = targetSlider.value;

            TweenerCore<float, float, FloatOptions> tween = targetSlider.DOValue(toValue, duration, snapping);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetSlider == null)
                return;

            targetSlider.value = originalValue;
        }
    }
}
#endif