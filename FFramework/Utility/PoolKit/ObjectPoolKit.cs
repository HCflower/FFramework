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
                GameObject obj = CreateNewObject(prefab);
                pool.available.Enqueue(obj);
            }
        }

        //从对象池中获取对象
        public static GameObject GetPoolObject(GameObject prefab, Transform transform = null)
        {
            PoolData pool = GetOrCreatePool(prefab);
            GameObject obj = pool.available.Count > 0 ? pool.available.Dequeue() : CreateNewObject(prefab);
            obj.transform.SetParent(null);
            obj.transform.position = transform == null ? Vector3.zero : transform.position;
            SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
            pool.inUse.Add(obj);
            // 存储预制件名称
            instanceToPrefabName[obj] = prefab.name;
            obj.SetActive(true);
            return obj;
        }

        //根据类型获取对象池中的对象
        public static T GetPoolObject<T>(T prefab, Transform transform = null) where T : UnityEngine.Object
        {
            GameObject obj;
            if (prefab is GameObject gameObject)
            {
                obj = GetPoolObject(gameObject, transform);
                return obj as T;
            }
            else if (prefab is Component component)
            {
                obj = GetPoolObject(component.gameObject, transform);
                return obj.GetComponent<T>();
            }
            else
            {
                Debug.LogError($"Unsupported types: {typeof(T).Name}");
                return null;
            }
        }

        //根据类型创建对象池中的对象 
        public static T GetPoolObject<T>() where T : UnityEngine.Object
        {
            string typeName = typeof(T).Name;

            // 检查是否已经有该类型的对象池
            if (!poolDict.TryGetValue(typeName, out PoolData pool))
            {
                // 创建新的对象池
                var parent = new GameObject($"{typeName}Pool").transform;
                parent.SetParent(GetPoolRoot());

                pool = new PoolData
                {
                    parent = parent,
                    available = new Queue<GameObject>(),
                    inUse = new HashSet<GameObject>()
                };
                poolDict.Add(typeName, pool);
            }

            GameObject obj;
            if (pool.available.Count > 0)
            {
                obj = pool.available.Dequeue();
            }
            else
            {
                // 创建新对象并添加组件
                obj = new GameObject(typeName);
                if (typeof(Component).IsAssignableFrom(typeof(T)))
                {
                    obj.AddComponent(typeof(T));
                }
            }

            obj.SetActive(true);
            pool.inUse.Add(obj);
            // 存储虚拟预制件键名
            instanceToPrefabName[obj] = typeName;
            obj.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());

            if (typeof(GameObject) == typeof(T))
            {
                return obj as T;
            }
            else
            {
                return obj.GetComponent<T>();
            }
        }

        // 创建新对象
        private static GameObject CreateNewObject(GameObject prefab)
        {
            GameObject obj = UnityEngine.Object.Instantiate(prefab);
            obj.SetActive(false);
            obj.name = prefab.name;
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
