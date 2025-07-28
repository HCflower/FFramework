using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 伤害检测轨道集合ScriptableObject
    /// 存储所有伤害检测轨道数据的文件
    /// </summary>
    // [CreateAssetMenu(fileName = "InjuryDetectionTracks", menuName = "FFramework/Tracks/Injury Detection Tracks", order = 7)]
    public class InjuryDetectionTrackSO : ScriptableObject
    {
        [Header("伤害检测轨道列表 (多轨道并行)")]
        [Tooltip("所有伤害检测轨道数据列表")]
        public List<InjuryDetectionTrack> injuryDetectionTracks = new List<InjuryDetectionTrack>();

        /// <summary>
        /// 获取所有轨道的最大持续时间
        /// </summary>
        public float GetMaxTrackDuration(float frameRate)
        {
            float maxDuration = 0;
            foreach (var track in injuryDetectionTracks)
            {
                if (track.isEnabled)
                    maxDuration = Mathf.Max(maxDuration, track.GetTrackDuration(frameRate));
            }
            return maxDuration;
        }

        /// <summary>
        /// 验证所有轨道数据有效性
        /// </summary>
        public bool ValidateAllTracks()
        {
            foreach (var track in injuryDetectionTracks)
            {
                if (!track.ValidateTrack()) return false;
            }
            return true;
        }

        /// <summary>
        /// 添加新的伤害检测轨道
        /// </summary>
        public InjuryDetectionTrack AddTrack(string trackName = "")
        {
            var newTrack = new InjuryDetectionTrack();
            if (!string.IsNullOrEmpty(trackName))
                newTrack.trackName = trackName;
            else
                newTrack.trackName = $"Damage Detection Track {injuryDetectionTracks.Count}";

            newTrack.trackIndex = injuryDetectionTracks.Count;
            injuryDetectionTracks.Add(newTrack);
            return newTrack;
        }

        /// <summary>
        /// 移除指定索引的轨道
        /// </summary>
        public bool RemoveTrack(int index)
        {
            if (index >= 0 && index < injuryDetectionTracks.Count)
            {
                injuryDetectionTracks.RemoveAt(index);
                // 重新分配索引
                for (int i = 0; i < injuryDetectionTracks.Count; i++)
                {
                    injuryDetectionTracks[i].trackIndex = i;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 确保至少有一个轨道
        /// </summary>
        public void EnsureTrackExists()
        {
            if (injuryDetectionTracks.Count == 0)
            {
                AddTrack();
            }
        }

        private void OnValidate()
        {
            EnsureTrackExists();
        }
    }

    /// <summary>
    /// 伤害检测轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class InjuryDetectionTrack : TrackBase
    {
        public List<InjuryDetectionClip> injuryDetectionClips = new List<InjuryDetectionClip>();

        public InjuryDetectionTrack()
        {
            trackName = "Damage Detection";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in injuryDetectionClips)
            {
                maxFrame = Mathf.Max(maxFrame, clip.EndFrame);
            }
            return maxFrame / frameRate;
        }

        /// <summary>
        /// 验证轨道数据有效性
        /// </summary>
        public override bool ValidateTrack()
        {
            if (string.IsNullOrEmpty(trackName)) return false;

            foreach (var clip in injuryDetectionClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        [Serializable]
        public class InjuryDetectionClip : ClipBase
        {
            [Tooltip("目标层级")] public LayerMask targetLayers = -1;
            [Tooltip("是否是多段伤害检测")] public bool isMultiInjuryDetection = false;
            [Tooltip("多段伤害检测间隔"), Min(0)] public float multiInjuryDetectionInterval = 0.1f;

            [Tooltip("碰撞体类型")] public ColliderType colliderType = ColliderType.Box;
            [Header("Sector collider setting")]
            [Tooltip("扇形内圆半径"), Min(0)] public float innerCircleRadius = 0;
            [Tooltip("扇形外圆半径"), Min(1)] public float outerCircleRadius = 1;
            [Tooltip("扇形角度"), Range(0, 360)] public float sectorAngle = 0;
            [Tooltip("扇形厚度"), Min(0.1f)] public float sectorThickness = 0.1f;

            [Header("Transform")]
            [Tooltip("碰撞体位置")] public Vector3 position = Vector3.zero;
            [Tooltip("碰撞体旋转")] public Vector3 rotation = Vector3.zero;
            [Tooltip("碰撞体缩放")] public Vector3 scale = Vector3.one;

            public override int EndFrame => startFrame + Mathf.Max(1, durationFrame);

            /// <summary>
            /// 验证伤害检测片段数据有效性
            /// </summary>
            public override bool ValidateClip()
            {
                return !string.IsNullOrEmpty(clipName) && durationFrame > 0;
            }
        }
    }

    // 碰撞体类型
    public enum ColliderType
    {
        Box = 0,        // 立方体
        Sphere = 1,     // 球体
        Capsule = 2,    // 胶囊体   
        sector = 3,     // 扇形
    }

}
