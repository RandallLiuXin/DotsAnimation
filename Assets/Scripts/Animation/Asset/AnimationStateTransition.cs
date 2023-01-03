using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Tinder.Animation.Asset
{
    [Serializable]
    public class TransitionNodeData
    {
        public byte m_Priority;
        public bool m_HasEndTime;
        [Min(0)]
        public float m_EndTime;
        [Min(0)]
        public float m_TransitionDuration;
        public TransitionBlendMode m_Mode;
        public List<AnimationTransitionValueCondition> m_ValueConditions;
        public List<AnimationTransitionRemainingTimeCondition> m_RemainingTimeConditions;
    }

    [Serializable]
    public struct AnimationTransitionValueCondition
    {
        public string ParameterAssetName;
        public CompareType CompareType;
        public ConditionParameterType ParameterType;

        public bool bValue;
        public int iValue;
        public float fValue;
    }

    [Serializable]
    public struct AnimationTransitionRemainingTimeCondition
    {
        public byte ClipNodeConfigId;
        public RemainingTimeType RemainingTimeType;
        public float fRemainingTime;
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct AnimationTransitionConditionBlob
    {
        //parameter condition
        [FieldOffset(0)] public sbyte ParameterIndex;
        //animation remaining
        [FieldOffset(0)] public byte ClipIndex;

        //common
        [FieldOffset(2)] public CompareType CompareType;
        [FieldOffset(4)] public ConditionParameterType ParameterType;

        //parameter condition
        [FieldOffset(8)] public bool bValue;
        [FieldOffset(8)] public int iValue;
        [FieldOffset(8)] public float fValue;

        //animation remaining
        [FieldOffset(6)] public RemainingTimeType RemainingTimeType;
        [FieldOffset(8)] public float fRemainingTime;
    }

    public class AnimationTransitionUtility
    {
        public static bool DoEvaluate(AnimationTransitionConditionBlob condition, bool inputValue)
        {
            return DoEvaluateInternal(condition, condition.bValue.CompareTo(inputValue));
        }
        public static bool DoEvaluate(AnimationTransitionConditionBlob condition, int inputValue)
        {
            return DoEvaluateInternal(condition, condition.iValue.CompareTo(inputValue));
        }
        public static bool DoEvaluate(AnimationTransitionConditionBlob condition, float inputValue)
        {
            return DoEvaluateInternal(condition, condition.fValue.CompareTo(inputValue));
        }
        public static bool DoEvaluate(AnimationTransitionConditionBlob condition, SingleClipNodeState singleClipNode)
        {
            float fTime;
            switch (condition.RemainingTimeType)
            {
                case RemainingTimeType.AbsoluteTime:
                    fTime = singleClipNode.GetRemainingAbsoluteTime();
                    break;
                case RemainingTimeType.RatioTime:
                    fTime = singleClipNode.GetRemainingRatioTime();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return DoEvaluateInternal(condition, fTime.CompareTo(condition.fRemainingTime));
        }
        private static bool DoEvaluateInternal(AnimationTransitionConditionBlob condition, int compareResult)
        {
            return condition.CompareType switch
            {
                CompareType.LessThan => compareResult < 0,
                CompareType.LessThanOrEqual => compareResult <= 0,
                CompareType.GreaterThan => compareResult > 0,
                CompareType.GreaterThanOrEqual => compareResult >= 0,
                CompareType.Equal => compareResult == 0,
                CompareType.NotEqual => compareResult != 0,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public struct AnimationStateTransitionConvertContext
    {
        public byte FromStateIndex;
        public byte ToStateIndex;

        public byte Priority;

        public bool HasEndTime;
        public float EndTime;
        public float TransitionDuration;

        public TransitionBlendMode Mode;
        public AnimationTransitionConditionsConvertContext ConditionContexts;
    }

    //Alignment must be a power of two, modify the offset manully
    [StructLayout(LayoutKind.Explicit)]
    public struct AnimationStateTransitionBlob
    {
        [FieldOffset(0)] public byte FromStateIndex;
        [FieldOffset(2)] public byte ToStateIndex;

        [FieldOffset(4)] public byte Priority;

        [FieldOffset(6)] public bool HasEndTime;
        [FieldOffset(8)] public float EndTime;
        [FieldOffset(12)] public float TransitionDuration;

        [FieldOffset(16)] public TransitionBlendMode Mode;
        [FieldOffset(20)] public AnimationTransitionConditionBlobs ConditionBlobs;
    }

    public struct AnimationTransitionConditionsConvertContext
    {
        public UnsafeList<AnimationTransitionConditionBlob> Conditions;
    }

    public struct AnimationTransitionConditionBlobs
    {
        public BlobArray<AnimationTransitionConditionBlob> Conditions;
    }
}
