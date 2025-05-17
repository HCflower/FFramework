using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;
using UnityEngine;

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
        private string loadPathUrl => Application.persistentDataPath + "/";
        //主包名
        public string mainAssetBundleName;

        //AssetBundle包管理字典
        private Dictionary<string, AssetBundleData> assetBundleDic = new Dictionary<string, AssetBundleData>();

        public class AssetBundleData
        {
            public AssetBundle assetBundle;
            public int referenceCount;
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
                    assetBundleDic.Add(dependency, new AssetBundleData
                    {
                        assetBundle = assetBundle,
                        referenceCount = 1
                    });
                }
                else
                {
                    assetBundleDic[dependency].referenceCount++;
                }
            }
            //加载资源主包
            if (!assetBundleDic.ContainsKey(abName))
            {
                assetBundle = AssetBundle.LoadFromFile(loadPathUrl + abName);
                assetBundleDic.Add(abName, new AssetBundleData
                {
                    assetBundle = assetBundle,
                    referenceCount = 1
                });
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
            Object obj = assetBundleDic[abName].assetBundle.LoadAsset(resName);
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
            Object obj = assetBundleDic[abName].assetBundle.LoadAsset(resName, type);
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
            T obj = assetBundleDic[abName].assetBundle.LoadAsset<T>(resName);
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
            AssetBundleRequest requestObj = assetBundleDic[abName].assetBundle.LoadAssetAsync(resName);
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
            AssetBundleRequest requestObj = assetBundleDic[abName].assetBundle.LoadAssetAsync(resName, type);
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
            AssetBundleRequest requestObj = assetBundleDic[abName].assetBundle.LoadAssetAsync<T>(resName);
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
                assetBundleDic[abName].referenceCount--;
                if (assetBundleDic[abName].referenceCount <= 0)
                {
                    assetBundleDic[abName].assetBundle.Unload(false);
                    assetBundleDic.Remove(abName);
                }

                // 减少依赖包的引用计数
                string[] dependencies = manifest.GetAllDependencies(abName);
                foreach (string dependency in dependencies)
                {
                    if (assetBundleDic.ContainsKey(dependency))
                    {
                        assetBundleDic[dependency].referenceCount--;
                        if (assetBundleDic[dependency].referenceCount <= 0)
                        {
                            assetBundleDic[dependency].assetBundle.Unload(false);
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