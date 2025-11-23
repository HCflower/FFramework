#if DOTWEEN_ENABLED
using DG.Tweening;
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public sealed class RT_PathTweenAction : PathTweenAction
    {
        public override Type TargetComponentType => typeof(RectTransform);

        private RectTransform targetRectTransform;
        private RectTransform rootCanvasRectTransform;
        private RectTransform RootCanvasRectTransform
        {
            get
            {
                if (rootCanvasRectTransform == null)
                    rootCanvasRectTransform = targetRectTransform.GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();

                return rootCanvasRectTransform;
            }
        }

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            if (targetRectTransform == null || targetRectTransform.gameObject != target)
            {
                targetRectTransform = target.transform as RectTransform;

                if (targetRectTransform == null)
                {
                    Debug.LogWarning($"The <b>\"{target.name}\"</b> GameObject does not have a <b>{TargetComponentType.Name}</b> component required  for " +
                        $"the <b>\"{DisplayName}\"</b> action. Please consider assigning a <b>{TargetComponentType.Name}</b> component or removing the action.", target);
                    return null;
                }
            }

            return base.GenerateTween_Internal(target, duration);
        }

        protected override Vector3[] GetPositionsFromVectorInput()
        {
            Vector3[] targetsWorldPosition = new Vector3[Positions.Length];
            
            for (int i = 0; i < Positions.Length; i++)
            {
                Vector2 pos = Positions[i];

                if (LocalSpace)
                    targetsWorldPosition[i] = pos;
                else
                    targetsWorldPosition[i] = RootCanvasRectTransform.TransformPoint(pos);
            }

            return targetsWorldPosition;
        }
    }
}
#endif