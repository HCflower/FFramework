using System.Collections.Generic;
using FFramework.Kit;
using UnityEngine;

namespace FFramework
{
    /// <summary>
    /// 全局设置
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GlobalSetting : SingletonMono<GlobalSetting>
    {
        [Header("Language Setting")]
        [Tooltip("全局语言类型")][SerializeField] private LanguageType languageType;
        [Tooltip("全局本地化数据")] public LocalizationData GlobalLocalizationData;
        public LanguageType LanguageType
        {
            get => languageType;
            set
            {
                if (languageType != value)
                {
                    languageType = value;
                    LocalizationKit.TryGetLanguageType(GlobalLocalizationData);
                    LocalizationKit.Trigger();
                }
            }
        }

        [Header("Audio Setting")]
        [Tooltip("总音量")][Range(0, 1)] public float volume = 0.8f;
        public float Volume
        {
            get => volume;
            set
            {
                volume = value;
                AudioKit.UpdateVolume("Main", Volume);
            }
        }
        [Tooltip("背景音乐音量")][Range(0, 1)] public float bgmVolume = 0.8f;
        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = value;
                AudioKit.UpdateVolume("BGM", BGMVolume);
            }
        }
        [Tooltip("音效音量")][Range(0, 1)] public float sfxVolume = 0.8f;
        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = value;
                AudioKit.UpdateVolume("SFX", SFXVolume);
            }
        }
        [Tooltip("音频数据JSON文件")] public TextAsset audioClipsDataFile;
        [Tooltip("音频数据SO文件")] public AudioClipSettingSO AudioClipSetting;

        [Header("RedDotSystem")]
        public RedDotKitConfig redDotSystemConfig;

        [Header("Root Nodes Prefab")]
        [Tooltip("有子对象或需要面板赋值的根节点")]
        [SerializeField] private List<GameObject> rootNodeList = new List<GameObject>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 仅在运行模式下触发
            if (Application.isPlaying)
            {
                // 检查并更新语言类型
                LocalizationKit.TryGetLanguageType(GlobalLocalizationData);
                LocalizationKit.Trigger();
                // 更新音量设置
                AudioKit.UpdateVolume("Main", volume);
                AudioKit.UpdateVolume("BGM", bgmVolume);
                AudioKit.UpdateVolume("SFX", sfxVolume);
            }
        }

        [Button("加载音频数据")]
        private void LoadAudioData()
        {
            AudioClipSetting.audioClipsDataFile = this.audioClipsDataFile;
            AudioClipSetting.LoadData();
        }

        [Button("检查Or创建所有根节点")]
        private void CheckOrCreateAllGameRoot()
        {
            foreach (var rootPrefab in rootNodeList)
            {
                if (rootPrefab == null) continue;

                //获取组件类型
                var componentType = rootPrefab.GetComponent<MonoBehaviour>()?.GetType();
                if (componentType == null) continue;

                //查找场景中是否已存在该类型的对象
                var existingObj = GameObject.FindObjectOfType(componentType, true);
                if (existingObj == null)
                {
                    var newObj = GameObject.Instantiate(rootPrefab);
                    newObj.name = rootPrefab.name;
                    Debug.Log($"The root node has been created:<color=yellow>{newObj.name}</color>");
                }
            }
        }
#endif
    }
}