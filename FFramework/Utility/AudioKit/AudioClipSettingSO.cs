using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FFramework.Kit
{
    /// <summary>
    /// 音频数据SO类
    /// </summary>
    [CreateAssetMenu(fileName = "AduioDataSO", menuName = "FFramework/AduioData", order = 0)]
    public class AudioClipSettingSO : ScriptableObject
    {
        [Header("音频组件")][Tooltip("音频混音器")] public AudioMixer AudioMixer;
        public TextAsset audioClipsDataFile;
        public List<AudioClipSetting> AudioClipsSetting = new List<AudioClipSetting>();

#if UNITY_EDITOR
        [Button("保存数据")]
        public void SaveData()
        {
            try
            {
                // 创建数据包装器
                AudioClipSettingDataWrapper wrapper = new AudioClipSettingDataWrapper();

                // 将AudioClipSetting数据转换为AudioClipSettingData
                foreach (var setting in AudioClipsSetting)
                {
                    AudioClipSettingData data = new AudioClipSettingData
                    {
                        name = setting.clipName,
                        clip = setting.clip != null ? setting.clip.name : string.Empty,
                        audioMixerGroup = setting.audioMixerGroup != null ? setting.audioMixerGroup.name : string.Empty,
                        volume = setting.volume,
                        loop = setting.loop,
                        playOnAwake = setting.playOnAwake
                    };
                    wrapper.AudioClipsSetting.Add(data);
                }

                // 序列化包装器类为JSON
                string json = JsonUtility.ToJson(wrapper, true);
                string soPath = AssetDatabase.GetAssetPath(this);
                string jsonPath = System.IO.Path.ChangeExtension(soPath, "json");

                // 写入JSON文件
                System.IO.File.WriteAllText(jsonPath, json);

                // 更新audioClipsDataFile引用
                AssetDatabase.ImportAsset(jsonPath);
                TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                audioClipsDataFile = jsonAsset;

                AssetDatabase.Refresh();
                Debug.Log($"<color=green>AudioClip data is saved as JSON: {jsonPath}</color>");

                // 保存资源以确保audioClipsDataFile引用被保存
                AssetDatabase.SaveAssetIfDirty(this);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<color=red>Error saving AudioClip data: {e.Message}</color>");
            }
        }
#endif

#if UNITY_EDITOR
        [Button("加载数据")]
#endif
        public void LoadData()
        {
            if (audioClipsDataFile == null)
            {
                Debug.LogWarning("<color=yellow>Unable to load data without specifying a JSON data file for AudioClip.</color>");
                return;
            }
            try
            {
                // 从JSON反序列化
                AudioClipSettingDataWrapper wrapper = JsonUtility.FromJson<AudioClipSettingDataWrapper>(audioClipsDataFile.text);

                if (wrapper == null || wrapper.AudioClipsSetting == null)
                {
                    Debug.LogError("<color=red>The JSON data is incorrectly formatted or empty.</color>");
                    return;
                }

                // 清空现有数据
                AudioClipsSetting.Clear();

                // 将AudioClipSettingData转换回AudioClipSetting
                foreach (var data in wrapper.AudioClipsSetting)
                {
                    AudioClipSetting setting = new AudioClipSetting();

                    // 设置基本属性
                    setting.clipName = data.name;
                    setting.volume = data.volume;
                    setting.loop = data.loop;
                    setting.playOnAwake = data.playOnAwake;

                    // 加载AudioClip对象（从Resources/Audio文件夹）
                    if (!string.IsNullOrEmpty(data.clip))
                    {
                        AudioClip audioClip = Resources.Load<AudioClip>("Audio/" + data.clip);
                        if (audioClip != null)
                        {
                            setting.clip = audioClip;
                        }
                    }
                    // 找到并设置AudioMixerGroup
                    if (!string.IsNullOrEmpty(data.audioMixerGroup) && AudioMixer != null)
                    {
                        // 尝试在AudioMixer中查找名称匹配的AudioMixerGroup
                        AudioMixerGroup[] groups = AudioMixer.FindMatchingGroups(data.audioMixerGroup);
                        if (groups != null && groups.Length > 0)
                        {
                            setting.audioMixerGroup = groups[0];
                        }
                        else
                        {
                            Debug.LogWarning($"<color=yellow>The AudioMixerGroup could not be found in the AudioMixer: {data.audioMixerGroup}</color>");
                            setting.audioMixerGroup = null;
                        }
                    }
                    AudioClipsSetting.Add(setting);
                }

                Debug.Log("<color=green>AudioClip loads data from JSON successfully.</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<color=red>An error occurred while loading JSON data: {e.Message}</color>");
            }
        }


        // 添加包装器类
        [System.Serializable]
        public class AudioClipSettingDataWrapper
        {
            public List<AudioClipSettingData> AudioClipsSetting = new List<AudioClipSettingData>();
        }

        [System.Serializable]
        public class AudioClipSettingData
        {
            public string name;
            public string clip;
            public string audioMixerGroup;
            public float volume;
            public bool loop;
            public bool playOnAwake;
        }
    }
}