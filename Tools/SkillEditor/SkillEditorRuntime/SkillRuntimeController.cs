using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

namespace FFramework.Kit
{
    /// <summary>
    /// 技能运行时控制器 - 负责执行技能配置中定义的各种轨道和片段
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillRuntimeController : MonoBehaviour
    {
        [Tooltip("技能配置文件")] public SkillConfig skillConfig;
        [Tooltip("技能控制的摄像机")] public Camera skillCamera;
        [Tooltip("技能动画状态机")] public Animator skillAnimator;
        // 自动获取
        public ISkillEvent skillEvent => GetComponent<ISkillEvent>();
    }
}
