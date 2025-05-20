using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;
using UnityEngine;
using System.IO;

namespace FFramework
{
    /// <summary>
    /// 资源加载器
    /// 支持同步加载,异步加载,AB包引用计数自动卸载
    /// </summary>
    public class AssetBundlesResLoader : SingletonMono<AssetBundlesResLoader>
    {
        //主包
        private AssetBundle mainAssetBundle = null;
        //包依赖
        private AssetBundleManifest manifest = null;
        //AB加载路径
        private string loadPathUrl => Path.Combine(Application.persistentDataPath, "");
        //主包名
        public string mainAssetBundleName;

        //AssetBundle包管理字典
        private Dictionary<string, AssetBundleData> assetBundleDic = new Dictionary<string, AssetBundleData>();

        //AssetBundle包数据类
        public class AssetBundleData
        {
            // 私有字段
            private AssetBundle assetBundle;
            private int referenceCount;
            private float lastUsedTime;

            // 公共属性
            public AssetBundle AssetBundle => assetBundle;
            public int ReferenceCount => referenceCount;
            public float LastUsedTime => lastUsedTime;

            // 构造函数
            public AssetBundleData(AssetBundle assetBundle)
            {
                this.assetBundle = assetBundle;
                referenceCount = 1;
                lastUsedTime = Time.realtimeSinceStartup;
            }

            // 增加引用计数
            public void AddReference()
            {
                referenceCount++;
                lastUsedTime = Time.realtimeSinceStartup;
            }

            // 减少引用计数并返回是否可以卸载
            public bool RemoveReference()
            {
                referenceCount--;
                lastUsedTime = Time.realtimeSinceStartup;
                return referenceCount <= 0;
            }

            // 卸载资源
            public void Unload(bool unloadAllLoadedObjects)
            {
                if (assetBundle != null)
                {
                    assetBundle.Unload(unloadAllLoadedObjects);
                    assetBundle = null;
                }
                referenceCount = 0;
            }

            // 检查是否可以卸载
            public bool CanUnload()
            {
                return referenceCount <= 0;
            }
        }

        //加载AssetBundle
        private void LoadAssetBundle(string abName)
        {
            //加载依赖信息包
            if (mainAssetBundle == null)
            {
                mainAssetBundle = AssetBundle.LoadFromFile(loadPathUrl + mainAssetBundleName);
                manifest = mainAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            }
            //获取依赖信息
            AssetBundle assetBundle = null;
            string[] dependencies = manifest.GetAllDependencies(abName);
            //加载依赖
            foreach (string dependency in dependencies)
            {
                if (!assetBundleDic.ContainsKey(dependency))
                {
                    assetBundle = AssetBundle.LoadFromFile(loadPathUrl + dependency);
                    assetBundleDic.Add(dependency, new AssetBundleData(assetBundle));
                }
                else
                {
                    assetBundleDic[dependency].AddReference();
                }
            }
            //加载资源主包
            if (!assetBundleDic.ContainsKey(abName))
            {
                assetBundle = AssetBundle.LoadFromFile(loadPathUrl + abName);
                assetBundleDic.Add(abName, new AssetBundleData(assetBundle));
            }
            else
            {
                assetBundleDic[abName].AddReference();
            }
        }

        #region  同步加载方法

        /// <summary>
        /// 同步加载资源
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// </summary>
        public Object LoadRes(string abName, string resName)
        {
            LoadAssetBundle(abName);
            //加载资源
            Object obj = assetBundleDic[abName].AssetBundle.LoadAsset(resName);
            return obj;
        }

        /// <summary>
        /// 同步加载资源(Lua中常用)
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// type -> 资源类型
        /// </summary>
        public Object LoadRes(string abName, string resName, System.Type type)
        {
            LoadAssetBundle(abName);
            //加载资源
            Object obj = assetBundleDic[abName].AssetBundle.LoadAsset(resName, type);
            return obj;
        }


        /// <summary>
        /// 同步加载资源(C#中常用)
        /// T -> 资源类型
        /// abName -> AssetBundle包名
        /// resName -> 需要加载资源名
        /// </summary>
        public T LoadRes<T>(string abName, string resName) where T : Object
        {
            LoadAssetBundle(abName);
            //加载资源
            T obj = assetBundleDic[abName].AssetBundle.LoadAsset<T>(resName);
            return obj;
        }

        #endregion

        #region  异步加载方法

        /// <summary>
        /// 异步加载方法
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// callBack -> 回调函数
        /// </summary>
        public void LoadResAsync(string abName, string resName, UnityAction<Object> callBack)
        {
            StartCoroutine(ReallyLoadResAsync(abName, resName, callBack));
        }

        //异步加载资源
        private IEnumerator ReallyLoadResAsync(string abName, string resName, UnityAction<Object> callBack)
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync(resName);
            yield return requestObj;
            callBack(requestObj.asset);
        }

        /// <summary>
        /// 异步加载方法(Lua中常用)
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// type -> 资源类型 
        /// callBack -> 回调函数
        /// </summary>
        public void LoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callBack)
        {
            StartCoroutine(ReallyLoadResAsync(abName, resName, type, callBack));
        }

        //异步加载资源
        private IEnumerator ReallyLoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callBack)
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync(resName, type);
            yield return requestObj;
            callBack(requestObj.asset);
        }

        /// <summary>
        /// 异步加载方法(C#中常用)
        /// T -> 资源类型
        /// abName -> AssetBundle包名
        /// resName -> 资源名
        /// callBack -> 回调函数
        /// </summary>
        public void LoadResAsync<T>(string abName, string resName, UnityAction<T> callBack) where T : Object
        {
            StartCoroutine(ReallyLoadResAsync<T>(abName, resName, callBack));
        }

        //异步加载资源
        private IEnumerator ReallyLoadResAsync<T>(string abName, string resName, UnityAction<T> callBack) where T : Object
        {
            LoadAssetBundle(abName);
            //加载资源
            AssetBundleRequest requestObj = assetBundleDic[abName].AssetBundle.LoadAssetAsync<T>(resName);
            yield return requestObj;
            callBack(requestObj.asset as T);
        }

        #endregion

        #region  AssetBundle卸载方法

        /// <summary>
        /// 单个包卸载
        /// 自动卸载引用为0的依赖包
        /// abName -> AssetBundle包名
        /// </summary>
        public void UnLoadAssetBundle(string abName)
        {
            if (assetBundleDic.ContainsKey(abName))
            {
                // 减少主包的引用计数
                if (assetBundleDic[abName].RemoveReference())
                {
                    assetBundleDic[abName].Unload(false);
                    assetBundleDic.Remove(abName);
                }

                // 减少依赖包的引用计数
                string[] dependencies = manifest.GetAllDependencies(abName);
                foreach (string dependency in dependencies)
                {
                    if (assetBundleDic.ContainsKey(dependency))
                    {
                        if (assetBundleDic[dependency].RemoveReference())
                        {
                            assetBundleDic[dependency].Unload(false);
                            assetBundleDic.Remove(dependency);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 所有包卸载
        /// </summary>
        public void UnLoadAllAssetBundle()
        {
            AssetBundle.UnloadAllAssetBundles(false);
            assetBundleDic.Clear();
            mainAssetBundle = null;
            manifest = null;
        }

        #endregion
    }
}