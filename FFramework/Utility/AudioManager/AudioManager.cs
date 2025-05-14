using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

namespace FFramework
{
    /// <summary>
    /// 音频管理器 - 组件版
    /// TODO:添加音频音量大小过渡功能
    /// </summary>
    public class AudioManager : SingletonMono<AudioManager>
    {
        AudioManager() => IsDontDestroyOnLoad = true;

        [Header("音频控制")]
        [Tooltip("总音量")][SerializeField][Range(0, 1)] private float volume = 0.8f;
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
                UpdateVolume("Main", LinearToDecibels(Volume));
            }
        }
        [Tooltip("BGM音量")]
        [SerializeField][Range(0, 1)] private float bgmVolume = 0.8f;
        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = value;
                UpdateVolume("BGM", LinearToDecibels(BGMVolume));
            }
        }
        [Tooltip("SFX音量")]
        [SerializeField][Range(0, 1)] private float sfxVolume = 0.8f;
        public float SFXVolume
        {
            get => sfxVolume; set
            {
                sfxVolume = value;
                UpdateVolume("SFX", LinearToDecibels(SFXVolume));
            }

        }

        [Tooltip("是否静音")]
        [SerializeField] private bool isMute = false;
        public bool IsMute
        {
            get => isMute;
            set
            {
                isMute = value;
                UpdateIsMute();
            }
        }

        [Header("音频组件")]
        [SerializeField][Tooltip("音频混音器")] private AudioMixer AudioMixer;
        [SerializeField][Tooltip("BGM音频组件")] private AudioSource BGMAudioSource;
        [SerializeField][Tooltip("SFX音频组件")] private AudioSource SFXAudioSource;

        [Header("音频数据")]
        [SerializeField][Tooltip("是否使用SO文件读取音频数据")] private bool isUseSO = false;
        [SerializeField][Tooltip("音频数据SO文件")] private AduioDataSO audioDataSO;
        [SerializeField] private List<AudioData> audioDataList = new List<AudioData>();
        //缓存音频Clip字典
        private Dictionary<string, AudioData> audioSourceDic = new Dictionary<string, AudioData>();

        #region 数据更新

        protected override void Awake()
        {
            base.Awake();
            BGMAudioSource = new GameObject("BGM-AudioSource").AddComponent<AudioSource>();
            BGMAudioSource.transform.SetParent(this.transform);
            SFXAudioSource = new GameObject("SFX-AudioSource").AddComponent<AudioSource>();
            SFXAudioSource.transform.SetParent(this.transform);
            Init();
        }

        //初始化
        private void Init()
        {
            UpdateVolume("BGM", LinearToDecibels(bgmVolume));
            UpdateVolume("SFX", LinearToDecibels(sfxVolume));
            //初始化音频数据
            List<AudioData> audioDatas = new();
            if (isUseSO) audioDatas = audioDataSO.audioDataList;
            else audioDatas = this.audioDataList;
            foreach (var data in audioDatas)
            {
                audioSourceDic.Add(data.clipName, data);
            }
        }

        /// <summary>
        /// 更新是否静音
        /// </summary> 
        public void UpdateIsMute()
        {
            if (isMute)
            {
                BGMAudioSource.Pause();
                SFXAudioSource.Pause();
            }
            else
            {
                BGMAudioSource.UnPause();
                SFXAudioSource.UnPause();
            }
        }

        //更新音量
        private void UpdateVolume(string audioMixerGroup, float volume)
        {
            AudioMixer.SetFloat(audioMixerGroup, volume);
        }

        //将(0,1)转换到(-80,0)db
        private float LinearToDecibels(float linearVolume)
        {
            if (linearVolume <= 0)
                return -80f; // 静音
            return Mathf.Log10(linearVolume) * 20; // 转换为 db
        }

        #endregion

        #region  Resources加载 - 使用时请确保Resources文件夹下有对应的音频文件

        //从Resources文件夹下加载音频
        private AudioClip LoadAudioClip(string path)
        {
            return (AudioClip)Resources.Load(path);
        }

        /// <summary>
        /// 从Resources文件夹下加载音频缓存进入字典
        /// </summary>
        /// <param name="audioName">音频名称</param>
        /// <param name="audioMixerGroup">音频混合器分组</param>    
        public void GetAudioClip(string audioName, string audioMixerGroup, float volume = 1f, bool loop = false, bool playOnAwake = false)
        {
            if (!audioSourceDic.ContainsKey(audioName))
            {
                //初始化数据
                AudioData audioData = new AudioData();
                audioData.clip = LoadAudioClip(audioName);
                audioData.clipName = audioData.clip.name;
                audioData.volume = volume;
                audioData.loop = loop;
                audioData.playOnAwake = playOnAwake;
                audioData.audioMixerGroup = AudioMixer.FindMatchingGroups(audioMixerGroup)[0];
                //缓存进入字典
                audioSourceDic.Add(audioData.clipName, audioData);
            }
        }

        #endregion

        #region  从缓存字典加载

        /// <summary>
        /// 初始化音频数据
        /// </summary>
        private void InitAudioData(AudioSource audioSource, string audioName)
        {
            if (!audioSourceDic.ContainsKey(audioName))
            {
                Debug.Log($"<color=red>{audioName} is non existent</color>");
                return;
            }

            var audioData = audioSourceDic[audioName];
            audioSource.clip = audioData.clip;
            audioSource.outputAudioMixerGroup = audioData.audioMixerGroup;
            audioSource.volume = audioData.volume;
            audioSource.loop = audioData.loop;
            audioSource.playOnAwake = audioData.playOnAwake;
        }

        /// <summary>
        /// 设置音频数据
        /// </summary>
        public void SetAudioData(string audioName, float volume = 1f, bool loop = false, bool playOnAwake = false)
        {
            if (!audioSourceDic.ContainsKey(audioName))
            {
                Debug.Log($"<color=red>{audioName} is non existent</color>");
                return;
            }

            var audioData = audioSourceDic[audioName];
            audioData.volume = volume;
            audioData.loop = loop;
            audioData.playOnAwake = playOnAwake;

            // 更新混音器音量
            UpdateVolume("BGM", LinearToDecibels(BGMVolume));
            UpdateVolume("SFX", LinearToDecibels(SFXVolume));
        }

        #region BGM

        /// <summary>
        /// 播放BGM音频
        /// </summary>
        /// <param name="audioName">音频名称</param>
        /// <param name="isWait">是否需要等待</param>
        public void PlayBGMAudio(string audioName, bool isWait = false, bool is3D = false)
        {
            InitAudioData(BGMAudioSource, audioName);
            BGMAudioSource.spatialBlend = is3D ? 1 : 0;
            if (isWait && !BGMAudioSource.isPlaying) BGMAudioSource.Play();
            else BGMAudioSource.Play();
        }

        /// <summary>
        /// 停止BGM音频
        /// </summary>
        public void StopBGMAudio()
        {
            if (BGMAudioSource != null)
            {
                BGMAudioSource.Stop();
                BGMAudioSource.clip = null;
            }
        }

        #endregion

        #region SFX

        /// <summary>
        /// 播放SFX音频
        /// </summary>
        public void PlaySFXAudio(string audioName, bool isWait = false, bool is3D = false)
        {
            InitAudioData(SFXAudioSource, audioName);
            SFXAudioSource.spatialBlend = is3D ? 1 : 0;
            if (isWait && !SFXAudioSource.isPlaying) SFXAudioSource.Play();
            else SFXAudioSource.Play();
        }

        /// <summary>
        /// 停止SFX音频
        /// </summary>
        public void StopSFXAudio()
        {
            if (SFXAudioSource != null)
            {
                SFXAudioSource.Stop();
                SFXAudioSource.clip = null;
            }
        }

        #endregion

        #region Other

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="audioSource">播放音频组件</param>
        /// <param name="audioName">音频名称</param>
        /// <param name="is3D">是否是3D空间</param>  
        public void PlayAudio(AudioSource audioSource, string audioName, bool isWait = false, bool is3D = false)
        {
            InitAudioData(audioSource, audioName);
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
        public void PlayAudioOneShot(AudioSource audioSource, string audioName, bool is3D = false)
        {
            InitAudioData(audioSource, audioName);
            audioSource.spatialBlend = is3D ? 1 : 0;
            audioSource.PlayOneShot(audioSource.clip);
        }

        /// <summary>
        /// 暂停音频
        /// </summary>
        /// <param name="audioSource">播放音频组件</param>
        public void StopAudio(AudioSource audioSource)
        {
            if (audioSource != null)
            {
                if (audioSource.isPlaying) audioSource.Stop();
            }
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// 音频数据类
    /// </summary>
    [System.Serializable]
    public class AudioData
    {
        [Tooltip("音频名称")] public string clipName;
        [Tooltip("音频剪辑")] public AudioClip clip;
        [Tooltip("音频分组")] public AudioMixerGroup audioMixerGroup;
        [Tooltip("音量")][Range(0, 1)] public float volume = 1f;
        [Tooltip("是否循环")] public bool loop = false;
        [Tooltip("是否在唤醒时播放")] public bool playOnAwake = false;
    }

    /// <summary>
    /// 音频数据SO类
    /// </summary>
    [CreateAssetMenu(fileName = "AduioDataSO", menuName = "Aduio/AduioData", order = 0)]
    public class AduioDataSO : ScriptableObject
    {
        public List<AudioData> audioDataList = new List<AudioData>();
    }
}
