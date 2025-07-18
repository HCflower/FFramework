using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

namespace SkillEditor
{
    /// <summary>
    /// UI构建器
    /// </summary>
    public class SkillEditorUIBuilder
    {
        private readonly SkillEditorEvent skillEditorEvent;
        // SkillEditorData已为静态类，无需成员变量
        private VisualElement allTrackContent;
        private VisualElement trackControlContent; // 添加轨道控制区域引用

        public SkillEditorUIBuilder(SkillEditorEvent skillEditorEvent)
        {
            // SkillEditorData为静态类，无需赋值
            this.skillEditorEvent = skillEditorEvent;
            // 订阅刷新事件
            if (skillEditorEvent != null)
                skillEditorEvent.OnRefreshRequested += OnRefreshRequested;
        }

        #region 主要结构创建
        public VisualElement CreateMainContent(VisualElement parent)
        {
            return CreateAndAddElement(parent, "MainContent");
        }

        public VisualElement CreateGlobalControlArea(VisualElement parent)
        {
            return CreateAndAddElement(parent, "GlobalControlContent");
        }

        public VisualElement CreateTrackArea(VisualElement parent)
        {
            return CreateAndAddElement(parent, "TrackContent");
        }

        public TrackStructureResult CreateTrackStructure(VisualElement parent)
        {
            // 创建轨道控制区域
            var controlArea = CreateAndAddElement(parent, "TrackItemControlContent");
            CreateTrackControlButtons(controlArea);
            CreateSearchAndAddArea(controlArea);

            // 创建轨道控制滚动视图
            var (controlScrollView, controlContent) = CreateScrollView(controlArea, "TrackScrollView", "TrackScrollViewContent");
            this.trackControlContent = controlContent;
            SetScrollViewVisibility(controlScrollView, false, false);

            // 创建轨道区域
            var trackArea = CreateAndAddElement(parent, "TrackControlContent");
            var (trackScrollView, trackContent) = CreateScrollView(trackArea, "TrackScrollView", "TrackScrollViewContent");
            this.allTrackContent = trackContent;
            trackContent.style.width = SkillEditorData.CalculateTimelineWidth();

            return new TrackStructureResult
            {
                ControlArea = controlArea,
                ControlScrollView = controlScrollView,
                ControlContent = controlContent,
                TrackArea = trackArea,
                TrackScrollView = trackScrollView,
                TrackContent = trackContent
            };
        }

        // 新增结构体用于返回复杂结果
        public struct TrackStructureResult
        {
            public VisualElement ControlArea;
            public ScrollView ControlScrollView;
            public VisualElement ControlContent;
            public VisualElement TrackArea;
            public ScrollView TrackScrollView;
            public VisualElement TrackContent;
        }
        #endregion

        #region 全局控制区域
        public void RefreshGlobalControl(VisualElement globalControlContent)
        {
            globalControlContent.Clear();
            if (SkillEditorData.IsGlobalControlShow)
                CreateGlobalControlFunction(globalControlContent);
            else
                CreateGlobalControlShowButton(globalControlContent);
        }

        private void CreateGlobalControlShowButton(VisualElement parent)
        {
            var buttonWrapper = CreateAndAddElement(parent, "GlobalControlShowButtonWrapper");
            var showButton = CreateButton("", "Global Setting", () => skillEditorEvent.TriggerGlobalControlToggled(true));
            showButton.AddToClassList("GlobalControlShowButton");
            buttonWrapper.Add(showButton);
        }

        private void CreateGlobalControlFunction(VisualElement parent)
        {
            var scrollView = CreateScrollView(parent, "GlobalControlFunctionContent");

            // 技能配置区域
            CreateSkillConfigSection(scrollView);

            // 控制按钮区域
            CreateGlobalControlButtons(scrollView);
        }

        private void CreateSkillConfigSection(VisualElement parent)
        {
            CreateObjectField(parent, "技能配置:", typeof(SkillConfig), SkillEditorData.CurrentSkillConfig, (obj) =>
            {
                SkillEditorData.CurrentSkillConfig = obj as SkillConfig;
                skillEditorEvent.TriggerSkillConfigChanged(SkillEditorData.CurrentSkillConfig);
                // 切换配置后刷新编辑器视图
                skillEditorEvent.TriggerRefreshRequested();
            });

            // 动画帧率输入框逻辑简化，闭包安全
            TextField frameRateField = null;
            frameRateField = CreateInputField(parent, "动画帧率:", SkillEditorData.CurrentSkillConfig?.frameRate.ToString() ?? "30", value =>
            {
                if (SkillEditorData.CurrentSkillConfig == null)
                {
                    Debug.LogWarning("当前没有加载的技能配置，无法设置帧率");
                    if (frameRateField != null) frameRateField.value = "30";
                    return;
                }
                if (float.TryParse(value, out float frameRate))
                {
                    frameRate = Mathf.Clamp(frameRate, 1f, 120f);
                    SkillEditorData.CurrentSkillConfig.frameRate = frameRate;
                    if (frameRateField != null) frameRateField.value = frameRate.ToString();
                }
                else
                {
                    Debug.LogWarning($"无效的帧率值: {value}，请输入有效的数字");
                    if (frameRateField != null) frameRateField.value = SkillEditorData.CurrentSkillConfig.frameRate.ToString();
                }
            });
            // 技能Icon选择器（可扩展为真正的资源选择）
            CreateImageSelector(parent, "技能Icon:");

            // 技能名称
            CreateInputField(parent, "技能名称:", SkillEditorData.CurrentSkillConfig?.skillName ?? "", value =>
            {
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    SkillEditorData.CurrentSkillConfig.skillName = value;
                }
            });

            // 技能ID
            CreateInputField(parent, "技能ID:", SkillEditorData.CurrentSkillConfig?.skillId.ToString() ?? "", value =>
            {
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    if (int.TryParse(value, out int skillId))
                    {
                        SkillEditorData.CurrentSkillConfig.skillId = skillId;
                    }
                    else
                    {
                        Debug.LogWarning($"无效的技能ID值: {value}，请输入有效的整数");
                    }
                }
            });

            // 技能描述
            CreateMultilineInputField(parent, "技能描述:", SkillEditorData.CurrentSkillConfig?.description ?? "", value =>
            {
                if (SkillEditorData.CurrentSkillConfig != null)
                {
                    SkillEditorData.CurrentSkillConfig.description = value;
                }
            });
        }

        private void CreateGlobalControlButtons(VisualElement parent)
        {
            // 隐藏全局控制区域按钮
            CreateButton(parent, "↩", "隐藏全局控制区域", () =>
            {
                skillEditorEvent.TriggerGlobalControlToggled(false);
            });

            // 刷新视图按钮
            CreateButton(parent, "刷新视图", "", () =>
            {
                skillEditorEvent.TriggerRefreshRequested();
            });

            // 保存配置文件按钮
            CreateButton(parent, "保存配置文件", "", () =>
            {
                // 保存逻辑
                AssetDatabase.SaveAssetIfDirty(SkillEditorData.CurrentSkillConfig);
                Debug.Log($"已保存技能配置: {SkillEditorData.CurrentSkillConfig?.skillName}");
            });

            // 添加配置按钮
            CreateAddConfigButton(parent);

            // 删除配置文件按钮
            var deleteButton = CreateButton(parent, "删除配置文件", "", () =>
            {
                // 删除逻辑
            });
            deleteButton.style.backgroundColor = Color.red;
        }

        #endregion

        #region 轨道控制区域
        public void RefreshTrackContent(VisualElement trackContent)
        {
            if (trackContent == null) return;

            float newWidth = SkillEditorData.CalculateTimelineWidth();
            UpdateTrackContentWidth(trackContent, newWidth);

            var scrollView = trackContent.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null)
            {
                ForceUpdateScrollView(scrollView);
            }
        }

        private void UpdateTrackContentWidth(VisualElement trackContent, float width)
        {
            trackContent.style.width = width;
            trackContent.style.minWidth = width;
        }

        private void CreateTrackControlButtons(VisualElement parent)
        {
            var buttonContent = CreateAndAddElement(parent, "TrackItemControlButtonContent");

            // 播放控制按钮组
            CreatePlaybackControls(buttonContent);

            // 帧输入区域
            CreateFrameInputArea(buttonContent);
        }

        private void CreatePlaybackControls(VisualElement parent)
        {
            // 上一帧按钮
            CreateIconButton(parent, "d_Animation.PrevKey", "上一帧", () =>
            {
                skillEditorEvent.TriggerCurrentFrameChanged(SkillEditorData.CurrentFrame - 1);
            });

            // 播放/暂停按钮
            CreateIconButton(parent, SkillEditorData.IsPlaying ? "d_PauseButton@2x" : "d_Animation.Play", "播放", () =>
            {
                SkillEditorData.IsPlaying = !SkillEditorData.IsPlaying;
                skillEditorEvent.TriggerPlayStateChanged(SkillEditorData.IsPlaying);
            });

            // 下一帧按钮
            CreateIconButton(parent, "d_Animation.NextKey", "下一帧", () =>
            {
                skillEditorEvent.TriggerCurrentFrameChanged(SkillEditorData.CurrentFrame + 1);
            });

            // 循环播放按钮
            CreateIconButton(parent, "d_preAudioLoopOff@2x", "循环播放", () =>
            {
                SkillEditorData.IsLoop = !SkillEditorData.IsLoop;
            });
        }

        // 创建帧输入区域
        private void CreateFrameInputArea(VisualElement parent)
        {
            var frameInputContent = new VisualElement();
            frameInputContent.AddToClassList("FrameInputContent");
            parent.Add(frameInputContent);

            // 当前帧输入
            var currentFrameInput = CreateIntegerField("当前帧", SkillEditorData.CurrentFrame, (value) =>
            {
                skillEditorEvent.TriggerCurrentFrameChanged(value);
            });
            currentFrameInput.name = "current-frame-input"; // 添加名称以便查找
            frameInputContent.Add(currentFrameInput);

            // 分隔符
            var separator = new Label("|");
            separator.AddToClassList("SeparatorLabel");
            frameInputContent.Add(separator);

            // 最大帧输入，优先用配置中的maxFrames（不为0），否则用SkillEditorData.MaxFrame
            IntegerField maxFrameInput = null;
            int maxFrameValue = (SkillEditorData.CurrentSkillConfig != null && SkillEditorData.CurrentSkillConfig.maxFrames > 0)
                ? SkillEditorData.CurrentSkillConfig.maxFrames
                : SkillEditorData.MaxFrame;
            maxFrameInput = CreateIntegerField("最大帧", maxFrameValue, (value) =>
            {
                SkillEditorData.CurrentSkillConfig.maxFrames = value;
                skillEditorEvent.TriggerMaxFrameChanged(value);
                skillEditorEvent.TriggerRefreshRequested();
            });
            frameInputContent.Add(maxFrameInput);

            // 切换配置时自动刷新最大帧输入框显示
            skillEditorEvent.OnSkillConfigChanged += (config) =>
            {
                if (maxFrameInput != null)
                {
                    maxFrameInput.value = (config != null && config.maxFrames > 0) ? config.maxFrames : SkillEditorData.MaxFrame;
                }
            };
        }

        private void CreateSearchAndAddArea(VisualElement parent)
        {
            var searchAddArea = CreateAndAddElement(parent, "SearchOrAddTrack");

            // 搜索输入框
            CreateSearchInput(searchAddArea);

            // 添加轨道按钮
            CreateAddTrackButton(searchAddArea);
        }

        private void CreateSearchInput(VisualElement parent)
        {
            var searchInput = new TextField();
            searchInput.AddToClassList("SearchTrackInput");
            searchInput.tooltip = "搜索轨道";

            var textElement = searchInput.Q<TextElement>(className: "unity-text-element");
            if (textElement != null) textElement.style.marginLeft = 18;

            var searchIcon = new Image();
            searchIcon.AddToClassList("SearchTrackInputIcon");
            searchInput.Add(searchIcon);
            parent.Add(searchInput);
        }

        private void CreateAddTrackButton(VisualElement parent)
        {
            var addButton = new Button();
            addButton.AddToClassList("AddTrackButton");
            addButton.clicked += () => ShowTrackCreationMenu(addButton);
            parent.Add(addButton);
        }
        private void ShowTrackCreationMenu(VisualElement button)
        {
            var menu = new GenericMenu();

            // 创建动画轨道菜单项（只允许一个）
            bool hasAnimationTrack = SkillEditorData.tracks.Exists(t => t.TrackType == TrackType.AnimationTrack);
            menu.AddItem(new GUIContent("创建 Animation Track"), hasAnimationTrack, () =>
            {
                if (!hasAnimationTrack) CreateTrack(TrackType.AnimationTrack);
                else Debug.LogWarning("已存在动画轨道，无法创建新的动画轨道。");
            });

            // 创建音频轨道菜单项
            menu.AddItem(new GUIContent("创建 Audio Track"), false, () => CreateTrack(TrackType.AudioTrack));

            // 创建特效轨道菜单项
            menu.AddItem(new GUIContent("创建 Effect Track"), false, () => CreateTrack(TrackType.EffectTrack));

            // 创建攻击轨道菜单项
            menu.AddItem(new GUIContent("创建 Attack Track"), false, () => CreateTrack(TrackType.AttackTrack));

            // 创建事件轨道菜单项
            menu.AddItem(new GUIContent("创建 Event Track"), false, () => CreateTrack(TrackType.EventTrack));

            var rect = button.worldBound;
            menu.DropDown(new Rect(rect.x, rect.yMax, 0, 0));
        }

        // 创建轨道
        private void CreateTrack(TrackType trackType)
        {
            // 默认轨道名称可用类型+序号
            string trackName = $"{trackType}_{SkillEditorData.tracks.Count + 1}";
            var trackControl = new SkillEditorTrackControl(trackControlContent, trackType, trackName);
            var track = new SkillEditorTrack(allTrackContent, trackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig);
            var trackInfo = new SkillEditorTrackInfo(trackControl, track, trackType, trackName);
            SkillEditorData.tracks.Add(trackInfo);

            // 重新订阅事件
            SubscribeTrackEvents(trackControl);

            // Debug.Log($"成功创建{trackType}轨道，当前轨道数量: {SkillEditorData.tracks.Count}");
        }

        // 刷新事件
        public void OnRefreshRequested()
        {
            // 清空现有轨道UI
            trackControlContent?.Clear();
            allTrackContent?.Clear();
            // 重新创建所有轨道UI
            RefreshAllTracks();
        }

        // 刷新所有轨道UI
        private void RefreshAllTracks()
        {
            if (SkillEditorData.tracks == null) return;

            // 需要重新创建UI元素，而不是重用旧的
            var tracksToRecreate = new List<SkillEditorTrackInfo>(SkillEditorData.tracks);
            SkillEditorData.tracks.Clear();

            //TODO 优先创建动画轨道 - 实现轨道拖拽后可以考虑移除
            foreach (var oldTrackInfo in tracksToRecreate.Where(t => t.TrackType == TrackType.AnimationTrack))
            {
                var newTrackControl = new SkillEditorTrackControl(trackControlContent, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                var newTrack = new SkillEditorTrack(allTrackContent, oldTrackInfo.TrackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig);
                var newTrackInfo = new SkillEditorTrackInfo(newTrackControl, newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                SkillEditorData.tracks.Add(newTrackInfo);
                SubscribeTrackEvents(newTrackControl);
            }
            // 再创建其它类型轨道
            foreach (var oldTrackInfo in tracksToRecreate.Where(t => t.TrackType != TrackType.AnimationTrack))
            {
                var newTrackControl = new SkillEditorTrackControl(trackControlContent, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                var newTrack = new SkillEditorTrack(allTrackContent, oldTrackInfo.TrackType, SkillEditorData.CalculateTimelineWidth(), SkillEditorData.CurrentSkillConfig);
                var newTrackInfo = new SkillEditorTrackInfo(newTrackControl, newTrack, oldTrackInfo.TrackType, oldTrackInfo.TrackName);
                SkillEditorData.tracks.Add(newTrackInfo);
                SubscribeTrackEvents(newTrackControl);
            }
        }

        // 订阅轨道事件
        private void SubscribeTrackEvents(SkillEditorTrackControl trackControl)
        {
            trackControl.OnDeleteTrack += (ctrl) =>
           {
               var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
               if (info != null)
               {
                   SkillEditorData.tracks.Remove(info);
                   Debug.Log($"删除轨道: {info.TrackName}，剩余轨道数量: {SkillEditorData.tracks.Count}");
                   skillEditorEvent?.TriggerRefreshRequested();
               }
           };
            // 订阅激活/失活事件
            trackControl.OnActiveStateChanged += (ctrl, isActive) =>
            {
                var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
                if (info != null)
                {
                    info.IsActive = isActive;
                    info.Control.RefreshState(isActive);
                    Debug.Log($"轨道[{info.TrackName}]激活状态: {(isActive ? "激活" : "失活")}");
                }
            };
            // 订阅添加轨道项事件
            trackControl.OnAddTrackItem += (ctrl) =>
            {
                var info = SkillEditorData.tracks.Find(t => t.Control == ctrl);
                if (info != null)
                {
                    if (info.TrackType == TrackType.EventTrack)
                    {
                        info.Track.AddTrackItem("Event");
                    }
                    else if (info.TrackType == TrackType.AttackTrack)
                    {
                        info.Track.AddTrackItem("Attack");
                    }
                }
            };
        }

        #endregion

        #region 辅助方法
        private VisualElement CreateAndAddElement(VisualElement parent, string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        private ScrollView CreateScrollView(VisualElement parent, string className)
        {
            var scrollView = new ScrollView();
            scrollView.AddToClassList(className);
            SetScrollViewVisibility(scrollView, false, false);
            parent.Add(scrollView);
            return scrollView;
        }

        private void SetScrollViewVisibility(ScrollView scrollView, bool horizontal, bool vertical)
        {
            scrollView.horizontalScrollerVisibility = horizontal ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = vertical ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        }

        private void ForceUpdateScrollView(ScrollView scrollView)
        {
            scrollView.MarkDirtyRepaint();
            scrollView.contentContainer.MarkDirtyRepaint();

            EditorApplication.delayCall += () =>
            {
                if (scrollView != null)
                {
                    var originalVisibility = scrollView.horizontalScrollerVisibility;
                    scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

                    EditorApplication.delayCall += () =>
                    {
                        if (scrollView != null)
                        {
                            scrollView.horizontalScrollerVisibility = originalVisibility;
                        }
                    };
                }
            };
        }
        #endregion

        #region 通用UI创建方法
        public Button CreateButton(string text, string tooltip = "", Action onClick = null)
        {
            var button = new Button();
            button.text = text;
            button.tooltip = tooltip;
            if (onClick != null) button.clicked += onClick;
            return button;
        }

        public Button CreateButton(VisualElement parent, string text, string tooltip = "", Action onClick = null)
        {
            var controlContent = new VisualElement();
            controlContent.AddToClassList("ControlButtonContent");

            var button = CreateButton(text, tooltip, onClick);
            button.AddToClassList("Field");
            controlContent.Add(button);

            parent.Add(controlContent);
            return button;
        }

        public Button CreateIconButton(VisualElement parent, string iconPath, string tooltip = "", Action onClick = null)
        {
            var button = new Button();
            button.AddToClassList("TrackItemControlButton");
            button.tooltip = tooltip;
            button.style.backgroundImage = Resources.Load<Texture2D>($"Icon/{iconPath}");
            if (onClick != null) button.clicked += onClick;

            parent.Add(button);
            return button;
        }

        public ObjectField CreateObjectField(VisualElement parent, string labelText, Type objectType, UnityEngine.Object currentValue = null, Action<UnityEngine.Object> onValueChanged = null)
        {
            var container = CreateLabeledContainer(parent, labelText);

            var objectField = new ObjectField();
            objectField.AddToClassList("Field");
            objectField.objectType = objectType;
            objectField.value = currentValue;

            if (onValueChanged != null)
            {
                objectField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            container.Add(objectField);
            return objectField;
        }

        public TextField CreateInputField(VisualElement parent, string labelText, string currentValue = "", Action<string> onValueChanged = null)
        {
            var container = CreateLabeledContainer(parent, labelText);

            TextField inputField = new TextField();
            inputField.AddToClassList("Field");
            inputField.value = currentValue;
            inputField.tooltip = labelText;
            inputField.style.fontSize = 10;
            if (onValueChanged != null)
            {
                inputField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            container.Add(inputField);
            return inputField;
        }

        public TextField CreateMultilineInputField(VisualElement parent, string labelText, string currentValue = "", Action<string> onValueChanged = null)
        {
            var inputField = CreateInputField(parent, labelText, currentValue, onValueChanged);
            inputField.multiline = true;
            inputField.style.whiteSpace = WhiteSpace.Normal;
            inputField.style.fontSize = 10;
            inputField.style.height = 60;
            return inputField;
        }

        public IntegerField CreateIntegerField(string tooltip, int currentValue, Action<int> onValueChanged = null)
        {
            var integerField = new IntegerField();
            integerField.AddToClassList("FrameInput");
            integerField.tooltip = tooltip;
            integerField.value = currentValue;

            if (onValueChanged != null)
            {
                integerField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
            }

            return integerField;
        }

        public void CreateImageSelector(VisualElement parent, string labelText)
        {
            var container = CreateLabeledContainer(parent, labelText);

            Image skillIcon = new Image();
            skillIcon.AddToClassList("SkillIcon");
            container.Add(skillIcon);
            // 如果当前技能配置有图标，显示它
            if (SkillEditorData.CurrentSkillConfig?.skillIcon != null)
            {
                skillIcon.image = SkillEditorData.CurrentSkillConfig.skillIcon.texture;
            }
            var selectButton = new Button();
            selectButton.AddToClassList("SelectIcon");
            selectButton.tooltip = "选择技能Icon";
            selectButton.clicked += () => OpenImageSelector(skillIcon);
            skillIcon.Add(selectButton);
        }

        // 打开图片选择器
        private void OpenImageSelector(Image targetImage)
        {
            // 使用EditorUtility.OpenFilePanel打开文件选择对话框
            string imagePath = EditorUtility.OpenFilePanel(
                "选择技能图标",
                "Assets",
                "png,jpg,jpeg,tga,psd,tiff,gif,bmp");

            if (!string.IsNullOrEmpty(imagePath))
            {
                // 将绝对路径转换为相对于Assets的路径
                string relativePath = GetRelativeAssetPath(imagePath);

                if (!string.IsNullOrEmpty(relativePath))
                {
                    // 加载选中的图片资源
                    var selectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
                    if (selectedSprite != null)
                    {
                        // 更新UI显示
                        targetImage.image = selectedSprite.texture;

                        // 保存到技能配置
                        if (SkillEditorData.CurrentSkillConfig != null)
                        {
                            SkillEditorData.CurrentSkillConfig.skillIcon = selectedSprite;
                            Debug.Log($"技能图标已设置为: {selectedSprite.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载图片资源: {relativePath}");
                    }
                }
                else
                {
                    Debug.LogWarning("选择的文件不在Assets文件夹内请选择项目内的图片资源");
                }
            }
        }

        // 获取相对路径
        private string GetRelativeAssetPath(string absolutePath)
        {
            // 获取项目Assets文件夹的绝对路径
            string assetsPath = Application.dataPath;

            // 检查选择的文件是否在Assets文件夹内
            if (absolutePath.StartsWith(assetsPath))
            {
                // 转换为相对路径（相对于Assets父目录）
                string relativePath = "Assets" + absolutePath.Substring(assetsPath.Length);
                return relativePath.Replace('\\', '/'); // 统一使用正斜杠
            }

            return null;
        }

        private void CreateAddConfigButton(VisualElement parent)
        {
            var controlContent = new VisualElement();
            controlContent.AddToClassList("ControlButtonContent");

            var addButton = new Button();
            addButton.AddToClassList("Field");
            addButton.style.flexDirection = FlexDirection.Row;
            addButton.text = "创建配置文件";
            addButton.clicked += () => HandleAddConfigClick(addButton);

            controlContent.Add(addButton);
            parent.Add(controlContent);
        }

        private void HandleAddConfigClick(Button addButton)
        {
            addButton.text = "";
            if (addButton.Q<TextField>() != null) return;

            var configName = new TextField();
            configName.AddToClassList("AddConfigInput");
            configName.tooltip = "请输入SkillConfig名称";
            configName.value = "";
            addButton.Add(configName);

            var confirmButton = new Button();
            confirmButton.text = "+";
            confirmButton.style.fontSize = 20;
            confirmButton.AddToClassList("AddConfigButton");
            addButton.Add(confirmButton);

            var cancelButton = new Button();
            cancelButton.text = "X";
            cancelButton.AddToClassList("AddConfigButton");
            addButton.Add(cancelButton);

            // 定义确认和取消的处理方法
            System.Action confirmAction = () =>
            {
                Debug.Log($"创建配置文件: {configName.value}");
                ResetAddConfigButton(addButton, configName, confirmButton, cancelButton);
            };

            System.Action cancelAction = () =>
            {
                ResetAddConfigButton(addButton, configName, confirmButton, cancelButton);
            };

            // 绑定按钮事件
            confirmButton.clicked += confirmAction;
            cancelButton.clicked += cancelAction;

            // 键盘事件处理
            configName.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    confirmAction();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    cancelAction();
                    evt.StopPropagation();
                }
            });

            configName.Focus();
        }

        private void ResetAddConfigButton(Button addButton, TextField configName, Button confirmButton, Button cancelButton)
        {
            addButton.Remove(configName);
            addButton.Remove(confirmButton);
            addButton.Remove(cancelButton);
            addButton.text = "创建配置文件";
        }

        public (ScrollView scrollView, VisualElement content) CreateScrollView(VisualElement parent, string scrollViewClass, string contentClass)
        {
            var scrollView = new ScrollView();
            scrollView.AddToClassList(scrollViewClass);

            var content = new VisualElement();
            content.AddToClassList(contentClass);
            scrollView.Add(content);

            parent.Add(scrollView);
            return (scrollView, content);
        }

        private VisualElement CreateLabeledContainer(VisualElement parent, string labelText)
        {
            var container = new VisualElement();
            container.AddToClassList("LabeledContainerContent");

            var label = new Label(labelText);
            label.AddToClassList("LabeledContainerTitle");
            container.Add(label);

            parent.Add(container);
            return container;
        }
        #endregion
    }
}

