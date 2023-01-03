using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using GraphProcessor;
using UnityEngine.Assertions;

namespace Tinder.Animation.Asset
{
    public struct PoseLink { }
    public struct StateLink { }
    public struct TransitionLink { }

    public abstract class AnimationNodeAsset : BaseNode
    {
        public abstract AnimationNodeType NodeType { get; }
        public virtual int ClipCount
        {
            get
            {
                return 0;
            }
        }
        public virtual IEnumerable<AnimationClip> Clips
        {
            get
            {
                return null;
            }
        }
    }

    public abstract class AnimationClipNodeAsset : AnimationNodeAsset
    {
        protected override void Enable()
        {
            base.Enable();
            IGetAnimationGlobalId animationConfig = graph as IGetAnimationGlobalId;
            Debug.Assert(animationConfig != null);
            if (m_ClipConfigId == 0)
                m_ClipConfigId = animationConfig.GetAnimationNodeGlobalId();
        }

        [SerializeField, HideInInspector]
        protected byte m_ClipConfigId;
        public byte GetClipConfigId => m_ClipConfigId;
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct PoseLinkBlob
    {
        [FieldOffset(0)] public AnimationNodeType LinkNodeType;
        [FieldOffset(2)] public AnimationBufferId LinkId;

        public bool IsValid => LinkNodeType != AnimationNodeType.Count;
        public static PoseLinkBlob Null => new() { LinkId = AnimationBufferId.Null, LinkNodeType = AnimationNodeType.Count };
    }
}
