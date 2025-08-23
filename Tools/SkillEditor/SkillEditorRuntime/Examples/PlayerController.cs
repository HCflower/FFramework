using FFramework.Kit;
using UnityEngine;

namespace SkillEditorExamples
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Animation Clips")]
        public AnimationClip idle;
        public AnimationClip run;
        public AnimationClip jump;
        public float transitionTime = 0.15f;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float jumpHeight = 2f;
        public float gravity = -9.81f;
        public float rotationSpeed = 10f;
        public Vector3 velocity;
        public PlaySmartAnima playSmartAnima;
        public SkillRuntimeController skillRuntime;
        public CharacterController characterController;

        public FSMStateMachine<PlayerController> stateMachine;
        public bool canMove = true;
        public bool canRotate = true; // 新增参数

        private Player_Idle player_Idle;
        private Vector3 lastMovementDirection;

        void Start()
        {
            player_Idle = new Player_Idle();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (playSmartAnima == null)
                playSmartAnima = GetComponent<PlaySmartAnima>();

            stateMachine = new FSMStateMachine<PlayerController>(this);
            stateMachine.SetDefault(player_Idle);

            lastMovementDirection = transform.forward;
        }

        void Update()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 inputDirection = new Vector3(moveX, 0, moveZ);

            if (inputDirection.magnitude > 0.1f)
            {
                Vector3 worldDirection = new Vector3(inputDirection.x, 0, inputDirection.z);
                if (worldDirection.magnitude > 0.1f)
                {
                    lastMovementDirection = worldDirection.normalized;

                    // 只有允许旋转时才执行旋转
                    if (canRotate)
                    {
                        SmoothRotateToMovementDirection();
                    }

                    if (canMove)
                    {
                        velocity = lastMovementDirection * moveSpeed;
                        characterController.Move(velocity * Time.deltaTime);
                    }
                    else
                    {
                        velocity = Vector3.zero;
                    }
                }
            }
            else
            {
                velocity = Vector3.zero;
            }

            stateMachine.Update();
        }

        /// <summary>
        /// 平滑旋转到移动方向
        /// </summary>
        private void SmoothRotateToMovementDirection()
        {
            if (lastMovementDirection.magnitude < 0.1f) return;

            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(lastMovementDirection);

            // 平滑插值旋转
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // 其他方法保持不变...
        public void FaceMovementDirectionImmediately()
        {
            if (lastMovementDirection.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(lastMovementDirection);
            }
        }

        public void FaceDirection(Vector3 direction, bool immediate = false)
        {
            if (direction.magnitude < 0.1f) return;

            lastMovementDirection = direction.normalized;

            if (immediate)
            {
                transform.rotation = Quaternion.LookRotation(lastMovementDirection);
            }
        }

        public Vector3 GetMovementDirection()
        {
            return lastMovementDirection;
        }

        public float GetMovementAngle()
        {
            if (lastMovementDirection.magnitude < 0.1f) return 0f;

            Vector3 flatDirection = new Vector3(lastMovementDirection.x, 0, lastMovementDirection.z);
            return Vector3.SignedAngle(Vector3.forward, flatDirection.normalized, Vector3.up);
        }
    }
}