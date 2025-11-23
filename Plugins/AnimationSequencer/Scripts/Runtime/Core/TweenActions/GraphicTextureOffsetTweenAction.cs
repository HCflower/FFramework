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
    public sealed class GraphicTextureOffsetTweenAction : TextureOffsetTweenAction
    {
        public override Type TargetComponentType => typeof(Graphic);

        private Graphic targetGraphic;
        private Vector2 originalTextureOffset;

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

                //Create a clon of the current material (UI only).
                if (Application.isPlaying)
                    targetGraphic.material = UnityEngine.Object.Instantiate(targetGraphic.material);
            }

            if (Application.isPlaying)
                originalTextureOffset = targetGraphic.material.mainTextureOffset;

            TweenerCore<Vector2, Vector2, VectorOptions> tween = null;
            if (Application.isPlaying)
            {
                tween = targetGraphic.material.DOOffset(toOffset, duration);
            }
            else
            {
                Vector2 myVector = Vector2.zero;
                tween = DOTween.To(() => myVector, x => myVector = x, Vector2.one, duration);
            }
            tween.SetOptions(axisConstraint);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetGraphic == null || !Application.isPlaying)
                return;

            targetGraphic.material.mainTextureOffset = originalTextureOffset;
        }
    }
}
#endif