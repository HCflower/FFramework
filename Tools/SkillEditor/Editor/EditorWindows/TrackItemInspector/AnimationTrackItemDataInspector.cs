using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    [CustomEditor(typeof(AnimationTrackItemData))]
    public class AnimationTrackItemDataInspector : Editor
    {
        private VisualElement root;
        private AnimationTrackItemData targetData;

        public override VisualElement CreateInspectorGUI()
        {
            targetData = target as AnimationTrackItemData;
            root = new VisualElement();
            root.styleSheets.Add(Resources.Load<StyleSheet>("USS/ItemDataInspectorStyle"));

            // 标题样式
            Label title = new Label("动画轨道项信息");
            title.AddToClassList("ItemDataViewTitle");
            title.text = "动画轨道项信息";
            root.Add(title);

            // 轨道名称 
            VisualElement nameContent = ItemDataViewContent("动画轨道项名称:");
            Label nameLabel = new Label();
            nameLabel.BindProperty(serializedObject.FindProperty("trackItemName"));
            nameContent.Add(nameLabel);
            root.Add(nameContent);

            // 帧数信息 
            VisualElement frameContent = ItemDataViewContent("动画片段总帧数:");
            Label frameLabel = new Label();
            frameLabel.BindProperty(serializedObject.FindProperty("frameCount"));
            frameContent.Add(frameLabel);
            root.Add(frameContent);

            // 动画片段字段
            VisualElement clipContent = ItemDataViewContent("动画片段:");
            ObjectField clipField = new ObjectField() { objectType = typeof(AnimationClip) };
            clipField.AddToClassList("ObjectField");
            clipField.BindProperty(serializedObject.FindProperty("animationClip"));
            clipField.RegisterValueChangedCallback(evt => OnAnimationClipChanged(evt.newValue as AnimationClip));
            clipContent.Add(clipField);
            root.Add(clipContent);

            // 动画片段起始帧
            VisualElement startFrameContent = ItemDataViewContent("动画片段起始帧:");
            IntegerField startFrameField = new IntegerField();
            startFrameField.AddToClassList("TextField");
            startFrameField.BindProperty(serializedObject.FindProperty("startFrame"));
            startFrameField.RegisterValueChangedCallback(evt => OnStartFrameChanged(evt.newValue));
            startFrameContent.Add(startFrameField);
            root.Add(startFrameContent);

            // 动画片段持续帧
            VisualElement clipDurationFrameContent = ItemDataViewContent("动画片段持续帧:");
            IntegerField durationFrameField = new IntegerField();
            durationFrameField.AddToClassList("TextField");
            durationFrameField.BindProperty(serializedObject.FindProperty("durationFrame"));
            durationFrameField.RegisterValueChangedCallback(evt => OnDurationFrameChanged(evt.newValue));
            clipDurationFrameContent.Add(durationFrameField);
            root.Add(clipDurationFrameContent);

            // 播放速度
            VisualElement speedContent = ItemDataViewContent("播放速度:");
            FloatField speedField = new FloatField();
            speedField.AddToClassList("TextField");
            speedField.BindProperty(serializedObject.FindProperty("playSpeed"));
            speedField.RegisterValueChangedCallback(evt => OnPlaySpeedChanged(evt.newValue));
            speedContent.Add(speedField);
            root.Add(speedContent);

            // 循环状态
            VisualElement loopContent = ItemDataViewContent("是否启用循环:");
            Toggle loopToggle = new Toggle();
            loopToggle.AddToClassList("Toggle");
            loopToggle.BindProperty(serializedObject.FindProperty("isLoop"));
            loopToggle.RegisterValueChangedCallback(evt => OnLoopChanged(evt.newValue));
            loopContent.Add(loopToggle);
            root.Add(loopContent);

            // 是否启用根运动
            VisualElement rootMotionContent = ItemDataViewContent("是否应用根运动:");
            Toggle rootMotionToggle = new Toggle();
            rootMotionToggle.AddToClassList("Toggle");
            rootMotionToggle.BindProperty(serializedObject.FindProperty("applyRootMotion"));
            rootMotionToggle.RegisterValueChangedCallback(evt => OnApplyRootMotionChanged(evt.newValue));
            rootMotionContent.Add(rootMotionToggle);
            root.Add(rootMotionContent);

            // 删除轨道项
            VisualElement deleteContent = ItemDataViewContent("");
            Button deleteButton = new Button(OnDeleteButtonClicked)
            {
                text = "删除动画轨道项"
            };
            deleteButton.AddToClassList("DeleteButton");
            deleteContent.Add(deleteButton);
            root.Add(deleteContent);
            return root;
        }

        private VisualElement ItemDataViewContent(string titleName)
        {
            VisualElement content = new VisualElement();
            content.AddToClassList("ItemDataViewContent");
            content.AddToClassList("ItemDataViewContent-Animation"); // 添加动画轨道项特有的样式类
            if (!string.IsNullOrEmpty(titleName))
            {
                //标题文本
                Label title = new Label(titleName);
                title.style.width = 100;
                content.Add(title);
            }
            return content;
        }

        #region 数据同步方法

        /// <summary>
        /// 统一的配置数据更新方法
        /// </summary>
        /// <param name="updateAction">更新操作的委托</param>
        /// <param name="operationName">操作名称，用于调试信息</param>
        private void UpdateAnimationTrackConfig(System.Action<FFramework.Kit.AnimationTrack.AnimationClip> updateAction, string operationName = "更新配置")
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null || targetData == null)
            {
                Debug.LogWarning($"无法执行 {operationName}：技能配置或动画轨道为空");
                return;
            }

            // 优先通过名称查找，因为名称相对稳定
            var candidateClips = skillConfig.trackContainer.animationTrack.animationClips
                .Where(clip => clip.clipName == targetData.trackItemName).ToList();

            if (candidateClips.Count == 0)
            {
                Debug.LogWarning($"无法执行 {operationName}：找不到名称为 '{targetData.trackItemName}' 的动画片段");
                return;
            }

            FFramework.Kit.AnimationTrack.AnimationClip targetConfigClip = null;

            if (candidateClips.Count == 1)
            {
                targetConfigClip = candidateClips[0];
            }
            else
            {
                // 如果有多个同名片段，尝试通过起始帧匹配
                var exactMatch = candidateClips.FirstOrDefault(clip => clip.startFrame == targetData.startFrame);
                if (exactMatch != null)
                {
                    targetConfigClip = exactMatch;
                }
                else
                {
                    // 如果找不到精确匹配，使用第一个同名片段并给出警告
                    Debug.LogWarning($"找到多个同名动画片段 '{targetData.trackItemName}'，使用第一个片段进行 {operationName}");
                    targetConfigClip = candidateClips[0];
                }
            }

            // 执行更新操作
            if (targetConfigClip != null)
            {
                updateAction(targetConfigClip);
                MarkSkillConfigDirty();
            }
            else
            {
                Debug.LogError($"执行 {operationName} 失败：无法找到目标配置片段");
            }
        }

        /// <summary>
        /// 动画片段改变时的回调
        /// </summary>
        private void OnAnimationClipChanged(AnimationClip newClip)
        {
            UpdateAnimationTrackConfig(configClip => configClip.clip = newClip, "动画片段更新");
        }

        /// <summary>
        /// 起始帧改变时的回调
        /// </summary>
        private void OnStartFrameChanged(int newStartFrame)
        {
            UpdateAnimationTrackConfig(configClip => configClip.startFrame = newStartFrame, "起始帧更新");
        }

        /// <summary>
        /// 持续帧数改变时的回调
        /// </summary>
        private void OnDurationFrameChanged(int newDurationFrame)
        {
            UpdateAnimationTrackConfig(configClip => configClip.durationFrame = newDurationFrame, "持续帧数更新");
        }

        /// <summary>
        /// 播放速度改变时的回调
        /// </summary>
        private void OnPlaySpeedChanged(float newPlaySpeed)
        {
            UpdateAnimationTrackConfig(configClip => configClip.playSpeed = newPlaySpeed, "播放速度更新");
        }

        /// <summary>
        /// 循环状态改变时的回调
        /// </summary>
        private void OnLoopChanged(bool newLoopState)
        {
            UpdateAnimationTrackConfig(configClip => configClip.isLoop = newLoopState, "循环状态更新");
        }

        /// <summary>
        /// 根运动应用状态改变时的回调
        /// </summary>
        private void OnApplyRootMotionChanged(bool newApplyRootMotion)
        {
            UpdateAnimationTrackConfig(configClip => configClip.applyRootMotion = newApplyRootMotion, "根运动状态更新");
        }

        /// <summary>
        /// 删除按钮点击事件处理
        /// </summary>
        private void OnDeleteButtonClicked()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("删除确认",
                $"确定要删除动画轨道项 \"{targetData.trackItemName}\" 吗？\n\n此操作将会：\n• 从界面移除此轨道项\n• 删除对应的配置数据\n• 删除轨道项数据文件\n• 无法撤销",
                "确认删除", "取消"))
            {
                DeleteAnimationTrackItem();
            }
        }

        /// <summary>
        /// 删除动画轨道项的完整流程
        /// 包括移除UI元素、删除配置数据和触发界面刷新
        /// </summary>
        private void DeleteAnimationTrackItem()
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.animationTrack == null || targetData == null)
            {
                Debug.LogWarning("无法删除轨道项：技能配置或动画轨道为空");
                return;
            }

            // 查找要删除的动画片段配置
            FFramework.Kit.AnimationTrack.AnimationClip targetConfigClip = null;

            // 优先通过名称查找动画片段配置
            var candidateClips = skillConfig.trackContainer.animationTrack.animationClips
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
                    if (exactMatch != null)
                    {
                        targetConfigClip = exactMatch;
                    }
                    else
                    {
                        targetConfigClip = candidateClips[0];
                    }
                }
            }

            if (targetConfigClip != null)
            {
                // 从配置中移除动画片段数据
                skillConfig.trackContainer.animationTrack.animationClips.Remove(targetConfigClip);

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

                Debug.Log($"动画轨道项 \"{targetData.trackItemName}\" 删除成功");
            }
            else
            {
                Debug.LogWarning($"无法删除轨道项：找不到对应的动画片段配置 \"{targetData.trackItemName}\"");
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

        #endregion
    }
}
