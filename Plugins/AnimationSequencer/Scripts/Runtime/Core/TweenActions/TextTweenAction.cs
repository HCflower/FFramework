#if DOTWEEN_ENABLED
using DG.Tweening;
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public abstract class TextTweenAction : TweenActionBase
    {
        [SerializeField]
        protected string toText;
        public string ToText
        {
            get => toText;
            set => toText = value;
        }

        [SerializeField]
        protected bool richText;
        public bool RichText
        {
            get => richText;
            set => richText = value;
        }

        [SerializeField]
        protected ScrambleMode scrambleMode = ScrambleMode.None;
        public ScrambleMode ScrambleMode
        {
            get => scrambleMode;
            set => scrambleMode = value;
        }
    }
}
#endif