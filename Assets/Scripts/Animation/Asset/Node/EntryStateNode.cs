using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;


namespace Tinder.Animation.Asset
{
	[Serializable, NodeMenuItem("Custom/EntryStateNode")]
	public class EntryStateNode : AnimationNodeAsset
    {
		[Output(name = "DefaultState", allowMultiple = false)]
		public TransitionLink DefaultState;

		public override string name => "EntryStateNode";

		public override AnimationNodeType NodeType => AnimationNodeType.EntryNode;
    }
}
