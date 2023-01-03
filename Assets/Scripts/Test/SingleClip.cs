using Unity.Entities;
using Tinder.Animation;

public struct SingleClip : IComponentData
{
    public BlobAssetReference<SkeletonClipSetBlob> blob;
}
