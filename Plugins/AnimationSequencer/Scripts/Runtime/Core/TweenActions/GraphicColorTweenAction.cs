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
    public sealed class GraphicColorTweenAction : ColorTweenAction
    {
        public override Type TargetComponentType => typeof(Graphic);

        private Graphic targetGraphic;
        private Color originalColor;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetGraphic == null || targetGraphic.gameObject != target)
            {
                targetGraphic = target.GetComponent<Graphic>();
                if (targetGraphic == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalColor = targetGraphic.color;

            TweenerCore<Color, Color, ColorOptions> tween = targetGraphic.DOColor(toColor, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetGraphic == null)
                return;

            targetGraphic.color = originalColor;
        }
    }
}
#endif