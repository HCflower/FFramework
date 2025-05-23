using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 音频管理器  
    /// </summary>
    public class AudioManager : SingletonMono<AudioManager>
    {
        AudioManager() => IsDontDestroyOnLoad = true;
        [Header("音频组件")]
        [Tooltip("BGM音频组件")] public AudioSource BGMAudioSource;
        [Tooltip("SFX音频组件")] public AudioSource SFXAudioSource;
        [Tooltip("音频数据SO文件")] public AudioClipSettingSO AudioClipSettings;

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        //初始化
        private void Init()
        {
            if (AudioClipSettings.AudioClipSettings.Count > 0)
            {
                AudioKit.InitAudioDic();
            }
        }
    }
}
