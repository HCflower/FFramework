using UnityEngine;
namespace FFramework
{
    public class DialogueDebug : IBranchAction
    {
        public void Execute()
        {
            Debug.Log("大爱仙尊!");
        }
    }
}
