using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Tinder.Animation.Asset;

namespace Tinder.Animation
{
    public interface AnimationParameterComponent
    {
        public int HashValue { get; }
#if UNITY_EDITOR || DEBUG
        public FixedString64Bytes ParameterName { get; }
#endif
    }

    public struct BoolParameter : IBufferElementData, AnimationParameterComponent
    {
        public int HashValue => m_HashValue;

        public int m_HashValue;
        public bool m_Value;
#if UNITY_EDITOR || DEBUG
        public FixedString64Bytes ParameterName => m_ParameterName;
        public FixedString64Bytes m_ParameterName;
#endif

        public BoolParameter(BoolParameterBlob boolParameterBlob)
        {
#if UNITY_EDITOR || DEBUG
            m_ParameterName = boolParameterBlob.Name;
#endif
            m_HashValue = boolParameterBlob.HashValue;
            m_Value = boolParameterBlob.DefaultValue;
        }
    }

    public struct IntParameter : IBufferElementData, AnimationParameterComponent
    {
        public int HashValue => m_HashValue;

        public int m_HashValue;
        public int m_Value;
#if UNITY_EDITOR || DEBUG
        public FixedString64Bytes ParameterName => m_ParameterName;
        public FixedString64Bytes m_ParameterName;
#endif

        public IntParameter(IntParameterBlob intParameterBlob)
        {
#if UNITY_EDITOR || DEBUG
            m_ParameterName = intParameterBlob.Name;
#endif
            m_HashValue = intParameterBlob.HashValue;
            m_Value = intParameterBlob.DefaultValue;
        }
    }

    public struct FloatParameter : IBufferElementData, AnimationParameterComponent
    {
        public int HashValue => m_HashValue;

        public int m_HashValue;
        public float m_Value;
#if UNITY_EDITOR || DEBUG
        public FixedString64Bytes ParameterName => m_ParameterName;
        public FixedString64Bytes m_ParameterName;
#endif

        public FloatParameter(FloatParameterBlob floatParameterBlob)
        {
#if UNITY_EDITOR || DEBUG
            m_ParameterName = floatParameterBlob.Name;
#endif
            m_HashValue = floatParameterBlob.HashValue;
            m_Value = floatParameterBlob.DefaultValue;
        }
    }
}
