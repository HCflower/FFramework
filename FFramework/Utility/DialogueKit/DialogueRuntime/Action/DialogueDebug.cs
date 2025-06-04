using UnityEngine;
namespace FFramework.Kit
{
    public class DialogueDebug : IBranchAction
    {
        public void Execute()
        {
            Debug.Log("对话事件!");
        }
    }
}
