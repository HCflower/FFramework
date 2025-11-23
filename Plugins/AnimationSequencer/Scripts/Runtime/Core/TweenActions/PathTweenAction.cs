#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    // Created by Pablo Huaxteco
    [Serializable]
    public class PathTweenAction : TweenActionBase
    {
        public override Type TargetComponentType => typeof(Transform);
        public override string DisplayName => "Path";

        [SerializeField]
        private DataInputType inputType = DataInputType.Object;
        public DataInputType InputType
        {
            get => inputType;
            set => inputType = value;
        }

        [SerializeField]
        private Vector3[] positions;
        public Vector3[] Positions
        {
            get => positions;
            set => positions = value;
        }

        [Tooltip("If true, the tween will use local coordinates of the object, moving it relative to its parent's position and rotation. " +
            "If false, the tween will operate in world space coordinates.")]
        [ShowIf("inputType == DataInputType.Vector")]
        [SerializeField]
        private bool localSpace = true;
        public bool LocalSpace
        {
            get => localSpace;
            set => localSpace = value;
        }

        [SerializeField]
        private Transform[] targets;
        public Transform[] Targets
        {
            get => targets;
            set => targets = value;
        }

        [SerializeField]
        private Color gizmoColor;
        public Color GizmoColor
        {
            get => gizmoColor;
            set => gizmoColor = value;
        }

        [Tooltip("Higher values create smoother curves but are more performance-intensive. Default is 10; 5 works well for gentle curves.")]
        [SerializeField]
        private int resolution = 10;
        public int Resolution
        {
            get => resolution;
            set => resolution = value;
        }

        [SerializeField]
        private PathMode pathMode = PathMode.Full3D;
        public PathMode PathMode
        {
            get => pathMode;
            set => pathMode = value;
        }

        [SerializeField]
        private PathType pathType = PathType.CatmullRom;
        public PathType PathType
        {
            get => pathType;
            set => pathType = value;
        }

        [SerializeField]
        private bool closePath;
        public bool ClosePath
        {
            get => closePath;
            set => closePath = value;
        }

        protected Transform targetTransform;
        private Vector3 originalPosition;

        protected override Tweener GenerateTween_Internal(GameObject target, float duration)
        {
            targetTransform = target.transform;

            if ((inputType == DataInputType.Vector && positions.Length == 0) || (inputType == DataInputType.Object && targets.Length == 0))
            {
                Debug.LogWarning($"The <b>\"{DisplayName}\"</b> Action does not have <b>\"Targets\"</b>. Please consider assigning <b>\"Targets\"</b> or removing the action.");
                return null;
            }

            TweenerCore<Vector3, Path, PathOptions> tween;
            if (inputType == DataInputType.Vector && localSpace)
            {
                originalPosition = targetTransform.localPosition;
                tween = targetTransform.DOLocalPath(GetPositions(), duration, pathType, pathMode, resolution, gizmoColor);
            }
            else
            {
                originalPosition = targetTransform.position;
                tween = targetTransform.DOPath(GetPositions(), duration, pathType, pathMode, resolution, gizmoColor);
            }
            tween.SetOptions(closePath);

            return tween;
        }

        private Vector3[] GetPositions()
        {
            switch (inputType)
            {
                case DataInputType.Vector:
                    return GetPositionsFromVectorInput();
                case DataInputType.Object:
                    return GetPositionsFromObjectInput();
            }

            return null;
        }

        protected virtual Vector3[] GetPositionsFromVectorInput()
        {
            return positions;
        }

        private Vector3[] GetPositionsFromObjectInput()
        {
            Vector3[] result = new Vector3[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                Transform pointTransform = targets[i];
                result[i] = pointTransform.position;
            }

            return result;
        }

        protected override void ResetToInitialState_Internal()
        {
            if (targetTransform == null)
                return;

            if (localSpace)
                targetTransform.localPosition = originalPosition;
            else
                targetTransform.position = originalPosition;
        }
    }
}
#endif