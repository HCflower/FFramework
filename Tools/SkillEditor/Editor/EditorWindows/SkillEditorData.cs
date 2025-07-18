using System.Collections.Generic;
using FFramework.Kit;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器数据管理器
    /// </summary>
    public static class SkillEditorData
    {
        #region 数据
        public static SkillConfig CurrentSkillConfig { get; set; }
        public static bool IsGlobalControlShow { get; set; } = false;
        public static List<SkillEditorTrackInfo> tracks = new();

        // 时间轴配置
        public static float FrameUnitWidth { get; private set; } = 10f;
        public static int CurrentFrame { get; private set; } = 1;
        public static int MaxFrame { get; private set; } = 50;
        public static float TrackViewContentOffsetX { get; set; } = 0f;
        public static int MajorTickInterval { get; set; } = 5;
        public static bool IsPlaying { get; set; } = false;
        public static bool IsLoop { get; set; } = false;
        #endregion

        public static void SetCurrentFrame(int frame)
        {
            CurrentFrame = Mathf.Clamp(frame, 0, MaxFrame);
        }

        public static void SetMaxFrame(int frame)
        {
            MaxFrame = Mathf.Max(1, frame);
        }

        public static void SetFrameUnitWidth(float width)
        {
            FrameUnitWidth = Mathf.Clamp(width, 10f, 50f);
        }

        public static float CalculateTimelineWidth()
        {
            return MaxFrame * FrameUnitWidth;
        }

        public static void SaveData()
        {
            // 保存数据逻辑
        }
    }

    // 轨道类型枚举
    public enum TrackType
    {
        AnimationTrack,  //  动画轨道
        AudioTrack,      //  音频轨道    
        EffectTrack,     //  特效轨道
        AttackTrack,     //  攻击轨道
        EventTrack,      //  事件轨道
    }

    /// <summary>
    /// 轨道信息结构体
    /// </summary>
    public class SkillEditorTrackInfo
    {
        public SkillEditorTrackControl Control { get; set; }
        public SkillEditorTrack Track { get; set; }
        public TrackType TrackType { get; set; }
        public string TrackName { get; set; }
        public bool IsActive { get; set; } = true;

        public SkillEditorTrackInfo(SkillEditorTrackControl control, SkillEditorTrack track, TrackType type, string name)
        {
            Control = control;
            Track = track;
            TrackType = type;
            TrackName = name;
            IsActive = true;
        }
    }
}
