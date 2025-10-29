using System.Collections.Generic;
using FFramework.Architecture;
using System.Collections;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace FFramework.Utility
{
    /// <summary>
    /// 资源加载管理器
    /// 目前只有Resources文件夹加载
    /// </summary>
    public class ResLoad : SingletonMono<ResLoad>
    {
        // 资源缓存字典
        private Dictionary<string, Object> resourceCache = new Dictionary<string, Object>();

        // 正在加载的资源请求
        private Dictionary<string, List<Action<Object>>> loadingRequests = new Dictionary<string, List<Action<Object>>>();

        #region 同步加载

        /// <summary>
        /// 加载资源(同步)
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径（相对于Resources文件夹）</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="instantiate">是否实例化GameObject（仅对GameObject有效）</param>
        /// <returns>加载的资源</returns>
        public T LoadRes<T>(string path, bool useCache = true, bool instantiate = true) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("ResLoad: 资源路径不能为空");
                return null;
            }

            // 检查缓存
            if (useCache && resourceCache.TryGetValue(path, out Object cachedRes))
            {
                if (cachedRes is T)
                {
                    return ProcessResource(cachedRes as T, instantiate);
                }
            }

            // 加载资源
            T res = Resources.Load<T>(path);
            if (res == null)
            {
                Debug.LogError($"ResLoad: 无法加载资源 {path}");
                return null;
            }

            // 缓存资源（GameObject类型根据是否实例化决定是否缓存）
            if (useCache && (!(res is GameObject) || !instantiate))
            {
                resourceCache[path] = res;
            }

            return ProcessResource(res, instantiate);
        }

        /// <summary>
        /// 加载资源(同步) - 非泛型版本
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="type">资源类型</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="instantiate">是否实例化GameObject（仅对GameObject有效）</param>
        /// <returns>加载的资源</returns>
        public Object LoadRes(string path, Type type, bool useCache = true, bool instantiate = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("ResLoad: 资源路径不能为空");
                return null;
            }

            // 检查缓存
            if (useCache && resourceCache.TryGetValue(path, out Object cachedRes))
            {
                if (type.IsAssignableFrom(cachedRes.GetType()))
                {
                    return ProcessResource(cachedRes, instantiate);
                }
            }

            // 加载资源
            Object res = Resources.Load(path, type);
            if (res == null)
            {
                Debug.LogError($"ResLoad: 无法加载资源 {path}");
                return null;
            }

            // 缓存资源
            if (useCache && (!(res is GameObject) || !instantiate))
            {
                resourceCache[path] = res;
            }

            return ProcessResource(res, instantiate);
        }

        #endregion

        #region 异步加载

        /// <summary>
        /// 加载资源(异步协程)
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径（相对于Resources文件夹）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="instantiate">是否实例化GameObject（仅对GameObject有效）</param>
        /// <param name="progress">进度回调</param>
        public void LoadResAsync<T>(string path, Action<T> callback, bool useCache = true, bool instantiate = true, Action<float> progress = null) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("ResLoad: 资源路径不能为空");
                callback?.Invoke(null);
                return;
            }

            if (callback == null)
            {
                Debug.LogError("ResLoad: 回调函数不能为空");
                return;
            }

            // 检查缓存
            if (useCache && resourceCache.TryGetValue(path, out Object cachedRes))
            {
                if (cachedRes is T)
                {
                    callback(ProcessResource(cachedRes as T, instantiate));
                    return;
                }
            }

            // 检查是否正在加载
            string cacheKey = $"{path}_{typeof(T).Name}_{instantiate}";
            if (loadingRequests.ContainsKey(cacheKey))
            {
                // 添加到等待列表
                loadingRequests[cacheKey].Add((res) => callback(res as T));
                return;
            }

            // 开始异步加载
            StartCoroutine(LoadResourceCoroutine<T>(path, callback, useCache, instantiate, progress));
        }

        /// <summary>
        /// 加载资源协程
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="callback">回调函数</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="instantiate">是否实例化GameObject</param>
        /// <param name="progress">进度回调</param>
        /// <returns></returns>
        private IEnumerator LoadResourceCoroutine<T>(string path, Action<T> callback, bool useCache, bool instantiate, Action<float> progress) where T : Object
        {
            string cacheKey = $"{path}_{typeof(T).Name}_{instantiate}";

            // 添加到加载列表
            if (!loadingRequests.ContainsKey(cacheKey))
            {
                loadingRequests[cacheKey] = new List<Action<Object>>();
            }
            loadingRequests[cacheKey].Add((res) => callback(res as T));

            // 开始异步加载
            ResourceRequest request = Resources.LoadAsync<T>(path);

            // 等待加载完成，同时报告进度
            while (!request.isDone)
            {
                progress?.Invoke(request.progress);
                yield return null;
            }

            // 加载完成
            progress?.Invoke(1.0f);
            T res = request.asset as T;

            if (res == null)
            {
                Debug.LogError($"ResLoad: 无法异步加载资源 {path}");
            }
            else
            {
                // 缓存资源
                if (useCache && (!(res is GameObject) || !instantiate))
                {
                    resourceCache[path] = res;
                }
            }

            // 处理资源并回调所有等待的请求
            var callbacks = loadingRequests[cacheKey];
            loadingRequests.Remove(cacheKey);

            foreach (var cb in callbacks)
            {
                try
                {
                    Object processedRes = res != null ? ProcessResource(res, instantiate) : null;
                    cb?.Invoke(processedRes);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ResLoad: 资源加载回调异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 批量异步加载资源
        /// </summary>
        /// <param name="paths">资源路径数组</param>
        /// <param name="callback">完成回调</param>
        /// <param name="progress">进度回调</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="instantiate">是否实例化GameObject</param>
        public void LoadMultipleResAsync(string[] paths, Action<Object[]> callback, Action<float> progress = null, bool useCache = true, bool instantiate = true)
        {
            StartCoroutine(LoadMultipleResourcesCoroutine(paths, callback, progress, useCache, instantiate));
        }

        /// <summary>
        /// 批量加载资源协程
        /// </summary>
        private IEnumerator LoadMultipleResourcesCoroutine(string[] paths, Action<Object[]> callback, Action<float> progress, bool useCache, bool instantiate)
        {
            Object[] results = new Object[paths.Length];
            int completedCount = 0;

            for (int i = 0; i < paths.Length; i++)
            {
                int index = i; // 闭包变量
                LoadResAsync<Object>(paths[i], (res) =>
                {
                    results[index] = res;
                    completedCount++;
                }, useCache, instantiate);
            }

            // 等待所有加载完成
            while (completedCount < paths.Length)
            {
                float currentProgress = (float)completedCount / paths.Length;
                progress?.Invoke(currentProgress);
                yield return null;
            }

            progress?.Invoke(1.0f);
            callback?.Invoke(results);
        }

        #endregion

        #region 资源处理

        /// <summary>
        /// 处理资源（根据instantiate参数决定是否实例化GameObject）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resource">原始资源</param>
        /// <param name="instantiate">是否实例化GameObject</param>
        /// <returns>处理后的资源</returns>
        private T ProcessResource<T>(T resource, bool instantiate = true) where T : Object
        {
            if (resource == null) return null;

            // GameObject根据instantiate参数决定是否实例化
            if (resource is GameObject gameObject)
            {
                if (instantiate)
                {
                    return Instantiate(gameObject) as T;
                }
                else
                {
                    return resource; // 返回原始预制体
                }
            }

            // 其他类型直接返回
            return resource;
        }

        /// <summary>
        /// 处理资源（非泛型版本）
        /// </summary>
        /// <param name="resource">原始资源</param>
        /// <param name="instantiate">是否实例化GameObject</param>
        /// <returns>处理后的资源</returns>
        private Object ProcessResource(Object resource, bool instantiate = true)
        {
            if (resource == null) return null;

            // GameObject根据instantiate参数决定是否实例化
            if (resource is GameObject gameObject)
            {
                if (instantiate)
                {
                    return Instantiate(gameObject);
                }
                else
                {
                    return resource; // 返回原始预制体
                }
            }

            // 其他类型直接返回
            return resource;
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 加载预制体（不实例化，常用于对象池）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>预制体资源</returns>
        public T LoadPrefab<T>(string path, bool useCache = true) where T : Object
        {
            return LoadRes<T>(path, useCache, false);
        }

        /// <summary>
        /// 异步加载预制体（不实例化，常用于对象池）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="callback">回调函数</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="progress">进度回调</param>
        public void LoadPrefabAsync<T>(string path, Action<T> callback, bool useCache = true, Action<float> progress = null) where T : Object
        {
            LoadResAsync<T>(path, callback, useCache, false, progress);
        }

        /// <summary>
        /// 加载GameObject实例（自动实例化）
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>GameObject实例</returns>
        public GameObject LoadGameObject(string path, bool useCache = true)
        {
            return LoadRes<GameObject>(path, useCache, true);
        }

        /// <summary>
        /// 异步加载GameObject实例（自动实例化）
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="callback">回调函数</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <param name="progress">进度回调</param>
        public void LoadGameObjectAsync(string path, Action<GameObject> callback, bool useCache = true, Action<float> progress = null)
        {
            LoadResAsync<GameObject>(path, callback, useCache, true, progress);
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 预加载资源到缓存
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="instantiate">是否实例化GameObject</param>
        public void PreloadResource<T>(string path, bool instantiate = true) where T : Object
        {
            LoadRes<T>(path, true, instantiate);
        }

        /// <summary>
        /// 清理指定资源缓存
        /// </summary>
        /// <param name="path">资源路径</param>
        public void ClearCache(string path)
        {
            if (resourceCache.ContainsKey(path))
            {
                resourceCache.Remove(path);
                Debug.Log($"ResLoad: 清理缓存 {path}");
            }
        }

        /// <summary>
        /// 清理所有资源缓存
        /// </summary>
        public void ClearAllCache()
        {
            int count = resourceCache.Count;
            resourceCache.Clear();
            Debug.Log($"ResLoad: 清理所有缓存，共 {count} 个资源");
        }

        /// <summary>
        /// 获取缓存信息
        /// </summary>
        /// <returns>缓存的资源路径数组</returns>
        public string[] GetCachedPaths()
        {
            string[] paths = new string[resourceCache.Count];
            int index = 0;
            foreach (string path in resourceCache.Keys)
            {
                paths[index++] = path;
            }
            return paths;
        }

        /// <summary>
        /// 获取缓存大小
        /// </summary>
        /// <returns>缓存的资源数量</returns>
        public int GetCacheCount()
        {
            return resourceCache.Count;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>是否存在</returns>
        public bool ResourceExists(string path)
        {
            return Resources.Load(path) != null;
        }

        /// <summary>
        /// 卸载未使用的资源
        /// </summary>
        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
            Debug.Log("ResLoad: 卸载未使用的资源");
        }

        #endregion

        protected override void OnDestroy()
        {
            // 清理所有缓存和加载请求
            ClearAllCache();
            loadingRequests.Clear();
        }
    }
}