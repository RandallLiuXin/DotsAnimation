using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

namespace Tinder.Animation.Asset
{
    [Serializable, NodeMenuItem("Animation/StateNode")]
    public class StateNode : AnimationNodeAsset
    {
        public override AnimationNodeType NodeType => AnimationNodeType.State;

        [Input(name = "FromState", allowMultiple = true)]
        public TransitionLink Transition;

        [Output(name = "ToState", allowMultiple = true)]
        public StateLink ToState;

        public string StateName
        {
            get
            {
                return m_StateName;
            }
            set
            {
                m_StateName = value;
                StateAsset.m_StateName = m_StateName;
            }
        }

        private string m_StateName;

        [HideInInspector]
        public AnimationStateAsset StateAsset;

        public override string name => "StateNode";
    }
}
