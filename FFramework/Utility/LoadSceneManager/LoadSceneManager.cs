using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace FFramework
{
    public class LoadSceneManager : SingletonMono<LoadSceneManager>
    {
        LoadSceneManager() => IsDontDestroyOnLoad = true;
        public float progress;                                      // 加载进度
        private bool canUnLoadAllScene = false;                     // 是否可以卸载所有场景
        private SceneBase currentScene;                             // 当前场景

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 设置和加载场景
        /// </summary>
        public void SetAndLoadScene(SceneBase scene)
        {
            if (currentScene != null)
                currentScene.OnExit();
            currentScene = scene;
            if (currentScene != null)
                currentScene.OnEnter();
        }

        public void LoadSceneAsync(string sceneName, out bool isSelf)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                isSelf = true;
                return;
            }
            isSelf = false;
            canUnLoadAllScene = false;
            StartCoroutine(LoadSceneAsync(sceneName));
            if (canUnLoadAllScene)
                UnloadScene();
        }

        //开始异步加载场景
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            //等待场景加载完成
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            //输出打印加载进度
            while (!asyncOperation.isDone)
            {
                progress = asyncOperation.progress;
                Debug.Log($"Loading: {progress * 100}%");
                if (asyncOperation.progress >= 0.9f)
                {
                    Scene scene = SceneManager.GetSceneByName(sceneName);
                    SceneManager.SetActiveScene(scene);
                    Debug.Log("Loading Complete");
                    break;
                }
            }
            canUnLoadAllScene = true;
            yield return null;
        }

        //卸载场景
        public void UnloadScene()
        {
            StartCoroutine(UnloadAllScenesCoroutine());
        }

        private IEnumerator UnloadAllScenesCoroutine()
        {
            // 获取已加载的场景数量
            int sceneCount = SceneManager.sceneCount;
            var scenesToUnload = new List<Scene>();

            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != SceneManager.GetActiveScene())
                {
                    scenesToUnload.Add(scene);
                }
            }

            // 逐个卸载存储的场景
            foreach (var scene in scenesToUnload)
            {
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(scene);

                if (asyncUnload == null)
                {
                    continue;
                }
                // 等待卸载完成
                while (!asyncUnload.isDone)
                {
                    yield return null;
                }
            }

            // 所有场景卸载完成
            Debug.Log("All scenes have been unloaded.");
        }
    }
}
