using System.Collections.Generic;

namespace AssetBundleToolEditor
{
    /// <summary>
    /// 工具编辑器数据
    /// </summary>
    public static class AssetBundleEditorData
    {
        //当前选中的配置文件
        public static AssetBundleConfig currentABConfig;
        //当前选中的AssetBundle名称
        public static AssetBundleGroup currentAssetBundleGroup;
        //当前AB元素项搜索范围类型
        public static ABItemSearchType currentABItemSearchType = ABItemSearchType.Self;
        //当前选中的AssetBundle元素名称
        public static AssetBundleAssetsData currentAsset;
        //AB包过滤显示类型
        public static ABItemShowType currentABItemShowType;
        //当前过滤后的AB包组列表
        public static List<AssetBundleGroup> currentFilteredGroups;

        #region  Setting

        //本地AssetBundle包保存路径
        public static string localAssetBundleSavePath;

        //远端AssetBundle包保存路径
        public static string remoteAssetBundleSavePath;

        //构建AssetBundle包时是否清理文件夹
        public static bool isClearFolderWhenBuild = false;

        #endregion

        public static void ClearData()
        {
            currentAssetBundleGroup = null;
            currentAsset = null;
        }
    }

    //构建本地包/远端包数据
    public enum BuildPathType
    {
        Local,        //本地
        Remote,       //远端
    }

    //AB包下载类型
    public enum NetworkProtocolsType
    {
        FTP,          //Ftp
        HTTP,         //Http
    }

    //构建平台选择
    public enum BuildTarget
    {
        Windows,
        Android,
        IOS,
    }

    //搜索范围类型
    public enum ABItemSearchType
    {
        Self,       //本AssetBundle内搜索
        All,        //全部AssetBundle内搜索
    }

    //AB包过滤显示类型
    public enum ABItemShowType
    {
        All,         //全部
        Scene,       //场景
        Asset,       //资源
        GameObject,  //游戏对象
        Texture2D,   //贴图
        AudioClip,   //音频
        Animation,   //动画
        Material,    //材质
        TextAsset,   //文本
    }
}