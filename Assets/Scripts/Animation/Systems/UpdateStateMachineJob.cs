using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Tinder.Animation.Asset;
using Unity.Assertions;
using System;
using Unity.Collections;
using Unity.Burst;

namespace Tinder.Animation
{
    [BurstCompile]
    public partial struct UpdateStateMachineJob : IJobEntity
    {
        public NativeParallelHashSet<Entity>.ParallelWriter m_EntitiesNeedToInitWriter;

        public void Execute(Entity entity,
            ref AnimationStateMachineComponent stateMachine,
            ref AnimationStateTransitionRequestComponent animationStateTransitionRequest,
            ref DynamicBuffer<AnimationStateComponent> animationStates,
            in AnimationCurrentStateComponent animationCurrentState,
            in AnimationStateTransitionComponent animationStateTransition,
            in DynamicBuffer<BoolParameter> boolParameters,
            in DynamicBuffer<IntParameter> intParameters,
            in DynamicBuffer<FloatParameter> floatParameters,
            in DynamicBuffer<SingleClipNodeState> singleClipNodeStates)
        {
            //need Initialize
            if (StateMachineNeedInitialize(animationCurrentState, stateMachine))
            {
                ref var stateMachineBlob = ref stateMachine.GetStateMachineBlob();
                stateMachine.m_CurrentState = CreateState(stateMachineBlob.DefaultState, ref animationStates, ref stateMachineBlob);

                animationStateTransitionRequest = new AnimationStateTransitionRequestComponent
                {
                    m_NextStateId = stateMachine.m_CurrentState.m_StateId,
                    m_TransitionDuration = 0
                };

                m_EntitiesNeedToInitWriter.Add(entity);
            }

            //need evaluate
            if (StateMachineNeedEvaluate(animationCurrentState, stateMachine, animationStateTransition))
            {
                ref var stateMachineBlob = ref stateMachine.GetStateMachineBlob();
                ref var currentStateBlob = ref stateMachineBlob.States[stateMachine.m_CurrentState.m_StateIndex];
                var currentAnimationState = animationStates.GetByBufferId(ref stateMachine.m_CurrentState.m_StateId);
                //check transitions
                if (EvaluateTransitions(currentAnimationState, ref currentStateBlob, out var transitionIndex, boolParameters, intParameters, floatParameters, singleClipNodeStates))
                {
                    ref var transition = ref currentStateBlob.Transitions[transitionIndex];
#if UNITY_EDITOR || DEBUG
                    stateMachine.m_PreviousState = stateMachine.m_CurrentState;
#endif
                    stateMachine.m_CurrentState = CreateState(transition.ToStateIndex, ref animationStates, ref stateMachineBlob);
                    animationStateTransitionRequest = new AnimationStateTransitionRequestComponent
                    {
                        m_NextStateId = stateMachine.m_CurrentState.m_StateId,
                        m_TransitionDuration = transition.TransitionDuration
                    };

                    m_EntitiesNeedToInitWriter.Add(entity);
                }
            }
        }

        public static bool StateMachineNeedInitialize(in AnimationCurrentStateComponent animationCurrentState,
            in AnimationStateMachineComponent stateMachine)
        {
            return !animationCurrentState.IsValid && !stateMachine.m_CurrentState.IsValid;
        }

        public static bool StateMachineNeedEvaluate(in AnimationCurrentStateComponent animationCurrentState,
            in AnimationStateMachineComponent stateMachine,
            in AnimationStateTransitionComponent animationStateTransition)
        {
            if (stateMachine.m_CurrentState.IsValid
                && animationCurrentState.IsValid
                && animationCurrentState.m_StateId == stateMachine.m_CurrentState.m_StateId)
            {
                return true;
            }

            if (stateMachine.m_CurrentState.IsValid
                && animationStateTransition.IsValid
                && animationStateTransition.m_StateId == stateMachine.m_CurrentState.m_StateId)
            {
                return true;
            }

            return false;
        }

        private static StateMachineStateRef CreateState(byte stateIndex,
            ref DynamicBuffer<AnimationStateComponent> animationStates,
            ref StateMachineBlob stateMachineBlob)
        {
            ref var state = ref stateMachineBlob.States[stateIndex];

            var animationState = new AnimationStateComponent
            {
                m_StateIndex = stateIndex,
                m_Time = 0,
                m_Weight = 0,
                m_NeedInit = true
            };

            var runtimeId = animationStates.Add(animationState);
            Assert.IsTrue(runtimeId <= sbyte.MaxValue);
            return new StateMachineStateRef
            {
                m_StateIndex = stateIndex,
                m_StateId = new AnimationBufferId(stateIndex, (sbyte)runtimeId)
            };
        }

        private static bool EvaluateTransitions(in AnimationStateComponent animationState, ref AnimationStateBlob state, out short transitionIndex,
            in DynamicBuffer<BoolParameter> boolParameters,
            in DynamicBuffer<IntParameter> intParameters,
            in DynamicBuffer<FloatParameter> floatParameters,
            in DynamicBuffer<SingleClipNodeState> singleClipNodeStates)
        {
            for (short i = 0; i < state.Transitions.Length; i++)
            {
                if (EvaluateTransitionGroup(animationState, ref state.Transitions[i], boolParameters, intParameters, floatParameters, singleClipNodeStates))
                {
                    transitionIndex = i;
                    return true;
                }
            }

            transitionIndex = -1;
            return false;
        }

        private static bool EvaluateTransitionGroup(in AnimationStateComponent animationState, ref AnimationStateTransitionBlob transitionBlob,
            in DynamicBuffer<BoolParameter> boolParameters,
            in DynamicBuffer<IntParameter> intParameters,
            in DynamicBuffer<FloatParameter> floatParameters,
            in DynamicBuffer<SingleClipNodeState> singleClipNodeStates)
        {
            if (transitionBlob.HasEndTime && animationState.m_Time < transitionBlob.EndTime)
            {
                return false;
            }

            ref var conditions = ref transitionBlob.ConditionBlobs.Conditions;
            var shouldTriggerTransition = true;
            for (int index = 0; index < conditions.Length; index++)
            {
                var condition = conditions[index];
                switch (condition.ParameterType)
                {
                    case ConditionParameterType.Bool:
                        shouldTriggerTransition &= AnimationTransitionUtility.DoEvaluate(condition, boolParameters[condition.ParameterIndex].m_Value);
                        break;
                    case ConditionParameterType.Int:
                        shouldTriggerTransition &= AnimationTransitionUtility.DoEvaluate(condition, intParameters[condition.ParameterIndex].m_Value);
                        break;
                    case ConditionParameterType.Float:
                        shouldTriggerTransition &= AnimationTransitionUtility.DoEvaluate(condition, floatParameters[condition.ParameterIndex].m_Value);
                        break;
                    case ConditionParameterType.RemainingTime:
                        {
                            AnimationBufferId bufferId = new AnimationBufferId(condition.ClipIndex);
                            var clipIndex = singleClipNodeStates.GetIndexByBufferId(ref bufferId);
                            Assert.IsTrue(clipIndex != -1);
                            var clipNode = singleClipNodeStates[clipIndex];
                            shouldTriggerTransition &= AnimationTransitionUtility.DoEvaluate(condition, clipNode);
                        }
                        break;
                    default:
                        Assert.IsTrue(false);
                        break;
                }
            }

            return shouldTriggerTransition;
        }
    }
}
