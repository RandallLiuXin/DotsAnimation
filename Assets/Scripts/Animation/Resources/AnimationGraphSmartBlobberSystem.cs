using System.Collections.Generic;
using Tinder.Authoring;
using Tinder.Authoring.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Tinder.Animation.Authoring.Systems;
using System.Linq;
using System;
using Tinder.Unsafe;
using UnityEngine.Assertions;

namespace Tinder.Animation.Asset
{
    public struct AnimationGraphBlobBakeData
    {
        public AnimationGraphAsset AnimationGraphAsset;
    }

    public struct AnimationParameterContext
    {
        public UnsafeList<BoolParameterBlob> BoolParameters;
        public UnsafeList<IntParameterBlob> IntParameters;
        public UnsafeList<FloatParameterBlob> FloatParameters;
    }

    public struct AnimationParameterContextBlob
    {
        public BlobArray<BoolParameterBlob> BoolParameters;
        public BlobArray<IntParameterBlob> IntParameters;
        public BlobArray<FloatParameterBlob> FloatParameters;
    }

    public struct AnimationNodeContext
    {
        public FinalPoseNodeBlob FinalNode;

        public UnsafeList<SingleClipNodeBlob> SingleClipNodes;
        public UnsafeList<StateMachineNodeBlob> StateMachineNodes;
    }

    public struct AnimationNodeContextBlob
    {
        public FinalPoseNodeBlob FinalNode;

        public BlobArray<SingleClipNodeBlob> SingleClipNodes;
        public BlobArray<StateMachineNodeBlob> StateMachineNodes;
    }

    public struct AnimationNodeRuntimeConvertContext
    {
        public SingleClipNode[] SingleClipNodes;
        public StateMachineNode[] StateMachineNodes;

        public int FindAnimationNodeAssetIndexInList(AnimationNodeAsset targetAsset)
        {
            switch (targetAsset.NodeType)
            {
                case AnimationNodeType.SingleClip:
                    for (int index = 0; index < SingleClipNodes.Length; index++)
                    {
                        var node = SingleClipNodes[index];
                        if (node.Equals(targetAsset))
                            return node.GetClipConfigId;
                    }
                    Assert.IsTrue(false);
                    return -1;
                case AnimationNodeType.StateMachine:
                    for (int index = 0; index < StateMachineNodes.Length; index++)
                    {
                        var node = StateMachineNodes[index];
                        if (node.Equals(targetAsset))
                            return index;
                    }
                    Assert.IsTrue(false);
                    return -1;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public partial class AnimationGraphBlobberSystemHelper
    {
        public static void BuildParameters(AnimationGraphAsset animationGraphAsset, ref AnimationGraphBlobConverter converter,
            Allocator allocator)
        {
            var context = new AnimationParameterContext();

            var boolParameters = animationGraphAsset.exposedParameters.OfType<GraphProcessor.BoolParameter>().ToArray();
            var intParameters = animationGraphAsset.exposedParameters.OfType<GraphProcessor.IntParameter>().ToArray();
            var floatParameters = animationGraphAsset.exposedParameters.OfType<GraphProcessor.FloatParameter>().ToArray();

            context.BoolParameters = new UnsafeList<BoolParameterBlob>(boolParameters.Length, allocator);
            context.IntParameters = new UnsafeList<IntParameterBlob>(intParameters.Length, allocator);
            context.FloatParameters = new UnsafeList<FloatParameterBlob>(floatParameters.Length, allocator);

            foreach (var item in boolParameters)
                context.BoolParameters.Add(new BoolParameterBlob
                {
                    Name = item.name,
                    DefaultValue = (bool)item.value,
                });
            foreach (var item in intParameters)
                context.IntParameters.Add(new IntParameterBlob
                {
                    Name = item.name,
                    DefaultValue = (int)item.value,
                });
            foreach (var item in floatParameters)
                context.FloatParameters.Add(new FloatParameterBlob
                {
                    Name = item.name,
                    DefaultValue = (float)item.value,
                });

            converter.ParametersContext = context;
        }

        public static void BuildNodes(AnimationGraphAsset animationGraphAsset, ref AnimationGraphBlobConverter converter,
            Allocator allocator)
        {
            var context = new AnimationNodeContext();
            BuildAnimationNodeContext(animationGraphAsset, in animationGraphAsset.nodes, ref context, allocator);
            converter.NodesContext = context;
        }

        private static void BuildAnimationNodeContext(AnimationGraphAsset animationGraphAsset, in List<GraphProcessor.BaseNode> nodes, ref AnimationNodeContext context,
            Allocator allocator)
        {
            var stateMachines = animationGraphAsset.m_StateMachines;
            var clipAssets = animationGraphAsset.GetClips();

            var convertContext = new AnimationNodeRuntimeConvertContext();

            var finalPoseNodes = nodes.OfType<FinalPoseNode>().ToArray();
            Assert.IsTrue(finalPoseNodes.Length == 1);
            var finalPoseNode = finalPoseNodes[0];

            convertContext.StateMachineNodes = nodes.OfType<StateMachineNode>().ToArray();
            context.StateMachineNodes = new UnsafeList<StateMachineNodeBlob>(convertContext.StateMachineNodes.Length, allocator);
            convertContext.SingleClipNodes = nodes.OfType<SingleClipNode>().ToArray();
            context.SingleClipNodes = new UnsafeList<SingleClipNodeBlob>(convertContext.SingleClipNodes.Length, allocator);

            foreach (var item in convertContext.StateMachineNodes)
                context.StateMachineNodes.Add(new StateMachineNodeBlob(stateMachines, item));
            foreach (var item in convertContext.SingleClipNodes)
                context.SingleClipNodes.Add(new SingleClipNodeBlob(clipAssets, item));

            context.FinalNode = new FinalPoseNodeBlob(convertContext, finalPoseNode);
        }

        public static void BuildStateMachines(AnimationGraphAsset animationGraphAsset, ref AnimationGraphBlobConverter converter,
            Allocator allocator)
        {
            converter.StateMachines = new UnsafeList<StateMachineConvertContext>(animationGraphAsset.m_StateMachines.Count, allocator);

            foreach (var item in animationGraphAsset.m_StateMachines)
            {
                int defaultState = item.m_States.FindIndex(state => state.Equals(item.m_DefaultState));
                Debug.Assert(defaultState < byte.MaxValue && defaultState >= byte.MinValue);
                var stateMachine = new StateMachineConvertContext
                {
                    DefaultState = (byte)defaultState,
                    States = new UnsafeList<AnimationStateConvertContext>(item.m_States.Count, allocator)
                };

                foreach (var state in item.m_States)
                {
                    var context = new AnimationStateConvertContext();
                    context.Name = state.m_StateName;
                    BuildAnimationNodeContext(animationGraphAsset, in state.nodes, ref context.Nodes, allocator);

                    var transitions = item.m_Transitions.FindAll(transition => transition.FromStateAsset.Equals(state));
                    context.Transitions = new UnsafeList<AnimationStateTransitionConvertContext>(transitions.Count, allocator);
                    foreach (var transition in transitions)
                    {
                        var fromStateIndex = item.m_States.FindIndex(state => state.Equals(transition.FromStateAsset));
                        var toStateIndex = item.m_States.FindIndex(state => state.Equals(transition.ToStateAsset));
                        Debug.Assert(fromStateIndex < ushort.MaxValue && fromStateIndex >= ushort.MinValue);
                        Debug.Assert(toStateIndex < ushort.MaxValue && toStateIndex >= ushort.MinValue);

                        var conditionContext = new AnimationTransitionConditionsConvertContext();
                        conditionContext.Conditions = new UnsafeList<AnimationTransitionConditionBlob>(
                            transition.Data.m_ValueConditions.Count + transition.Data.m_RemainingTimeConditions.Count, allocator);

                        for (int index = 0; index < transition.Data.m_ValueConditions.Count; index++)
                        {
                            var condition = transition.Data.m_ValueConditions[index];
                            var conditionBlob = new AnimationTransitionConditionBlob
                            {
                                CompareType = condition.CompareType,
                                ParameterType = condition.ParameterType
                            };

                            #region set ParameterIndex and condition value
                            switch (condition.ParameterType)
                            {
                                case ConditionParameterType.Bool:
                                    {
                                        conditionBlob.bValue = condition.bValue;

                                        conditionBlob.ParameterIndex = -1;
                                        var boolParameters = converter.ParametersContext.BoolParameters;
                                        for (sbyte conditionIndex = 0; conditionIndex < boolParameters.Length; conditionIndex++)
                                        {
                                            var parameter = boolParameters[conditionIndex];
                                            if (parameter.HashValue != condition.ParameterAssetName.GetHashCode())
                                                continue;
                                            conditionBlob.ParameterIndex = conditionIndex;
                                            break;
                                        }
                                    }
                                    break;
                                case ConditionParameterType.Int:
                                    {
                                        conditionBlob.iValue = condition.iValue;

                                        conditionBlob.ParameterIndex = -1;
                                        var intParameters = converter.ParametersContext.IntParameters;
                                        for (sbyte conditionIndex = 0; conditionIndex < intParameters.Length; conditionIndex++)
                                        {
                                            var parameter = intParameters[conditionIndex];
                                            if (parameter.HashValue != condition.ParameterAssetName.GetHashCode())
                                                continue;
                                            conditionBlob.ParameterIndex = conditionIndex;
                                            break;
                                        }
                                    }
                                    break;
                                case ConditionParameterType.Float:
                                    {
                                        conditionBlob.fValue = condition.fValue;

                                        conditionBlob.ParameterIndex = -1;
                                        var floatParameters = converter.ParametersContext.FloatParameters;
                                        for (sbyte conditionIndex = 0; conditionIndex < floatParameters.Length; conditionIndex++)
                                        {
                                            var parameter = floatParameters[conditionIndex];
                                            if (parameter.HashValue != condition.ParameterAssetName.GetHashCode())
                                                continue;
                                            conditionBlob.ParameterIndex = conditionIndex;
                                            break;
                                        }
                                    }
                                    break;
                                default:
                                    Assert.IsTrue(false);
                                    break;
                            }
                            #endregion

                            Assert.IsTrue(conditionBlob.ParameterIndex != -1);
                            conditionContext.Conditions.Add(conditionBlob);
                        }

                        for (int index = 0; index < transition.Data.m_RemainingTimeConditions.Count; index++)
                        {
                            var condition = transition.Data.m_RemainingTimeConditions[index];
                            var conditionBlob = new AnimationTransitionConditionBlob
                            {
                                CompareType = CompareType.LessThanOrEqual,
                                ParameterType = ConditionParameterType.RemainingTime,
                                RemainingTimeType = condition.RemainingTimeType,
                                fRemainingTime = condition.fRemainingTime,
                                ClipIndex = condition.ClipNodeConfigId
                            };

                            Assert.IsTrue(conditionBlob.ParameterIndex != -1);
                            conditionContext.Conditions.Add(conditionBlob);
                        }

                        var transitionContext = new AnimationStateTransitionConvertContext
                        {
                            FromStateIndex = (byte)fromStateIndex,
                            ToStateIndex = (byte)toStateIndex,
                            Priority = transition.Data.m_Priority,
                            HasEndTime = transition.Data.m_HasEndTime,
                            EndTime = transition.Data.m_EndTime,
                            TransitionDuration = transition.Data.m_TransitionDuration,
                            Mode = transition.Data.m_Mode,
                            ConditionContexts = conditionContext
                        };
                        context.Transitions.Add(transitionContext);
                    }

                    stateMachine.States.Add(context);
                }

                converter.StateMachines.Add(stateMachine);
            }
        }
    }

    public struct AnimationGraphBlobConverter : ISmartBlobberSimpleBuilder<AnimationGraphBlob>
    {
        public AnimationNodeContext NodesContext;

        public UnsafeList<StateMachineConvertContext> StateMachines;
        public AnimationParameterContext ParametersContext;

        public unsafe BlobAssetReference<AnimationGraphBlob> BuildBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AnimationGraphBlob>();

            //Node
            {
                root.Nodes = new AnimationNodeContextBlob();
                root.Nodes.FinalNode = NodesContext.FinalNode;
                builder.ConstructFromNativeArray(ref root.Nodes.StateMachineNodes, NodesContext.StateMachineNodes.Ptr, NodesContext.StateMachineNodes.Length);
                builder.ConstructFromNativeArray(ref root.Nodes.SingleClipNodes, NodesContext.SingleClipNodes.Ptr, NodesContext.SingleClipNodes.Length);
            }

            //StateMachines
            {
                var stateMachinesLength = StateMachines.Length;
                var stateMachineBlobs = builder.Allocate(ref root.StateMachines, stateMachinesLength);
                for (ushort index = 0; index < stateMachinesLength; index++)
                {
                    var stateMachineContext = StateMachines[index];
                    stateMachineBlobs[index] = new StateMachineBlob
                    {
                        DefaultState = stateMachineContext.DefaultState
                    };

                    //States
                    {
                        var context = stateMachineContext.States;
                        var stateBlobs = builder.Allocate(ref stateMachineBlobs[index].States, context.Length);
                        for (ushort stateIndex = 0; stateIndex < context.Length; stateIndex++)
                        {
                            var stateContext = context[stateIndex];
                            stateBlobs[stateIndex] = new AnimationStateBlob
                            {
                                Name = stateContext.Name,
                                Nodes = new AnimationNodeContextBlob()
                            };

                            //Nodes
                            stateBlobs[stateIndex].Nodes.FinalNode = stateContext.Nodes.FinalNode;
                            builder.ConstructFromNativeArray(ref stateBlobs[stateIndex].Nodes.StateMachineNodes,
                                stateContext.Nodes.StateMachineNodes.Ptr,
                                stateContext.Nodes.StateMachineNodes.Length);
                            builder.ConstructFromNativeArray(ref stateBlobs[stateIndex].Nodes.SingleClipNodes, 
                                stateContext.Nodes.SingleClipNodes.Ptr, 
                                stateContext.Nodes.SingleClipNodes.Length);

                            //Transitions
                            {
                                var transitionsContext = stateContext.Transitions;
                                var transitionBlobs = builder.Allocate(ref stateBlobs[stateIndex].Transitions, transitionsContext.Length);
                                for (ushort transitionIndex = 0; transitionIndex < transitionsContext.Length; transitionIndex++)
                                {
                                    var transitionContext = transitionsContext[transitionIndex];
                                    transitionBlobs[transitionIndex] = new AnimationStateTransitionBlob
                                    {
                                        FromStateIndex = transitionContext.FromStateIndex,
                                        ToStateIndex = transitionContext.ToStateIndex,
                                        Priority = transitionContext.Priority,
                                        HasEndTime = transitionContext.HasEndTime,
                                        EndTime = transitionContext.EndTime,
                                        TransitionDuration = transitionContext.TransitionDuration,
                                        Mode = transitionContext.Mode,
                                        ConditionBlobs = new AnimationTransitionConditionBlobs()
                                    };
                                    builder.ConstructFromNativeArray(ref transitionBlobs[transitionIndex].ConditionBlobs.Conditions,
                                        transitionContext.ConditionContexts.Conditions.Ptr,
                                        transitionContext.ConditionContexts.Conditions.Length);
                                }
                            }
                        }
                    }
                }
            }

            //Parameters
            {
                root.Parameters = new AnimationParameterContextBlob();
                builder.ConstructFromNativeArray(ref root.Parameters.BoolParameters, ParametersContext.BoolParameters.Ptr, ParametersContext.BoolParameters.Length);
                builder.ConstructFromNativeArray(ref root.Parameters.IntParameters, ParametersContext.IntParameters.Ptr, ParametersContext.IntParameters.Length);
                builder.ConstructFromNativeArray(ref root.Parameters.FloatParameters, ParametersContext.FloatParameters.Ptr, ParametersContext.FloatParameters.Length);
            }

            return builder.CreateBlobAssetReference<AnimationGraphBlob>(Allocator.Persistent);
        }
    }
}

namespace Tinder.Animation.Asset.Systems
{
    [UpdateAfter(typeof(SkeletonClipSetSmartBlobberSystem))]
    internal class AnimationGraphSmartBlobberSystem : SmartBlobberConversionSystem<AnimationGraphBlob, AnimationGraphBlobBakeData, AnimationGraphBlobConverter>
    {
        protected override bool Filter(in AnimationGraphBlobBakeData input, GameObject gameObject, out AnimationGraphBlobConverter converter)
        {
            var allocator = World.UpdateAllocator.ToAllocator;
            converter = CreateConverter(input.AnimationGraphAsset, allocator);
            return true;
        }

        private AnimationGraphBlobConverter CreateConverter(AnimationGraphAsset animationGraphAsset, Allocator allocator)
        {
            var converter = new AnimationGraphBlobConverter();
            AnimationGraphBlobberSystemHelper.BuildParameters(animationGraphAsset, ref converter, allocator);
            AnimationGraphBlobberSystemHelper.BuildNodes(animationGraphAsset, ref converter, allocator);
            AnimationGraphBlobberSystemHelper.BuildStateMachines(animationGraphAsset, ref converter, allocator);
            return converter;
        }
    }
}
