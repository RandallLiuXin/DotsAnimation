using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Assertions;
using GraphProcessor;

namespace Tinder.Animation.Asset
{
    [Serializable]
    [NodeMenuItem("Animation/SingleClip")]
    public class SingleClipNode : AnimationClipNodeAsset
    {
        public AnimationClip Clip;
        [Input(name = "Speed"), ShowAsDrawer]
        public float m_Speed = 1.0f;
        [Input(name = "Loop"), ShowAsDrawer]
        public bool m_Loop = false;

        [Output(name = "OutputPose")]
        public PoseLink m_OutputPose;

        public override AnimationNodeType NodeType => AnimationNodeType.SingleClip;

        public override int ClipCount
        {
            get
            {
                return 1;
            }
        }

        public override IEnumerable<AnimationClip> Clips
        {
            get
            {
                yield return Clip;
            }
        }

        public override string name => "SingleClip";
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct SingleClipNodeBlob
    {
        [FieldOffset(0)] public ushort ClipIndex;
        [FieldOffset(4)] public bool Loop;
        [FieldOffset(6)] public byte ClipConfigId;
        [FieldOffset(8)] public float Speed;
        [FieldOffset(16)] public float ClipLength;

        public SingleClipNodeBlob(List<AnimationClip> clips, SingleClipNode clipNode)
        {
            var index = clips.FindIndex(clip => clip.Equals(clipNode.Clip));
            Assert.IsTrue(index >= 0);
            ClipIndex = (ushort)index;
            Speed = clipNode.m_Speed;
            Loop = clipNode.m_Loop;
            ClipConfigId = clipNode.GetClipConfigId;
            ClipLength = clips[index].length;
        }
    }
}
