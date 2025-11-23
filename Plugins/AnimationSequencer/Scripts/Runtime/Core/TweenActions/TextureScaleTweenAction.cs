#if DOTWEEN_ENABLED
using DG.Tweening;
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public abstract class TextureScaleTweenAction : TweenActionBase
    {
        public override string DisplayName => "Texture Scale";

        [SerializeField]
        protected Vector2 toScale;
        public Vector2 ToScale
        {
            get => toScale;
            set => toScale = value;
        }

        [Tooltip("Specifies the axis or combination of axes along which the animation will apply. " +
            "Use this to constrain movement to a single axis (X, Y, or Z) or a combination of them.")]
        [SerializeField]
        protected AxisConstraint axisConstraint;
        public AxisConstraint AxisConstraint
        {
            get => axisConstraint;
            set => axisConstraint = value;
        }
    }
}
#endif