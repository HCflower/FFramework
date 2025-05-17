using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace FFramework
{
    /// <summary>
    /// 对象池
    /// 支持:预热对象池,延迟返回,动态扩容
    /// </summary>
    public class GameObjectPool : SingletonMono<GameObjectPool>
    {
        GameObjectPool() => IsDontDestroyOnLoad = true;//禁止销毁
        [SerializeField] private Transform poolRoot => this.transform;
        // Key改为预制件名称
        private Dictionary<string, PoolData> poolDict = new Dictionary<string, PoolData>();
        // 反向映射
        private Dictionary<GameObject, string> instanceToPrefabName = new Dictionary<GameObject, string>();

        private class PoolData
        {
            public Transform parent;
            public Queue<GameObject> available = new Queue<GameObject>();
            public HashSet<GameObject> inUse = new HashSet<GameObject>();
        }

        //通过预制件名称获取唯一池
        private PoolData GetOrCreatePool(GameObject prefab)
        {
            string prefabName = prefab.name;

            if (!poolDict.TryGetValue(prefabName, out PoolData pool))
            {
                var parent = new GameObject($"{prefabName}Pool").transform;
                parent.SetParent(poolRoot);

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
        public void InitPool(GameObject prefab, int size)
        {
            PoolData pool = GetOrCreatePool(prefab);
            for (int i = 0; i < size; i++)
            {
                GameObject obj = CreateNewObject(prefab, pool.parent);
                pool.available.Enqueue(obj);
            }
        }

        //从对象池中获取对象
        public GameObject GetPoolObject(GameObject prefab)
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
        public T GetPoolObject<T>(GameObject prefab) where T : UnityEngine.Object
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
        private GameObject CreateNewObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.name = prefab.name;
            obj.SetActive(false);
            return obj;
        }
        //将对象返回对象池
        public void ReturnPool(GameObject obj, float delay = 0)
        {
            if (!instanceToPrefabName.TryGetValue(obj, out string prefabName)) return;
            StartCoroutine(DelayReturn(obj, prefabName, delay));
        }
        //延迟返回
        private IEnumerator DelayReturn(GameObject obj, string prefabName, float delay)
        {
            yield return new WaitForSeconds(delay);
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