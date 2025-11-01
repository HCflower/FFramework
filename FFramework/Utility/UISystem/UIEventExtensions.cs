using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

namespace FFramework.Utility
{
    /// <summary>
    /// UI事件绑定静态扩展类 - 重构版本
    /// </summary>
    public static class UIEventExtensions
    {
        #region 核心绑定方法

        /// <summary>
        /// 通用事件绑定方法
        /// </summary>
        private static T BindEvent<T>(UIPanel panel, string componentName, Action<T> bindAction, Action cleanupAction, bool autoTrack) where T : Component
        {
            GameObject targetObj = UISystem.Instance.FindChildGameObject(panel.gameObject, componentName);
            if (targetObj == null)
            {
                Debug.LogError($"未找到名为 {componentName} 的GameObject");
                return null;
            }

            T component = targetObj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"GameObject {componentName} 没有{typeof(T).Name}组件");
                return null;
            }

            bindAction?.Invoke(component);

            if (autoTrack && panel != null)
            {
                // 包装清理动作，增加安全检查
                panel.AddEventCleanup(() =>
                {
                    try
                    {
                        if (panel != null && panel.gameObject != null)
                        {
                            cleanupAction?.Invoke();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[UIEventExtensions] 清理 {typeof(T).Name}.{componentName} 事件失败: {e.Message}");
                    }
                }, $"{typeof(T).Name}.{componentName}");
            }

            return component;
        }

        #endregion

        #region Button事件扩展

        /// <summary>
        /// 绑定Button点击事件
        /// </summary>
        public static Button BindButton(this UIPanel panel, string buttonName, UnityAction action, bool autoTrack = true)
        {
            return BindEvent<Button>(panel, buttonName,
                button => button.onClick.AddListener(action),
                () =>
                {
                    // 更安全的清理方式
                    try
                    {
                        var button = panel?.GetButton(buttonName);
                        if (button != null && button.onClick != null)
                        {
                            button.onClick.RemoveListener(action);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[UIEventExtensions] 清理Button事件失败: {e.Message}");
                    }
                },
                autoTrack);
        }

        /// <summary>
        /// 绑定Button点击事件（直接传入Button）
        /// </summary>
        public static Button BindClick(this Button button, UnityAction action, UIPanel panel = null)
        {
            if (button == null) return null;

            button.onClick.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() => button?.onClick.RemoveListener(action), "Button");
            }

            return button;
        }

        /// <summary>
        /// 清除Button事件
        /// </summary>
        public static Button ClearClick(this Button button)
        {
            button?.onClick.RemoveAllListeners();
            return button;
        }

        #endregion

        #region Toggle事件扩展

        /// <summary>
        /// 绑定Toggle值变化事件
        /// </summary>
        public static Toggle BindToggle(this UIPanel panel, string toggleName, UnityAction<bool> action, bool autoTrack = true)
        {
            return BindEvent<Toggle>(panel, toggleName,
                toggle => toggle.onValueChanged.AddListener(action),
                () => panel.GetToggle(toggleName)?.onValueChanged.RemoveListener(action),
                autoTrack);
        }

        /// <summary>
        /// 绑定Toggle值变化事件（直接传入Toggle）
        /// </summary>
        public static Toggle BindValueChanged(this Toggle toggle, UnityAction<bool> action, UIPanel panel = null)
        {
            if (toggle == null) return null;

            toggle.onValueChanged.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() => toggle?.onValueChanged.RemoveListener(action), "Toggle");
            }

            return toggle;
        }

        #endregion

        #region Slider事件扩展

        /// <summary>
        /// 绑定Slider值变化事件
        /// </summary>
        public static Slider BindSlider(this UIPanel panel, string sliderName, UnityAction<float> action, bool autoTrack = true)
        {
            return BindEvent<Slider>(panel, sliderName,
                slider => slider.onValueChanged.AddListener(action),
                () => panel.GetSlider(sliderName)?.onValueChanged.RemoveListener(action),
                autoTrack);
        }

        /// <summary>
        /// 绑定Slider值变化事件（直接传入Slider）
        /// </summary>
        public static Slider BindValueChanged(this Slider slider, UnityAction<float> action, UIPanel panel = null)
        {
            if (slider == null) return null;

            slider.onValueChanged.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() => slider?.onValueChanged.RemoveListener(action), "Slider");
            }

            return slider;
        }

        #endregion

        #region InputField事件扩展

        /// <summary>
        /// 绑定InputField值变化事件
        /// </summary>
        public static InputField BindInputField(this UIPanel panel, string inputFieldName, UnityAction<string> action, bool autoTrack = true)
        {
            return BindEvent<InputField>(panel, inputFieldName,
                input => input.onValueChanged.AddListener(action),
                () => panel.GetInputField(inputFieldName)?.onValueChanged.RemoveListener(action),
                autoTrack);
        }

        /// <summary>
        /// 绑定InputField结束编辑事件
        /// </summary>
        public static InputField BindInputFieldEndEdit(this UIPanel panel, string inputFieldName, UnityAction<string> action, bool autoTrack = true)
        {
            return BindEvent<InputField>(panel, inputFieldName,
                input => input.onEndEdit.AddListener(action),
                () => panel.GetInputField(inputFieldName)?.onEndEdit.RemoveListener(action),
                autoTrack);
        }

        /// <summary>
        /// 绑定InputField值变化事件（直接传入InputField）
        /// </summary>
        public static InputField BindValueChanged(this InputField inputField, UnityAction<string> action, UIPanel panel = null)
        {
            if (inputField == null) return null;

            inputField.onValueChanged.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() => inputField?.onValueChanged.RemoveListener(action), "InputField");
            }

            return inputField;
        }

        #endregion

        #region Dropdown事件扩展

        /// <summary>
        /// 绑定Dropdown值变化事件
        /// </summary>
        public static Dropdown BindDropdown(this UIPanel panel, string dropdownName, UnityAction<int> action, bool autoTrack = true)
        {
            return BindEvent<Dropdown>(panel, dropdownName,
                dropdown => dropdown.onValueChanged.AddListener(action),
                () => panel.GetDropdown(dropdownName)?.onValueChanged.RemoveListener(action),
                autoTrack);
        }

        /// <summary>
        /// 绑定Dropdown值变化事件（直接传入Dropdown）
        /// </summary>
        public static Dropdown BindValueChanged(this Dropdown dropdown, UnityAction<int> action, UIPanel panel = null)
        {
            if (dropdown == null) return null;

            dropdown.onValueChanged.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() => dropdown?.onValueChanged.RemoveListener(action), "Dropdown");
            }

            return dropdown;
        }

        #endregion

        #region ScrollRect事件扩展

        /// <summary>
        /// 绑定ScrollRect滚动事件
        /// </summary>
        public static ScrollRect BindScrollRect(this UIPanel panel, string scrollRectName, UnityAction<Vector2> action, bool autoTrack = true)
        {
            return BindEvent<ScrollRect>(panel, scrollRectName,
                scroll => scroll.onValueChanged.AddListener(action),
                () => panel.GetScrollRect(scrollRectName)?.onValueChanged.RemoveListener(action),
                autoTrack);
        }

        #endregion

        #region TMP组件事件扩展

        /// <summary>
        /// 绑定TMP_InputField值变化事件
        /// </summary>
        public static TMP_InputField BindTMPInputField(this UIPanel panel, string inputFieldName, UnityAction<string> action, bool autoTrack = true)
        {
            return BindEvent<TMP_InputField>(panel, inputFieldName,
                input => input.onValueChanged.AddListener(action),
                () => panel.GetTMPInputField(inputFieldName)?.onValueChanged.RemoveListener(action),
                autoTrack);
        }

        /// <summary>
        /// 绑定TMP_Dropdown值变化事件
        /// </summary>
        public static TMP_Dropdown BindTMPDropdown(this UIPanel panel, string dropdownName, UnityAction<int> action, bool autoTrack = true)
        {
            return BindEvent<TMP_Dropdown>(panel, dropdownName,
                dropdown => dropdown.onValueChanged.AddListener(action),
                () => panel.GetTMPDropdown(dropdownName)?.onValueChanged.RemoveListener(action),
                autoTrack);
        }

        #endregion

        #region EventTrigger事件扩展

        /// <summary>
        /// 绑定EventTrigger事件
        /// </summary>
        public static EventTrigger BindEventTrigger(this UIPanel panel, string objectName, EventTriggerType eventType, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            GameObject targetObj = UISystem.Instance.FindChildGameObject(panel.gameObject, objectName);
            if (targetObj == null)
            {
                Debug.LogError($"未找到名为 {objectName} 的GameObject");
                return null;
            }

            EventTrigger trigger = UISystem.Instance.GetOrAddComponent<EventTrigger>(targetObj);

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = eventType
            };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);

            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    var entryToRemove = trigger?.triggers.Find(e => e.eventID == eventType);
                    entryToRemove?.callback.RemoveListener(action);
                }, $"EventTrigger.{objectName}");
            }

            return trigger;
        }

        // 快捷EventTrigger方法
        public static EventTrigger BindPointerEnter(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
            => panel.BindEventTrigger(objectName, EventTriggerType.PointerEnter, action, autoTrack);

        public static EventTrigger BindPointerExit(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
            => panel.BindEventTrigger(objectName, EventTriggerType.PointerExit, action, autoTrack);

        public static EventTrigger BindPointerClick(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
            => panel.BindEventTrigger(objectName, EventTriggerType.PointerClick, action, autoTrack);

        public static EventTrigger BindDrag(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
            => panel.BindEventTrigger(objectName, EventTriggerType.Drag, action, autoTrack);

        #endregion

        #region 批量事件绑定

        /// <summary>
        /// 批量绑定按钮事件
        /// </summary>
        public static void BindButtons(this UIPanel panel, Dictionary<string, UnityAction> buttonEvents, bool autoTrack = true)
        {
            if (buttonEvents == null) return;
            foreach (var kvp in buttonEvents)
            {
                panel.BindButton(kvp.Key, kvp.Value, autoTrack);
            }
        }

        /// <summary>
        /// 批量绑定事件（通用版本）
        /// </summary>
        public static void BindEvents<T>(this UIPanel panel, Dictionary<string, T> events, Func<UIPanel, string, T, bool, Component> bindFunc, bool autoTrack = true)
        {
            if (events == null) return;
            foreach (var kvp in events)
            {
                bindFunc(panel, kvp.Key, kvp.Value, autoTrack);
            }
        }

        #endregion

        #region 组件获取扩展

        /// <summary>
        /// 获取子物体组件（通用版本）
        /// </summary>
        public static T GetComponent<T>(this UIPanel panel, string objectName) where T : Component
        {
            GameObject targetObj = UISystem.Instance.FindChildGameObject(panel.gameObject, objectName);
            return targetObj?.GetComponent<T>();
        }

        // 常用组件获取
        public static Button GetButton(this UIPanel panel, string buttonName) => panel.GetComponent<Button>(buttonName);
        public static Toggle GetToggle(this UIPanel panel, string toggleName) => panel.GetComponent<Toggle>(toggleName);
        public static Slider GetSlider(this UIPanel panel, string sliderName) => panel.GetComponent<Slider>(sliderName);
        public static InputField GetInputField(this UIPanel panel, string inputFieldName) => panel.GetComponent<InputField>(inputFieldName);
        public static Dropdown GetDropdown(this UIPanel panel, string dropdownName) => panel.GetComponent<Dropdown>(dropdownName);
        public static ScrollRect GetScrollRect(this UIPanel panel, string scrollRectName) => panel.GetComponent<ScrollRect>(scrollRectName);
        public static Image GetImage(this UIPanel panel, string imageName) => panel.GetComponent<Image>(imageName);
        public static Text GetText(this UIPanel panel, string textName) => panel.GetComponent<Text>(textName);
        public static RawImage GetRawImage(this UIPanel panel, string rawImageName) => panel.GetComponent<RawImage>(rawImageName);

        // TMP组件获取
        public static TextMeshProUGUI GetTMPText(this UIPanel panel, string textName) => panel.GetComponent<TextMeshProUGUI>(textName);
        public static TMP_InputField GetTMPInputField(this UIPanel panel, string inputFieldName) => panel.GetComponent<TMP_InputField>(inputFieldName);
        public static TMP_Dropdown GetTMPDropdown(this UIPanel panel, string dropdownName) => panel.GetComponent<TMP_Dropdown>(dropdownName);

        #endregion

        #region 便捷设置方法

        /// <summary>
        /// 设置组件属性（通用版本）
        /// </summary>
        public static T SetProperty<T>(this UIPanel panel, string componentName, Action<T> setAction) where T : Component
        {
            T component = panel.GetComponent<T>(componentName);
            setAction?.Invoke(component);
            return component;
        }

        // 常用设置方法
        public static Button SetButtonInteractable(this UIPanel panel, string buttonName, bool interactable)
            => panel.SetProperty<Button>(buttonName, btn => btn.interactable = interactable);

        public static Toggle SetToggleValue(this UIPanel panel, string toggleName, bool value, bool sendCallback = true)
            => panel.SetProperty<Toggle>(toggleName, toggle =>
            {
                if (sendCallback) toggle.isOn = value;
                else toggle.SetIsOnWithoutNotify(value);
            });

        public static Slider SetSliderValue(this UIPanel panel, string sliderName, float value, bool sendCallback = true)
            => panel.SetProperty<Slider>(sliderName, slider =>
            {
                if (sendCallback) slider.value = value;
                else slider.SetValueWithoutNotify(value);
            });

        public static Text SetText(this UIPanel panel, string textName, string text)
            => panel.SetProperty<Text>(textName, textComp => textComp.text = text);

        public static TextMeshProUGUI SetTMPText(this UIPanel panel, string textName, string text)
            => panel.SetProperty<TextMeshProUGUI>(textName, textComp => textComp.text = text);

        public static Image SetImageSprite(this UIPanel panel, string imageName, Sprite sprite)
            => panel.SetProperty<Image>(imageName, img => img.sprite = sprite);

        public static Image SetImageColor(this UIPanel panel, string imageName, Color color)
            => panel.SetProperty<Image>(imageName, img => img.color = color);

        #endregion

        #region 事件解绑扩展

        /// <summary>
        /// 解绑所有UI事件
        /// </summary>
        public static void UnbindAllEvents(this UIPanel panel)
        {
            if (panel == null) return;

            // 使用反射或硬编码方式清理所有事件
            var buttons = panel.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons) btn?.onClick.RemoveAllListeners();

            var toggles = panel.GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles) toggle?.onValueChanged.RemoveAllListeners();

            var sliders = panel.GetComponentsInChildren<Slider>(true);
            foreach (var slider in sliders) slider?.onValueChanged.RemoveAllListeners();

            var inputFields = panel.GetComponentsInChildren<InputField>(true);
            foreach (var input in inputFields)
            {
                input?.onValueChanged.RemoveAllListeners();
                input?.onEndEdit.RemoveAllListeners();
            }

            var dropdowns = panel.GetComponentsInChildren<Dropdown>(true);
            foreach (var dropdown in dropdowns) dropdown?.onValueChanged.RemoveAllListeners();

            var scrollRects = panel.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scroll in scrollRects) scroll?.onValueChanged.RemoveAllListeners();

            // TMP组件
            var tmpInputs = panel.GetComponentsInChildren<TMP_InputField>(true);
            foreach (var input in tmpInputs)
            {
                input?.onValueChanged.RemoveAllListeners();
                input?.onEndEdit.RemoveAllListeners();
            }

            var tmpDropdowns = panel.GetComponentsInChildren<TMP_Dropdown>(true);
            foreach (var dropdown in tmpDropdowns) dropdown?.onValueChanged.RemoveAllListeners();

            var eventTriggers = panel.GetComponentsInChildren<EventTrigger>(true);
            foreach (var trigger in eventTriggers)
            {
                if (trigger?.triggers != null)
                {
                    foreach (var entry in trigger.triggers)
                        entry.callback.RemoveAllListeners();
                    trigger.triggers.Clear();
                }
            }

            Debug.Log($"解绑面板 {panel.name} 的所有UI事件");
        }

        #endregion
    }
}