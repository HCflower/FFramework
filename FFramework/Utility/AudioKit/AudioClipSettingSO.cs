using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 音频数据SO类
    /// </summary>
    [CreateAssetMenu(fileName = "AduioDataSO", menuName = "Aduio/AduioData", order = 0)]
    public class AudioClipSettingSO : ScriptableObject
    {
        [Header("音频控制")][Tooltip("总音量")][Range(0, 1)] public float volume = 0.8f;
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
                AudioKit.UpdateVolume("Main", Volume);
            }
        }
        [Tooltip("BGM音量")][Range(0, 1)] public float bgmVolume = 0.8f;
        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = value;
                AudioKit.UpdateVolume("BGM", BGMVolume);
            }
        }
        [Tooltip("SFX音量")][Range(0, 1)] public float sfxVolume = 0.8f;
        public float SFXVolume
        {
            get => sfxVolume; set
            {
                sfxVolume = value;
                AudioKit.UpdateVolume("SFX", SFXVolume);
            }
        }
        [Header("音频组件")][Tooltip("音频混音器")] public AudioMixer AudioMixer;
        public List<AudioClipSetting> AudioClipSettings = new List<AudioClipSetting>();
    }
}