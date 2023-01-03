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
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(AnimationChangeStateSystem))]
    public partial class AnimationStateMachineSystem : SystemBase
    {
        EntityQuery m_StateMachineEntityQuery;

        protected override void OnCreate()
        {
            m_StateMachineEntityQuery = GetEntityQuery(
                    ComponentType.ReadWrite<AnimationStateMachineComponent>(),
                    ComponentType.ReadOnly<AnimationGraphRefComponent>()
                );
        }

        protected override void OnUpdate()
        {
            var dependency = Dependency;

            var stateMachineCount = m_StateMachineEntityQuery.CalculateEntityCount();
            NativeParallelHashSet<Entity> entitiesNeedToInit = new NativeParallelHashSet<Entity>(stateMachineCount, Allocator.TempJob);
            var writer = entitiesNeedToInit.AsParallelWriter();

            //1. init state machine and evaluate transition
            dependency = new UpdateStateMachineJob 
            { 
                m_EntitiesNeedToInitWriter = writer 
            }.ScheduleParallel(dependency);

            //2. create new state node context
            dependency = new AddAnimationStateMachineNodeJob
            {
                EntitiesNeedToInit = entitiesNeedToInit
            }.ScheduleParallel(dependency);

            Dependency = entitiesNeedToInit.Dispose(dependency);
        }
    }
}
