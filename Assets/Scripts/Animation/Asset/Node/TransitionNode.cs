using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

namespace Tinder.Animation.Asset
{
    [Serializable, NodeMenuItem("Animation/TransitionNode")]
    public class TransitionNode : AnimationNodeAsset
    {
        public override AnimationNodeType NodeType => AnimationNodeType.Transition;

        [Input(name = "From", allowMultiple = false)]
        public StateLink FromState;

        [Output(name = "To", allowMultiple = false)]
        public TransitionLink ToState;

        [SerializeField, HideInInspector]
        public TransitionNodeData Data;

        public AnimationStateAsset FromStateAsset
        {
            get
            {
                if (GetInputNodes() == null)
                    return null;
                var node = GetInputNodes().ToList()[0] as StateNode;
                if (node == null)
                    return null;
                return node.StateAsset;
            }
        }

        public AnimationStateAsset ToStateAsset
        {
            get
            {
                if (GetOutputNodes() == null)
                    return null;
                var node = GetOutputNodes().ToList()[0] as StateNode;
                if (node == null)
                    return null;
                return node.StateAsset;
            }
        }

        public override string name => "TransitionNode";
    }
}
