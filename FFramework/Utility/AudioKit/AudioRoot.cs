using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 音频管理器  
    /// </summary>
    public class AudioRoot : SingletonMono<AudioRoot>
    {
        AudioRoot() => IsDontDestroyOnLoad = true;
        [Header("音频组件")]
        [Tooltip("BGM音频组件")] public AudioSource BGMAudioSource;
        [Tooltip("SFX音频组件")] public AudioSource SFXAudioSource;
        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        //初始化
        private void Init()
        {
            if (GlobalSetting.Instance.AudioClipSetting.AudioClipsSetting.Count > 0)
            {
                AudioKit.InitAudioDic();
            }
        }
    }
}
