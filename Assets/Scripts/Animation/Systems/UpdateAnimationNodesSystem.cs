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
    [UpdateBefore(typeof(AnimationBlendWeightsSystem))]
    public partial class UpdateAnimationNodesSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var dependency = Dependency;

            dependency = new UpdateAnimationStateMachineNodeJob
            {
            }.ScheduleParallel(dependency);

            Dependency = dependency;
        }
    }
}
