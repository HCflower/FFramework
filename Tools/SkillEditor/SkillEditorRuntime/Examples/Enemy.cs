using DG.Tweening;
using UnityEngine;

namespace SkillEditorExamples
{
    public class Enemy : MonoBehaviour
    {
        private Color defaultColor;
        public Color targetColor = Color.red;
        public Vector3 targetScale = new Vector3(1.25f, 1.25f, 1.25f);
        public float transitionTime = 1f;
        public float duration = 1f;

        void Start()
        {
            defaultColor = GetComponent<Renderer>().material.color;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                // 颜色动画: 先设置到targetColor，持续transitionTime，再恢复原始颜色，延迟duration
                GetComponent<Renderer>().material.DOColor(targetColor, transitionTime)
                .OnComplete(() =>
                {
                    GetComponent<Renderer>().material.DOColor(defaultColor, transitionTime)
                    .SetDelay(duration);
                });

                // 缩放动画：先放大到targetScale倍，持续transitionTime，再恢复原始缩放，延迟duration
                Vector3 originalScale = transform.localScale;
                transform.DOScale(Vector3.Scale(originalScale, targetScale), transitionTime)
                .OnComplete(() =>
                {
                    transform.DOScale(originalScale, transitionTime)
                    .SetDelay(duration);
                });
            }
        }

        void OnDestroy()
        {
            GetComponent<Renderer>().material.DOKill();
            transform.DOKill();
        }
    }
}
