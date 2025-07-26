using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using FFramework.Kit;

namespace FFramework.Examples
{
    /// <summary>
    /// LoadAssetKit Resources 加载示例
    /// 演示图片和预制件的加载功能
    /// 
    /// 使用说明：
    /// 1. 将此脚本挂载到场景中的GameObject上
    /// 2. 在Inspector中配置UI组件引用：
    ///    - displayImage: 用于显示加载的图片
    ///    - loadSpriteBtn: 图片加载按钮
    ///    - loadPrefabBtn: 预制件加载按钮
    ///    - batchLoadBtn: 批量加载按钮
    ///    - clearCacheBtn: 清理缓存按钮
    ///    - statusText: 状态显示文本
    ///    - prefabContainer: 预制件实例化的父对象
    /// 3. 在Resources文件夹中准备对应路径的资源文件
    /// 4. 运行场景，点击按钮测试各种加载功能
    /// 
    /// 资源路径要求：
    /// - UI/Icons/player_icon.png
    /// - UI/Icons/enemy_icon.png
    /// - UI/Icons/item_icon.png
    /// - UI/Buttons/button_normal.png
    /// - Prefabs/Player.prefab
    /// - Prefabs/UI/Panel.prefab
    /// - Prefabs/Effects/Explosion.prefab
    /// </summary>
    public class LoadAssetKitExample : MonoBehaviour
    {
        #region 资源路径配置

        /// <summary>
        /// 资源路径常量
        /// </summary>
        private static class ResourcePaths
        {
            // 图片资源路径
            public const string PLAYER_ICON = "UI/Icons/player_icon";
            public const string ENEMY_ICON = "UI/Icons/enemy_icon";
            public const string ITEM_ICON = "UI/Icons/item_icon";
            public const string BUTTON_NORMAL = "UI/Buttons/button_normal";

            // 预制件资源路径
            public const string PLAYER_PREFAB = "Prefabs/Player";
            public const string UI_PANEL_PREFAB = "Prefabs/UI/Panel";
            public const string EFFECT_PREFAB = "Prefabs/Effects/Explosion";
        }

        #endregion

        [Header("UI 组件")]
        public Image displayImage;
        public Button loadSpriteBtn;
        public Button loadPrefabBtn;
        public Button batchLoadBtn;
        public Button clearCacheBtn;
        public Text statusText;
        public Transform prefabContainer; // 用于实例化预制件的父对象

        private CancellationTokenSource cancellationTokenSource;

        void Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            SetupButtonEvents();
            UpdateStatus("LoadAssetKit Resources 示例已初始化");
        }

        void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            LoadAssetKit.ClearCache();
        }

        private void SetupButtonEvents()
        {
            if (loadSpriteBtn != null)
                loadSpriteBtn.onClick.AddListener(() => LoadSpriteExample().Forget());

            if (loadPrefabBtn != null)
                loadPrefabBtn.onClick.AddListener(() => LoadPrefabExample().Forget());

            if (batchLoadBtn != null)
                batchLoadBtn.onClick.AddListener(() => BatchLoadExample().Forget());

            if (clearCacheBtn != null)
                clearCacheBtn.onClick.AddListener(ClearCacheExample);
        }

        /// <summary>
        /// 图片资源加载示例
        /// </summary>
        public async UniTask LoadSpriteExample()
        {
            UpdateStatus("开始加载图片资源...");

            try
            {
                // 示例1: 同步加载图片
                UpdateStatus("同步加载玩家图标...");
                var playerIcon = LoadAssetKit.LoadAssetFromRes<Sprite>(ResourcePaths.PLAYER_ICON);
                if (playerIcon != null && displayImage != null)
                {
                    displayImage.sprite = playerIcon;
                    UpdateStatus("同步加载成功");
                }
                else
                {
                    UpdateStatus("同步加载失败");
                }

                await UniTask.Delay(1000, cancellationToken: cancellationTokenSource.Token);

                // 示例2: 异步加载图片
                UpdateStatus("异步加载敌人图标...");
                var enemyIcon = await LoadAssetKit.LoadAssetFromResAsync<Sprite>(
                    ResourcePaths.ENEMY_ICON, true, cancellationTokenSource.Token);

                if (enemyIcon != null && displayImage != null)
                {
                    displayImage.sprite = enemyIcon;
                    UpdateStatus("异步加载成功");
                }
                else
                {
                    UpdateStatus("异步加载失败");
                }

                await UniTask.Delay(1000, cancellationToken: cancellationTokenSource.Token);

                // 示例3: 加载按钮图片
                UpdateStatus("加载按钮图片...");
                var buttonSprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>(
                    ResourcePaths.BUTTON_NORMAL, true, cancellationTokenSource.Token);

                if (buttonSprite != null && displayImage != null)
                {
                    displayImage.sprite = buttonSprite;
                    UpdateStatus("按钮图片加载成功");
                }
                else
                {
                    UpdateStatus("按钮图片加载失败");
                }
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("图片加载已取消");
            }
            catch (Exception ex)
            {
                UpdateStatus($"图片加载异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 预制件资源加载示例
        /// </summary>
        public async UniTask LoadPrefabExample()
        {
            UpdateStatus("开始加载预制件资源...");

            try
            {
                // 示例1: 同步加载预制件
                UpdateStatus("同步加载玩家预制件...");
                var playerPrefab = LoadAssetKit.LoadAssetFromRes<GameObject>(ResourcePaths.PLAYER_PREFAB);
                if (playerPrefab != null)
                {
                    var instance = Instantiate(playerPrefab, prefabContainer);
                    instance.name = "Player_Sync";
                    UpdateStatus("玩家预制件同步加载成功");
                }
                else
                {
                    UpdateStatus("玩家预制件加载失败");
                }

                await UniTask.Delay(1000, cancellationToken: cancellationTokenSource.Token);

                // 示例2: 异步加载预制件
                UpdateStatus("异步加载UI面板预制件...");
                var uiPanelPrefab = await LoadAssetKit.LoadAssetFromResAsync<GameObject>(
                    ResourcePaths.UI_PANEL_PREFAB, true, cancellationTokenSource.Token);

                if (uiPanelPrefab != null)
                {
                    var instance = Instantiate(uiPanelPrefab, prefabContainer);
                    instance.name = "UIPanel_Async";
                    UpdateStatus("UI面板预制件异步加载成功");
                }
                else
                {
                    UpdateStatus("UI面板预制件加载失败");
                }

                await UniTask.Delay(1000, cancellationToken: cancellationTokenSource.Token);

                // 示例3: 异步加载特效预制件
                UpdateStatus("异步加载特效预制件...");
                var effectPrefab = await LoadAssetKit.LoadAssetFromResAsync<GameObject>(
                    ResourcePaths.EFFECT_PREFAB, true, cancellationTokenSource.Token);

                if (effectPrefab != null)
                {
                    var instance = Instantiate(effectPrefab, prefabContainer);
                    instance.name = "Effect_Async";
                    UpdateStatus("特效预制件异步加载成功");
                }
                else
                {
                    UpdateStatus("特效预制件加载失败");
                }
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("预制件加载已取消");
            }
            catch (Exception ex)
            {
                UpdateStatus($"预制件加载异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量加载示例
        /// </summary>
        public async UniTask BatchLoadExample()
        {
            UpdateStatus("开始批量加载资源...");

            var spritePaths = new string[]
            {
                ResourcePaths.PLAYER_ICON,
                ResourcePaths.ENEMY_ICON,
                ResourcePaths.ITEM_ICON
            };

            try
            {
                // 批量加载图片
                UpdateStatus("批量加载图片资源...");
                for (int i = 0; i < spritePaths.Length; i++)
                {
                    var path = spritePaths[i];
                    UpdateStatus($"加载图片: {path}");

                    var sprite = await LoadAssetKit.LoadAssetFromResAsync<Sprite>(
                        path, true, cancellationTokenSource.Token);

                    if (sprite != null)
                    {
                        if (displayImage != null)
                        {
                            displayImage.sprite = sprite;
                        }
                        Debug.Log($"成功加载图片: {path}");
                    }
                    else
                    {
                        Debug.LogWarning($"加载图片失败: {path}");
                    }

                    await UniTask.Delay(800, cancellationToken: cancellationTokenSource.Token);
                }

                UpdateStatus("批量加载完成");
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("批量加载已取消");
            }
            catch (Exception ex)
            {
                UpdateStatus($"批量加载异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理缓存和实例化对象
        /// </summary>
        public void ClearCacheExample()
        {
            // 清理缓存
            LoadAssetKit.ClearCache();
            UpdateStatus("缓存已清理");

            // 清理显示的图片
            if (displayImage != null)
            {
                displayImage.sprite = null;
            }

            // 清理实例化的预制件
            if (prefabContainer != null)
            {
                for (int i = prefabContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(prefabContainer.GetChild(i).gameObject);
                }
            }

            UpdateStatus("所有资源已清理");
        }

        #region 辅助方法

        private void UpdateStatus(string message)
        {
            Debug.Log($"[LoadAssetKitExample] {message}");
            if (statusText != null)
            {
                statusText.text = $"{DateTime.Now:HH:mm:ss} - {message}";
            }
        }

        #endregion

        #region 编辑器测试方法

        [ContextMenu("测试图片加载")]
        public void TestSpriteLoad()
        {
            LoadSpriteExample().Forget();
        }

        [ContextMenu("测试预制件加载")]
        public void TestPrefabLoad()
        {
            LoadPrefabExample().Forget();
        }

        [ContextMenu("测试批量加载")]
        public void TestBatchLoad()
        {
            BatchLoadExample().Forget();
        }

        [ContextMenu("清理所有资源")]
        public void TestClearAll()
        {
            ClearCacheExample();
        }

        #endregion
    }
}
