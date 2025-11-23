#if DOTWEEN_ENABLED
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public abstract class ShakeBaseTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Transform);
        public override string[] ExcludedFields => new string[] { "direction", "ease", "relative" };

        [Tooltip("Defines the shake strength on each axis.")]
        [SerializeField]
        protected Vector3 strength = Vector3.one;
        public Vector3 Strength
        {
            get => strength;
            set => strength = value;
        }

        [Tooltip("Indicates how much will the shake vibrate.")]
        [SerializeField]
        protected int vibrato = 10;
        public int Vibrato
        {
            get => vibrato;
            set => vibrato = value;
        }

        [Tooltip("Specifies the degree of randomness for the shake (0 to 180). Values over 90 may reduce quality. Set to 0 for a single directional shake.")]
        [SerializeField]
        protected float randomness = 90;
        public float Randomness
        {
            get => randomness;
            set => randomness = value;
        }

        [Tooltip("If true, the shake will fade out smoothly by the end of the tween; if false, it maintains full intensity.")]
        [SerializeField]
        protected bool fadeout = true;
        public bool Fadeout
        {
            get => fadeout;
            set => fadeout = value;
        }
    }
}
#endif