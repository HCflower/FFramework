#if DOTWEEN_ENABLED
#if TMP_ENABLED
using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Renamed by Pablo Huaxteco
    [Serializable]
    public sealed class TMP_TextTweenAction : TextTweenAction
    {
        public override Type TargetComponentType => typeof(TMP_Text);
        public override string DisplayName => "Text (TMP)";

        private TMP_Text targetTmpText;
        private string originalText;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetTmpText == null || targetTmpText.gameObject != target)
            {
                targetTmpText = target.GetComponent<TMP_Text>();
                if (targetTmpText == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            originalText = targetTmpText.text;

            TweenerCore<string, string, StringOptions> tween = targetTmpText.DOText(toText, duration, richText, scrambleMode);

            return tween;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTmpText == null)
                return;

            targetTmpText.text = originalText;
        }
    }
}
#endif
#endif