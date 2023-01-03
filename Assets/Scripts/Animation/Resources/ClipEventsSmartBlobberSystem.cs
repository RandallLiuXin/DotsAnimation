using System.Collections.Generic;
using Tinder.Authoring.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using Tinder.Unsafe;

namespace Tinder.Animation.Event
{
    public struct RaisedAnimationEvent : IBufferElementData
    {
        public float ClipWeight;

        public FixedString64Bytes FunctionEvent;
        public int intParameter;
        public float floatParameter;
        public FixedString64Bytes stringParameter;
    }
}

namespace Tinder.Animation.Event.Authoring
{
    public struct AnimationClipEvent
    {
        public byte ClipIndex;
        public FixedString64Bytes FunctionEvent;
        public float ClipTime;

        public int intParameter;
        public float floatParameter;
        public FixedString64Bytes stringParameter;
    }

    public struct ClipEvents
    {
        public BlobArray<AnimationClipEvent> Events;
    }

    public struct ClipEventsBlob
    {
        public BlobArray<ClipEvents> ClipEvents;
    }

    public struct ClipEventsBlobBakeData
    {
        public AnimationClip[] Clips;
    }

    public struct ClipEventsConversionData
    {
        public UnsafeList<AnimationClipEvent> Events;
    }

    public struct ClipEventsBlobConverter : ISmartBlobberSimpleBuilder<ClipEventsBlob>
    {
        public UnsafeList<ClipEventsConversionData> ClipEvents;
        public unsafe BlobAssetReference<ClipEventsBlob> BuildBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ClipEventsBlob>();
            var clipEvents = builder.Allocate(ref root.ClipEvents, ClipEvents.Length);
            for (var i = 0; i < ClipEvents.Length; i++)
            {
                builder.ConstructFromNativeArray(ref clipEvents[i].Events, ClipEvents[i].Events.Ptr, ClipEvents[i].Events.Length);
            }
            return builder.CreateBlobAssetReference<ClipEventsBlob>(Allocator.Persistent);
        }
    }

    [UpdateAfter(typeof(Animation.Authoring.Systems.SkeletonClipSetSmartBlobberSystem))]
    public class ClipEventsSmartBlobberSystem : SmartBlobberConversionSystem<ClipEventsBlob, ClipEventsBlobBakeData, ClipEventsBlobConverter>
    {
        protected override bool Filter(in ClipEventsBlobBakeData input, GameObject gameObject, out ClipEventsBlobConverter converter)
        {
            converter = CreateConverter(input.Clips, World.UpdateAllocator.ToAllocator);
            return true;
        }

        private static ClipEventsBlobConverter CreateConverter(AnimationClip[] clips, Allocator allocator)
        {
            var converter = new ClipEventsBlobConverter();
            converter.ClipEvents = new UnsafeList<ClipEventsConversionData>(clips.Length, allocator);
            Assert.IsTrue(clips.Length <= byte.MaxValue);
            for (byte clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                var clip = clips[clipIndex];
                Assert.IsNotNull(clip);
                var clipAssetEvents = clip.events;
                var clipEvents = new ClipEventsConversionData
                {
                    Events = new UnsafeList<AnimationClipEvent>(clipAssetEvents.Length, allocator)
                };
                for (var eventIndex = 0; eventIndex < clipAssetEvents.Length; eventIndex++)
                {
                    ref var clipAssetEvent = ref clipAssetEvents[eventIndex];
                    clipEvents.Events.Add(new AnimationClipEvent
                    {
                        ClipIndex = clipIndex,
                        FunctionEvent = clipAssetEvent.functionName,
                        intParameter = clipAssetEvent.intParameter,
                        floatParameter = clipAssetEvent.floatParameter,
                        stringParameter = clipAssetEvent.stringParameter,
                        ClipTime = clipAssetEvent.time
                    });
                }

                converter.ClipEvents.Add(clipEvents);
            }

            return converter;
        }
    }
}
