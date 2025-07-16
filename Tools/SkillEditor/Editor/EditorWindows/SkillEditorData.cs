using System.Collections.Generic;
using FFramework.Kit;
using UnityEngine;

namespace SkillEditor
{
    /// <summary>
    /// 技能编辑器数据管理器
    /// </summary>
    public class SkillEditorData
    {
        #region 数据
        public SkillConfig CurrentSkillConfig { get; set; }
        public bool IsGlobalControlShow { get; set; } = false;
        public List<(SkillEditorTrackControl control, SkillEditorTrack track)> tracks = new();
        // 时间轴配置
        public float FrameUnitWidth { get; private set; } = 10f;
        public int CurrentFrame { get; private set; } = 1;
        public int MaxFrame { get; private set; } = 50;
        public float TrackViewContentOffsetX { get; set; } = 0f;
        public int MajorTickInterval { get; set; } = 5;
        public bool IsPlaying { get; set; } = false;
        public bool IsLoop { get; set; } = false;
        #endregion

        public void SetCurrentFrame(int frame)
        {
            CurrentFrame = Mathf.Clamp(frame, 0, MaxFrame);
        }

        public void SetMaxFrame(int frame)
        {
            MaxFrame = Mathf.Max(1, frame);
        }

        public void SetFrameUnitWidth(float width)
        {
            FrameUnitWidth = Mathf.Clamp(width, 10f, 50f);
        }

        public float CalculateTimelineWidth()
        {
            return MaxFrame * FrameUnitWidth;
        }

        public void SaveData()
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
}
