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
        public Vector3 velocity;
        public PlaySmartAnima playSmartAnima;
        public SkillRuntimeController skillRuntime;
        public CharacterController characterController;

        public FSMStateMachine<PlayerController> stateMachine;

        private Player_Idle player_Idle;
        void Start()
        {
            player_Idle = new Player_Idle();

            // 确保引用正确
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (playSmartAnima == null)
                playSmartAnima = GetComponent<PlaySmartAnima>();
            // 创建状态机
            stateMachine = new FSMStateMachine<PlayerController>(this);
            // stateMachine.SetDefault<Player_Idle>();
            stateMachine.SetDefault(player_Idle);
        }

        void Update()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // 将输入方向从本地空间转换到世界空间
            velocity = transform.TransformDirection(new Vector3(moveX, 0, moveZ));
            velocity.y = 0; // 确保没有垂直移动
            velocity.Normalize();

            characterController.Move(velocity * moveSpeed * Time.deltaTime);
            stateMachine.Update();
        }
    }
}