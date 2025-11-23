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
    public sealed class SpriteRendererColorTweenAction : ColorTweenAction
    {
        public override Type TargetComponentType => typeof(SpriteRenderer);

        private SpriteRenderer targetSpriteRenderer;
        private Color originalColor;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetSpriteRenderer == null || targetSpriteRenderer.gameObject != target)
            {
                targetSpriteRenderer = target.GetComponent<SpriteRenderer>();
                if (targetSpriteRenderer == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalColor = targetSpriteRenderer.color;

            TweenerCore<Color, Color, ColorOptions> tween = targetSpriteRenderer.DOColor(toColor, duration);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetSpriteRenderer == null)
                return;

            targetSpriteRenderer.color = originalColor;
        }
    }
}
#endif