using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 游戏物体轨道集合ScriptableObject
    /// 存储所有游戏物体轨道数据的文件
    /// </summary>
    // [CreateAssetMenu(fileName = "GameObjectTracks", menuName = "FFramework/Tracks/GameObject Tracks", order = 8)]
    public class GameObjectTrackSO : ScriptableObject
    {
        [Header("游戏物体轨道列表 (多轨道并行)")]
        [Tooltip("所有游戏物体轨道数据列表")]
        public List<GameObjectTrack> gameObjectTracks = new List<GameObjectTrack>();

        /// <summary>
        /// 获取所有轨道的最大持续时间
        /// </summary>
        public float GetMaxTrackDuration(float frameRate)
        {
            float maxDuration = 0;
            foreach (var track in gameObjectTracks)
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
            foreach (var track in gameObjectTracks)
            {
                if (!track.ValidateTrack()) return false;
            }
            return true;
        }

        /// <summary>
        /// 添加新的游戏物体轨道
        /// </summary>
        public GameObjectTrack AddTrack(string trackName = "")
        {
            var newTrack = new GameObjectTrack();
            if (!string.IsNullOrEmpty(trackName))
                newTrack.trackName = trackName;
            else
                newTrack.trackName = $"GameObject Track {gameObjectTracks.Count}";

            newTrack.trackIndex = gameObjectTracks.Count;
            gameObjectTracks.Add(newTrack);
            return newTrack;
        }

        /// <summary>
        /// 移除指定索引的轨道
        /// </summary>
        public bool RemoveTrack(int index)
        {
            if (index >= 0 && index < gameObjectTracks.Count)
            {
                gameObjectTracks.RemoveAt(index);
                // 重新分配索引
                for (int i = 0; i < gameObjectTracks.Count; i++)
                {
                    gameObjectTracks[i].trackIndex = i;
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
            if (gameObjectTracks.Count == 0)
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
    /// 游戏物体轨道 - 支持多轨道并行
    /// </summary>
    [Serializable]
    public class GameObjectTrack : TrackBase
    {
        public List<GameObjectClip> gameObjectClips = new List<GameObjectClip>();

        public GameObjectTrack()
        {
            trackName = "GameObject Track";
        }

        public override float GetTrackDuration(float frameRate)
        {
            int maxFrame = 0;
            foreach (var clip in gameObjectClips)
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

            foreach (var clip in gameObjectClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        [Serializable]
        public class GameObjectClip : ClipBase
        {
            [Header("游戏物体设置")]
            public GameObject prefab;
            [Tooltip("是否自动销毁")] public bool autoDestroy = true;
            [Tooltip("生成位置偏移")] public Vector3 positionOffset = Vector3.zero;
            [Tooltip("生成旋转偏移")] public Vector3 rotationOffset = Vector3.zero;
            [Tooltip("生成缩放")] public Vector3 scale = Vector3.one;

            [Header("父对象设置")]
            [Tooltip("是否作为子对象")] public bool useParent = false;
            [Tooltip("父对象名称")] public string parentName = "";

            [Header("生命周期设置")]
            [Tooltip("延迟销毁时间(秒), -1表示不销毁")] public float destroyDelay = -1f;

            /// <summary>
            /// 验证游戏物体片段数据有效性
            /// </summary>
            public override bool ValidateClip()
            {
                return !string.IsNullOrEmpty(clipName) && prefab != null;
            }
        }
    }
}
