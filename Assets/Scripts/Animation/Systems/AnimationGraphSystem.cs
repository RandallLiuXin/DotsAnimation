using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Assertions;
using Unity.Collections;

namespace Tinder.Animation
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(AnimationStateMachineSystem))]
    public partial class AnimationGraphSystem : SystemBase
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
            //1. update nodes which in the graph
            dependency = new UpdateAnimationGraphNodeJob
            {
            }.ScheduleParallel(dependency);

            //2. set animation state machine weight
            var stateMachineCount = m_StateMachineEntityQuery.CalculateEntityCount();
            var entitiesWeightMap = new NativeParallelHashMap<Entity, float>(stateMachineCount, Allocator.TempJob);
            var entitiesWeightMapWriter = entitiesWeightMap.AsParallelWriter();

            dependency = Entities.WithAll<AnimationGraphComponent>().ForEach((
                ref DynamicBuffer<AnimationMachineEntityBuffer> machineEntities,
                ref DynamicBuffer<StateMachineNodeState> stateMachineNodeStates) =>
            {
                for (int index = 0; index < stateMachineNodeStates.Length; index++)
                {
                    var stateMachineNode = stateMachineNodeStates[index];
                    if (stateMachineNode.m_Weight < Mathf.Epsilon)
                        continue;

                    var machineEntity = machineEntities.GetByBufferId(ref stateMachineNode.m_StateMachineIndex);
                    //update buffer id cache
                    stateMachineNodeStates[index] = stateMachineNode;

                    entitiesWeightMapWriter.TryAdd(machineEntity.m_StateMachineEntity, stateMachineNode.m_Weight);
                }
            }).ScheduleParallel(dependency);

            dependency = Entities.WithAll<AnimationStateMachineComponent, AnimationGraphRefComponent>()
                .WithReadOnly(entitiesWeightMap).ForEach((
                Entity entity,
                ref AnimationStateMachineComponent animationStateMachine) =>
                {
                    if (!entitiesWeightMap.ContainsKey(entity))
                        return;

                    animationStateMachine.m_Weight = entitiesWeightMap[entity];
                }).ScheduleParallel(dependency);

            Dependency = entitiesWeightMap.Dispose(dependency);
        }
    }
}

