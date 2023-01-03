using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Assertions;
using GraphProcessor;

namespace Tinder.Animation.Asset
{
    [Serializable]
    [NodeMenuItem("Animation/StateMachine")]
    public class StateMachineNode : AnimationNodeAsset
    {
        public override AnimationNodeType NodeType => AnimationNodeType.StateMachine;

        [Output(name = "OutputPose")]
        public PoseLink m_OutputPose;

        public string StateMachineName;

        [HideInInspector]
        public AnimationStateMachineAsset StateMachineAsset;

        public override string name => "StateMachine";
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct StateMachineNodeBlob
    {
        [FieldOffset(0)] public byte StateMachineIndex;

        public StateMachineNodeBlob(List<AnimationStateMachineAsset> stateMachines, StateMachineNode StateMachineNode)
        {
            Assert.IsTrue(stateMachines.Count <= byte.MaxValue);
            var index = stateMachines.FindIndex(stateMachine => stateMachine.Equals(StateMachineNode.StateMachineAsset));
            Assert.IsTrue(index >= 0);
            StateMachineIndex = (byte)index;
        }
    }
}
