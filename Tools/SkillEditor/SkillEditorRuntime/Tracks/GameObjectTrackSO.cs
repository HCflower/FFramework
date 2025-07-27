using System.Collections.Generic;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 游戏物体轨道ScriptableObject
    /// 独立的游戏物体轨道数据文件
    /// </summary>
    [CreateAssetMenu(fileName = "GameObjectTrack", menuName = "FFramework/Tracks/GameObject Track", order = 8)]
    public class GameObjectTrackSO : ScriptableObject
    {
        [Header("轨道基础信息")]
        [Tooltip("轨道名称")] public string trackName = "GameObject Track";
        [Tooltip("是否启用轨道")] public bool isEnabled = true;
        [Tooltip("轨道优先级，数值越大优先级越高")] public int trackIndex;

        [Header("游戏物体片段列表")]
        public List<GameObjectTrack.GameObjectClip> gameObjectClips = new List<GameObjectTrack.GameObjectClip>();

        /// <summary>
        /// 获取轨道持续时间
        /// </summary>
        public float GetTrackDuration(float frameRate)
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
        public bool ValidateTrack()
        {
            if (string.IsNullOrEmpty(trackName)) return false;

            foreach (var clip in gameObjectClips)
            {
                if (!clip.ValidateClip()) return false;
            }
            return true;
        }

        /// <summary>
        /// 转换为运行时轨道数据
        /// </summary>
        public GameObjectTrack ToRuntimeTrack()
        {
            var track = new GameObjectTrack
            {
                trackName = this.trackName,
                isEnabled = this.isEnabled,
                trackIndex = this.trackIndex,
                gameObjectClips = new List<GameObjectTrack.GameObjectClip>(this.gameObjectClips)
            };
            return track;
        }

        /// <summary>
        /// 从运行时轨道数据同步
        /// </summary>
        public void FromRuntimeTrack(GameObjectTrack track)
        {
            this.trackName = track.trackName;
            this.isEnabled = track.isEnabled;
            this.trackIndex = track.trackIndex;
            this.gameObjectClips = new List<GameObjectTrack.GameObjectClip>(track.gameObjectClips);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(trackName))
                trackName = "GameObject Track";
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
        }
    }
}
