using UnityEngine;

namespace SkillEditor
{
    public class AudioTrackItemData : BaseTrackItemData
    {
        public AudioClip audioClip;             // 音频片段
        public float volume = 1f;               // 音量
        public float pitch;                     // 音调
        public bool isLoop;                     // 是否循环播放
    }
}
