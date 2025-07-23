using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(AudioTrackItemData))]
    public class AudioTrackItemDataInspector : Editor
    {
        private VisualElement root;
        private AudioTrackItemData targetData;

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as AudioTrackItemData;
            root = new VisualElement();
            root.styleSheets.Add(Resources.Load<StyleSheet>("USS/ItemDataInspectorStyle"));

            // 标题样式
            Label title = new Label("音频轨道项信息");
            title.AddToClassList("ItemDataViewTitle");
            title.text = "音频轨道项信息";
            root.Add(title);

            // 轨道名称 
            VisualElement nameContent = ItemDataViewContent("音频轨道项名称:");
            Label nameLabel = new Label();
            nameLabel.BindProperty(serializedObject.FindProperty("trackItemName"));
            nameContent.Add(nameLabel);
            root.Add(nameContent);

            // 帧数信息 
            VisualElement frameContent = ItemDataViewContent("音频片段总帧数:");
            Label frameLabel = new Label();
            frameLabel.BindProperty(serializedObject.FindProperty("frameCount"));
            frameContent.Add(frameLabel);
            root.Add(frameContent);

            // 音频片段字段
            VisualElement clipContent = ItemDataViewContent("音频片段:");
            ObjectField clipField = new ObjectField() { objectType = typeof(AudioClip) };
            clipField.AddToClassList("ObjectField");
            clipField.BindProperty(serializedObject.FindProperty("audioClip"));
            clipField.RegisterValueChangedCallback(evt => OnAudioClipChanged(evt.newValue as AudioClip));
            clipContent.Add(clipField);
            root.Add(clipContent);

            // 音频片段起始帧
            VisualElement startFrameContent = ItemDataViewContent("音频片段起始帧:");
            IntegerField startFrameField = new IntegerField();
            startFrameField.AddToClassList("TextField");
            startFrameField.BindProperty(serializedObject.FindProperty("startFrame"));
            startFrameField.RegisterValueChangedCallback(evt => OnStartFrameChanged(evt.newValue));
            startFrameContent.Add(startFrameField);
            root.Add(startFrameContent);

            // 音频片段持续帧
            VisualElement durationFrameContent = ItemDataViewContent("音频片段持续帧:");
            IntegerField durationFrameField = new IntegerField();
            durationFrameField.AddToClassList("TextField");
            durationFrameField.BindProperty(serializedObject.FindProperty("durationFrame"));
            durationFrameField.RegisterValueChangedCallback(evt => OnDurationFrameChanged(evt.newValue));
            durationFrameContent.Add(durationFrameField);
            root.Add(durationFrameContent);

            // 音量字段
            VisualElement volumeContent = ItemDataViewContent("音量:");
            FloatField volumeField = new FloatField();
            volumeField.AddToClassList("TextField");
            volumeField.BindProperty(serializedObject.FindProperty("volume"));
            volumeField.RegisterValueChangedCallback(evt => OnVolumeChanged(evt.newValue));
            volumeContent.Add(volumeField);
            root.Add(volumeContent);

            // 音调字段
            VisualElement pitchContent = ItemDataViewContent("音调:");
            FloatField pitchField = new FloatField();
            pitchField.AddToClassList("TextField");
            pitchField.BindProperty(serializedObject.FindProperty("pitch"));
            pitchField.RegisterValueChangedCallback(evt => OnPitchChanged(evt.newValue));
            pitchContent.Add(pitchField);
            root.Add(pitchContent);

            // 循环状态
            VisualElement loopContent = ItemDataViewContent("是否启用循环:");
            Toggle loopToggle = new Toggle();
            loopToggle.AddToClassList("Toggle");
            loopToggle.BindProperty(serializedObject.FindProperty("isLoop"));
            loopToggle.RegisterValueChangedCallback(evt => OnLoopChanged(evt.newValue));
            loopContent.Add(loopToggle);
            root.Add(loopContent);

            // 删除轨道项
            VisualElement deleteContent = ItemDataViewContent("");
            Button deleteButton = new Button(OnDeleteButtonClicked)
            {
                text = "删除音频轨道项"
            };
            deleteButton.AddToClassList("DeleteButton");
            deleteContent.Add(deleteButton);
            root.Add(deleteContent);

            return root;
        }

        private void OnAudioClipChanged(AudioClip newClip)
        {
            UpdateAudioTrackConfig(configClip => configClip.clip = newClip, "音频片段更新");
        }

        private void OnStartFrameChanged(int newValue)
        {
            UpdateAudioTrackConfig(configClip => configClip.startFrame = newValue, "起始帧更新");
        }

        private void OnDurationFrameChanged(int newValue)
        {
            UpdateAudioTrackConfig(configClip => configClip.durationFrame = newValue, "持续帧更新");
        }

        private void OnVolumeChanged(float newValue)
        {
            UpdateAudioTrackConfig(configClip => configClip.volume = newValue, "音量更新");
        }

        private void OnPitchChanged(float newValue)
        {
            UpdateAudioTrackConfig(configClip => configClip.pitch = newValue, "音调更新");
        }

        private void OnLoopChanged(bool newValue)
        {
            UpdateAudioTrackConfig(configClip => configClip.isLoop = newValue, "循环状态更新");
        }

        private void OnDeleteButtonClicked()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("删除确认",
                $"确定要删除音频轨道项 \"{targetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteAudioTrackItem();
            }
        }

        #region 数据同步方法

        /// <summary>
        /// 统一的音频配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateAudioTrackConfig(System.Action<FFramework.Kit.AudioTrack.AudioClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.audioTracks == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或音频轨道为空");
                return;
            }

            // 查找对应的音频片段配置
            FFramework.Kit.AudioTrack.AudioClip targetConfigClip = null;

            foreach (var audioTrack in skillConfig.trackContainer.audioTracks)
            {
                if (audioTrack.audioClips != null)
                {
                    var candidateClips = audioTrack.audioClips
                        .Where(clip => clip.clipName == targetData.trackItemName).ToList();

                    if (candidateClips.Count > 0)
                    {
                        if (candidateClips.Count == 1)
                        {
                            targetConfigClip = candidateClips[0];
                        }
                        else
                        {
                            // 如果有多个同名片段，尝试通过起始帧匹配
                            var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == targetData.startFrame);
                            targetConfigClip = exactMatch ?? candidateClips[0];
                        }
                        break;
                    }
                }
            }

            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到对应的音频片段配置");
            }
        }

        /// <summary>
        /// 标记技能配置为已修改
        /// </summary>
        private void MarkSkillConfigDirty()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig != null)
            {
                UnityEditor.EditorUtility.SetDirty(skillConfig);
            }
        }

        /// <summary>
        /// 删除音频轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteAudioTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.audioTracks == null || targetData == null)
            {
                Debug.LogWarning("无法删除轨道项：技能配置或音频轨道为空");
                return;
            }

            // 标记要删除的音频片段配置
            FFramework.Kit.AudioTrack.AudioClip targetConfigClip = null;
            FFramework.Kit.AudioTrack parentAudioTrack = null;

            // 查找对应的音频片段配置
            foreach (var audioTrack in skillConfig.trackContainer.audioTracks)
            {
                if (audioTrack.audioClips != null)
                {
                    var candidateClips = audioTrack.audioClips
                        .Where(clip => clip.clipName == targetData.trackItemName).ToList();

                    if (candidateClips.Count > 0)
                    {
                        if (candidateClips.Count == 1)
                        {
                            targetConfigClip = candidateClips[0];
                            parentAudioTrack = audioTrack;
                        }
                        else
                        {
                            // 如果有多个同名片段，尝试通过起始帧匹配
                            var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == targetData.startFrame);
                            if (exactMatch != null)
                            {
                                targetConfigClip = exactMatch;
                                parentAudioTrack = audioTrack;
                            }
                            else
                            {
                                targetConfigClip = candidateClips[0];
                                parentAudioTrack = audioTrack;
                            }
                        }
                        break;
                    }
                }
            }

            if (targetConfigClip != null && parentAudioTrack != null)
            {
                // 从配置中移除音频片段数据
                parentAudioTrack.audioClips.Remove(targetConfigClip);

                // 标记技能配置为已修改
                MarkSkillConfigDirty();

                // 删除轨道项的ScriptableObject数据文件
                if (targetData != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GetAssetPath(targetData));
                }

                // 清空Inspector选择
                UnityEditor.Selection.activeObject = null;

                // 触发界面刷新以移除UI元素
                var window = UnityEditor.EditorWindow.GetWindow<SkillEditor>();
                if (window != null)
                {
                    // 使用EditorApplication.delayCall确保在下一帧执行刷新
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        window.Repaint();

                        // 通过反射获取skillEditorEvent实例并触发刷新
                        var skillEditorEventField = typeof(SkillEditor).GetField("skillEditorEvent",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (skillEditorEventField != null)
                        {
                            var skillEditorEvent = skillEditorEventField.GetValue(window) as SkillEditorEvent;
                            skillEditorEvent?.TriggerRefreshRequested();
                        }
                    };
                }

                Debug.Log($"音频轨道项 \"{targetData.trackItemName}\" 删除成功");
            }
            else
            {
                Debug.LogWarning($"无法删除轨道项：找不到对应的音频片段配置 \"{targetData.trackItemName}\"");
            }
        }

        #endregion

        private VisualElement ItemDataViewContent(string titleName)
        {
            VisualElement content = new VisualElement();
            content.AddToClassList("ItemDataViewContent");
            content.AddToClassList("ItemDataViewContent-Audio"); // 添加音频特定样式
            if (!string.IsNullOrEmpty(titleName))
            {
                //标题文本
                Label title = new Label(titleName);
                title.style.width = 100;
                content.Add(title);
            }
            return content;
        }
    }
}