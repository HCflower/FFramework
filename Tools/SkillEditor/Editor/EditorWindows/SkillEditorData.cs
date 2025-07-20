using System.Collections.Generic;
using FFramework.Kit;
using UnityEditor;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器数据管理器
    /// 负责管理编辑器的全局状态、配置参数和轨道数据
    /// </summary>
    public static class SkillEditorData
    {
        #region 数据属性
        /// <summary>当前编辑的技能配置</summary>
        public static SkillConfig CurrentSkillConfig { get; set; }

        /// <summary>全局控制面板是否显示</summary>
        public static bool IsGlobalControlShow { get; set; } = false;

        /// <summary>编辑器中的轨道信息列表</summary>
        public static List<SkillEditorTrackInfo> tracks = new();

        /// <summary>每帧的显示宽度（像素）</summary>
        public static float FrameUnitWidth { get; private set; } = 10f;

        /// <summary>当前选中的帧数</summary>
        public static int CurrentFrame { get; private set; } = 1;

        /// <summary>时间轴最大帧数</summary>
        public static int MaxFrame { get; private set; } = 100;

        /// <summary>轨道视图内容的X轴偏移量</summary>
        public static float TrackViewContentOffsetX { get; set; } = 0f;

        /// <summary>主刻度间隔（帧数）</summary>
        public static int MajorTickInterval { get; set; } = 5;

        /// <summary>是否正在播放技能</summary>
        public static bool IsPlaying { get; set; } = false;

        /// <summary>是否循环播放</summary>
        public static bool IsLoop { get; set; } = false;
        #endregion

        #region 公共方法

        /// <summary>
        /// 设置当前帧数
        /// 自动限制在有效范围内（0到MaxFrame之间）
        /// </summary>
        /// <param name="frame">要设置的帧数</param>
        public static void SetCurrentFrame(int frame)
        {
            CurrentFrame = Mathf.Clamp(frame, 0, MaxFrame);
        }

        /// <summary>
        /// 设置最大帧数
        /// 确保最大帧数至少为1，并更新当前帧以保持在有效范围内
        /// </summary>
        /// <param name="frame">要设置的最大帧数</param>
        public static void SetMaxFrame(int frame)
        {
            MaxFrame = Mathf.Max(1, frame);
            // 确保当前帧不超出新的最大帧数
            if (CurrentFrame > MaxFrame)
            {
                CurrentFrame = MaxFrame;
            }
        }

        /// <summary>
        /// 设置帧单位宽度
        /// 限制宽度在合理范围内（10-50像素）以确保良好的显示效果
        /// </summary>
        /// <param name="width">要设置的帧单位宽度（像素）</param>
        public static void SetFrameUnitWidth(float width)
        {
            FrameUnitWidth = Mathf.Clamp(width, 10f, 50f);
        }

        /// <summary>
        /// 计算时间轴总宽度
        /// 基于最大帧数和帧单位宽度计算整个时间轴的显示宽度
        /// </summary>
        /// <returns>时间轴总宽度（像素）</returns>
        public static float CalculateTimelineWidth()
        {
            return MaxFrame * FrameUnitWidth;
        }

        /// <summary>
        /// 保存编辑器数据
        /// 将当前编辑器状态和配置保存到持久化存储
        /// </summary>
        public static void SaveData()
        {
            // TODO: 实现数据持久化保存逻辑
            if (CurrentSkillConfig)
            {
                AssetDatabase.SaveAssetIfDirty(CurrentSkillConfig);
            }
        }

        /// <summary>
        /// 重置编辑器数据为默认状态
        /// 清空所有轨道数据并恢复默认设置
        /// </summary>
        public static void ResetData()
        {
            CurrentSkillConfig = null;
            IsGlobalControlShow = false;
            tracks.Clear();
            FrameUnitWidth = 10f;
            CurrentFrame = 1;
            MaxFrame = 50;
            TrackViewContentOffsetX = 0f;
            MajorTickInterval = 5;
            IsPlaying = false;
            IsLoop = false;
        }

        /// <summary>
        /// 获取指定类型的轨道数量
        /// 统计当前编辑器中特定类型轨道的数量
        /// </summary>
        /// <param name="trackType">要统计的轨道类型</param>
        /// <returns>指定类型的轨道数量</returns>
        public static int GetTrackCountByType(TrackType trackType)
        {
            int count = 0;
            foreach (var track in tracks)
            {
                if (track.TrackType == trackType)
                    count++;
            }
            return count;
        }

        #endregion
    }

    /// <summary>
    /// 轨道类型枚举
    /// 定义技能编辑器支持的所有轨道类型
    /// </summary>
    public enum TrackType
    {
        /// <summary>动画轨道 - 控制角色动画播放</summary>
        AnimationTrack,
        /// <summary>音频轨道 - 播放音效和背景音乐</summary>
        AudioTrack,
        /// <summary>特效轨道 - 显示视觉特效</summary>
        EffectTrack,
        /// <summary>攻击轨道 - 处理伤害检测和攻击判定</summary>
        AttackTrack,
        /// <summary>事件轨道 - 触发自定义事件</summary>
        EventTrack,
    }

    /// <summary>
    /// 轨道信息结构体
    /// 封装单个轨道的完整信息，包括控制器、显示轨道和基本属性
    /// </summary>
    public class SkillEditorTrackInfo
    {
        /// <summary>轨道控制器，负责轨道的操作和管理</summary>
        public SkillEditorTrackControl Control { get; set; }

        /// <summary>轨道显示组件，负责轨道的视觉呈现</summary>
        public SkillEditorTrack Track { get; set; }

        /// <summary>轨道类型</summary>
        public TrackType TrackType { get; set; }

        /// <summary>轨道显示名称</summary>
        public string TrackName { get; set; }

        /// <summary>轨道是否激活状态</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 构造函数，创建轨道信息实例
        /// </summary>
        /// <param name="control">轨道控制器</param>
        /// <param name="track">轨道显示组件</param>
        /// <param name="type">轨道类型</param>
        /// <param name="name">轨道名称</param>
        public SkillEditorTrackInfo(SkillEditorTrackControl control, SkillEditorTrack track, TrackType type, string name)
        {
            Control = control;
            Track = track;
            TrackType = type;
            TrackName = name;
            IsActive = true;
        }

        /// <summary>
        /// 切换轨道激活状态
        /// 快捷方法用于切换轨道的激活/非激活状态
        /// </summary>
        public void ToggleActive()
        {
            IsActive = !IsActive;
        }

        /// <summary>
        /// 检查轨道是否有效
        /// 验证轨道的关键组件是否存在且有效
        /// </summary>
        /// <returns>轨道是否有效</returns>
        public bool IsValid()
        {
            return Control != null && Track != null && !string.IsNullOrEmpty(TrackName);
        }
    }
}
