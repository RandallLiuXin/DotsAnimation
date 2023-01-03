using Unity.Entities;
using Unity.Transforms;
using Tinder.Animation;
using Unity.Mathematics;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial class GSingleClipPlayerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float t = (float)Time.ElapsedTime;

        Entities.ForEach((ref DynamicBuffer<OptimizedBoneToRoot> btrBuffer, in OptimizedSkeletonHierarchyBlobReference hierarchyRef, in SingleClip singleClip) =>
        {
            ref var clip = ref singleClip.blob.Value.clips[0];
            var clipTime = clip.LoopToClipTime(t);

            var bones = btrBuffer.Reinterpret<float4x4>().AsNativeArray();

            for (int i = 1; i < bones.Length; i++)
            {
                var boneTransform = clip.SampleBone(i, clipTime);

                var mat = float4x4.TRS(boneTransform.translation, boneTransform.rotation, boneTransform.scale);
                var parentIndex = hierarchyRef.blob.Value.parentIndices[i];
                bones[i] = math.mul(bones[parentIndex], mat);
            }
        }).ScheduleParallel();
    }
}
