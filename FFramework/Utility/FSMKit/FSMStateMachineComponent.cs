using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FFramework.Kit
{
    /// <summary>
    /// Unity状态机组件，自动处理Update循环
    /// </summary>
    public class FSMStateMachineComponent : MonoBehaviour
    {
        #region 序列化字段

        [Header("状态机设置")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool enableFixedUpdate = true;
        [SerializeField] private bool enableLateUpdate = false;

#if UNITY_EDITOR
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool logStateChanges = false;
#endif
        #endregion

        #region 私有字段

        private FSMStateMachine stateMachine;
        private bool isInitialized = false;

        #endregion

        #region 公共属性

        /// <summary>状态机实例</summary>
        public FSMStateMachine StateMachine => stateMachine;

        /// <summary>是否已初始化</summary>
        public bool IsInitialized => isInitialized;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeStateMachine();
        }

        private void Start()
        {
            if (autoStart)
            {
                OnAutoStart();
            }
        }

        private void Update()
        {
            if (isInitialized && stateMachine.IsRunning)
            {
                stateMachine.Update();
            }
        }

        private void FixedUpdate()
        {
            if (enableFixedUpdate && isInitialized && stateMachine.IsRunning)
            {
                stateMachine.FixedUpdate();
            }
        }

        private void LateUpdate()
        {
            if (enableLateUpdate && isInitialized && stateMachine.IsRunning)
            {
                stateMachine.LateUpdate();
            }
        }

        private void OnDestroy()
        {
            CleanUp();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (isInitialized)
            {
                if (pauseStatus)
                    stateMachine.Pause();
                else
                    stateMachine.Resume();
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化状态机
        /// </summary>
        public void InitializeStateMachine()
        {
            if (isInitialized) return;

            stateMachine = new FSMStateMachine();
            isInitialized = true;

            // 设置日志回调
            if (logStateChanges)
            {
                SetupLogging();
            }

            OnStateMachineInitialized();
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        public void StartStateMachine<T>() where T : IState, new()
        {
            if (!isInitialized)
            {
                Debug.LogError("状态机未初始化");
                return;
            }

            stateMachine.Start<T>();
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        public void StartStateMachine(IState initialState)
        {
            if (!isInitialized)
            {
                Debug.LogError("状态机未初始化");
                return;
            }

            stateMachine.Start(initialState);
        }

        /// <summary>
        /// 停止状态机
        /// </summary>
        public void StopStateMachine()
        {
            if (isInitialized)
            {
                stateMachine.Stop();
            }
        }

        /// <summary>
        /// 暂停状态机
        /// </summary>
        public void PauseStateMachine()
        {
            if (isInitialized)
            {
                stateMachine.Pause();
            }
        }

        /// <summary>
        /// 恢复状态机
        /// </summary>
        public void ResumeStateMachine()
        {
            if (isInitialized)
            {
                stateMachine.Resume();
            }
        }

        /// <summary>
        /// 添加状态转换条件
        /// </summary>
        public void AddTransition<TFrom, TTo>(Func<bool> condition)
            where TFrom : IState
            where TTo : IState
        {
            if (isInitialized)
            {
                stateMachine.AddTransition<TFrom, TTo>(condition);
            }
        }

        /// <summary>
        /// 添加全局转换条件
        /// </summary>
        public void AddGlobalTransition<TTo>(Func<bool> condition) where TTo : IState
        {
            if (isInitialized)
            {
                stateMachine.AddGlobalTransition<TTo>(condition);
            }
        }

        #endregion

        #region 虚方法（子类可重写）

        /// <summary>
        /// 状态机初始化完成时调用
        /// </summary>
        protected virtual void OnStateMachineInitialized() { }

        /// <summary>
        /// 自动启动时调用
        /// </summary>
        protected virtual void OnAutoStart()
        {
            Debug.LogWarning($"{gameObject.name}: 状态机组件启用了自动启动，但未指定初始状态。请重写OnAutoStart方法。");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置日志记录
        /// </summary>
        private void SetupLogging()
        {
            // 这里可以添加状态切换的日志逻辑
            // 由于当前状态机没有暴露状态切换事件，这里暂时留空
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void CleanUp()
        {
            if (isInitialized)
            {
                stateMachine?.Dispose();
                stateMachine = null;
                isInitialized = false;
            }
        }

        #endregion

#if UNITY_EDITOR
        #region 调试

        private void OnGUI()
        {
            if (!showDebugInfo || !isInitialized) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"状态机: {gameObject.name}", EditorGUIUtility.isProSkin ? GUI.skin.label : GUI.skin.label);
            GUILayout.Label($"状态: {(stateMachine?.IsRunning == true ? "运行中" : "已停止")}");
            GUILayout.Label($"当前状态: {stateMachine?.GetCurrentStateName() ?? "无"}");
            GUILayout.Label($"统计信息: {stateMachine?.GetStatistics() ?? "无"}");

            GUILayout.Space(10);

            if (GUILayout.Button(stateMachine?.IsRunning == true ? "暂停" : "恢复"))
            {
                if (stateMachine?.IsRunning == true)
                    PauseStateMachine();
                else
                    ResumeStateMachine();
            }

            if (GUILayout.Button("停止"))
            {
                StopStateMachine();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
#endif
    }
}
