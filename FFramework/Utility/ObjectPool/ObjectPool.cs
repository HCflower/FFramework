using System.Collections.Generic;
using FFramework.Architecture;
using UnityEngine;

namespace FFramework.Utility
{
    /// <summary>
    /// 对象池组件
    /// </summary>
    public class ObjectPool : SingletonMono<ObjectPool>
    {
        /// <summary>
        /// 对象池数据结构
        /// </summary>
        public class PoolData
        {
            // 池父节点
            public GameObject parent;
            // 对象列表
            public List<GameObject> objectList = new List<GameObject>();
            // 原始预制体引用（用于创建新对象）
            public GameObject prefab;

            // 构造函数（通过预制体）
            public PoolData(GameObject poolRoot, GameObject prefab)
            {
                this.prefab = prefab;
                this.parent = new GameObject(prefab.name + "-Pool");
                this.parent.transform.SetParent(poolRoot.transform);
            }

            // 构造函数（通过Resources路径）
            public PoolData(GameObject poolRoot, string objectName)
            {
                this.prefab = Resources.Load<GameObject>(objectName);
                this.parent = new GameObject(objectName + "-Pool");
                this.parent.transform.SetParent(poolRoot.transform);
            }

            // 获取对象
            public GameObject GetObjectFromPool()
            {
                // 清理无效对象
                // 从后向前遍历以安全移除
                for (int i = objectList.Count - 1; i >= 0; i--)
                {
                    if (objectList[i] == null)
                    {
                        objectList.RemoveAt(i);
                    }
                }

                if (objectList.Count > 0)
                {
                    // 取出最后一个对象
                    GameObject obj = objectList[objectList.Count - 1];
                    objectList.RemoveAt(objectList.Count - 1);
                    return obj;
                }
                return null;
            }

            // 将对象返回到池中
            public void ReturnObjectToPool(GameObject obj)
            {
                // 避免重复添加
                if (obj == null || objectList.Contains(obj)) return;

                obj.SetActive(false);
                obj.transform.SetParent(parent.transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                objectList.Add(obj);
            }

            // 预热对象
            public void PrewarmObjects(int count)
            {
                if (prefab == null) return;

                for (int i = 0; i < count; i++)
                {
                    GameObject obj = GameObject.Instantiate(prefab);
                    obj.name = prefab.name;
                    obj.SetActive(false);
                    obj.transform.SetParent(parent.transform);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    objectList.Add(obj);
                }
            }

            // 获取池中对象数量
            public int Count => objectList.Count;

            // 清理池
            public void Clear()
            {
                for (int i = objectList.Count - 1; i >= 0; i--)
                {
                    if (objectList[i] != null)
                    {
                        GameObject.Destroy(objectList[i]);
                    }
                }
                objectList.Clear();

                if (parent != null)
                {
                    GameObject.Destroy(parent);
                    parent = null;
                }
            }
        }

        // 对象池字典
        private Dictionary<string, PoolData> poolDic = new Dictionary<string, PoolData>();
        // 根节点
        private GameObject poolRoot;

        #region 对象获取方法

        /// <summary>
        /// 从对象池中获取对象（通过Resources加载）
        /// </summary>
        /// <param name="objectName">对象名称</param>
        /// <returns></returns>
        public GameObject GetResourcesObjectFromPool(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;

            EnsurePoolRoot();

            // 确保池存在
            if (!poolDic.ContainsKey(objectName))
            {
                poolDic[objectName] = new PoolData(poolRoot, objectName);
            }

            // 从池中获取对象
            GameObject obj = poolDic[objectName].GetObjectFromPool();
            if (obj == null)
            {
                if (poolDic[objectName].prefab == null)
                {
                    Debug.LogError($"无法从Resources加载对象: {objectName}");
                    return null;
                }
                obj = GameObject.Instantiate(poolDic[objectName].prefab);
                obj.name = objectName;
            }

            SetupObjectFromPool(obj);
            return obj;
        }

        /// <summary>
        /// 从对象池中获取对象（通过传入预制体）
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <returns></returns>
        public GameObject GetAssetsObjectFromPool(GameObject prefab)
        {
            if (prefab == null) return null;

            EnsurePoolRoot();

            string poolName = prefab.name;

            // 确保池存在
            if (!poolDic.ContainsKey(poolName))
            {
                poolDic[poolName] = new PoolData(poolRoot, prefab);
            }

            // 从池中获取对象
            GameObject obj = poolDic[poolName].GetObjectFromPool();
            if (obj == null)
            {
                obj = GameObject.Instantiate(prefab);
                obj.name = prefab.name;
            }

            SetupObjectFromPool(obj);
            return obj;
        }

        #endregion

        #region 对象归还和清理

        /// <summary>
        /// 返回对象到对象池
        /// </summary>
        /// <param name="obj">对象</param>
        public void ReturnObjectToPool(GameObject obj)
        {
            if (obj == null) return;

            EnsurePoolRoot();

            // 调用接口回调
            IPoolObject poolObj = obj.GetComponent<IPoolObject>();
            poolObj?.OnAfterReturnToPool();

            // 获取或创建池
            string poolName = obj.name;
            if (!poolDic.ContainsKey(poolName))
            {
                Debug.LogWarning($"对象 {poolName} 没有对应的对象池，创建新池");
                // 尝试从Resources加载原始预制体
                GameObject prefab = Resources.Load<GameObject>(poolName);
                if (prefab != null)
                {
                    poolDic[poolName] = new PoolData(poolRoot, poolName);
                }
                else
                {
                    // 如果无法找到原始预制体，创建一个临时池
                    poolDic[poolName] = new PoolData(poolRoot, obj);
                }
            }

            // 返回到池中
            poolDic[poolName].ReturnObjectToPool(obj);
        }

        /// <summary>
        /// 清理对象池 - 场景切换时调用
        /// </summary>
        public void ClearPool()
        {
            foreach (var pool in poolDic.Values)
            {
                pool.Clear();
            }
            poolDic.Clear();

            if (poolRoot != null)
            {
                GameObject.Destroy(poolRoot);
                poolRoot = null;
            }
        }

        /// <summary>
        /// 清理指定对象池
        /// </summary>
        /// <param name="poolName">池名称</param>
        public void ClearPool(string poolName)
        {
            if (poolDic.ContainsKey(poolName))
            {
                poolDic[poolName].Clear();
                poolDic.Remove(poolName);
            }
        }

        #endregion

        #region 预热功能

        /// <summary>
        /// 预热对象池（通过Resources加载）
        /// </summary>
        /// <param name="objectName">对象名称</param>
        /// <param name="count">预热数量</param>
        public void PrewarmResourcesPool(string objectName, int count)
        {
            if (string.IsNullOrEmpty(objectName) || count <= 0) return;

            EnsurePoolRoot();

            // 确保池存在
            if (!poolDic.ContainsKey(objectName))
            {
                poolDic[objectName] = new PoolData(poolRoot, objectName);
            }

            // 预热对象
            poolDic[objectName].PrewarmObjects(count);
        }

        /// <summary>
        /// 预热对象池（通过预制体）
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="count">预热数量</param>
        public void PrewarmAssetsPool(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;

            EnsurePoolRoot();

            string poolName = prefab.name;
            // 确保池存在
            if (!poolDic.ContainsKey(poolName))
            {
                poolDic[poolName] = new PoolData(poolRoot, prefab);
            }

            // 预热对象
            poolDic[poolName].PrewarmObjects(count);
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 设置从池中获取的对象状态
        /// </summary>
        private void SetupObjectFromPool(GameObject obj)
        {
            // 只设置父级为null和激活状态，不重置位置信息
            obj.transform.SetParent(null);
            obj.SetActive(true);

            // 调用接口回调
            IPoolObject poolObj = obj.GetComponent<IPoolObject>();
            poolObj?.OnBeforeGetFromPool();
        }

        /// <summary>
        /// 确保池根节点存在
        /// </summary>
        private void EnsurePoolRoot()
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject("ObjectPoolRoot");
                // DontDestroyOnLoad(poolRoot);
            }
        }

        #endregion

        #region 池状态查询

        /// <summary>
        /// 获取指定池的对象数量
        /// </summary>
        public int GetPoolCount(string poolName)
        {
            return poolDic.ContainsKey(poolName) ? poolDic[poolName].Count : 0;
        }

        /// <summary>
        /// 检查池是否存在
        /// </summary>
        public bool HasPool(string poolName)
        {
            return poolDic.ContainsKey(poolName);
        }

        /// <summary>
        /// 获取所有池的名称
        /// </summary>
        public string[] GetAllPoolNames()
        {
            string[] names = new string[poolDic.Count];
            int index = 0;
            foreach (string key in poolDic.Keys)
            {
                names[index++] = key;
            }
            return names;
        }

        #endregion
    }

    /// <summary>
    /// 对象池对象接口 - 实现该接口的对象可以在获取和归还时执行特定逻辑
    /// </summary>
    public interface IPoolObject
    {
        /// <summary>
        /// 当从对象池中获取出前时调用
        /// </summary>
        void OnBeforeGetFromPool();

        /// <summary>
        /// 当归还对象到对象池时调用
        /// </summary>
        void OnAfterReturnToPool();
    }
}