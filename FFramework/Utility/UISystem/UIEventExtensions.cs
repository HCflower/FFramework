using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

namespace FFramework.Utility
{
    /// <summary>
    /// UI事件绑定静态扩展类
    /// </summary>
    public static class UIEventExtensions
    {
        #region Button事件扩展(带自动追踪)

        /// <summary>
        /// 为Button添加点击事件（通过子物体名称）- 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="buttonName">按钮名称</param>
        /// <param name="action">点击事件</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static Button BindButton(this UIPanel panel, string buttonName, UnityAction action, bool autoTrack = true)
        {
            GameObject buttonObj = UISystem.FindChildGameObject(panel.gameObject, buttonName);
            if (buttonObj == null)
            {
                Debug.LogError($"未找到名为 {buttonName} 的GameObject");
                return null;
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"GameObject {buttonName} 没有Button组件");
                return null;
            }

            button.onClick.AddListener(action);

            // 自动追踪事件用于清理
            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (button != null)
                        button.onClick.RemoveListener(action);
                });
            }

            Debug.Log($"绑定按钮事件: {buttonName}");
            return button;
        }

        /// <summary>
        /// 为Button添加点击事件（直接传入Button）- 带自动追踪
        /// </summary>
        /// <param name="button">按钮</param>
        /// <param name="action">点击事件</param>
        /// <param name="panel">用于追踪的面板（可选）</param>
        public static Button BindClick(this Button button, UnityAction action, UIPanel panel = null)
        {
            if (button == null) return null;
            button.onClick.AddListener(action);

            // 如果提供了面板，自动追踪事件
            if (panel != null)
            {
                panel.AddEventCleanup(() =>
                {
                    if (button != null)
                        button.onClick.RemoveListener(action);
                });
            }

            return button;
        }

        /// <summary>
        /// 移除Button的所有点击事件
        /// </summary>
        /// <param name="button">按钮</param>
        public static Button ClearClick(this Button button)
        {
            if (button == null) return null;
            button.onClick.RemoveAllListeners();
            return button;
        }

        /// <summary>
        /// 移除Button的指定点击事件
        /// </summary>
        /// <param name="button">按钮</param>
        /// <param name="action">要移除的事件</param>
        public static Button UnbindClick(this Button button, UnityAction action)
        {
            if (button == null) return null;
            button.onClick.RemoveListener(action);
            return button;
        }

        #endregion

        #region Toggle事件扩展(带自动追踪)

        /// <summary>
        /// 为Toggle添加值变化事件（通过子物体名称）- 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="toggleName">Toggle名称</param>
        /// <param name="action">值变化事件</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static Toggle BindToggle(this UIPanel panel, string toggleName, UnityAction<bool> action, bool autoTrack = true)
        {
            GameObject toggleObj = UISystem.FindChildGameObject(panel.gameObject, toggleName);
            if (toggleObj == null)
            {
                Debug.LogError($"未找到名为 {toggleName} 的GameObject");
                return null;
            }

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            if (toggle == null)
            {
                Debug.LogError($"GameObject {toggleName} 没有Toggle组件");
                return null;
            }

            toggle.onValueChanged.AddListener(action);

            // 自动追踪事件用于清理
            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (toggle != null)
                        toggle.onValueChanged.RemoveListener(action);
                });
            }

            Debug.Log($"绑定Toggle事件: {toggleName}");
            return toggle;
        }

        /// <summary>
        /// 为Toggle添加值变化事件（直接传入Toggle）- 带自动追踪
        /// </summary>
        /// <param name="toggle">Toggle</param>
        /// <param name="action">值变化事件</param>
        /// <param name="panel">用于追踪的面板（可选）</param>
        public static Toggle BindValueChanged(this Toggle toggle, UnityAction<bool> action, UIPanel panel = null)
        {
            if (toggle == null) return null;
            toggle.onValueChanged.AddListener(action);

            // 如果提供了面板，自动追踪事件
            if (panel != null)
            {
                panel.AddEventCleanup(() =>
                {
                    if (toggle != null)
                        toggle.onValueChanged.RemoveListener(action);
                });
            }

            return toggle;
        }

        /// <summary>
        /// 移除Toggle的所有值变化事件
        /// </summary>
        /// <param name="toggle">Toggle</param>
        public static Toggle ClearValueChanged(this Toggle toggle)
        {
            if (toggle == null) return null;
            toggle.onValueChanged.RemoveAllListeners();
            return toggle;
        }

        /// <summary>
        /// 移除Toggle的指定值变化事件
        /// </summary>
        /// <param name="toggle">Toggle</param>
        /// <param name="action">要移除的事件</param>
        public static Toggle UnbindValueChanged(this Toggle toggle, UnityAction<bool> action)
        {
            if (toggle == null) return null;
            toggle.onValueChanged.RemoveListener(action);
            return toggle;
        }

        #endregion

        #region Slider事件扩展(带自动追踪)

        /// <summary>
        /// 为Slider添加值变化事件（通过子物体名称）- 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="sliderName">Slider名称</param>
        /// <param name="action">值变化事件</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static Slider BindSlider(this UIPanel panel, string sliderName, UnityAction<float> action, bool autoTrack = true)
        {
            GameObject sliderObj = UISystem.FindChildGameObject(panel.gameObject, sliderName);
            if (sliderObj == null)
            {
                Debug.LogError($"未找到名为 {sliderName} 的GameObject");
                return null;
            }

            Slider slider = sliderObj.GetComponent<Slider>();
            if (slider == null)
            {
                Debug.LogError($"GameObject {sliderName} 没有Slider组件");
                return null;
            }

            slider.onValueChanged.AddListener(action);

            // 自动追踪事件用于清理
            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (slider != null)
                        slider.onValueChanged.RemoveListener(action);
                });
            }

            Debug.Log($"绑定Slider事件: {sliderName}");
            return slider;
        }

        /// <summary>
        /// 为Slider添加值变化事件（直接传入Slider）- 带自动追踪
        /// </summary>
        /// <param name="slider">Slider</param>
        /// <param name="action">值变化事件</param>
        /// <param name="panel">用于追踪的面板（可选）</param>
        public static Slider BindValueChanged(this Slider slider, UnityAction<float> action, UIPanel panel = null)
        {
            if (slider == null) return null;
            slider.onValueChanged.AddListener(action);

            // 如果提供了面板，自动追踪事件
            if (panel != null)
            {
                panel.AddEventCleanup(() =>
                {
                    if (slider != null)
                        slider.onValueChanged.RemoveListener(action);
                });
            }

            return slider;
        }

        /// <summary>
        /// 移除Slider的所有值变化事件
        /// </summary>
        /// <param name="slider">Slider</param>
        public static Slider ClearValueChanged(this Slider slider)
        {
            if (slider == null) return null;
            slider.onValueChanged.RemoveAllListeners();
            return slider;
        }

        /// <summary>
        /// 移除Slider的指定值变化事件
        /// </summary>
        /// <param name="slider">Slider</param>
        /// <param name="action">要移除的事件</param>
        public static Slider UnbindValueChanged(this Slider slider, UnityAction<float> action)
        {
            if (slider == null) return null;
            slider.onValueChanged.RemoveListener(action);
            return slider;
        }

        #endregion

        #region InputField事件扩展(带自动追踪)

        /// <summary>
        /// 为InputField添加文本变化事件（通过子物体名称）- 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="inputFieldName">InputField名称</param>
        /// <param name="action">文本变化事件</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static InputField BindInputField(this UIPanel panel, string inputFieldName, UnityAction<string> action, bool autoTrack = true)
        {
            GameObject inputObj = UISystem.FindChildGameObject(panel.gameObject, inputFieldName);
            if (inputObj == null)
            {
                Debug.LogError($"未找到名为 {inputFieldName} 的GameObject");
                return null;
            }

            InputField inputField = inputObj.GetComponent<InputField>();
            if (inputField == null)
            {
                Debug.LogError($"GameObject {inputFieldName} 没有InputField组件");
                return null;
            }

            inputField.onValueChanged.AddListener(action);

            // 自动追踪事件用于清理
            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (inputField != null)
                        inputField.onValueChanged.RemoveListener(action);
                });
            }

            Debug.Log($"绑定InputField事件: {inputFieldName}");
            return inputField;
        }

        /// <summary>
        /// 为InputField添加结束编辑事件 - 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="inputFieldName">InputField名称</param>
        /// <param name="action">结束编辑事件</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static InputField BindInputFieldEndEdit(this UIPanel panel, string inputFieldName, UnityAction<string> action, bool autoTrack = true)
        {
            GameObject inputObj = UISystem.FindChildGameObject(panel.gameObject, inputFieldName);
            if (inputObj == null)
            {
                Debug.LogError($"未找到名为 {inputFieldName} 的GameObject");
                return null;
            }

            InputField inputField = inputObj.GetComponent<InputField>();
            if (inputField == null)
            {
                Debug.LogError($"GameObject {inputFieldName} 没有InputField组件");
                return null;
            }

            inputField.onEndEdit.AddListener(action);

            // 自动追踪事件用于清理
            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (inputField != null)
                        inputField.onEndEdit.RemoveListener(action);
                });
            }

            Debug.Log($"绑定InputField结束编辑事件: {inputFieldName}");
            return inputField;
        }

        /// <summary>
        /// 为InputField添加值变化事件（直接传入InputField）- 带自动追踪
        /// </summary>
        /// <param name="inputField">InputField</param>
        /// <param name="action">值变化事件</param>
        /// <param name="panel">用于追踪的面板（可选）</param>
        public static InputField BindValueChanged(this InputField inputField, UnityAction<string> action, UIPanel panel = null)
        {
            if (inputField == null) return null;
            inputField.onValueChanged.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() =>
                {
                    if (inputField != null)
                        inputField.onValueChanged.RemoveListener(action);
                });
            }

            return inputField;
        }

        /// <summary>
        /// 为InputField添加结束编辑事件（直接传入InputField）- 带自动追踪
        /// </summary>
        /// <param name="inputField">InputField</param>
        /// <param name="action">结束编辑事件</param>
        /// <param name="panel">用于追踪的面板（可选）</param>
        public static InputField BindEndEdit(this InputField inputField, UnityAction<string> action, UIPanel panel = null)
        {
            if (inputField == null) return null;
            inputField.onEndEdit.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() =>
                {
                    if (inputField != null)
                        inputField.onEndEdit.RemoveListener(action);
                });
            }

            return inputField;
        }

        /// <summary>
        /// 移除InputField的所有事件
        /// </summary>
        /// <param name="inputField">InputField</param>
        public static InputField ClearAllEvents(this InputField inputField)
        {
            if (inputField == null) return null;
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onEndEdit.RemoveAllListeners();
            return inputField;
        }

        #endregion

        #region Dropdown事件扩展(带自动追踪)

        /// <summary>
        /// 为Dropdown添加值变化事件（通过子物体名称）- 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="dropdownName">Dropdown名称</param>
        /// <param name="action">值变化事件</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static Dropdown BindDropdown(this UIPanel panel, string dropdownName, UnityAction<int> action, bool autoTrack = true)
        {
            GameObject dropdownObj = UISystem.FindChildGameObject(panel.gameObject, dropdownName);
            if (dropdownObj == null)
            {
                Debug.LogError($"未找到名为 {dropdownName} 的GameObject");
                return null;
            }

            Dropdown dropdown = dropdownObj.GetComponent<Dropdown>();
            if (dropdown == null)
            {
                Debug.LogError($"GameObject {dropdownName} 没有Dropdown组件");
                return null;
            }

            dropdown.onValueChanged.AddListener(action);

            // 自动追踪事件用于清理
            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (dropdown != null)
                        dropdown.onValueChanged.RemoveListener(action);
                });
            }

            Debug.Log($"绑定Dropdown事件: {dropdownName}");
            return dropdown;
        }

        /// <summary>
        /// 为Dropdown添加值变化事件（直接传入Dropdown）- 带自动追踪
        /// </summary>
        /// <param name="dropdown">Dropdown</param>
        /// <param name="action">值变化事件</param>
        /// <param name="panel">用于追踪的面板（可选）</param>
        public static Dropdown BindValueChanged(this Dropdown dropdown, UnityAction<int> action, UIPanel panel = null)
        {
            if (dropdown == null) return null;
            dropdown.onValueChanged.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() =>
                {
                    if (dropdown != null)
                        dropdown.onValueChanged.RemoveListener(action);
                });
            }

            return dropdown;
        }

        /// <summary>
        /// 移除Dropdown的所有值变化事件
        /// </summary>
        /// <param name="dropdown">Dropdown</param>
        public static Dropdown ClearValueChanged(this Dropdown dropdown)
        {
            if (dropdown == null) return null;
            dropdown.onValueChanged.RemoveAllListeners();
            return dropdown;
        }

        #endregion

        #region ScrollRect事件扩展(带自动追踪)

        /// <summary>
        /// 为ScrollRect添加滚动事件（通过子物体名称）- 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="scrollRectName">ScrollRect名称</param>
        /// <param name="action">滚动事件</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static ScrollRect BindScrollRect(this UIPanel panel, string scrollRectName, UnityAction<Vector2> action, bool autoTrack = true)
        {
            GameObject scrollObj = UISystem.FindChildGameObject(panel.gameObject, scrollRectName);
            if (scrollObj == null)
            {
                Debug.LogError($"未找到名为 {scrollRectName} 的GameObject");
                return null;
            }

            ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                Debug.LogError($"GameObject {scrollRectName} 没有ScrollRect组件");
                return null;
            }

            scrollRect.onValueChanged.AddListener(action);

            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (scrollRect != null)
                        scrollRect.onValueChanged.RemoveListener(action);
                });
            }

            Debug.Log($"绑定ScrollRect事件: {scrollRectName}");
            return scrollRect;
        }

        /// <summary>
        /// 为ScrollRect添加滚动事件（直接传入ScrollRect）- 带自动追踪
        /// </summary>
        /// <param name="scrollRect">ScrollRect</param>
        /// <param name="action">滚动事件</param>
        /// <param name="panel">用于追踪的面板（可选）</param>
        public static ScrollRect BindValueChanged(this ScrollRect scrollRect, UnityAction<Vector2> action, UIPanel panel = null)
        {
            if (scrollRect == null) return null;
            scrollRect.onValueChanged.AddListener(action);

            if (panel != null)
            {
                panel.AddEventCleanup(() =>
                {
                    if (scrollRect != null)
                        scrollRect.onValueChanged.RemoveListener(action);
                });
            }

            return scrollRect;
        }

        #endregion

        #region 高级事件扩展(带自动追踪)- 带自动追踪

        /// <summary>
        /// 为GameObject添加EventTrigger事件 - 带自动追踪
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="objectName">对象名称</param>
        /// <param name="eventType">事件类型</param>
        /// <param name="action">事件回调</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static EventTrigger BindEventTrigger(this UIPanel panel, string objectName, EventTriggerType eventType, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            GameObject targetObj = UISystem.FindChildGameObject(panel.gameObject, objectName);
            if (targetObj == null)
            {
                Debug.LogError($"未找到名为 {objectName} 的GameObject");
                return null;
            }

            EventTrigger trigger = targetObj.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = targetObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = eventType;
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);

            // 自动追踪事件用于清理
            if (autoTrack)
            {
                panel.AddEventCleanup(() =>
                {
                    if (trigger != null)
                    {
                        var entryToRemove = trigger.triggers.Find(e => e.eventID == eventType);
                        entryToRemove?.callback.RemoveListener(action);
                    }
                });
            }

            Debug.Log($"绑定EventTrigger事件: {objectName} - {eventType}");
            return trigger;
        }

        /// <summary>
        /// 为GameObject添加鼠标进入事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindPointerEnter(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.PointerEnter, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加鼠标离开事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindPointerExit(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.PointerExit, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加拖拽事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindDrag(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.Drag, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加点击事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindPointerClick(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.PointerClick, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加双击事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindPointerDoubleClick(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.PointerClick, (data) =>
            {
                var pointerData = data as PointerEventData;
                if (pointerData != null && pointerData.clickCount == 2)
                {
                    action?.Invoke(data);
                }
            }, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加鼠标按下事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindPointerDown(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.PointerDown, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加鼠标抬起事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindPointerUp(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.PointerUp, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加开始拖拽事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindBeginDrag(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.BeginDrag, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加结束拖拽事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindEndDrag(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.EndDrag, action, autoTrack);
        }

        /// <summary>
        /// 为GameObject添加拖拽放置事件 - 带自动追踪
        /// </summary>
        public static EventTrigger BindDrop(this UIPanel panel, string objectName, UnityAction<BaseEventData> action, bool autoTrack = true)
        {
            return panel.BindEventTrigger(objectName, EventTriggerType.Drop, action, autoTrack);
        }

        #endregion

        #region 批量事件绑定

        /// <summary>
        /// 批量绑定按钮事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="buttonEvents">按钮事件字典（按钮名称 -> 点击事件）</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static void BindButtons(this UIPanel panel, Dictionary<string, UnityAction> buttonEvents, bool autoTrack = true)
        {
            if (buttonEvents == null) return;

            foreach (var kvp in buttonEvents)
            {
                panel.BindButton(kvp.Key, kvp.Value, autoTrack);
            }
        }

        /// <summary>
        /// 批量绑定Toggle事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="toggleEvents">Toggle事件字典（Toggle名称 -> 值变化事件）</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static void BindToggles(this UIPanel panel, Dictionary<string, UnityAction<bool>> toggleEvents, bool autoTrack = true)
        {
            if (toggleEvents == null) return;

            foreach (var kvp in toggleEvents)
            {
                panel.BindToggle(kvp.Key, kvp.Value, autoTrack);
            }
        }

        /// <summary>
        /// 批量绑定Slider事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="sliderEvents">Slider事件字典（Slider名称 -> 值变化事件）</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static void BindSliders(this UIPanel panel, Dictionary<string, UnityAction<float>> sliderEvents, bool autoTrack = true)
        {
            if (sliderEvents == null) return;

            foreach (var kvp in sliderEvents)
            {
                panel.BindSlider(kvp.Key, kvp.Value, autoTrack);
            }
        }

        /// <summary>
        /// 批量绑定InputField事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="inputFieldEvents">InputField事件字典（InputField名称 -> 值变化事件）</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static void BindInputFields(this UIPanel panel, Dictionary<string, UnityAction<string>> inputFieldEvents, bool autoTrack = true)
        {
            if (inputFieldEvents == null) return;

            foreach (var kvp in inputFieldEvents)
            {
                panel.BindInputField(kvp.Key, kvp.Value, autoTrack);
            }
        }

        /// <summary>
        /// 批量绑定Dropdown事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="dropdownEvents">Dropdown事件字典（Dropdown名称 -> 值变化事件）</param>
        /// <param name="autoTrack">是否自动追踪事件用于销毁时清理</param>
        public static void BindDropdowns(this UIPanel panel, Dictionary<string, UnityAction<int>> dropdownEvents, bool autoTrack = true)
        {
            if (dropdownEvents == null) return;

            foreach (var kvp in dropdownEvents)
            {
                panel.BindDropdown(kvp.Key, kvp.Value, autoTrack);
            }
        }

        #endregion

        #region 事件解绑扩展

        /// <summary>
        /// 解绑指定按钮的所有事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="buttonName">按钮名称</param>
        public static void UnbindButton(this UIPanel panel, string buttonName)
        {
            GameObject buttonObj = UISystem.FindChildGameObject(panel.gameObject, buttonName);
            if (buttonObj == null) return;

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                Debug.Log($"解绑按钮事件: {buttonName}");
            }
        }

        /// <summary>
        /// 解绑指定Toggle的所有事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="toggleName">Toggle名称</param>
        public static void UnbindToggle(this UIPanel panel, string toggleName)
        {
            GameObject toggleObj = UISystem.FindChildGameObject(panel.gameObject, toggleName);
            if (toggleObj == null) return;

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                Debug.Log($"解绑Toggle事件: {toggleName}");
            }
        }

        /// <summary>
        /// 解绑指定Slider的所有事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="sliderName">Slider名称</param>
        public static void UnbindSlider(this UIPanel panel, string sliderName)
        {
            GameObject sliderObj = UISystem.FindChildGameObject(panel.gameObject, sliderName);
            if (sliderObj == null) return;

            Slider slider = sliderObj.GetComponent<Slider>();
            if (slider != null)
            {
                slider.onValueChanged.RemoveAllListeners();
                Debug.Log($"解绑Slider事件: {sliderName}");
            }
        }

        /// <summary>
        /// 解绑指定InputField的所有事件
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="inputFieldName">InputField名称</param>
        public static void UnbindInputField(this UIPanel panel, string inputFieldName)
        {
            GameObject inputObj = UISystem.FindChildGameObject(panel.gameObject, inputFieldName);
            if (inputObj == null) return;

            InputField inputField = inputObj.GetComponent<InputField>();
            if (inputField != null)
            {
                inputField.onValueChanged.RemoveAllListeners();
                inputField.onEndEdit.RemoveAllListeners();
                Debug.Log($"解绑InputField事件: {inputFieldName}");
            }
        }

        /// <summary>
        /// 解绑面板下所有UI组件的事件
        /// </summary>
        /// <param name="panel">面板</param>
        public static void UnbindAllEvents(this UIPanel panel)
        {
            if (panel == null) return;

            // 解绑所有Button事件
            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }

            // 解绑所有Toggle事件
            Toggle[] toggles = panel.GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles)
            {
                if (toggle != null)
                    toggle.onValueChanged.RemoveAllListeners();
            }

            // 解绑所有Slider事件
            Slider[] sliders = panel.GetComponentsInChildren<Slider>(true);
            foreach (var slider in sliders)
            {
                if (slider != null)
                    slider.onValueChanged.RemoveAllListeners();
            }

            // 解绑所有InputField事件
            InputField[] inputFields = panel.GetComponentsInChildren<InputField>(true);
            foreach (var inputField in inputFields)
            {
                if (inputField != null)
                {
                    inputField.onValueChanged.RemoveAllListeners();
                    inputField.onEndEdit.RemoveAllListeners();
                }
            }

            // 解绑所有Dropdown事件
            Dropdown[] dropdowns = panel.GetComponentsInChildren<Dropdown>(true);
            foreach (var dropdown in dropdowns)
            {
                if (dropdown != null)
                    dropdown.onValueChanged.RemoveAllListeners();
            }

            // 解绑所有ScrollRect事件
            ScrollRect[] scrollRects = panel.GetComponentsInChildren<ScrollRect>(true);
            foreach (var scrollRect in scrollRects)
            {
                if (scrollRect != null)
                    scrollRect.onValueChanged.RemoveAllListeners();
            }

            // 清理所有EventTrigger事件
            EventTrigger[] eventTriggers = panel.GetComponentsInChildren<EventTrigger>(true);
            foreach (var trigger in eventTriggers)
            {
                if (trigger != null)
                {
                    foreach (var entry in trigger.triggers)
                    {
                        entry.callback.RemoveAllListeners();
                    }
                    trigger.triggers.Clear();
                }
            }

            Debug.Log($"解绑面板 {panel.name} 的所有UI事件");
        }

        #endregion

        #region 链式调用支持

        /// <summary>
        /// 获取子物体的Button组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="buttonName">按钮名称</param>
        /// <returns>Button组件</returns>
        public static Button GetButton(this UIPanel panel, string buttonName)
        {
            GameObject buttonObj = UISystem.FindChildGameObject(panel.gameObject, buttonName);
            return buttonObj?.GetComponent<Button>();
        }

        /// <summary>
        /// 获取子物体的Toggle组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="toggleName">Toggle名称</param>
        /// <returns>Toggle组件</returns>
        public static Toggle GetToggle(this UIPanel panel, string toggleName)
        {
            GameObject toggleObj = UISystem.FindChildGameObject(panel.gameObject, toggleName);
            return toggleObj?.GetComponent<Toggle>();
        }

        /// <summary>
        /// 获取子物体的Slider组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="sliderName">Slider名称</param>
        /// <returns>Slider组件</returns>
        public static Slider GetSlider(this UIPanel panel, string sliderName)
        {
            GameObject sliderObj = UISystem.FindChildGameObject(panel.gameObject, sliderName);
            return sliderObj?.GetComponent<Slider>();
        }

        /// <summary>
        /// 获取子物体的InputField组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="inputFieldName">InputField名称</param>
        /// <returns>InputField组件</returns>
        public static InputField GetInputField(this UIPanel panel, string inputFieldName)
        {
            GameObject inputObj = UISystem.FindChildGameObject(panel.gameObject, inputFieldName);
            return inputObj?.GetComponent<InputField>();
        }

        /// <summary>
        /// 获取子物体的Dropdown组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="dropdownName">Dropdown名称</param>
        /// <returns>Dropdown组件</returns>
        public static Dropdown GetDropdown(this UIPanel panel, string dropdownName)
        {
            GameObject dropdownObj = UISystem.FindChildGameObject(panel.gameObject, dropdownName);
            return dropdownObj?.GetComponent<Dropdown>();
        }

        /// <summary>
        /// 获取子物体的ScrollRect组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="scrollRectName">ScrollRect名称</param>
        /// <returns>ScrollRect组件</returns>
        public static ScrollRect GetScrollRect(this UIPanel panel, string scrollRectName)
        {
            GameObject scrollObj = UISystem.FindChildGameObject(panel.gameObject, scrollRectName);
            return scrollObj?.GetComponent<ScrollRect>();
        }

        /// <summary>
        /// 获取子物体的Image组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="imageName">Image名称</param>
        /// <returns>Image组件</returns>
        public static Image GetImage(this UIPanel panel, string imageName)
        {
            GameObject imageObj = UISystem.FindChildGameObject(panel.gameObject, imageName);
            return imageObj?.GetComponent<Image>();
        }

        /// <summary>
        /// 获取子物体的Text组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="textName">Text名称</param>
        /// <returns>Text组件</returns>
        public static Text GetText(this UIPanel panel, string textName)
        {
            GameObject textObj = UISystem.FindChildGameObject(panel.gameObject, textName);
            return textObj?.GetComponent<Text>();
        }

        /// <summary>
        /// 获取子物体的RawImage组件（支持链式调用）
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="rawImageName">RawImage名称</param>
        /// <returns>RawImage组件</returns>
        public static RawImage GetRawImage(this UIPanel panel, string rawImageName)
        {
            GameObject rawImageObj = UISystem.FindChildGameObject(panel.gameObject, rawImageName);
            return rawImageObj?.GetComponent<RawImage>();
        }

        /// <summary>
        /// 获取子物体的任意组件（支持链式调用）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="panel">父面板</param>
        /// <param name="objectName">对象名称</param>
        /// <returns>组件</returns>
        public static T GetComponent<T>(this UIPanel panel, string objectName) where T : Component
        {
            GameObject targetObj = UISystem.FindChildGameObject(panel.gameObject, objectName);
            return targetObj?.GetComponent<T>();
        }

        #endregion

        #region 便捷设置方法

        /// <summary>
        /// 设置Button的可交互状态
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="buttonName">按钮名称</param>
        /// <param name="interactable">是否可交互</param>
        public static Button SetButtonInteractable(this UIPanel panel, string buttonName, bool interactable)
        {
            var button = panel.GetButton(buttonName);
            if (button != null)
            {
                button.interactable = interactable;
            }
            return button;
        }

        /// <summary>
        /// 设置Toggle的值
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="toggleName">Toggle名称</param>
        /// <param name="value">值</param>
        /// <param name="sendCallback">是否发送回调</param>
        public static Toggle SetToggleValue(this UIPanel panel, string toggleName, bool value, bool sendCallback = true)
        {
            var toggle = panel.GetToggle(toggleName);
            if (toggle != null)
            {
                if (sendCallback)
                    toggle.isOn = value;
                else
                    toggle.SetIsOnWithoutNotify(value);
            }
            return toggle;
        }

        /// <summary>
        /// 设置Slider的值
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="sliderName">Slider名称</param>
        /// <param name="value">值</param>
        /// <param name="sendCallback">是否发送回调</param>
        public static Slider SetSliderValue(this UIPanel panel, string sliderName, float value, bool sendCallback = true)
        {
            var slider = panel.GetSlider(sliderName);
            if (slider != null)
            {
                if (sendCallback)
                    slider.value = value;
                else
                    slider.SetValueWithoutNotify(value);
            }
            return slider;
        }

        /// <summary>
        /// 设置InputField的文本
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="inputFieldName">InputField名称</param>
        /// <param name="text">文本</param>
        /// <param name="sendCallback">是否发送回调</param>
        public static InputField SetInputFieldText(this UIPanel panel, string inputFieldName, string text, bool sendCallback = true)
        {
            var inputField = panel.GetInputField(inputFieldName);
            if (inputField != null)
            {
                if (sendCallback)
                    inputField.text = text;
                else
                    inputField.SetTextWithoutNotify(text);
            }
            return inputField;
        }

        /// <summary>
        /// 设置Dropdown的值
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="dropdownName">Dropdown名称</param>
        /// <param name="value">值</param>
        /// <param name="sendCallback">是否发送回调</param>
        public static Dropdown SetDropdownValue(this UIPanel panel, string dropdownName, int value, bool sendCallback = true)
        {
            var dropdown = panel.GetDropdown(dropdownName);
            if (dropdown != null)
            {
                if (sendCallback)
                    dropdown.value = value;
                else
                    dropdown.SetValueWithoutNotify(value);
            }
            return dropdown;
        }

        /// <summary>
        /// 设置Text的文本
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="textName">Text名称</param>
        /// <param name="text">文本</param>
        public static Text SetText(this UIPanel panel, string textName, string text)
        {
            var textComponent = panel.GetText(textName);
            if (textComponent != null)
            {
                textComponent.text = text;
            }
            return textComponent;
        }

        /// <summary>
        /// 设置Image的精灵
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="imageName">Image名称</param>
        /// <param name="sprite">精灵</param>
        public static Image SetImageSprite(this UIPanel panel, string imageName, Sprite sprite)
        {
            var image = panel.GetImage(imageName);
            if (image != null)
            {
                image.sprite = sprite;
            }
            return image;
        }

        /// <summary>
        /// 设置Image的颜色
        /// </summary>
        /// <param name="panel">父面板</param>
        /// <param name="imageName">Image名称</param>
        /// <param name="color">颜色</param>
        public static Image SetImageColor(this UIPanel panel, string imageName, Color color)
        {
            var image = panel.GetImage(imageName);
            if (image != null)
            {
                image.color = color;
            }
            return image;
        }

        #endregion
    }
}