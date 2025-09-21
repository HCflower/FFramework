using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 资源加载工具
    /// </summary>
    public static class LoadAssetKit
    {
        //资产缓存字典
        private static readonly Dictionary<string, UnityEngine.Object> assetCacheDic = new Dictionary<string, UnityEngine.Object>();

        /// <summary>
        /// 卸载指定资源
        /// </summary>
        public static void UnloadAsset(string resPath)
        {
            if (assetCacheDic.TryGetValue(resPath, out var asset))
            {
                if (asset != null) Resources.UnloadAsset(asset);
                assetCacheDic.Remove(resPath);
            }
        }

        /// <summary>
        /// 清理所有缓存资源
        /// </summary>
        public static void ClearCache()
        {
            foreach (var asset in assetCacheDic.Values)
            {
                if (asset != null) Resources.UnloadAsset(asset);
            }
            assetCacheDic.Clear();
        }

        /// <summary>
        /// 从Resource文件夹加载资源 - 同步
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resPath">资源路径</param>
        /// <param name="callback">完成后回调</param>
        public static T LoadAssetFromRes<T>(string resPath, Action<T> callback = null, bool isCache = true) where T : UnityEngine.Object
        {
            // 参数检查
            if (string.IsNullOrEmpty(resPath))
            {
                Debug.LogError("[LoadAssetKit]:The resource path cannot be empty!");
                return HandleResult(null, callback);
            }

            // 检查缓存
            if (assetCacheDic.TryGetValue(resPath, out var cachedAsset))
            {
                return HandleResult(cachedAsset as T, callback);
            }

            // 同步加载模式
            if (callback == null)
            {
                T asset = Resources.Load<T>(resPath);
                if (asset == null)
                {
                    Debug.LogError($"[ResourceLoader]:Resource load failed:{resPath} (Type: {typeof(T)}).");
                    return null;
                }
                if (isCache) assetCacheDic[resPath] = asset;
                return asset;
            }
            // 异步加载模式
            else
            {
                LoadAssetAsyncFromRes(resPath, callback, isCache, CancellationToken.None).Forget();
                return null;
            }
        }

        /// <summary>
        /// 从Resource文件夹异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resPath">资源路径</param>
        /// <param name="isCache">是否缓存</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>加载的资源</returns>
        public static async UniTask<T> LoadAssetFromResAsync<T>(string resPath, bool isCache = true, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            // 参数检查
            if (string.IsNullOrEmpty(resPath))
            {
                Debug.LogError("[LoadAssetKit]:The resource path cannot be empty!");
                return null;
            }

            // 检查缓存
            if (assetCacheDic.TryGetValue(resPath, out var cachedAsset))
            {
                return cachedAsset as T;
            }

            var asset = await LoadAssetAsyncFromRes<T>(resPath, null, isCache, cancellationToken);
            return asset;
        }

        // 异步加载协程
        private static async UniTask<T> LoadAssetAsyncFromRes<T>(string resPath, Action<T> callback, bool isCache = true, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(resPath);
            await request.ToUniTask(cancellationToken: cancellationToken);

            if (request.asset == null)
            {
                Debug.LogError($"[ResourceLoader]:Asynchronous load failure:{resPath} (Type: {typeof(T)})");
                callback?.Invoke(null);
                return null;
            }

            if (isCache) assetCacheDic[resPath] = request.asset;
            var result = request.asset as T;
            callback?.Invoke(result);
            return result;
        }

        // 统一处理结果返回
        private static T HandleResult<T>(T asset, Action<T> callback) where T : UnityEngine.Object
        {
            callback?.Invoke(asset);
            return asset;
        }
    }
}
