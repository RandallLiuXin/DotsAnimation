using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Tinder.Animation.Asset;
using Unity.Assertions;

namespace Tinder.Animation
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(AnimationGraphSystem))]
    public partial class AnimationSetParameterSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            //1. handle all events

            //2. sync to machine entity
            Entities.WithAll<AnimationGraphComponent>().
                WithoutBurst().ForEach((
                ref DynamicBuffer<AnimationMachineEntityBuffer> machineEntities,
                ref DynamicBuffer<BoolParameter> boolParameters,
                ref DynamicBuffer<IntParameter> intParameters,
                ref DynamicBuffer<FloatParameter> floatParameters) =>
            {
                for (int index = 0; index < machineEntities.Length; index++)
                {
                    var machineEntity = machineEntities[index].m_StateMachineEntity;

                    var boolBuffer = EntityManager.GetBuffer<BoolParameter>(machineEntity);
                    for (int parameterIndex = 0; parameterIndex < boolParameters.Length; parameterIndex++)
                        boolBuffer[parameterIndex] = boolParameters[parameterIndex];

                    var intBuffer = EntityManager.GetBuffer<IntParameter>(machineEntity);
                    for (int parameterIndex = 0; parameterIndex < intParameters.Length; parameterIndex++)
                        intBuffer[parameterIndex] = intParameters[parameterIndex];

                    var floatBuffer = EntityManager.GetBuffer<FloatParameter>(machineEntity);
                    for (int parameterIndex = 0; parameterIndex < floatParameters.Length; parameterIndex++)
                        floatBuffer[parameterIndex] = floatParameters[parameterIndex];
                }
            }).Run();
        }
    }
}
