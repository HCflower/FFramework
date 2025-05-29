using UnityEngine;
namespace DialogueTool
{
    public class DialogueDebug : IBranchAction
    {
        public void Execute()
        {
            Debug.Log("大爱仙尊!");
        }
    }
}
