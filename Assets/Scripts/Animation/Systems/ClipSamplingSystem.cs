using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Assertions;
using Tinder.Animation.Event;

namespace Tinder.Animation
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(TRSToLocalToParentSystem))]
    [UpdateBefore(typeof(TRSToLocalToWorldSystem))]
    public partial class ClipSamplingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var dependency = Dependency;

            //1. sample optimize skeleton
            dependency = Entities.WithAll<AnimationGraphComponent>().ForEach((
                ref DynamicBuffer<OptimizedBoneToRoot> boneToRootBuffer,
                in AnimationGraphComponent animationGraph,
                in OptimizedSkeletonHierarchyBlobReference hierarchyRef,
                in DynamicBuffer<AnimationClipSampler> samplers) =>
            {
                ref var clipBlobs = ref animationGraph.m_ClipsBlob.Value;

                var blender = new BufferPoseBlender(boneToRootBuffer);
                var activeSamplerCount = 0;

                Assert.IsTrue(samplers.Length < byte.MaxValue);
                for (byte i = 0; i < samplers.Length; i++)
                {
                    var sampler = samplers[i];

                    if (sampler.m_Weight > math.EPSILON)
                    {
                        ref var clipBlob = ref clipBlobs.clips[sampler.m_ClipIndex];
                        var sampleTime = sampler.m_Loop ? clipBlob.LoopToClipTime(sampler.m_Time) : sampler.m_Time;
                        clipBlob.SamplePose(ref blender, sampler.m_Weight, sampleTime);

                        activeSamplerCount++;
                    }
                }

                //Skip normalizing rotations for now. Magnitudes are already ~1 
                if (activeSamplerCount > 1)
                {
                    blender.NormalizeRotations();
                }
                if (activeSamplerCount > 0)
                {
                    blender.ApplyBoneHierarchyAndFinish(hierarchyRef.blob);
                }
            }).ScheduleParallel(dependency);

            //2. raise animation events
            dependency = Entities.WithAll<AnimationGraphComponent>().ForEach((
                ref DynamicBuffer<RaisedAnimationEvent> raisedAnimationEvents,
                in AnimationGraphComponent animationGraph,
                in DynamicBuffer<AnimationClipSampler> samplers) =>
            {
                raisedAnimationEvents.Clear();

                ref var clipEvents = ref animationGraph.m_ClipEventsBlob.Value.ClipEvents;
                for (var samplerIndex = 0; samplerIndex < samplers.Length; samplerIndex++)
                {
                    var sampler = samplers[samplerIndex];
                    if (sampler.m_Weight <= Mathf.Epsilon)
                        continue;

                    var clipIndex = sampler.m_ClipIndex;
                    var previousSamplerTime = sampler.m_PreviousTime;
                    var currentSamplerTime = sampler.m_Time;

                    ref var events = ref clipEvents[clipIndex].Events;
                    for (short i = 0; i < events.Length; i++)
                    {
                        ref var e = ref events[i];
                        bool shouldRaiseEvent;

                        if (previousSamplerTime > currentSamplerTime)
                        {
                            //this mean we looped the clip
                            Assert.IsTrue(sampler.m_Loop);
                            shouldRaiseEvent = (e.ClipTime > previousSamplerTime && e.ClipTime <= sampler.m_TotalTime) || (e.ClipTime > 0 && e.ClipTime <= currentSamplerTime);
                        }
                        else
                        {
                            shouldRaiseEvent = e.ClipTime > previousSamplerTime && e.ClipTime <= currentSamplerTime;
                        }

                        if (shouldRaiseEvent)
                        {
                            raisedAnimationEvents.Add(new RaisedAnimationEvent()
                            {
                                ClipWeight = sampler.m_Weight,
                                FunctionEvent = e.FunctionEvent,
                                intParameter = e.intParameter,
                                floatParameter = e.floatParameter,
                                stringParameter = e.stringParameter
                            });
                        }
                    }
                }
            }).ScheduleParallel(dependency);

            //3. send animation event to outside
            dependency = Entities.ForEach((in DynamicBuffer<RaisedAnimationEvent> raisedEvents) =>
            {

            }).ScheduleParallel(dependency);

            //4. Sample root delta (need to separate, since we support the animator without root motion)
            dependency = Entities.WithAll<AnimationGraphComponent>().ForEach((
                ref RootDeltaTranslation rootDeltaTranslation,
                ref RootDeltaRotation rootDeltaRotation,
                in AnimationGraphComponent animationGraph,
                in DynamicBuffer<AnimationClipSampler> samplers) =>
            {
                rootDeltaTranslation.Value = 0;
                rootDeltaRotation.Value = quaternion.identity;

                ref var clipBlobs = ref animationGraph.m_ClipsBlob.Value;
                bool needInit = true;
                var rootTf = new BoneTransform();
                var previousRootTf = new BoneTransform();

                Assert.IsTrue(samplers.Length < byte.MaxValue);
                for (byte i = 0; i < samplers.Length; i++)
                {
                    var sampler = samplers[i];
                    if (sampler.m_Weight <= math.EPSILON)
                        continue;

                    ref var clipBlob = ref clipBlobs.clips[sampler.m_ClipIndex];
                    var currentSampleTime = sampler.m_Loop ? clipBlob.LoopToClipTime(sampler.m_Time) : sampler.m_Time;
                    var previousSampleTime = sampler.m_Loop ? clipBlob.LoopToClipTime(sampler.m_PreviousTime) : sampler.m_PreviousTime;
                    if (currentSampleTime == previousSampleTime)
                        continue;

                    if (needInit)
                    {
                        rootTf = SampleWeightedFirstIndex(0, ref clipBlob, currentSampleTime, sampler.m_Weight);
                        previousRootTf = SampleWeightedFirstIndex(0, ref clipBlob, previousSampleTime, sampler.m_Weight);
                        needInit = false;
                    }
                    else
                    {
                        SampleWeightedNIndex(ref rootTf, 0, ref clipBlob, currentSampleTime, sampler.m_Weight);
                        SampleWeightedNIndex(ref previousRootTf, 0, ref clipBlob, previousSampleTime, sampler.m_Weight);
                    }
                }

                rootDeltaTranslation.Value = rootTf.translation - previousRootTf.translation;
                var inv = math.inverse(rootTf.rotation);
                rootDeltaRotation.Value = math.normalizesafe(math.mul(inv, previousRootTf.rotation));
            }).ScheduleParallel(dependency);

            //5. Apply root motion to entity
            dependency = Entities.WithAll<AnimationGraphComponent, ApplyRootMotionToEntity, SkeletonRootTag>().ForEach((
                ref Translation translation,
                ref Rotation rotation,
                in RootDeltaTranslation rootDeltaTranslation,
                in RootDeltaRotation rootDeltaRotation) =>
            {
                translation.Value += rootDeltaTranslation.Value;
                rotation.Value = math.mul(rootDeltaRotation.Value, rotation.Value);
            }).ScheduleParallel(dependency);

            Dependency = dependency;
        }

        private static BoneTransform SampleWeightedFirstIndex(int boneIndex, ref SkeletonClip clip, float time, float weight)
        {
            var bone = clip.SampleBone(boneIndex, time);
            bone.translation *= weight;
            var rot = bone.rotation;
            rot.value *= weight;
            bone.rotation = rot;
            bone.scale *= weight;
            return bone;
        }

        private static void SampleWeightedNIndex(ref BoneTransform bone, int boneIndex, ref SkeletonClip clip, float time, float weight)
        {
            var otherBone = clip.SampleBone(boneIndex, time);
            bone.translation += otherBone.translation * weight;

            //blends rotation. Negates opposing quaternions to be sure to choose the shortest path
            var otherRot = otherBone.rotation;
            var dot = math.dot(otherRot, bone.rotation);
            if (dot < 0)
            {
                otherRot.value = -otherRot.value;
            }

            var rot = bone.rotation;
            rot.value += otherRot.value * weight;
            bone.rotation = rot;

            bone.scale += otherBone.scale * weight;
        }
    }
}
