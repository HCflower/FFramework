using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;
using System;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器
    /// </summary>
    public class SkillEditor : EditorWindow
    {
        #region 数据

        private SkillConfig currentSkillConfig;     // 当前技能配置
        private bool isGlobalControlShow = false;   // 是否显示全局控制区域

        // 时间轴配置
        private float frameUnitWidth = 10f;          // 每帧单位宽度
        [Min(0)] private int currentFrame = 1;       // 当前帧
        private int maxFrame = 100;                  // 最大帧数
        private float trackViewContentOffsetX = 0f;  // 轨道视图内容偏移X
        private int majorTickInterval = 5;           // 主刻度间隔（每几帧显示一个主刻度）

        // 事件定义
        public System.Action<int> OnCurrentFrameChanged; // 当前帧改变事件

        #endregion

        #region GUI

        private VisualElement mainContent;

        // 全局控制区域
        private VisualElement globalControlContent;
        private VisualElement trackContent;
        private ObjectField configObjectField;
        private TextField nameInputField;
        private TextField idInputField;
        private TextField descriptionField;

        // 轨道
        private ScrollView track;
        private ScrollView trackItemContent;
        private VisualElement timeLineIMGUI;
        private IntegerField currentFrameInput;
        private IntegerField maxFrameInput;
        private VisualElement trackControlContent;

        #endregion

        /// <summary>
        /// 技能编辑窗口
        /// </summary>
        [MenuItem("FFramework/⚔️SkillEditor #S", priority = 5)]
        public static void SkillEditorCreateWindow()
        {
            SkillEditor window = GetWindow<SkillEditor>();
            window.minSize = new Vector2(900, 450);
            window.titleContent = new GUIContent("SkillEditor");
            window.Show();
        }

        // 窗口初始化
        private void OnEnable()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorStyle"));
            CreateMainContent(rootVisualElement);
            CreateGlobalControlContent(mainContent);
            // 创建轨道区域
            CreateTrackContent(mainContent);
            // 绘制当前帧指示器
            DrawCurrentFrameIndicator();
        }

        // 窗口关闭 - 保存数据
        private void OnDisable()
        {

        }

        // 刷新视图
        private void RefreshView()
        {
            // 刷新全局控制区域
            globalControlContent.Clear();
            if (isGlobalControlShow) CreateGlobalControlFunctionContent(globalControlContent);
            else CreateGlobalControlShowButton(globalControlContent);
        }

        #region 各个功能区域绘制

        // 创建主区域
        private void CreateMainContent(VisualElement visual)
        {
            mainContent = new VisualElement();
            mainContent.AddToClassList("MainContent");
            visual.Add(mainContent);
        }

        #region  全局控制区域

        // 创建全局控制区域
        private void CreateGlobalControlContent(VisualElement visual)
        {
            globalControlContent = new VisualElement();
            globalControlContent.AddToClassList("GlobalControlContent");
            visual.Add(globalControlContent);
            //创建全局控制区域显示按钮
            CreateGlobalControlShowButton(globalControlContent);
        }

        // 创建全局控制区域隐藏按钮
        private void CreateGlobalControlShowButton(VisualElement visual)
        {
            VisualElement buttonWrapper = new VisualElement();
            buttonWrapper.AddToClassList("GlobalControlShowButtonWrapper");
            Button hideOrShowButton = new Button();
            hideOrShowButton.AddToClassList("GlobalControlShowButton");
            hideOrShowButton.text = "◀"; //▶
            hideOrShowButton.tooltip = "显示全局控制区域";
            hideOrShowButton.clicked += () =>
            {
                isGlobalControlShow = true;
                RefreshView();
            };
            buttonWrapper.Add(hideOrShowButton);

            visual.Add(buttonWrapper);
        }

        // 创建全局控制区域功能区域
        private void CreateGlobalControlFunctionContent(VisualElement visual)
        {
            ScrollView functionContentScrollView = new ScrollView();
            // 设置滚动条隐藏
            functionContentScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            functionContentScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            functionContentScrollView.AddToClassList("GlobalControlFunctionContent");
            visual.Add(functionContentScrollView);

            // 创建技能配置选择区域
            CreateObjectField(functionContentScrollView, "技能配置:", ref configObjectField, typeof(SkillConfig), () =>
            {
                currentSkillConfig = configObjectField.value as SkillConfig;
            });
            if (currentSkillConfig) configObjectField.value = currentSkillConfig;

            CreateSelectImage(functionContentScrollView, "技能Icon:");

            // 创建名称设置区域
            CreateInputField(functionContentScrollView, "技能名称:", ref nameInputField, () =>
            {

            });

            // 创建技能ID设置区域
            CreateInputField(functionContentScrollView, "技能ID:", ref idInputField, () =>
            {

            });

            // 创建技能ID设置区域
            CreateInputField(functionContentScrollView, "技能描述:", ref descriptionField, () =>
            {

            });
            descriptionField.multiline = true;
            descriptionField.style.whiteSpace = WhiteSpace.Normal;
            descriptionField.style.height = 60;

            // 创建隐藏全局控制区域按钮
            CreateGlobalControlButton(functionContentScrollView, "↩", out Button _, () =>
            {
                isGlobalControlShow = false;
                RefreshView();
            }, "隐藏全局控制区域");

            // 创建刷新视图按钮
            CreateGlobalControlButton(functionContentScrollView, "Refresh View", out Button _, () =>
            {

            });

            // 创建保存数据按钮
            CreateGlobalControlButton(functionContentScrollView, "Save Config File", out Button _, () =>
            {

            });

            // 创建创建配置文件按钮
            CreateAddConfigButton(functionContentScrollView);

            // 创建删除配置文件按钮
            CreateGlobalControlButton(functionContentScrollView, "Delete Config File", out Button deleteButton, () =>
            {

            });
            deleteButton.style.backgroundColor = Color.red;
        }

        private void CreateAddConfigButton(VisualElement visual)
        {
            // 创建创建配置文件按钮
            VisualElement controlButtonContent = new VisualElement();
            controlButtonContent.AddToClassList("ControlButtonContent");
            // 添加配置按钮
            Button addConfigButton = new Button();
            addConfigButton.AddToClassList("Field");
            addConfigButton.style.flexDirection = FlexDirection.Row;
            addConfigButton.text = "Create Config File";
            addConfigButton.clicked += () =>
            {
                addConfigButton.text = "";
                // 避免重复添加输入框
                if (addConfigButton.Q<TextField>() != null) return;
                // 创建输入框
                TextField configName = new TextField();
                configName.AddToClassList("AddConfigInput");
                configName.tooltip = "请输入SkillConfig名称";
                configName.value = ""; // 确保初始值为空
                addConfigButton.Add(configName);
                // 创建确认按钮
                Button sureAddConfig = new Button();
                sureAddConfig.text = "+"; // 设置按钮文本
                sureAddConfig.style.fontSize = 20;
                sureAddConfig.AddToClassList("AddConfigButton");
                addConfigButton.Add(sureAddConfig);
                // 创建取消按钮
                Button cancelAddConfig = new Button();
                cancelAddConfig.AddToClassList("AddConfigButton");
                cancelAddConfig.text = "X"; // 设置取消按钮文本
                addConfigButton.Add(cancelAddConfig);
                // 确认按钮事件
                sureAddConfig.clicked += () =>
                {
                    // TODO: 创建配置文件
                    Debug.Log("创建配置文件");
                };

                // 取消按钮事件
                cancelAddConfig.clicked += () =>
                {
                    addConfigButton.Remove(configName);
                    addConfigButton.Remove(sureAddConfig);
                    addConfigButton.Remove(cancelAddConfig);
                    addConfigButton.text = "Create Config File";
                };

                // 回车键确认
                configName.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        evt.StopPropagation();
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        evt.StopPropagation();
                    }
                });

                // 聚焦到输入框
                configName.Focus();
            };
            controlButtonContent.Add(addConfigButton);

            visual.Add(controlButtonContent);
        }

        // 创建图像选择区域
        private void CreateSelectImage(ScrollView visual, string titleText)
        {
            VisualElement controlButtonContent = ControlButtonContent(titleText);
            // 选择技能Icon
            Image skillIcon = new Image();
            skillIcon.AddToClassList("SkillIcon");
            controlButtonContent.Add(skillIcon);
            // 创建选择技能Icon按钮
            Button selectIcon = new Button();
            selectIcon.AddToClassList("SelectIcon");
            selectIcon.text = "Select";
            selectIcon.tooltip = "选择技能Icon";
            selectIcon.clicked += () =>
            {
                // TODO: 选择技能Icon
                Debug.Log("选择技能Icon");
            };
            skillIcon.Add(selectIcon);

            visual.Add(controlButtonContent);
        }

        #endregion


        #region  轨道控制区域

        // 创建轨道控制区域
        private void CreateTrackContent(VisualElement visual)
        {
            trackContent = new VisualElement();
            trackContent.AddToClassList("TrackContent");
            visual.Add(trackContent);

            // 创建轨道项控制区域
            CreateTrackItemControlContent(trackContent);
            // 创建轨道控制区域
            CreateTrackControlContent(trackContent);

        }

        // 轨道项控制区域
        private void CreateTrackItemControlContent(VisualElement visual)
        {
            VisualElement trackControlContent = new VisualElement();
            trackControlContent.AddToClassList("TrackItemControlContent");
            visual.Add(trackControlContent);

            // 创建轨道项控制区域
            CreateItemTrackControlButtonContent(trackControlContent);
        }

        // 轨道项控制按钮区域
        private void CreateItemTrackControlButtonContent(VisualElement visual)
        {
            VisualElement controlButtonContent = new VisualElement();
            controlButtonContent.AddToClassList("TrackItemControlButtonContent");
            visual.Add(controlButtonContent);

            // 创建向左移动一帧按钮
            CreateTrackControlButton(controlButtonContent, "<", () =>
            {

            }, "向左移动一帧");

            // 创建暂停按钮
            CreateTrackControlButton(controlButtonContent, "P", () =>
            {

            }, "暂停");

            // 创建播放按钮
            CreateTrackControlButton(controlButtonContent, "S", () =>
            {

            }, "播放");

            // 创建向右移动一帧按钮
            CreateTrackControlButton(controlButtonContent, ">", () =>
            {

            }, "向右移动一帧");

            //创建帧显示区域
            CreateFrameInputContent(controlButtonContent);

        }

        // 轨道项控制按钮
        private void CreateTrackControlButton(VisualElement visual, string buttonText, Action action = null, string description = null)
        {
            Button controlButton = new Button();
            controlButton.AddToClassList("TrackItemControlButton");
            controlButton.text = buttonText;
            controlButton.tooltip = description;
            controlButton.clicked += () => { action?.Invoke(); };
            visual.Add(controlButton);
        }

        // 帧输入显示区域
        private void CreateFrameInputContent(VisualElement visual)
        {
            VisualElement frameInputContent = new VisualElement();
            frameInputContent.AddToClassList("FrameInputContent");
            visual.Add(frameInputContent);

            // 创建当前帧输入
            CreateFrameInput(frameInputContent, ref currentFrameInput, "当前帧");
            currentFrameInput.value = currentFrame;
            currentFrameInput.RegisterValueChangedCallback(evt => UpdateCurrentFrame(evt.newValue));
            // 创建中间分隔符
            Label separatorLabel = new Label();
            separatorLabel.AddToClassList("SeparatorLabel");
            separatorLabel.text = "|";
            frameInputContent.Add(separatorLabel);
            // 创建最大帧输入
            CreateFrameInput(frameInputContent, ref maxFrameInput, "最大帧");
            maxFrameInput.value = maxFrame;
            maxFrameInput.RegisterValueChangedCallback(evt => UpdateMaxFrame(evt.newValue));
        }

        // 创建帧输入
        private void CreateFrameInput(VisualElement visual, ref IntegerField frameInput, string description = null)
        {
            frameInput = new IntegerField();
            frameInput.AddToClassList("FrameInput");
            frameInput.tooltip = description;
            visual.Add(frameInput);
        }

        // 轨道控制区域
        private void CreateTrackControlContent(VisualElement visual)
        {
            trackControlContent = new VisualElement();
            trackControlContent.AddToClassList("TrackControlContent");
            visual.Add(trackControlContent);

            // 绘制时间轴
            CreateTimeLineIMGUI(trackControlContent);
        }

        #region  绘制时间轴

        // 创建时间轴IMGUI
        private void CreateTimeLineIMGUI(VisualElement visual)
        {
            timeLineIMGUI = new VisualElement();
            timeLineIMGUI.AddToClassList("TimeLineIMGUI");

            // 设置时间轴样式 
            timeLineIMGUI.style.width = CalculateTimelineWidth();
            timeLineIMGUI.style.position = Position.Relative;

            // 绘制刻度
            DrawTimelineScale();
            // 注册鼠标事件
            RegisterTimelineMouseEvents();

            visual.Add(timeLineIMGUI);
        }

        // 计算时间轴容器宽度的统一方法 - 添加额外空间(2)避免截断
        private float CalculateTimelineWidth()
        {
            return (maxFrame + 1) * frameUnitWidth;
        }

        // 注册时间轴鼠标事件
        private void RegisterTimelineMouseEvents()
        {
            if (timeLineIMGUI == null) return;

            bool isDragging = false;

            // 鼠标按下事件
            timeLineIMGUI.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) // 左键
                {
                    isDragging = true;
                    timeLineIMGUI.CaptureMouse(); // 捕获鼠标
                    HandleTimelineClick(evt.localMousePosition);
                    evt.StopPropagation();
                }
            });

            // 鼠标拖动事件
            timeLineIMGUI.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (isDragging)
                {
                    HandleTimelineClick(evt.localMousePosition);
                    evt.StopPropagation();
                }
            });

            // 鼠标释放事件
            timeLineIMGUI.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0 && isDragging)
                {
                    isDragging = false;
                    timeLineIMGUI.ReleaseMouse(); // 释放鼠标捕获
                    evt.StopPropagation();
                }
            });

            // 鼠标离开事件（确保拖动状态重置）
            timeLineIMGUI.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    timeLineIMGUI.ReleaseMouse();
                }
            });

            // 添加鼠标滚轮缩放功能
            timeLineIMGUI.RegisterCallback<WheelEvent>(evt =>
            {
                // 按住Ctrl键时进行缩放
                if (evt.ctrlKey)
                {
                    float zoomDelta = evt.delta.y > 0 ? -2f : 2f;
                    SetFrameUnitWidth(frameUnitWidth + zoomDelta);
                    evt.StopPropagation();
                }
            });
        }

        // 处理时间轴点击/拖动
        private void HandleTimelineClick(Vector2 localMousePosition)
        {
            // 计算鼠标位置对应的帧数
            float mouseX = localMousePosition.x;
            float exactFrame = mouseX / frameUnitWidth;

            // 将指示器移动到最近的刻度处
            int nearestFrame = GetNearestTickFrame(exactFrame);

            // 限制在有效范围内
            nearestFrame = Mathf.Clamp(nearestFrame, 0, maxFrame);

            // 更新当前帧
            UpdateCurrentFrame(nearestFrame);
        }

        // 获取最近的刻度帧
        private int GetNearestTickFrame(float exactFrame)
        {
            // 吸附到最近的整数帧
            return Mathf.RoundToInt(exactFrame);
        }

        // 绘制时间轴刻度
        private void DrawTimelineScale()
        {
            if (timeLineIMGUI == null) return;

            // 清空现有内容
            timeLineIMGUI.Clear();

            // 计算显示区域
            float timelineWidth = maxFrame * frameUnitWidth;

            // 起始索引计算
            int startIndex = trackViewContentOffsetX < 1 ? 0 : Mathf.CeilToInt(trackViewContentOffsetX / frameUnitWidth);

            // 计算起始偏移
            float startOffset = 0;
            if (startIndex > 0) startOffset = frameUnitWidth - (trackViewContentOffsetX % frameUnitWidth);

            // 绘制刻度线和标签
            DrawScaleTicks(startOffset, timelineWidth, startIndex);
        }

        // 绘制刻度线和标签
        private void DrawScaleTicks(float startOffset, float timelineWidth, int startIndex)
        {
            int index = startIndex;
            float maxDisplayWidth = Mathf.Min(timelineWidth, maxFrame * frameUnitWidth);

            // 确保至少绘制到最大帧
            for (float i = startOffset; i <= maxDisplayWidth || index <= maxFrame; i += frameUnitWidth)
            {
                // 如果超出显示范围但还没到最大帧，继续
                if (i > maxDisplayWidth && index < maxFrame)
                {
                    i = maxFrame * frameUnitWidth;
                    index = maxFrame;
                }

                // 如果索引超过最大帧，停止
                if (index > maxFrame) break;

                // 修改主刻度判断：每majorTickInterval帧一个主刻度
                bool isMajorTick = index % majorTickInterval == 0;

                // 创建刻度线
                CreateTickLine(i, isMajorTick);

                // 创建标签（仅主刻度或最大帧）
                if (isMajorTick || index == maxFrame)
                {
                    CreateTickLabel(i, index);
                }

                index++;

                // 防止无限循环
                if (i > maxDisplayWidth && index > maxFrame) break;
            }
        }

        // 创建单个刻度线
        private void CreateTickLine(float xPosition, bool isMajorTick)
        {
            VisualElement tickLine = new VisualElement();
            tickLine.AddToClassList("Timeline-tick");
            // 如果是主刻度，添加major类
            if (isMajorTick) tickLine.AddToClassList("major");
            // 只设置位置，其他样式通过USS控制
            tickLine.style.left = xPosition;
            timeLineIMGUI.Add(tickLine);
        }

        // 创建刻度标签
        private void CreateTickLabel(float xPosition, int frameIndex)
        {
            Label tickLabel = new Label();
            tickLabel.text = frameIndex.ToString();
            tickLabel.AddToClassList("Tick-label");
            // 帧居中
            tickLabel.style.left = xPosition - 9;
            timeLineIMGUI.Add(tickLabel);
        }

        // 绘制当前帧指示器
        private void DrawCurrentFrameIndicator()
        {
            if (currentFrame < 0 || currentFrame > maxFrame) return;

            // 清理现有指示器 - 在正确的容器中查找
            var existingIndicator = trackControlContent?.Q("Current-frame-indicator");
            existingIndicator?.RemoveFromHierarchy();

            VisualElement indicator = new VisualElement();
            indicator.name = "Current-frame-indicator";
            indicator.AddToClassList("Current-frame-indicator");

            // 设置位置和偏移
            float xPosition = currentFrame * frameUnitWidth;
            indicator.style.left = xPosition - 1;

            trackControlContent.Add(indicator);
        }

        // 更新当前帧
        public void UpdateCurrentFrame(int frame)
        {
            int oldFrame = currentFrame;
            currentFrame = Mathf.Clamp(frame, 0, maxFrame);

            // 更新输入框显示
            if (currentFrameInput != null)
            {
                currentFrameInput.SetValueWithoutNotify(currentFrame);
            }

            // 更新指示器位置
            UpdateFrameIndicatorPosition();

            // 如果帧数改变，触发事件
            if (oldFrame != currentFrame)
            {
                OnCurrentFrameChanged?.Invoke(currentFrame);
            }
        }

        // 更新帧指示器位置
        private void UpdateFrameIndicatorPosition()
        {
            // 在正确的容器中查找指示器
            var indicator = trackControlContent?.Q("Current-frame-indicator");
            if (indicator != null)
            {
                float xPosition = currentFrame * frameUnitWidth;
                indicator.style.left = xPosition - 1;
            }
        }

        // 更新最大帧数
        public void UpdateMaxFrame(int frame)
        {
            maxFrame = Mathf.Max(1, frame);
            if (maxFrameInput != null)
                maxFrameInput.value = maxFrame;

            // 更新时间轴宽度并重新绘制
            if (timeLineIMGUI != null)
            {
                timeLineIMGUI.style.width = CalculateTimelineWidth();
                DrawTimelineScale();
            }
        }

        // 设置帧单位宽度（用于缩放）
        public void SetFrameUnitWidth(float width)
        {
            frameUnitWidth = Mathf.Clamp(width, 10f, 50f);
            if (timeLineIMGUI != null)
            {
                timeLineIMGUI.style.width = CalculateTimelineWidth();
                DrawTimelineScale();
                // 缩放后更新指示器位置
                UpdateFrameIndicatorPosition();
            }
        }

        #endregion

        private void CreateTrackItem(VisualElement visual)
        {

        }

        private void CreateTrack(VisualElement visual)
        {

        }

        #endregion

        #endregion

        #region  通用绘制

        // 创建全局控制按钮
        private void CreateGlobalControlButton(VisualElement visual, string buttonText, out Button controlButton, Action action = null, string description = null)
        {
            VisualElement controlButtonContent = new VisualElement();
            controlButtonContent.AddToClassList("ControlButtonContent");
            // 按钮
            controlButton = new Button();
            controlButton.AddToClassList("Field");
            controlButton.text = buttonText;
            controlButton.tooltip = description;
            controlButton.clicked += () => { action?.Invoke(); };
            controlButtonContent.Add(controlButton);

            visual.Add(controlButtonContent);
        }

        // 创建ObjectField选择区域
        private void CreateObjectField(VisualElement visual, string titleText, ref ObjectField objectField, Type type, Action action = null)
        {
            VisualElement controlButtonContent = ControlButtonContent(titleText);
            // 对象选择区域
            objectField = new ObjectField();
            objectField.AddToClassList("Field");
            if (type != null && typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                objectField.objectType = type;
            }
            objectField.RegisterValueChangedCallback((e) => { action?.Invoke(); });
            controlButtonContent.Add(objectField);

            visual.Add(controlButtonContent);
        }

        // 创建inputField选择区域
        private void CreateInputField(VisualElement visual, string titleText, ref TextField inputField, Action action = null)
        {
            VisualElement controlButtonContent = ControlButtonContent(titleText);

            // 文本输入框
            inputField = new TextField();
            inputField.AddToClassList("Field");
            inputField.tooltip = titleText;
            inputField.RegisterValueChangedCallback((e) => { action?.Invoke(); });
            controlButtonContent.Add(inputField);

            visual.Add(controlButtonContent);
        }

        // 创建控制按钮区域
        private VisualElement ControlButtonContent(string titleText)
        {
            VisualElement controlButtonContent = new VisualElement();
            controlButtonContent.AddToClassList("ControlButtonContent");
            // title
            Label title = new Label();
            title.AddToClassList("ControlButtonContentTitle");
            title.text = titleText;
            controlButtonContent.Add(title);
            return controlButtonContent;
        }

        #endregion

    }
}