using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 场景加载工具类
    /// </summary>
    public static class LoadSceneKit
    {
        // 当前正在进行的异步操作
        private static AsyncOperation currentAsyncOp;

        // 加载进度(0-1)
        public static float LoadingProgress => currentAsyncOp?.progress ?? 0;

        // 是否正在加载场景
        public static bool IsLoading => currentAsyncOp != null && !currentAsyncOp.isDone;

        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="mode">加载模式</param>
        public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"The scene is loading asynchronously, please do not call synchronous loading at the same time!");
                return;
            }

            SceneManager.LoadScene(sceneName, mode);
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="onComplete">加载完成回调</param>
        /// <param name="mode">加载模式</param>
        /// <param name="allowActivation">是否允许立即激活场景</param>
        public static void LoadSceneAsync(string sceneName, Action onComplete = null,
            LoadSceneMode mode = LoadSceneMode.Single, bool allowActivation = false)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"The scene is already loading, please wait for the current load to complete!");
                return;
            }

            // 使用协程执行异步加载
            CoroutineRunner.StartStaticCoroutine(LoadSceneAsyncCoroutine(sceneName, onComplete, mode, allowActivation));
        }

        private static IEnumerator LoadSceneAsyncCoroutine(string sceneName, Action onComplete,
            LoadSceneMode mode, bool allowActivation)
        {
            currentAsyncOp = SceneManager.LoadSceneAsync(sceneName, mode);
            currentAsyncOp.allowSceneActivation = allowActivation;

            // 等待加载完成
            while (!currentAsyncOp.isDone)
            {
                // 如果不允许立即激活，需要手动检查进度
                if (!allowActivation && currentAsyncOp.progress >= 0.9f)
                {
                    // 此时可以调用 AllowActivation() 来激活场景
                    AllowActivation();
                    break;
                }
                yield return null;
            }

            currentAsyncOp = null;
            onComplete?.Invoke();
        }

        //允许激活已加载的场景(当allowActivation=false时使用)
        public static void AllowActivation()
        {
            if (currentAsyncOp != null)
            {
                currentAsyncOp.allowSceneActivation = true;
            }
        }

        /// <summary>
        /// 获取场景加载进度(0-1)
        /// </summary>
        public static float GetLoadingProgress()
        {
            return LoadingProgress;
        }

        /// <summary>
        /// 同步卸载场景（实际上是等待异步卸载完成）
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>是否成功卸载</returns>
        public static bool UnloadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"Loading the scene asynchronously, do not call uninstall at the same time!");
                return false;
            }

            return SceneManager.UnloadSceneAsync(sceneName).isDone;
        }

        /// <summary>
        /// 异步卸载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="onComplete">卸载完成回调</param>
        public static void UnloadSceneAsync(string sceneName, Action<bool> onComplete = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"The scene is already loading, please wait for the current load to complete!");
                onComplete?.Invoke(false);
                return;
            }
            // 使用协程执行异步卸载
            CoroutineRunner.StartStaticCoroutine(UnloadSceneAsyncCoroutine(sceneName, onComplete));
        }

        //卸载场景协程
        private static IEnumerator UnloadSceneAsyncCoroutine(string sceneName, Action<bool> onComplete)
        {
            // 检查场景是否已加载
            Scene sceneToUnload = SceneManager.GetSceneByName(sceneName);
            if (!sceneToUnload.IsValid())
            {
                Debug.LogWarning($"Scene {sceneName} does not exist or is not loaded, cannot be uninstalled!！");
                onComplete?.Invoke(false);
                yield break;
            }

            currentAsyncOp = SceneManager.UnloadSceneAsync(sceneName);

            // 等待卸载完成
            while (currentAsyncOp != null && !currentAsyncOp.isDone)
            {
                yield return null;
            }

            currentAsyncOp = null;
            onComplete?.Invoke(true);
        }
    }
}
