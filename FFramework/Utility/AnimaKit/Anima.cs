using UnityEngine.Playables;
using UnityEngine;

namespace FFramework.Kit
{
    /// <summary>
    /// 动画控制器基类
    /// </summary>
    public abstract class Anima : MonoBehaviour, IAnima
    {
        public Animator animator;
        [Tooltip("播放进度(0.0~1.0)"), Range(0.0f, 1.0f)] public float playProgress = 0.0f;
        [Tooltip("播放速度")] public float playSpeed = 1.0f;
        [Tooltip("是否循环播放")] public bool isLoop = false;
        protected PlayableGraph playableGraph;

        protected virtual void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            playableGraph = PlayableGraph.Create();
        }

        protected virtual void OnDestroy()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }

        [ContextMenu("播放动画")]
        public abstract void PlayAnima();
        [ContextMenu("暂停动画")]
        public abstract void PauseAnima();
        [ContextMenu("切换动画")]
        public abstract void ChangeAnima();
        // 获取动画播放进度
        public virtual void SetAnimaPlayProgress() { }
    }

    /// <summary>
    /// 动画接口
    /// </summary>
    public interface IAnima
    {
        public abstract void PlayAnima();
        public abstract void PauseAnima();
        public abstract void ChangeAnima();
        public void SetAnimaPlayProgress();
    }
}