#if DOTWEEN_ENABLED
using System;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public abstract class PunchBaseTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Transform);
        public override string[] ExcludedFields => new string[] { "direction", "ease", "relative" };

        [Tooltip("The punch strength (added to the Transform's current value).")]
        [SerializeField]
        protected Vector3 punch = Vector3.one;
        public Vector3 Punch
        {
            get => punch;
            set => punch = value;
        }

        [Tooltip("Indicates how much will the punch vibrate.")]
        [SerializeField]
        protected int vibrato = 10;
        public int Vibrato
        {
            get => vibrato;
            set => vibrato = value;
        }

        [Tooltip("Represents how much (0 to 1) the vector will go beyond the starting value when bouncing backwards. " +
            "1 creates full oscillation (dramatic effect), 0 keeps it between punch and start values (gentler movement).")]
        [SerializeField, Range(0f, 1f)]
        protected float elasticity = 1f;
        public float Elasticity
        {
            get => elasticity;
            set => elasticity = value;
        }
    }
}
#endif