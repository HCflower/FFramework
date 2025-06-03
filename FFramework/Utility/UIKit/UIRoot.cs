using UnityEngine;

namespace FFramework.Kit
{
    ///<summary>
    /// UI根节点
    /// </summary>
    public class UIRoot : SingletonMono<UIRoot>
    {
        #region UI层级  

        public Transform BackgroundLayer;       //背景层 - 静态背景
        public Transform ContentLayer;          //内容层 - 主要UI功能
        public Transform PopupLayer;            //弹窗层 - 消息弹窗
        public Transform GuideLayer;            //引导层 - 引导玩家操作    
        public Transform DebugLayer;            //调试层 - 创建和调试UI   

        #endregion
    }

    public enum UILayer
    {
        BackgroundLayer,
        ContentLayer,
        PopupLayer,
        GuideLayer,
        DebugLayer
    }
}