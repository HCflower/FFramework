using UnityEngine.UIElements;
using UnityEngine;
using System.Linq;

namespace SkillEditor
{
    /// <summary>
    /// 游戏物体轨道项
    /// 专门处理游戏物体轨道项的显示、交互和数据管理
    /// 提供游戏物体特有的预制体管理和Inspector数据功能
    /// </summary>
    public class GameObjectTrackItem : TrackItemViewBase
    {
        #region 私有字段

        /// <summary>轨道项持续帧数</summary>
        private int durationFrame;

        /// <summary>轨道索引，用于多轨道数据定位</summary>
        private int trackIndex;

        /// <summary>当前游戏物体轨道项数据对象</summary>
        private GameObjectTrackItemData currentGameObjectData;

        #endregion

        #region 构造函数

        /// <summary>
        /// 游戏物体轨道项构造函数
        /// 创建并初始化游戏物体轨道项的UI结构、样式和拖拽事件
        /// </summary>
        /// <param name="visual">父容器，轨道项将添加到此容器中</param>
        /// <param name="title">轨道项显示标题</param>
        /// <param name="durationFrame">轨道项持续帧数，影响宽度显示</param>
        /// <param name="startFrame">轨道项的起始帧位置，默认为0</param>
        /// <param name="trackIndex">轨道索引，用于多轨道数据定位，默认为0</param>
        public GameObjectTrackItem(VisualElement visual, string title, int durationFrame, int startFrame = 0, int trackIndex = 0)
        {
            this.durationFrame = durationFrame;
            this.startFrame = startFrame;
            this.trackIndex = trackIndex;

            // 创建并配置轨道项容器
            InitializeGameObjectTrackItem();

            // 创建轨道项内容
            itemContent = CreateGameObjectTrackItemContent(title);
            trackItem.Add(itemContent);

            // 设置宽度和位置
            SetWidth();
            UpdatePosition();
            visual.Add(trackItem);

            // 注册拖拽事件
            RegisterDragEvents();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取当前游戏物体轨道项的数据对象
        /// </summary>
        public GameObjectTrackItemData GameObjectData
        {
            get
            {
                if (currentGameObjectData == null)
                {
                    currentGameObjectData = CreateGameObjectTrackItemData();
                }
                return currentGameObjectData;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置轨道项的起始帧位置
        /// 根据帧位置和当前帧单位宽度计算实际的像素位置
        /// </summary>
        /// <param name="frame">起始帧位置</param>
        public override void SetStartFrame(int frame)
        {
            startFrame = frame;
            UpdatePosition();
        }

        /// <summary>
        /// 更新轨道项的位置
        /// 根据起始帧位置和当前帧单位宽度重新计算像素位置
        /// </summary>
        public override void UpdatePosition()
        {
            float pixelPosition = startFrame * SkillEditorData.FrameUnitWidth;
            trackItem.style.left = pixelPosition;
        }

        /// <summary>
        /// 根据帧数和单位宽度设置轨道项宽度
        /// 宽度会根据SkillEditorData中的帧单位宽度动态计算
        /// </summary>
        public override void SetWidth()
        {
            itemContent.style.width = durationFrame * SkillEditorData.FrameUnitWidth;
        }

        /// <summary>
        /// 更新轨道项的帧数，并重新计算宽度
        /// </summary>
        /// <param name="newFrameCount">新的帧数</param>
        public override void UpdateFrameCount(int newFrameCount)
        {
            durationFrame = newFrameCount;
            SetWidth();
        }

        /// <summary>
        /// 获取轨道项的起始帧位置
        /// </summary>
        /// <returns>起始帧位置</returns>
        public float GetStartFrame()
        {
            return startFrame;
        }

        /// <summary>
        /// 获取轨道项的结束帧位置
        /// </summary>
        /// <returns>结束帧位置</returns>
        public float GetEndFrame()
        {
            return startFrame + durationFrame;
        }

        /// <summary>
        /// 刷新轨道项的显示
        /// 更新宽度和位置以适应缩放变化
        /// </summary>
        public override void RefreshDisplay()
        {
            SetWidth();
            UpdatePosition();
        }

        #endregion

        #region 基类重写方法

        /// <summary>
        /// 游戏物体轨道项被选中时的处理
        /// 选中轨道项到Inspector面板
        /// </summary>
        protected override void OnTrackItemSelected()
        {
            SelectGameObjectTrackItemInInspector();
        }

        /// <summary>
        /// 起始帧发生变化时的处理
        /// 更新数据对象中的起始帧值
        /// </summary>
        /// <param name="newStartFrame">新的起始帧</param>
        protected override void OnStartFrameChanged(int newStartFrame)
        {
            // 只更新数据对象，不调用刷新（避免拖拽时频繁刷新）
            if (currentGameObjectData != null)
            {
                currentGameObjectData.startFrame = newStartFrame;
            }
        }

        /// <summary>
        /// 拖拽完成时的处理
        /// 更新Inspector显示
        /// </summary>
        protected override void OnDragCompleted()
        {
            if (currentGameObjectData != null)
            {
                UpdateInspectorPanel();
            }
        }

        #endregion

        #region 私有初始化方法

        /// <summary>
        /// 初始化游戏物体轨道项容器和基础样式
        /// </summary>
        private void InitializeGameObjectTrackItem()
        {
            trackItem = new VisualElement();
            trackItem.styleSheets.Add(Resources.Load<StyleSheet>("USS/SkillEditorTrackStyle"));
        }

        #endregion

        #region UI内容创建方法

        /// <summary>
        /// 创建游戏物体轨道项的内容容器
        /// 应用游戏物体轨道特定的样式类并添加标题标签
        /// </summary>
        /// <param name="title">轨道项显示标题</param>
        /// <returns>配置完成的游戏物体轨道项内容容器</returns>
        private VisualElement CreateGameObjectTrackItemContent(string title)
        {
            VisualElement itemContent = new VisualElement();
            itemContent.AddToClassList("TrackItemContent");
            itemContent.AddToClassList("TrackItem-GameObject"); // 游戏物体轨道特定样式
            itemContent.tooltip = title;

            // 添加标题标签
            AddTitleLabel(itemContent, title);

            return itemContent;
        }

        /// <summary>
        /// 为游戏物体轨道项内容添加标题标签
        /// </summary>
        /// <param name="itemContent">内容容器</param>
        /// <param name="title">标题文本</param>
        private void AddTitleLabel(VisualElement itemContent, string title)
        {
            Label titleLabel = new Label();
            titleLabel.AddToClassList("TrackItemTitle");
            titleLabel.text = title;
            itemContent.Add(titleLabel);
        }

        #endregion

        #region Inspector数据管理方法

        /// <summary>
        /// 在Inspector面板中选中当前游戏物体轨道项
        /// 创建游戏物体轨道项数据并设置为选中对象
        /// </summary>
        private void SelectGameObjectTrackItemInInspector()
        {
            if (currentGameObjectData == null)
            {
                currentGameObjectData = CreateGameObjectTrackItemData();
            }
            UnityEditor.Selection.activeObject = currentGameObjectData;
        }

        /// <summary>
        /// 强制刷新Inspector面板，确保数据变更后立即显示
        /// </summary>
        private void UpdateInspectorPanel()
        {
            if (currentGameObjectData != null)
            {
                // 同步轨道项的起始帧位置到数据对象
                currentGameObjectData.startFrame = startFrame;

                // 标记对象为脏状态，这会触发属性绑定的更新
                UnityEditor.EditorUtility.SetDirty(currentGameObjectData);
            }
        }

        /// <summary>
        /// 创建游戏物体轨道项的数据对象
        /// </summary>
        /// <returns>游戏物体轨道项数据对象</returns>
        private GameObjectTrackItemData CreateGameObjectTrackItemData()
        {
            string itemName = GetGameObjectTrackItemName();

            var gameObjectData = ScriptableObject.CreateInstance<GameObjectTrackItemData>();
            gameObjectData.trackItemName = itemName;
            gameObjectData.frameCount = durationFrame;
            gameObjectData.startFrame = startFrame;
            gameObjectData.trackIndex = trackIndex; // 设置轨道索引用于多轨道数据定位
            gameObjectData.durationFrame = durationFrame;

            // 设置游戏物体特有的默认属性
            SetDefaultGameObjectProperties(gameObjectData);

            // 从技能配置同步游戏物体数据
            SyncWithGameObjectConfigData(gameObjectData, itemName);

            return gameObjectData;
        }

        /// <summary>
        /// 获取游戏物体轨道项的显示名称
        /// 从标题标签中提取文本内容
        /// </summary>
        /// <returns>轨道项名称</returns>
        private string GetGameObjectTrackItemName()
        {
            // 通过CSS类名查找标签元素
            var titleLabel = itemContent.Q<Label>(className: "TrackItemTitle");
            return titleLabel?.text ?? "";
        }

        /// <summary>
        /// 设置游戏物体数据的默认属性
        /// </summary>
        /// <param name="gameObjectData">要设置的游戏物体数据对象</param>
        private void SetDefaultGameObjectProperties(GameObjectTrackItemData gameObjectData)
        {
            // 默认游戏物体属性
            gameObjectData.prefab = null;
            gameObjectData.autoDestroy = true;
            gameObjectData.positionOffset = Vector3.zero;
            gameObjectData.rotationOffset = Vector3.zero;
            gameObjectData.scale = Vector3.one;
            gameObjectData.useParent = false;
            gameObjectData.parentName = "";
            gameObjectData.destroyDelay = -1f;
        }

        /// <summary>
        /// 从技能配置同步游戏物体数据
        /// </summary>
        /// <param name="gameObjectData">要同步的数据对象</param>
        /// <param name="itemName">轨道项名称</param>
        private void SyncWithGameObjectConfigData(GameObjectTrackItemData gameObjectData, string itemName)
        {
            var skillConfig = SkillEditorData.CurrentSkillConfig;
            if (skillConfig?.trackContainer?.gameObjectTrack == null)
                return;

            // 查找对应轨道索引的游戏物体轨道
            var targetTrack = skillConfig.trackContainer.gameObjectTrack.gameObjectTracks?
                .FirstOrDefault(track => track.trackIndex == trackIndex);

            if (targetTrack?.gameObjectClips == null)
                return;

            // 查找对应的游戏物体片段配置
            var configClip = targetTrack.gameObjectClips
                .FirstOrDefault(clip => clip.clipName == itemName && clip.startFrame == startFrame);

            if (configClip != null)
            {
                // 从配置中恢复游戏物体属性
                gameObjectData.durationFrame = configClip.durationFrame;
                gameObjectData.prefab = configClip.prefab;
                gameObjectData.autoDestroy = configClip.autoDestroy;
                gameObjectData.positionOffset = configClip.positionOffset;
                gameObjectData.rotationOffset = configClip.rotationOffset;
                gameObjectData.scale = configClip.scale;
                gameObjectData.useParent = configClip.useParent;
                gameObjectData.parentName = configClip.parentName;
                gameObjectData.destroyDelay = configClip.destroyDelay;
            }
        }

        #endregion
    }
}
