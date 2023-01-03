using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Assertions;
using GraphProcessor;
using System.Linq;

namespace Tinder.Animation.Asset
{
    [Serializable]
    [NodeMenuItem("Animation/FinalPose")]
    public class FinalPoseNode : AnimationNodeAsset
    {
        public override AnimationNodeType NodeType => AnimationNodeType.FinalPose;

        [Input(name = "InputPose")]
        public PoseLink m_ResultPose;

        public override string name => "FinalPose";
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct FinalPoseNodeBlob
    {
        [FieldOffset(0)] public PoseLinkBlob PoseLinkBlob;

        public FinalPoseNodeBlob(AnimationNodeRuntimeConvertContext context, FinalPoseNode finalPoseNode)
        {
            var nodes = finalPoseNode.GetInputNodes().ToList();
            Assert.IsTrue(nodes.Count == 1 && nodes[0] is AnimationNodeAsset);
            var sourceNode = nodes[0] as AnimationNodeAsset;
            var index = context.FindAnimationNodeAssetIndexInList(sourceNode);
            Assert.IsTrue(index < byte.MaxValue && index >= 0);

            PoseLinkBlob = new PoseLinkBlob
            {
                LinkNodeType = sourceNode.NodeType,
                LinkId = new AnimationBufferId((byte)index)
            };
        }
    }
}
