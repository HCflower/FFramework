#if DOTWEEN_ENABLED
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public abstract class ColorTweenAction : TweenActionBase
    {
        public override string DisplayName => "Color";

        [SerializeField]
        protected Color toColor = Color.white;
        public Color ToColor
        {
            get => toColor;
            set => toColor = value;
        }
    }
}
#endif