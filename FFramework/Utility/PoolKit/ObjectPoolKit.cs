using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 对象池工具类
    /// </summary>
    public static class ObjectPoolKit
    {
        // Key改为预制件名称
        private static Dictionary<string, PoolData> poolDict = new Dictionary<string, PoolData>();
        // 反向映射
        private static Dictionary<GameObject, string> instanceToPrefabName = new Dictionary<GameObject, string>();

        private class PoolData
        {
            public Transform parent;
            public Queue<GameObject> available = new Queue<GameObject>();
            public HashSet<GameObject> inUse = new HashSet<GameObject>();
        }

        private static Transform GetPoolRoot()
        {
            return PoolRoot.Instance.transform;
        }
        //通过预制件名称获取唯一池
        private static PoolData GetOrCreatePool(GameObject prefab)
        {
            string prefabName = prefab.name;

            if (!poolDict.TryGetValue(prefabName, out PoolData pool))
            {
                var parent = new GameObject($"{prefabName}Pool").transform;
                parent.SetParent(GetPoolRoot());

                pool = new PoolData
                {
                    parent = parent,
                    available = new Queue<GameObject>(),
                    inUse = new HashSet<GameObject>()
                };
                poolDict.Add(prefabName, pool);
            }
            return pool;
        }

        // 初始化对象池
        public static void InitPool(GameObject prefab, int size)
        {
            PoolData pool = GetOrCreatePool(prefab);
            for (int i = 0; i < size; i++)
            {
                GameObject obj = CreateNewObject(prefab, pool.parent);
                pool.available.Enqueue(obj);
            }
        }

        //从对象池中获取对象
        public static GameObject GetPoolObject(GameObject prefab)
        {
            PoolData pool = GetOrCreatePool(prefab);
            GameObject obj = pool.available.Count > 0 ? pool.available.Dequeue() : CreateNewObject(prefab, pool.parent);
            obj.SetActive(true);
            pool.inUse.Add(obj);
            instanceToPrefabName[obj] = prefab.name; // 存储预制件名称
            obj.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
            return obj;
        }

        //根据类型获取对象池中的对象
        public static T GetPoolObject<T>(GameObject prefab) where T : UnityEngine.Object
        {
            GameObject obj = GetPoolObject(prefab);
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogWarning($"Object {prefab.name} 缺少组件: {typeof(T).Name}");
            }
            return component;
        }

        // 创建新对象
        private static GameObject CreateNewObject(GameObject prefab, Transform parent)
        {
            GameObject obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.name = prefab.name;
            obj.SetActive(false);
            return obj;
        }

        //将对象返回对象池
        public static void ReturnPool(GameObject obj)
        {
            if (!instanceToPrefabName.TryGetValue(obj, out string prefabName)) return;
            if (poolDict.TryGetValue(prefabName, out PoolData pool))
            {
                obj.SetActive(false);
                obj.transform.SetParent(pool.parent);
                pool.inUse.Remove(obj);
                pool.available.Enqueue(obj);
                instanceToPrefabName.Remove(obj);
            }
        }
    }
}
