using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Tinder.Animation
{
    public struct RootDeltaTranslation : IComponentData
    {
        public float3 Value;
    }

    public struct RootDeltaRotation : IComponentData
    {
        public quaternion Value;
    }

    internal struct ApplyRootMotionToEntity : IComponentData
    {
    }
}
