using System;
using System.Collections.Generic;
using Tinder.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Tinder.Animation.Authoring
{
    internal class SkeletonConversionContext : IComponentData
    {
        public BoneTransformData[] skeleton;
        public bool                isOptimized;
        public Animator            animator;
        public SkeletonAuthoring   authoring;

        /// <summary>
        /// used to generate copy skeleton hierarchy(if the skeleton has been optimized, we will deoptimize it)
        /// </summary>
        public GameObject shadowHierarchy
        {
            get
            {
                if (m_shadowHierarchy == null)
                {
                    m_shadowHierarchy = ShadowHierarchyBuilder.BuildShadowHierarchy(animator.gameObject, isOptimized);
                }
                return m_shadowHierarchy;
            }
        }
        private GameObject m_shadowHierarchy = null;

        public void DestroyShadowHierarchy()
        {
            if (m_shadowHierarchy != null)
            {
                m_shadowHierarchy.DestroyDuringConversion();
            }
        }
    }
}

