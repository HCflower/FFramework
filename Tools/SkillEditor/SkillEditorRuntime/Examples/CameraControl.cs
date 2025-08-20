using UnityEngine;

namespace SkillEditorExamples
{
    /// <summary>
    /// 摄像机控制
    /// </summary>
    public class CameraControl : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.3f;
        public Vector3 offest;
        private Vector3 velocity = Vector3.zero;

        void LateUpdate()
        {
            if (target != null)
            {
                Vector3 targetPosition = target.position + offest;

                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            }
        }
    }
}
