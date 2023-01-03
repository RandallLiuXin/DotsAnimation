using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Tinder.Animation;
using Tinder.Animation.Authoring;
using Tinder.Authoring;

[DisallowMultipleComponent]
public class SingleClipAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IRequestBlobAssets
{
    public List<AnimationClip> clips;

    private SmartBlobberHandle<SkeletonClipSetBlob> m_BlobHandle;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        SingleClip component = default(SingleClip);
        component.blob = m_BlobHandle.Resolve();
        dstManager.AddComponentData(entity, component);
    }

    public void RequestBlobAssets(Entity entity, EntityManager dstEntityManager, GameObjectConversionSystem conversionSystem)
    {
        var animator = gameObject.GetComponent<Animator>();
        Debug.Assert(animator != null);

        var bakeList = new List<SkeletonClipConfig>();
        foreach (var clip in clips)
        {
            var config = new SkeletonClipConfig();
            config.clip = clip;
            config.settings = SkeletonClipCompressionSettings.kDefaultSettings;
            config.events = new ClipEvent[] { };

            bakeList.Add(config);
        }

        m_BlobHandle = conversionSystem.CreateBlob(gameObject, new SkeletonClipSetBakeData()
        {
            animator = animator,
            clips = bakeList.ToArray()
        });
    }
}
