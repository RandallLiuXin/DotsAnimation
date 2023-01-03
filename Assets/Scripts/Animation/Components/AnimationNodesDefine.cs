using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Tinder.Animation.Asset;
using Unity.Burst;
using Unity.Assertions;
using Unity.Collections;
using Unity.Mathematics;

namespace Tinder.Animation
{
    public static class AnimationNodeDefines
    {
        public static void AddAnimationNodeBufferInternal(EntityManager dstManager, Entity entity)
        {
            //node data
            dstManager.AddBuffer<FinalPoseNodeState>(entity);
            dstManager.AddBuffer<SingleClipNodeState>(entity);
            dstManager.AddBuffer<StateMachineNodeState>(entity);
        }

        public static void AddAnimationNodeDatas(EntityManager dstManager, Entity entity, ref AnimationNodeContextBlob nodeContextBlob)
        {
            //only work for animation graph
            sbyte graphIndex = -1;
            var finalPoseNodeBuffer = dstManager.GetBuffer<FinalPoseNodeState>(entity);
            var singleClipNodeBuffer = dstManager.GetBuffer<SingleClipNodeState>(entity);
            var stateMachineNodeBuffer = dstManager.GetBuffer<StateMachineNodeState>(entity);

            AddAnimationNodeDatas(graphIndex, ref nodeContextBlob, 
                ref finalPoseNodeBuffer,
                ref singleClipNodeBuffer,
                ref stateMachineNodeBuffer);
        }

        public static void AddAnimationNodeDatas(sbyte stateIndex, 
            ref AnimationNodeContextBlob nodeContextBlob,
            ref DynamicBuffer<FinalPoseNodeState> finalPoseNodeStates,
            ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates)
        {
            {
                finalPoseNodeStates.Add(new FinalPoseNodeState
                {
                    m_UniqueId = 0,
                    m_StateIndex = stateIndex,
                    m_PoseLinkBlob = nodeContextBlob.FinalNode.PoseLinkBlob
                });
            }

            Assert.IsTrue(nodeContextBlob.SingleClipNodes.Length <= byte.MaxValue);
            for (byte nodeIndex = 0; nodeIndex < nodeContextBlob.SingleClipNodes.Length; nodeIndex++)
            {
                var nodeBlob = nodeContextBlob.SingleClipNodes[nodeIndex];
                singleClipNodeStates.Add(new SingleClipNodeState
                {
                    m_UniqueId = nodeBlob.ClipConfigId,
                    m_StateIndex = stateIndex,
                    m_ClipIndex = nodeBlob.ClipIndex,
                    m_ClipCount = 1,
                    m_Weight = 0.0f,
                    m_Loop = nodeBlob.Loop,
                    m_Speed = nodeBlob.Speed,
                    m_Time = 0.0f,
                    m_TotalTime = nodeBlob.ClipLength,
                });
            }

            Assert.IsTrue(nodeContextBlob.StateMachineNodes.Length <= byte.MaxValue);
            for (byte nodeIndex = 0; nodeIndex < nodeContextBlob.StateMachineNodes.Length; nodeIndex++)
            {
                var nodeBlob = nodeContextBlob.StateMachineNodes[nodeIndex];
                stateMachineNodeStates.Add(new StateMachineNodeState
                {
                    m_UniqueId = nodeIndex,
                    m_StateIndex = stateIndex,
                    m_StateMachineIndex = new AnimationBufferId(nodeBlob.StateMachineIndex),
                    m_Weight = 0.0f
                });
            }
        }

        public static void UpdateAnimationNode(
            sbyte stateIndex,
            float weight,
            ref DynamicBuffer<FinalPoseNodeState> finalPoseNodeStates,
            ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates)
        {
            //1. find the final pose node
            PoseLinkBlob poseLink = PoseLinkBlob.Null;
            int index = 0;
            for (; index < finalPoseNodeStates.Length; index++)
            {
                var finalPoseNode = finalPoseNodeStates[index];
                if (finalPoseNode.m_StateIndex != stateIndex)
                    continue;
                poseLink = finalPoseNode.m_PoseLinkBlob;
                break;
            }
            Assert.IsTrue(poseLink.IsValid, "we can't find the final pose");

            //2. recursion from final pose
            RecursionAnimationNode(ref poseLink, stateIndex, weight,
                ref singleClipNodeStates,
                ref stateMachineNodeStates);

            //3. refresh final pose node
            {
                var finalPoseNode = finalPoseNodeStates[index];
                finalPoseNode.m_PoseLinkBlob = poseLink;
                finalPoseNodeStates[index] = finalPoseNode;
            }
        }

        private static void RecursionAnimationNode(ref PoseLinkBlob poseLink,
            sbyte stateIndex,
            float weight,
            ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates)
        {
            switch (poseLink.LinkNodeType)
            {
                case AnimationNodeType.SingleClip:
                    {
                        sbyte index = singleClipNodeStates.GetIndexByBufferId(ref poseLink.LinkId, node => node.m_StateIndex == stateIndex);
                        SingleClipNodeState singleClipNode = singleClipNodeStates[index];
                        Assert.IsTrue(singleClipNode.m_StateIndex == stateIndex);
                        singleClipNode.m_Weight = weight;

                        singleClipNodeStates[index] = singleClipNode;
                        return;
                    }
                case AnimationNodeType.StateMachine:
                    {
                        sbyte index = stateMachineNodeStates.GetIndexByBufferId(ref poseLink.LinkId, node => node.m_StateIndex == stateIndex);
                        StateMachineNodeState stateMachineNode = stateMachineNodeStates.GetByBufferId(ref poseLink.LinkId);
                        Assert.IsTrue(stateMachineNode.m_StateIndex == stateIndex);
                        stateMachineNode.m_Weight = weight;

                        stateMachineNodeStates[index] = stateMachineNode;
                        return;
                    }
                case AnimationNodeType.BlendListByBoolNode:
                    break;
                case AnimationNodeType.BlendListByIntNode:
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }
    }

    [BurstCompile]
    public partial struct UpdateAnimationGraphNodeJob : IJobEntity
    {
        public void Execute(Entity entity,
            //node buffer
            ref DynamicBuffer<FinalPoseNodeState> finalPoseNodeStates,
            ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates,
            //other datas
            in AnimationGraphComponent animationGraph)
        {
            AnimationNodeDefines.UpdateAnimationNode(-1, 1.0f,
                ref finalPoseNodeStates, 
                ref singleClipNodeStates, 
                ref stateMachineNodeStates);
        }
    }

    [BurstCompile]
    public partial struct UpdateAnimationStateMachineNodeJob : IJobEntity
    {
        public void Execute(Entity entity,
            //node buffer
            ref DynamicBuffer<FinalPoseNodeState> finalPoseNodeStates,
            ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates,
            //other datas
            ref AnimationStateMachineComponent stateMachineComponent,
            ref DynamicBuffer<AnimationStateComponent> animationStates)
        {
            for (int index = 0; index < animationStates.Length; index++)
            {
                var state = animationStates[index];
                if (!state.IsValid)
                    continue;

                Assert.IsTrue(state.m_StateIndex <= sbyte.MaxValue);
                var stateIndex = (sbyte)state.m_StateIndex;
                AnimationNodeDefines.UpdateAnimationNode(stateIndex, state.m_Weight,
                    ref finalPoseNodeStates,
                    ref singleClipNodeStates,
                    ref stateMachineNodeStates);
            }
        }
    }

    #region NodeStateDefine

    public struct SingleClipNodeState : IBufferElementData, IElementWithUniqueId
    {
        public byte m_UniqueId;

        public sbyte m_StateIndex;

        public ushort m_ClipIndex;
        public byte m_ClipCount;

        public float m_Weight;

        public float m_Time;
        public float m_Speed;
        public bool m_Loop;
        public float m_TotalTime;

        public byte Id => m_UniqueId;

        public AnimationBlendWeight ToAnimationBlendWeight()
        {
            return new AnimationBlendWeight
            {
                m_ClipIndex = m_ClipIndex,
                m_Weight = m_Weight,
                m_Loop = m_Loop,
                m_Speed = m_Speed,
                m_Time = m_Time,
                m_TotalTime = m_TotalTime
            };
        }

        public float GetRemainingAbsoluteTime()
        {
            return m_Loop ? float.MaxValue : m_TotalTime - m_Time;
        }

        public float GetRemainingRatioTime()
        {
            return m_Loop ? float.MaxValue : (m_TotalTime - m_Time) / m_TotalTime;
        }
    }

    public struct StateMachineNodeState : IBufferElementData, IElementWithUniqueId
    {
        public byte m_UniqueId;

        public sbyte m_StateIndex;
        public AnimationBufferId m_StateMachineIndex;

        public float m_Weight;

        public byte Id => m_UniqueId;
    }

    public struct FinalPoseNodeState : IBufferElementData, IElementWithUniqueId
    {
        public byte m_UniqueId;

        public sbyte m_StateIndex;
        public PoseLinkBlob m_PoseLinkBlob;

        public byte Id => m_UniqueId;
    }

    #endregion

    #region Node State Manager

    [BurstCompile]
    public partial struct AddAnimationStateMachineNodeJob : IJobEntity
    {
        [ReadOnly]
        public NativeParallelHashSet<Entity> EntitiesNeedToInit;

        public void Execute(Entity entity,
            ref DynamicBuffer<FinalPoseNodeState> finalPoseNodeStates,
            ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates,
            ref DynamicBuffer<AnimationStateComponent> animationStates,
            in AnimationGraphRefComponent graphRef,
            in AnimationStateMachineComponent stateMachine)
        {
            if (!EntitiesNeedToInit.Contains(entity))
                return;

            for (int index = 0; index < animationStates.Length; index++)
            {
                var state = animationStates[index];
                if (!state.m_NeedInit)
                    continue;

                state.m_NeedInit = false;
                animationStates[index] = state;

                Assert.IsTrue(state.m_StateIndex <= sbyte.MaxValue);
                var stateIndex = (sbyte)state.m_StateIndex;

                //init node context
                ref var stateMachineBlob = ref stateMachine.GetStateMachineBlob();
                ref var nodeContextBlob = ref stateMachineBlob.States[stateIndex].Nodes;

                AnimationNodeDefines.AddAnimationNodeDatas(stateIndex, ref nodeContextBlob, ref finalPoseNodeStates, ref singleClipNodeStates, ref stateMachineNodeStates);
            }
        }
    }

    [BurstCompile]
    public partial struct RemoveAnimationStateMachineNodeJob : IJobEntity
    {
        public void Execute(Entity entity,
            ref DynamicBuffer<FinalPoseNodeState> finalPoseNodeStates,
            ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates,
            ref AnimationStateTransitionComponent animationStateTransition,
            ref DynamicBuffer<AnimationStateComponent> animationStates,
            in AnimationStateMachineComponent stateMachineComponent)
        {
            //remove the state which weight is equal 0
            var toAnimationStateIndex = animationStates.GetIndexByBufferId(ref animationStateTransition.m_StateId);
            for (var i = animationStates.Length - 1; i >= 0; i--)
            {
                var animationState = animationStates[i];
                if (i != toAnimationStateIndex && !animationState.IsValid)
                {
                    //clean up finalPoseNodeStates
                    for (var index = finalPoseNodeStates.Length - 1; index >= 0; index--)
                    {
                        var finalPoseNodeState = finalPoseNodeStates[index];
                        if (finalPoseNodeState.m_StateIndex != animationState.m_StateIndex)
                            continue;

                        finalPoseNodeStates.RemoveAtSwapBack(index);
                    }

                    //clean up singleClipNodeStates
                    for (var index = singleClipNodeStates.Length - 1; index >= 0; index--)
                    {
                        var singleClipNodeState = singleClipNodeStates[index];
                        if (singleClipNodeState.m_StateIndex != animationState.m_StateIndex)
                            continue;

                        singleClipNodeStates.RemoveAtSwapBack(index);
                    }

                    //clean up stateMachineNodeStates
                    for (var index = stateMachineNodeStates.Length - 1; index >= 0; index--)
                    {
                        var stateMachineNodeState = stateMachineNodeStates[index];
                        if (stateMachineNodeState.m_StateIndex != animationState.m_StateIndex)
                            continue;

                        stateMachineNodeStates.RemoveAtSwapBack(index);
                    }

                    animationStates.RemoveAtSwapBack(i);
                }
            }
        }
    }

    #endregion
}
