using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 音频工具
    /// </summary>
    public static class AudioKit
    {
        //缓存音频Clip字典
        public static Dictionary<string, AudioClipSetting> audioClipDic = new Dictionary<string, AudioClipSetting>();
        private static AudioMixer GetAudioMixer() => AudioManager.Instance.AudioClipSettings.AudioMixer;
        private static AudioClipSettingSO GetAudioClipSettings() => AudioManager.Instance.AudioClipSettings;

        //更新音量
        public static void UpdateVolume(string audioMixerGroup, float volume)
        {
            if (GetAudioMixer() != null)
            {
                GetAudioMixer().SetFloat(audioMixerGroup, LinearToDecibels(volume));
            }
        }

        //将(0,1)转换到(-80,0)db
        private static float LinearToDecibels(float linearVolume)
        {
            if (linearVolume <= 0) return -80f;     // 静音
            return Mathf.Log10(linearVolume) * 20;  // 转换为 db
        }

        /// <summary>
        /// 重Resource文件夹加载音频
        /// resPath -> 音频路径
        /// </summary>
        private static AudioClip LoadAudioFromRes(string resPath)
        {
            return (AudioClip)Resources.Load(resPath);
        }

        /// <summary>
        /// 初始化音频
        /// </summary>
        public static void InitAudioDic()
        {
            foreach (var audioSetting in GetAudioClipSettings().AudioClipSettings)
            {
                audioClipDic.Add(audioSetting.clipName, audioSetting);
            }
        }

        ///<summary>
        /// 从Resources文件夹下加载音频缓存进入字典
        /// </summary>
        /// <param name="resPath">音频名称</param>
        /// <param name="audioName">音轨组</param>
        /// <param name="audioMixerGroup">音轨组</param>
        /// <param name="volume">音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="playOnAwake">是否在Awake时播放</param>
        public static void GetAudioFromRes(string resPath, string audioName, string audioMixerGroup, float volume = 1f, bool loop = false, bool playOnAwake = false)
        {
            if (!audioClipDic.ContainsKey(audioName))
            {
                //初始化数据
                AudioClipSetting audioClipSetting = new AudioClipSetting();
                audioClipSetting.clipName = audioName;
                audioClipSetting.clip = LoadAudioFromRes(resPath);
                audioClipSetting.volume = volume;
                audioClipSetting.loop = loop;
                audioClipSetting.playOnAwake = playOnAwake;
                audioClipSetting.audioMixerGroup = GetAudioMixer().FindMatchingGroups(audioMixerGroup)[1];
                //缓存进入字典
                audioClipDic.Add(audioName, audioClipSetting);
            }
        }

        ///<summary>
        /// 加载音频资源缓存进入字典
        /// </summary>
        /// <param name="audioClip">音频资源</param>
        /// <param name="audioName">音轨组</param>
        /// <param name="audioMixerGroup">音轨组</param>
        /// <param name="volume">音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="playOnAwake">是否在Awake时播放</param>
        public static void GetAudioFromAssets(AudioClip audioClip, string audioName, string audioMixerGroup, float volume = 1f, bool loop = false, bool playOnAwake = false)
        {
            if (!audioClipDic.ContainsKey(audioName))
            {
                //初始化数据
                AudioClipSetting audioClipSetting = new AudioClipSetting();
                audioClipSetting.clipName = audioName;
                audioClipSetting.clip = audioClip;
                audioClipSetting.volume = volume;
                audioClipSetting.loop = loop;
                audioClipSetting.playOnAwake = playOnAwake;
                audioClipSetting.audioMixerGroup = GetAudioMixer().FindMatchingGroups(audioMixerGroup)[1];
                //缓存进入字典
                audioClipDic.Add(audioName, audioClipSetting);
            }
        }

        //初始化音频数据
        private static void InitAudioClipSetting(AudioSource audioSource, string audioName)
        {
            if (!audioClipDic.ContainsKey(audioName))
            {
                Debug.Log($"<color=red>{audioName} is non existent</color>");
                return;
            }

            var audioData = audioClipDic[audioName];
            audioSource.clip = audioData.clip;
            audioSource.outputAudioMixerGroup = audioData.audioMixerGroup;
            audioSource.volume = audioData.volume;
            audioSource.loop = audioData.loop;
            audioSource.playOnAwake = audioData.playOnAwake;
        }

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="audioSource">播放音频组件</param>
        /// <param name="audioName">音频名称</param>
        /// <param name="is3D">是否是3D空间</param>  
        public static void PlayAudio(AudioSource audioSource, string audioName, bool isWait = false, bool is3D = false)
        {
            InitAudioClipSetting(audioSource, audioName);
            audioSource.spatialBlend = is3D ? 1 : 0;
            if (isWait && !audioSource.isPlaying) audioSource.Play();
            else audioSource.Play();
        }

        /// <summary>
        /// 播放单个音频
        /// </summary>
        /// <param name="audioSource">播放音频组件</param>
        /// <param name="audioName">音频名称</param>
        /// <param name="is3D">是否是3D空间</param>    
        public static void PlayAudioOneShot(AudioSource audioSource, string audioName, bool is3D = false)
        {
            InitAudioClipSetting(audioSource, audioName);
            audioSource.spatialBlend = is3D ? 1 : 0;
            audioSource.PlayOneShot(audioSource.clip);
        }

        /// <summary>
        /// 暂停音频
        /// </summary>
        /// <param name="audioSource">播放音频组件</param>
        public static void StopAudio(AudioSource audioSource)
        {
            if (audioSource != null)
            {
                if (audioSource.isPlaying) audioSource.Stop();
            }
        }
    }
}