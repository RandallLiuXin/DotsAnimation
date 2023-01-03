using System.Linq;
using Unity.Entities;
using UnityEngine;
using Tinder.Authoring;
using Tinder.Animation;
using Tinder.Animation.Asset;
using Tinder.Animation.Asset.Systems;
using Unity.Assertions;
using System;
using Tinder.Animation.Event.Authoring;
using Tinder.Animation.Event;

namespace Tinder.Animation.Authoring
{
    public class AnimationGraphAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IRequestBlobAssets
    {
        public AnimationGraphAsset m_AnimationGraphAsset;
        public RootMotionMode m_RootMotionMode;
        public bool m_EnableEvents = true;

        //Blob
        private SmartBlobberHandle<AnimationGraphBlob> m_AnimationGraphBlobHandle;
        private SmartBlobberHandle<SkeletonClipSetBlob> m_ClipsBlobHandle;
        private SmartBlobberHandle<ClipEventsBlob> m_ClipEventsBlobHandle;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var stateGraphBlob = m_AnimationGraphBlobHandle.Resolve();
            var clipsBlob = m_ClipsBlobHandle.Resolve();
            var clipEventsBlob = m_ClipEventsBlobHandle.Resolve();

            AnimationGraphConversionUtils.AddAnimationGraphComponents(dstManager, entity, m_AnimationGraphAsset,
                stateGraphBlob, clipsBlob, clipEventsBlob);

            if (m_EnableEvents && m_AnimationGraphAsset.HasEventInClips)
                dstManager.AddBuffer<RaisedAnimationEvent>(entity);

            AnimationGraphConversionUtils.AddRootMotionComponents(dstManager, entity, m_RootMotionMode);
        }

        public void RequestBlobAssets(Entity entity, EntityManager dstEntityManager, GameObjectConversionSystem conversionSystem)
        {
            var animator = gameObject.GetComponent<Animator>();

            //Animation clips
            {
                var clips = m_AnimationGraphAsset.GetClipConfigs();
                m_ClipsBlobHandle = conversionSystem.CreateBlob(animator.gameObject, new SkeletonClipSetBakeData()
                {
                    animator = animator,
                    clips = clips.ToArray()
                });
            }

            //AnimationGraph
            m_AnimationGraphBlobHandle = conversionSystem.World.GetExistingSystem<AnimationGraphSmartBlobberSystem>().AddToConvert(gameObject, new AnimationGraphBlobBakeData
            {
                AnimationGraphAsset = m_AnimationGraphAsset
            });

            //Animation events
            {
                var clips = m_AnimationGraphAsset.GetClips();
                m_ClipEventsBlobHandle = conversionSystem.World.GetExistingSystem<ClipEventsSmartBlobberSystem>().AddToConvert(gameObject, new ClipEventsBlobBakeData { Clips = clips.ToArray() });
            }
        }
    }

    public static class AnimationGraphConversionUtils
    {
        public static void AddAnimationGraphComponents(EntityManager dstManager, Entity entity, 
            AnimationGraphAsset animationGraphAsset,
            BlobAssetReference<AnimationGraphBlob> aniamtionGraphBlob,
            BlobAssetReference<SkeletonClipSetBlob> clipsBlob,
            BlobAssetReference<ClipEventsBlob> clipEventsBlob)
        {
            //graph data
            {
                var animationGraph = new AnimationGraphComponent
                {
                    m_AnimationGraphBlob = aniamtionGraphBlob,
                    m_ClipsBlob = clipsBlob,
                    m_ClipEventsBlob = clipEventsBlob,
                };

                dstManager.AddComponentData(entity, animationGraph);
                dstManager.AddBuffer<AnimationMachineEntityBuffer>(entity);
                var clipSamplers = dstManager.AddBuffer<AnimationClipSampler>(entity);
                clipSamplers.Capacity = 10;

                AddAnimationNodeDatas(dstManager, entity, ref aniamtionGraphBlob.Value.Nodes);
            }

            //state machine data
            Assert.IsTrue(animationGraphAsset.m_StateMachines.Count <= byte.MaxValue);
            for (byte index = 0; index < (byte)animationGraphAsset.m_StateMachines.Count; index++)
            {
                var stateMachine = new AnimationStateMachineComponent
                {
                    m_AnimationGraphBlob = aniamtionGraphBlob,
                    m_StateMachineIndex = index,
                    m_Weight = 0.0f,
                    m_CurrentState = StateMachineStateRef.Null,
#if UNITY_EDITOR || DEBUG
                    m_PreviousState = StateMachineStateRef.Null
#endif
                };

                var fsmEntity = dstManager.CreateEntity();
                dstManager.AddComponentData(fsmEntity, stateMachine);
                AddAnimationStateComponents(dstManager, fsmEntity);
                AddAnimationNodeDatas(dstManager, fsmEntity);
                AddAnimationParameter(dstManager, fsmEntity, aniamtionGraphBlob);

                dstManager.AddComponentData(fsmEntity, new AnimationGraphRefComponent { m_GraphEntity = entity });

                var machineEntityBuffer = dstManager.GetBuffer<AnimationMachineEntityBuffer>(entity);
                machineEntityBuffer.Add(new AnimationMachineEntityBuffer { m_StateMachineEntity = fsmEntity, m_StateMachineIndex = index });
            }

            //Parameters
            {
                AddAnimationParameter(dstManager, entity, aniamtionGraphBlob);
            }
        }

        public static void AddAnimationStateComponents(EntityManager dstManager, Entity entity)
        {
            dstManager.AddBuffer<AnimationStateComponent>(entity);
            dstManager.AddComponentData(entity, AnimationStateTransitionComponent.Null);
            dstManager.AddComponentData(entity, AnimationStateTransitionRequestComponent.Null);
            dstManager.AddComponentData(entity, AnimationCurrentStateComponent.Null);
            dstManager.AddComponentData(entity, AnimationPreserveStateComponent.Null);
        }

        public static void AddAnimationNodeDatas(EntityManager dstManager, Entity entity)
        {
            AnimationNodeDefines.AddAnimationNodeBufferInternal(dstManager, entity);
            //the state graph node will be added only when the state is created
        }

        public static void AddAnimationNodeDatas(EntityManager dstManager, Entity entity, ref AnimationNodeContextBlob nodeContextBlob)
        {
            AnimationNodeDefines.AddAnimationNodeBufferInternal(dstManager, entity);
            //add animation graph node directly
            AnimationNodeDefines.AddAnimationNodeDatas(dstManager, entity, ref nodeContextBlob);
        }

        public static void AddRootMotionComponents(EntityManager dstManager, Entity entity, RootMotionMode mode)
        {
            switch (mode)
            {
                case RootMotionMode.Disabled:
                    break;
                case RootMotionMode.EnabledAuto:
                    dstManager.AddComponentData(entity, new RootDeltaTranslation());
                    dstManager.AddComponentData(entity, new RootDeltaRotation());
                    dstManager.AddComponentData(entity, new ApplyRootMotionToEntity());

                    break;
                case RootMotionMode.EnabledManual:
                    dstManager.AddComponentData(entity, new RootDeltaTranslation());
                    dstManager.AddComponentData(entity, new RootDeltaRotation());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void AddAnimationParameter(EntityManager dstManager, Entity entity, BlobAssetReference<AnimationGraphBlob> aniamtionGraphBlob)
        {
            //TODO Randall 动画图和动画状态机需要在这里做出区分，状态机仅保存自己需要的数据
            dstManager.AddBuffer<BoolParameter>(entity);
            dstManager.AddBuffer<IntParameter>(entity);
            dstManager.AddBuffer<FloatParameter>(entity);

            ref var parameters = ref aniamtionGraphBlob.Value.Parameters;

            var boolParameters = dstManager.GetBuffer<BoolParameter>(entity);
            foreach (var item in parameters.BoolParameters.ToArray())
                boolParameters.Add(new BoolParameter(item));

            var intParameters = dstManager.GetBuffer<IntParameter>(entity);
            foreach (var item in parameters.IntParameters.ToArray())
                intParameters.Add(new IntParameter(item));

            var floatParameters = dstManager.GetBuffer<FloatParameter>(entity);
            foreach (var item in parameters.FloatParameters.ToArray())
                floatParameters.Add(new FloatParameter(item));
        }
    }
}
