using Cysharp.Threading.Tasks;
using FFramework.Kit;
using UnityEngine;

namespace FFramework.Examples
{
    /// <summary>
    /// LoadSceneKit 简单使用示例
    /// 展示最基本的场景切换用法
    /// </summary>
    public class SimpleLoadSceneExample : MonoBehaviour
    {
        [Header("示例设置")]
        public string sceneToLoad = "GameScene";
        public bool useAsyncLoad = true;
        public bool useUniTaskVersion = true; // 新增：是否使用UniTask版本

        private void Start()
        {
            Debug.Log("SimpleLoadSceneExample 已启动");
            Debug.Log("按 Space 键切换场景");
            Debug.Log("按 U 键使用UniTask版本切换场景");
        }
        private void Update()
        {
            // 按空格键触发场景切换
            if (Input.GetKeyDown(KeyCode.Space))
            {
                LoadTargetScene();
            }

            // 按U键使用UniTask版本切换场景
            if (Input.GetKeyDown(KeyCode.U) && useUniTaskVersion)
            {
                LoadTargetSceneUniTask();
            }

            // 按 P 键显示当前进度
            if (Input.GetKeyDown(KeyCode.P))
            {
                ShowProgress();
            }
        }

        /// <summary>
        /// 加载目标场景
        /// </summary>
        private void LoadTargetScene()
        {
            if (LoadSceneKit.IsProcessing)
            {
                Debug.Log("场景正在切换中，请稍候...");
                return;
            }

            Debug.Log($"开始切换到场景: {sceneToLoad}");

            if (useAsyncLoad)
            {
                // 异步加载
                LoadSceneKit.LoadSceneAsync(sceneToLoad, OnSceneChangeStart, OnSceneLoadComplete);
            }
            else
            {
                // 同步加载
                LoadSceneKit.LoadScene(sceneToLoad, OnSceneChangeStart, OnSceneLoadComplete);
            }
        }

        /// <summary>
        /// 场景切换开始事件
        /// </summary>
        private void OnSceneChangeStart()
        {
            Debug.Log("🔄 场景切换开始！显示加载界面...");

            // 这里可以显示加载面板
            // UIKit.OpenPanel<LoadingPanel>();
        }

        /// <summary>
        /// 使用UniTask版本加载目标场景
        /// </summary>
        private async void LoadTargetSceneUniTask()
        {
            if (LoadSceneKit.IsProcessing)
            {
                Debug.LogWarning("⚠️ 场景正在处理中，请等待...");
                return;
            }

            Debug.Log($"🔄 开始使用UniTask切换场景: {sceneToLoad}");

            try
            {
                await LoadSceneKit.LoadSceneAsyncTask(
                    sceneToLoad,
                    OnSceneChangeStart,
                    OnSceneLoadComplete
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ UniTask场景加载出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 场景加载完成回调
        /// </summary>
        private void OnSceneLoadComplete(bool success)
        {
            if (success)
            {
                Debug.Log($"✅ 场景切换成功！当前场景: {LoadSceneKit.GetCurrentSceneName()}");
            }
            else
            {
                Debug.LogError("❌ 场景切换失败！");
            }

            // 这里可以隐藏加载面板
            // UIKit.ClosePanel<LoadingPanel>();
        }

        /// <summary>
        /// 显示当前进度信息
        /// </summary>
        private void ShowProgress()
        {
            var (load, unload, total) = LoadSceneKit.GetProgressDetails();
            Debug.Log($"📊 进度详情 - 加载: {load:F2}, 卸载: {unload:F2}, 总计: {total:F2}");
            Debug.Log($"🎯 是否处理中: {LoadSceneKit.IsProcessing}");
        }

        private void OnDestroy()
        {
            // 不再需要取消事件注册，因为事件是通过参数传入的
        }
    }
}
