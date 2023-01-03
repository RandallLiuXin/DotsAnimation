using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using Tinder.Animation.Authoring;
using GraphProcessor;

namespace Tinder.Animation.Asset
{
    [Serializable]
    [CreateAssetMenu(menuName = AssetConst.AnimationMenuPath + "/AnimationGraph")]
    public class AnimationGraphAsset : BaseGraph, IGetAnimationGlobalId
    {
        public List<AnimationClipNodeAsset> m_Nodes
        {
            get
            {
                return nodes.OfType<AnimationClipNodeAsset>().ToList();
            }
        }

        public List<AnimationStateMachineAsset> m_StateMachines
        {
            get
            {
                var machines = nodes.OfType<StateMachineNode>().ToList();
                var result = new List<AnimationStateMachineAsset>();
                foreach (var item in machines)
                {
                    result.Add(item.StateMachineAsset);
                }
                return result;
            }
        }

        private IEnumerable<AnimationClip> NodeClips => m_Nodes.SelectMany(node => node.Clips);
        private IEnumerable<AnimationClip> StateMachineClips => m_StateMachines.SelectMany(s => s.Clips);
        public int ClipCount => m_StateMachines.Sum(s => s.ClipCount) + m_Nodes.Sum(node => node.ClipCount);

        public List<AnimationClip> GetClips()
        {
            var clipAssets = new List<AnimationClip>();
            foreach (var item in NodeClips.ToArray())
            {
                if (item == null)
                    continue;
                clipAssets.Add(item);
            }

            foreach (var item in StateMachineClips.ToArray())
            {
                if (item == null)
                    continue;
                clipAssets.Add(item);
            }
            return clipAssets;
        }

        public List<SkeletonClipConfig> GetClipConfigs()
        {
            var clips = new List<SkeletonClipConfig>();
            foreach (var item in NodeClips.ToArray())
            {
                if (item == null)
                    continue;
                clips.Add(new SkeletonClipConfig()
                {
                    clip = item,
                    settings = SkeletonClipCompressionSettings.kDefaultSettings
                });
            }
            foreach (var item in StateMachineClips.ToArray())
            {
                if (item == null)
                    continue;
                clips.Add(new SkeletonClipConfig()
                {
                    clip = item,
                    settings = SkeletonClipCompressionSettings.kDefaultSettings
                });
            }
            return clips;
        }

        public byte GetAnimationNodeGlobalId()
        {
            List<AnimationClipNodeAsset> nodeAssets = new List<AnimationClipNodeAsset>();
            nodeAssets.AddRange(m_Nodes);
            foreach (var stateMachine in m_StateMachines)
            {
                foreach (var state in stateMachine.m_States)
                {
                    nodeAssets.AddRange(state.m_Nodes);
                }
            }

            for (byte id = 1; id <= byte.MaxValue; id++)
            {
                bool isValid = true;
                foreach (var item in nodeAssets)
                {
                    if (id != item.GetClipConfigId)
                        continue;
                    isValid = false;
                    break;
                }

                if (isValid)
                {
                    return id;
                }
            }

            Debug.LogError("you have run out all the available slots for the animation clip node");
            return default;
        }

        public bool HasEventInClips => NodeClips.Any(c => c.events.Length > 0) || StateMachineClips.Any(c => c.events.Length > 0);
    }

    public struct AnimationGraphBlob
    {
        public AnimationNodeContextBlob Nodes;

        public BlobArray<StateMachineBlob> StateMachines;
        public AnimationParameterContextBlob Parameters;
    }

    public interface IGetAnimationGlobalId
    {
        public byte GetAnimationNodeGlobalId();
    }
}
