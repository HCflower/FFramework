using FFramework.Kit;
using UnityEngine;

namespace SkillEditorExamples
{
    public class SkillEditorEventTest : SkillEvent
    {
        void Start()
        {
            // 注册技能事件
            AddSkillEventListener<GameObject>("OnInjuryDetection", OnInjuryDetection);
            AddSkillEventListener("Log", () => Debug.Log("Kill You !!!"));
        }

        public void OnInjuryDetection(GameObject target)
        {
            Debug.Log($"<color=yellow>Attack</color>" + target.name);
        }
    }
}