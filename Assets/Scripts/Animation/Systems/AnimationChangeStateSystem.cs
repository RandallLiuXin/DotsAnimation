using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Assertions;
using Unity.Mathematics;

namespace Tinder.Animation
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(UpdateAnimationNodesSystem))]
    public partial class AnimationChangeStateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var dependency = Dependency;

            float deltaTime = Time.DeltaTime;

            dependency = Entities.WithAll<AnimationStateMachineComponent>().ForEach((
                ref AnimationStateTransitionComponent animationStateTransition,
                ref AnimationCurrentStateComponent animationCurrentState,
                ref AnimationStateTransitionRequestComponent transitionRequest,
                ref DynamicBuffer<AnimationStateComponent> animationStates,
                in AnimationStateMachineComponent stateMachineComponent) =>
            {
                //handle new transitionRequest
                if (transitionRequest.IsValid)
                {
                    var newToStateIndex = animationStates.GetIndexByBufferId(ref transitionRequest.m_NextStateId);
                    //if we don't have a valid state, just transition instantly
                    var transitionDuration = animationCurrentState.IsValid ? transitionRequest.m_TransitionDuration : 0;
                    if (newToStateIndex >= 0)
                    {
                        animationStateTransition = new AnimationStateTransitionComponent
                        {
                            m_StateId = transitionRequest.m_NextStateId,
                            m_TransitionDuration = transitionDuration,
                        };

                        //reset toState time
                        var toState = animationStates[newToStateIndex];
                        toState.m_Time = 0;
                        animationStates[newToStateIndex] = toState;
                    }

                    transitionRequest = AnimationStateTransitionRequestComponent.Null;
                }

                //update all states
                for (var i = 0; i < animationStates.Length; i++)
                {
                    var animationState = animationStates[i];
                    animationState.m_Time += deltaTime;
                    animationStates[i] = animationState;
                }

                //calc transition blend weight
                var toAnimationStateIndex = animationStates.GetIndexByBufferId(ref animationStateTransition.m_StateId);
                if (toAnimationStateIndex >= 0)
                {
                    var totalWeight = stateMachineComponent.m_Weight;

                    //if the current transition has ended
                    if (animationStateTransition.HasEnded(animationStates[toAnimationStateIndex]))
                    {
                        animationCurrentState = new AnimationCurrentStateComponent(animationStateTransition.m_StateId);
                        animationStateTransition = AnimationStateTransitionComponent.Null;
                    }

                    //update animation state weight
                    var toAnimationState = animationStates[toAnimationStateIndex];
                    if (animationStateTransition.m_TransitionDuration < math.EPSILON)
                    {
                        toAnimationState.m_Weight = totalWeight;
                    }
                    else
                    {
                        toAnimationState.m_Weight = math.clamp(toAnimationState.m_Time / animationStateTransition.m_TransitionDuration, 0, totalWeight);
                    }
                    animationStates[toAnimationStateIndex] = toAnimationState;

                    //if we have more than one state, we need to recalc weight for each state
                    if (animationStates.Length > 1)
                    {
                        //normalize weights
                        var sumWeights = 0.0f;
                        for (var i = 0; i < animationStates.Length; i++)
                        {
                            if (i == toAnimationStateIndex)
                                continue;

                            sumWeights += animationStates[i].m_Weight;
                        }

                        Assert.IsTrue(sumWeights >= math.EPSILON, "Remaining weights are zero. Did AnimationStates not get cleaned up?");
                        var remainWeight = totalWeight - toAnimationState.m_Weight;
                        var inverseSumWeights = remainWeight / sumWeights;
                        for (var i = 0; i < animationStates.Length; i++)
                        {
                            if (i == toAnimationStateIndex)
                                continue;

                            var animationState = animationStates[i];
                            animationState.m_Weight *= inverseSumWeights;
                            animationStates[i] = animationState;
                        }
                    }
                }
            }).ScheduleParallel(dependency);

            dependency = new RemoveAnimationStateMachineNodeJob
            {
            }.ScheduleParallel(dependency);

            Dependency = dependency;
        }
    }
}
