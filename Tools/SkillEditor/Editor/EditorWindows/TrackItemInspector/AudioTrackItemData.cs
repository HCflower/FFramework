using UnityEngine;

namespace SkillEditor
{
    public class AudioTrackItemData : BaseTrackItemData
    {
        public AudioClip audioClip;             // 音频片段
        public float volume = 1f;               // 音量
        public float pitch;                     // 音调
        public float spatialBlend;             // 空间混合
        public float reverbZoneMix;            // 反响区域混合
    }
}
