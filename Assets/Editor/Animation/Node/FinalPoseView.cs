using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using GraphProcessor;
using Tinder.Animation.Asset;

namespace Tinder.Animation.Editor
{
	[NodeCustomEditor(typeof(FinalPoseNode))]
	public class FinalPoseView : BaseNodeView
	{
	}
}
