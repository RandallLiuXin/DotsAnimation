using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Tinder.Animation
{
    public struct AnimationBlendWeight
    {
        public ushort m_ClipIndex;
        public float m_Weight;

        public float m_Time;
        public float m_Speed;
        public bool m_Loop;

        //config
        public float m_TotalTime;
    }

    public struct AnimationClipSampler : IBufferElementData
    {
        public ushort m_ClipIndex;
        public float m_PreviousTime;
        public float m_Time;
        public float m_Weight;
        public bool m_Loop;

        //config
        public float m_TotalTime;
    }
}
