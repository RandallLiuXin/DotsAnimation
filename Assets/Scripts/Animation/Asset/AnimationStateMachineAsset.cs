using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using UnityEngine.Assertions;
using System.Linq;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Tinder.Animation.Asset
{
    [Serializable]
    public class AnimationStateMachineAsset : BaseGraph, IGetAnimationGlobalId
    {
        public AnimationGraphAsset m_OwnerGraph;
        public string m_StateMachineName;

        public AnimationStateAsset m_DefaultState
        {
            get
            {
                var entryNodes = nodes.OfType<EntryStateNode>().ToList();
                Assert.IsTrue(entryNodes.Count == 1);
                var entryNode = entryNodes[0];
                var outputNodes = entryNode.GetOutputNodes().ToList();
                Assert.IsTrue(outputNodes.Count == 1 && outputNodes[0] is StateNode);
                var stateNode = outputNodes[0] as StateNode;
                return stateNode.StateAsset;
            }
        }

        public List<AnimationStateAsset> m_States
        {
            get
            {
                var result = new List<AnimationStateAsset>();
                nodes.OfType<StateNode>().ToList().ForEach(state =>
                {
                    result.Add(state.StateAsset);
                });
                return result;
            }
        }
        public List<TransitionNode> m_Transitions => nodes.OfType<TransitionNode>().ToList();

        public IEnumerable<AnimationClip> Clips => m_States.SelectMany(s => s.Clips);
        public int ClipCount => m_States.Sum(s => s.ClipCount);

        public byte GetAnimationNodeGlobalId()
        {
            return m_OwnerGraph.GetAnimationNodeGlobalId();
        }
    }

    public struct StateMachineBlob
    {
        public byte DefaultState;
        public BlobArray<AnimationStateBlob> States;
    }

    public struct StateMachineConvertContext
    {
        public byte DefaultState;
        public UnsafeList<AnimationStateConvertContext> States;
    }
}
