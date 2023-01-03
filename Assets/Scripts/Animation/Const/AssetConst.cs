using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tinder.Animation.Asset
{
    public enum TransitionBlendMode : byte
    {
        Linear = 0,
        Cubic,
        Count
    }

    public enum AnimationNodeType : byte
    {
        EntryNode,
        FinalPose,
        SingleClip,
        StateMachine,
        State,
        Transition,
        BlendListByBoolNode,
        BlendListByIntNode,
        Count
    }

    public enum RootMotionMode
    {
        [Tooltip("Disable root motion")]
        Disabled,
        [Tooltip("Auto apply root motion")]
        EnabledAuto,
        [Tooltip("Store root motion deltas, need to apply manually")]
        EnabledManual
    }

    public enum ConditionParameterType : byte
    {
        Bool,
        Int,
        Float,
        RemainingTime,
        Count
    }

    public enum CompareType : byte
    {
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Equal,
        NotEqual,
    }

    public enum RemainingTimeType : byte
    {
        AbsoluteTime,
        RatioTime
    }

    public class AssetConst
    {
        public const string AnimationMenuPath = "Tinder/Animation";
        public const string MenuPath = "Tinder/Asset";
    }
}
