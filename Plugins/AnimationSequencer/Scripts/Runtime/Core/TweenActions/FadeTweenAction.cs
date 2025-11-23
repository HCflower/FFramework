#if DOTWEEN_ENABLED
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public abstract class FadeTweenAction : TweenActionBase
    {
        public override string DisplayName => "Fade";

        [SerializeField, Range(0, 1)]
        protected float toAlpha;
        public float ToAlpha
        {
            get => toAlpha;
            set => toAlpha = Mathf.Clamp01(value);
        }
    }
}
#endif