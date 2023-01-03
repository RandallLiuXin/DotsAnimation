using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace Tinder.Animation
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(ClipSamplingSystem))]
    public partial class AnimationBlendWeightsSystem : SystemBase
    {
        EntityQuery m_AnimationGraphEntityQuery;
        EntityQuery m_AnimationStateMachineEntityQuery;

        protected override void OnCreate()
        {
            m_AnimationGraphEntityQuery = GetEntityQuery(ComponentType.ReadOnly<AnimationGraphComponent>());
            m_AnimationStateMachineEntityQuery = GetEntityQuery(ComponentType.ReadOnly<AnimationStateMachineComponent>());
        }

        protected override void OnUpdate()
        {
            var dependency = Dependency;

            var animationGraphCount = m_AnimationGraphEntityQuery.CalculateEntityCount();
            var animationStateMachineCount = m_AnimationStateMachineEntityQuery.CalculateEntityCount();
            var blendWeightMap = new NativeParallelMultiHashMap<Entity, AnimationBlendWeight>(animationGraphCount + animationStateMachineCount * 2, Allocator.TempJob);
            var blendWeightMapWriter = blendWeightMap.AsParallelWriter();
            float deltaTime = Time.DeltaTime;

            //1. update all SingleClipNodeState time
            dependency = Entities.WithAny<AnimationGraphComponent, AnimationStateMachineComponent>().ForEach((
                Entity entity,
                ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates) =>
            {
                for (int index = 0; index < singleClipNodeStates.Length; index++)
                {
                    var item = singleClipNodeStates[index];
                    if (item.m_Weight == 0 || item.m_ClipCount == 0)
                        continue;

                    item.m_Time = item.m_Time + deltaTime * item.m_Speed;

                    singleClipNodeStates[index] = item;
                }
            }).ScheduleParallel(dependency);

            //2. go through all animation state machine
            dependency = Entities.WithAll<AnimationStateMachineComponent, AnimationGraphRefComponent>().ForEach((
                Entity entity,
                in DynamicBuffer<SingleClipNodeState> singleClipNodeStates) =>
            {
                foreach (var item in singleClipNodeStates)
                {
                    if (item.m_Weight == 0 || item.m_ClipCount == 0)
                        continue;

                    blendWeightMapWriter.Add(entity, item.ToAnimationBlendWeight());
                }
            }).ScheduleParallel(dependency);

            //3. go through all aniamtion graph
            dependency = Entities.WithAll<AnimationGraphComponent>().ForEach((
                Entity entity,
                in DynamicBuffer<SingleClipNodeState> singleClipNodeStates) =>
            {
                foreach (var item in singleClipNodeStates)
                {
                    if (item.m_Weight == 0 || item.m_ClipCount == 0)
                        continue;

                    blendWeightMapWriter.Add(entity, item.ToAnimationBlendWeight());
                }
            }).ScheduleParallel(dependency);

            //4. create new aniamtion clip for all animation graph
            dependency = Entities.WithAll<AnimationGraphComponent>().WithReadOnly(blendWeightMap).ForEach((
                Entity entity,
                ref DynamicBuffer<AnimationClipSampler> clipSamplers,
                in DynamicBuffer<AnimationMachineEntityBuffer> animationMachines) =>
            {
                if (blendWeightMap.ContainsKey(entity))
                {
                    foreach (var blendWeight in blendWeightMap.GetValuesForKey(entity))
                    {
                        bool foundSampler = false;
                        for (int index = 0; index < clipSamplers.Length; index++)
                        {
                            var sampler = clipSamplers[index];
                            if (sampler.m_ClipIndex != blendWeight.m_ClipIndex)
                                continue;

                            sampler.m_PreviousTime = sampler.m_Time;
                            sampler.m_Time = blendWeight.m_Time;
                            sampler.m_Weight = blendWeight.m_Weight;
                            sampler.m_Loop = blendWeight.m_Loop;
                            sampler.m_TotalTime = blendWeight.m_TotalTime;
                            clipSamplers[index] = sampler;
                            foundSampler = true;
                            break;
                        }

                        if (!foundSampler)
                        {
                            clipSamplers.Add(new AnimationClipSampler
                            {
                                m_ClipIndex = blendWeight.m_ClipIndex,
                                m_PreviousTime = 0.0f,
                                m_Time = blendWeight.m_Time,
                                m_Weight = blendWeight.m_Weight,
                                m_Loop = blendWeight.m_Loop,
                                m_TotalTime = blendWeight.m_TotalTime
                            });
                        }
                    }
                }

                foreach (var child in animationMachines)
                {
                    if (!blendWeightMap.ContainsKey(child.m_StateMachineEntity))
                        continue;

                    foreach (var blendWeight in blendWeightMap.GetValuesForKey(child.m_StateMachineEntity))
                    {
                        bool foundSampler = false;
                        for (int index = 0; index < clipSamplers.Length; index++)
                        {
                            var sampler = clipSamplers[index];
                            if (sampler.m_ClipIndex != blendWeight.m_ClipIndex)
                                continue;

                            sampler.m_PreviousTime = sampler.m_Time;
                            sampler.m_Time = blendWeight.m_Time;
                            sampler.m_Weight = blendWeight.m_Weight;
                            sampler.m_Loop = blendWeight.m_Loop;
                            sampler.m_TotalTime = blendWeight.m_TotalTime;
                            clipSamplers[index] = sampler;
                            foundSampler = true;
                            break;
                        }

                        if (!foundSampler)
                        {
                            clipSamplers.Add(new AnimationClipSampler
                            {
                                m_ClipIndex = blendWeight.m_ClipIndex,
                                m_PreviousTime = 0.0f,
                                m_Time = blendWeight.m_Time,
                                m_Weight = blendWeight.m_Weight,
                                m_Loop = blendWeight.m_Loop,
                                m_TotalTime = blendWeight.m_TotalTime
                            });
                        }
                    }
                }
            }).ScheduleParallel(dependency);

            //5. clean up useless data
            //dependency = Entities.WithAll<AnimationStateMachineComponent>().ForEach((
            //    Entity entity,
            //    ref DynamicBuffer<SingleClipNodeState> singleClipNodeStates,
            //    in DynamicBuffer<AnimationStateComponent> animationStates) =>
            //{
            //    for (int index = singleClipNodeStates.Length - 1; index >= 0; index--)
            //    {
            //        var singleClipNode = singleClipNodeStates[index];
            //        bool isVaild = false;
            //        foreach (var item in animationStates)
            //        {
            //            if (item.m_StateIndex != singleClipNode.m_StateIndex)
            //                continue;

            //            isVaild = true;
            //        }

            //        if (!isVaild)
            //            singleClipNodeStates.RemoveAtSwapBack(index);
            //    }
            //}).ScheduleParallel(dependency);

            //6. clean up useless clip sampler data
            dependency = Entities.WithAll<AnimationGraphComponent>().WithReadOnly(blendWeightMap).ForEach((
                Entity entity,
                ref DynamicBuffer<AnimationClipSampler> clipSamplers,
                in DynamicBuffer<AnimationMachineEntityBuffer> animationMachines) =>
            {
                foreach (var child in animationMachines)
                {
                    if (!blendWeightMap.ContainsKey(child.m_StateMachineEntity))
                        continue;

                    for (int index = clipSamplers.Length - 1; index >= 0; index--)
                    {
                        var sampler = clipSamplers[index];
                        bool isVaild = false;
                        foreach (var blendWeight in blendWeightMap.GetValuesForKey(child.m_StateMachineEntity))
                        {
                            if (blendWeight.m_ClipIndex != sampler.m_ClipIndex)
                                continue;

                            isVaild = true;
                        }

                        if (!isVaild)
                            clipSamplers.RemoveAtSwapBack(index);
                    }
                }
            }).ScheduleParallel(dependency);

            dependency = blendWeightMap.Dispose(dependency);

            Dependency = dependency;
        }
    }
}
