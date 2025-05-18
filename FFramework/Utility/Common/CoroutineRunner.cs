using System.Collections;

namespace FFramework.Kit
{
    /// <summary>
    /// 协程辅助单例类
    /// </summary>
    public class CoroutineRunner : SingletonMono<CoroutineRunner>
    {
        public CoroutineRunner() => IsDontDestroyOnLoad = true;

        /// <summary>
        /// 启动协程
        /// </summary>
        public static void StartStaticCoroutine(IEnumerator coroutine)
        {
            Instance.StartCoroutine(coroutine);
        }

        /// <summary>
        /// 停止协程
        /// </summary>
        public static void StopStaticCoroutine(IEnumerator coroutine)
        {
            if (coroutine != null) Instance.StopCoroutine(coroutine);
        }

        /// <summary>
        /// 停止所有由CoroutineRunner启动的协程
        /// </summary>
        public static void StopAllStaticCoroutines()
        {
            Instance.StopAllCoroutines();
        }
    }
}