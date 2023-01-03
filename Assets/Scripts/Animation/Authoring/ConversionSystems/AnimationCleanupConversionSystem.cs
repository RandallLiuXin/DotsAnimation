using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

// Note: We avoid adding components that would otherwise be added at runtime by the reactive systems.
// That way we keep the serialized data size down.
namespace Tinder.Animation.Authoring.Systems
{
    [UpdateInGroup(typeof(GameObjectConversionGroup), OrderFirst = true)]
    [ConverterVersion("Tinder", 1)]
    //[DisableAutoCreation]
    public class AnimationCleanupConversionSystem : GameObjectConversionSystem
    {
        EntityQuery m_skeletonQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_skeletonQuery = GetEntityQuery(typeof(SkeletonConversionContext));
        }

        protected override void OnUpdate()
        {
            var transformComponentsToRemove = new ComponentTypes(typeof(Translation), typeof(Rotation), typeof(NonUniformScale));

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            Entities.ForEach((SkeletonConversionContext context) =>
            {
                var entity = GetPrimaryEntity(context.animator);

                DstEntityManager.AddComponent<SkeletonRootTag>(entity);

                if (context.isOptimized)
                {
                    var ltrBuffer = DstEntityManager.AddBuffer<OptimizedBoneToRoot>(entity).Reinterpret<float4x4>();
                    ltrBuffer.ResizeUninitialized(context.skeleton.Length);
                    short i = 0;
                    foreach (var b in context.skeleton)
                    {
                        var ltp = float4x4.TRS(b.localPosition, b.localRotation, b.localScale);
                        if (b.parentIndex < 0)
                            ltrBuffer[i] = ltp;
                        else
                            ltrBuffer[i] = math.mul(ltrBuffer[b.parentIndex], ltp);

                        if (b.gameObjectTransform != null)
                        {
                            var boneEntity = GetPrimaryEntity(b.gameObjectTransform);
                            ecb.AddComponent(boneEntity, new BoneOwningSkeletonReference { skeletonRoot = entity });
                            if (b.gameObjectTransform.parent == context.animator.transform)
                            {
                                ecb.AddComponent(boneEntity, new CopyLocalToParentFromBone { boneIndex = i });
                                ecb.RemoveComponent(boneEntity, transformComponentsToRemove);
                            }
                        }

                        i++;
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }

                context.DestroyShadowHierarchy();
            });

            ecb.Playback(DstEntityManager);
            ecb.Dispose();

            EntityManager.RemoveComponent<SkeletonConversionContext>(m_skeletonQuery);
        }
    }
}
