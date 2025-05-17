using UnityEngine.Audio;
using UnityEngine;

namespace FFramework.Kit
{
    [System.Serializable]
    public class AudioClipSetting
    {
        [Tooltip("音频名称")] public string clipName;
        [Tooltip("音频剪辑")] public AudioClip clip;
        [Tooltip("音频分组")] public AudioMixerGroup audioMixerGroup;
        [Tooltip("音量")][Range(0, 1)] public float volume = 1f;
        [Tooltip("是否循环")] public bool loop = false;
        [Tooltip("是否在唤醒时播放")] public bool playOnAwake = false;
    }
}