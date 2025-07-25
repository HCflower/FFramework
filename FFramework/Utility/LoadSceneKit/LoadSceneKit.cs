using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 优化的场景加载工具类
    /// 功能：同步/异步场景切换，自动卸载旧场景，进度监控，事件回调
    /// </summary>
    public static class LoadSceneKit
    {
        // 当前正在进行的异步操作
        private static AsyncOperation currentLoadOp;
        private static AsyncOperation currentUnloadOp;

        // 状态记录
        private static string currentSceneName;
        private static string previousSceneName;
        private static bool isProcessing = false;        // 加载进度(0-1)
        public static float LoadingProgress => currentLoadOp?.progress ?? 0f;

        // 卸载进度(0-1) 
        public static float UnloadingProgress => currentUnloadOp?.progress ?? 0f;

        // 总进度(0-1) - 加载50% + 卸载50%
        public static float TotalProgress => (LoadingProgress + UnloadingProgress) * 0.5f;

        // 是否正在处理场景切换
        public static bool IsProcessing => isProcessing;

        /// <summary>
        /// 同步切换场景
        /// </summary>
        /// <param name="sceneName">目标场景名称</param>
        /// <param name="onChangeScene">场景切换开始时的回调（用于显示加载面板等）</param>
        /// <param name="onComplete">完成回调(是否成功)</param>
        public static void LoadScene(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)
        {
            if (isProcessing)
            {
                Debug.LogWarning("场景正在切换中，请等待当前操作完成！");
                onComplete?.Invoke(false);
                return;
            }

            try
            {
                // 触发场景切换事件
                onChangeScene?.Invoke();

                // 记录当前场景
                previousSceneName = SceneManager.GetActiveScene().name;

                // 同步加载新场景
                SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

                // 设置新场景为激活状态
                Scene newScene = SceneManager.GetSceneByName(sceneName);
                if (newScene.IsValid())
                {
                    SceneManager.SetActiveScene(newScene);
                    currentSceneName = sceneName;

                    // 卸载旧场景
                    if (!string.IsNullOrEmpty(previousSceneName) && previousSceneName != sceneName)
                    {
                        SceneManager.UnloadSceneAsync(previousSceneName);
                    }

                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"场景 {sceneName} 加载失败！");
                    onComplete?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"场景切换失败: {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        /// <summary>
        /// 异步切换场景
        /// </summary>
        /// <param name="sceneName">目标场景名称</param>
        /// <param name="onChangeScene">场景切换开始时的回调（用于显示加载面板等）</param>
        /// <param name="onComplete">完成回调(是否成功)</param>
        public static void LoadSceneAsync(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)
        {
            if (isProcessing)
            {
                Debug.LogWarning("场景正在切换中，请等待当前操作完成！");
                onComplete?.Invoke(false);
                return;
            }

            // 使用UniTask执行异步切换
            LoadSceneAsyncTask(sceneName, onChangeScene, onComplete).Forget();
        }

        /// <summary>
        /// 异步切换场景 - UniTask版本
        /// </summary>
        /// <param name="sceneName">目标场景名称</param>
        /// <param name="onChangeScene">场景切换开始时的回调</param>
        /// <param name="onComplete">完成回调</param>
        public static async UniTask<bool> LoadSceneAsyncTask(string sceneName, Action onChangeScene = null, Action<bool> onComplete = null)
        {
            if (isProcessing)
            {
                Debug.LogWarning("场景正在切换中，请等待当前操作完成！");
                onComplete?.Invoke(false);
                return false;
            }

            isProcessing = true;
            bool success = false;

            try
            {
                // 触发场景切换事件
                onChangeScene?.Invoke();

                // 记录当前场景
                previousSceneName = SceneManager.GetActiveScene().name;

                // 第一阶段：异步加载新场景
                currentLoadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (currentLoadOp == null)
                {
                    Debug.LogError($"无法开始加载场景: {sceneName}");
                    isProcessing = false;
                    onComplete?.Invoke(false);
                    return false;
                }

                currentLoadOp.allowSceneActivation = false;

                // 等待加载到90%
                await UniTask.WaitUntil(() => currentLoadOp.progress >= 0.9f);

                // 激活新场景
                currentLoadOp.allowSceneActivation = true;

                // 等待加载完全完成
                await currentLoadOp.ToUniTask();

                // 设置新场景为激活状态
                Scene newScene = SceneManager.GetSceneByName(sceneName);
                if (newScene.IsValid())
                {
                    SceneManager.SetActiveScene(newScene);
                    currentSceneName = sceneName;

                    // 第二阶段：卸载旧场景
                    if (!string.IsNullOrEmpty(previousSceneName) && previousSceneName != sceneName)
                    {
                        currentUnloadOp = SceneManager.UnloadSceneAsync(previousSceneName);

                        if (currentUnloadOp != null)
                        {
                            // 等待卸载完成
                            await currentUnloadOp.ToUniTask();
                        }
                    }

                    success = true;
                }
                else
                {
                    Debug.LogError($"场景 {sceneName} 加载失败！");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"异步场景切换失败: {e.Message}");
            }
            finally
            {
                // 清理操作引用
                currentLoadOp = null;
                currentUnloadOp = null;
                isProcessing = false;

                onComplete?.Invoke(success);
            }

            return success;
        }        /// <summary>
                 /// 获取当前场景名称
                 /// </summary>
        public static string GetCurrentSceneName()
        {
            return currentSceneName ?? SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// 获取加载阶段进度(0-0.5)
        /// </summary>
        public static float GetLoadProgress()
        {
            return LoadingProgress * 0.5f;
        }

        /// <summary>
        /// 获取卸载阶段进度(0-0.5)
        /// </summary>
        public static float GetUnloadProgress()
        {
            return UnloadingProgress * 0.5f;
        }

        /// <summary>
        /// 获取总进度(0-1)
        /// </summary>
        public static float GetTotalProgress()
        {
            return TotalProgress;
        }

        /// <summary>
        /// 获取进度详情
        /// </summary>
        public static (float load, float unload, float total) GetProgressDetails()
        {
            return (LoadingProgress, UnloadingProgress, TotalProgress);
        }
    }
}
