using UnityEngine;

namespace FFramework
{
    ///<summary>
    /// UI基类
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;

        protected virtual void OnEnable()
        {
            Init();
        }

        /// <summary>
        /// 初始化UI 
        /// </summary>
        public abstract void Init();

        //显示UI
        public virtual void Show()
        {
            GetOrSetCanvasGroup();
            //显示UI时，允许点击
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
        }

        //锁定UI
        public virtual void OnLock()
        {
            GetOrSetCanvasGroup();
            //锁定UI时，不允许点击
            canvasGroup.blocksRaycasts = false;
        }

        //解锁UI
        public virtual void OnUnLock()
        {
            GetOrSetCanvasGroup();
            //解锁UI时，允许点击
            canvasGroup.blocksRaycasts = true;
        }

        //关闭UI
        public virtual void Close()
        {
            GetOrSetCanvasGroup();
            //关闭UI时，不允许点击
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        //获取或设置CanvasGroup
        private void GetOrSetCanvasGroup()
        {
            if (!TryGetComponent<CanvasGroup>(out canvasGroup))
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}