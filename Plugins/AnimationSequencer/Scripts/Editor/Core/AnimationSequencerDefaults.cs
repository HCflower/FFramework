#if DOTWEEN_ENABLED
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Modified by Pablo Huaxteco
    [CreateAssetMenu(menuName = "Animation Sequencer/Create Animation Sequencer Defaults", fileName = "AnimationSequencerDefaults")]
    public sealed class AnimationSequencerDefaults : EditorDefaultResourceSingleton<AnimationSequencerDefaults>
    {
        [Header("Animation Sequencer defaults (New Instance)")]
        [SerializeField]
        private AutoplayType autoplayMode;
        public AutoplayType AutoplayMode => autoplayMode;
        
        [SerializeField]
        private bool startPaused;
        public bool StartPaused => startPaused;

        [SerializeField]
        private PlayType playType;
        public PlayType PlayType => playType;

        [SerializeField]
        private UpdateType updateType;
        public UpdateType UpdateType => updateType;

        [SerializeField]
        private bool timeScaleIndependent;
        public bool TimeScaleIndependent => timeScaleIndependent;

        [SerializeField]
        private bool dynamicStartValues;
        public bool DynamicStartValues => dynamicStartValues;

        [SerializeField]
        private int loops;
        public int Loops => loops;

        [SerializeField]
        private LoopType loopType;
        public LoopType LoopType => loopType;

        [SerializeField]
        private bool autoKill = true;
        public bool AutoKill => autoKill;

        [Header("Actions defaults (New Actions)")]
        [SerializeField]
        private AnimationDirection direction;
        public AnimationDirection Direction => direction;

        [SerializeField]
        private CustomEase ease = CustomEase.InOutQuad;
        public CustomEase Ease => ease;

        [Header("Inherit Values from Previous Actions")]
        [SerializeField]
        private bool usePreviousDirection = true;
        public bool UsePreviousDirection => usePreviousDirection;

        [SerializeField]
        private bool usePreviousEase = true;
        public bool UsePreviousEase => usePreviousEase;
    }
}
#endif