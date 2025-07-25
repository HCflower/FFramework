using UnityEngine.UI;
using UnityEngine;

namespace FFramework.Kit.Examples
{
    /// <summary>
    /// EventKit使用示例
    /// </summary>
    public class EventKitExample : MonoBehaviour
    {
        [Header("UI组件引用")]
        public Button testButton;
        public Image testImage;
        public Text testText;
        public GameObject dragObject;

        private void Start()
        {
            SetupBasicEvents();
            SetupAdvancedEvents();
            SetupDragEvents();
        }

        /// <summary>
        /// 基础事件绑定示例
        /// </summary>
        private void SetupBasicEvents()
        {
            // 基础点击事件
            if (testButton != null)
            {
                testButton.BindClick(() => Debug.Log("按钮被点击了！"));

                // 也可以获取事件数据
                testButton.BindClick(eventData =>
                {
                    Debug.Log($"按钮被 {eventData.button} 键点击了！");
                });
            }

            // 图片悬停效果
            if (testImage != null)
            {
                testImage.BindClickWithRaycast(eventData =>
                {
                    Debug.Log("图片被点击了！");
                    testImage.color = Random.ColorHSV();
                });

                testImage.BindHover(
                    () => testImage.color = Color.yellow,
                    () => testImage.color = Color.white
                );
            }

            // 文本点击事件
            if (testText != null)
            {
                testText.BindClick(() =>
                {
                    testText.text = "文本被点击了!时间：" + System.DateTime.Now.ToString("HH:mm:ss");
                });
            }
        }

        /// <summary>
        /// 高级事件绑定示例
        /// </summary>
        private void SetupAdvancedEvents()
        {
            // 创建一个测试对象
            var testObj = new GameObject("TestEventObject");
            testObj.transform.SetParent(transform);

            // 添加Image组件以接收射线检测
            var image = testObj.AddComponent<Image>();
            image.color = Color.blue;

            // 链式调用绑定多个事件
            EventKit.Get(testObj)
                .SetOnPointerClick(eventData =>
                {
                    if (eventData.IsLeftClick())
                        Debug.Log("左键点击");
                    else if (eventData.IsRightClick())
                        Debug.Log("右键点击");
                })
                .SetOnPointerEnter(eventData =>
                {
                    image.color = Color.green;
                    Debug.Log("鼠标进入");
                })
                .SetOnPointerExit(eventData =>
                {
                    image.color = Color.blue;
                    Debug.Log("鼠标离开");
                })
                .SetOnScroll(eventData =>
                {
                    Debug.Log($"滚轮滚动：{eventData.scrollDelta}");
                });
        }

        /// <summary>
        /// 拖拽事件示例
        /// </summary>
        private void SetupDragEvents()
        {
            if (dragObject != null)
            {
                // 基础拖拽
                dragObject.BindDrag(
                    onBeginDrag: eventData => Debug.Log("开始拖拽"),
                    onDrag: eventData => Debug.Log($"拖拽中，位置：{eventData.position}"),
                    onEndDrag: eventData => Debug.Log("结束拖拽")
                );

                // 或者使用DragKit进行高级拖拽
                DragKit.Get(dragObject)
                    .SetDragConfig(enableDrag: true, returnToOriginal: true, returnSpeed: 3f)
                    .SetVisualEffects(scaleOnDrag: true, dragScale: Vector3.one * 1.2f, fadeOnDrag: true, dragAlpha: 0.5f)
                    .SetConstraints(constrainToParent: true, constrainToScreen: true)
                    .SetCallbacks(
                        onBeginDrag: eventData => Debug.Log("DragKit: 开始拖拽"),
                        onDrag: eventData => Debug.Log("DragKit: 拖拽中"),
                        onEndDrag: eventData => Debug.Log("DragKit: 结束拖拽")
                    );
            }
        }

        /// <summary>
        /// 运行时动态绑定事件示例
        /// </summary>
        public void DynamicEventBinding()
        {
            // 创建新的UI元素
            var newButton = new GameObject("DynamicButton");
            newButton.transform.SetParent(transform);

            var button = newButton.AddComponent<Button>();
            var image = newButton.AddComponent<Image>();

            // 动态绑定事件
            button.BindClick(() =>
            {
                Debug.Log("动态创建的按钮被点击了！");
                // 可以在这里移除事件
                EventKit.Get(button).ClearAllEvents();
            });
        }

        /// <summary>
        /// 事件数据处理示例
        /// </summary>
        private void EventDataExample()
        {
            if (testImage != null)
            {
                testImage.BindClick(eventData =>
                {
                    // 检查点击类型
                    if (eventData.IsLeftClick())
                    {
                        Debug.Log("左键点击");
                    }
                    else if (eventData.IsRightClick())
                    {
                        Debug.Log("右键点击");
                    }

                    // 获取世界坐标
                    Vector3 worldPos = eventData.GetWorldPosition();
                    Debug.Log($"世界坐标：{worldPos}");

                    // 获取UI坐标
                    Vector2 uiPos = eventData.GetUIPosition(testImage.rectTransform);
                    Debug.Log($"UI坐标：{uiPos}");

                    // 检查点击次数
                    if (eventData.clickCount == 2)
                    {
                        Debug.Log("双击！");
                    }
                });
            }
        }
    }
}
