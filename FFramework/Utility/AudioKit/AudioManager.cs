using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 音频管理器  
    /// </summary>
    public class AudioManager : SingletonMono<AudioManager>
    {
        [Header("音频组件")]
        [SerializeField][Tooltip("BGM音频组件")] private AudioSource BGMAudioSource;
        [SerializeField][Tooltip("SFX音频组件")] private AudioSource SFXAudioSource;
        [SerializeField][Tooltip("音频数据SO文件")] public AudioClipSettingSO AudioClipSettings;

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
            if (AudioClipSettings.AudioClipSettings.Count > 0)
            {
                AudioKit.InitAudioDic();
            }
        }
    }
}
