using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GraphProcessor;
using Tinder.Animation.Asset;

namespace Tinder.Animation.Editor
{
    public class AnimationStateEditor : BaseGraphWindow
    {
        AnimationToolbarView toolbarView;

        protected override void OnDestroy()
        {
            graphView?.Dispose();
        }

        protected override void InitializeWindow(BaseGraph graph)
        {
            titleContent = new GUIContent("Animation State Editor");

            Debug.Assert(graph != null);
            var animationStateAsset = graph as AnimationStateAsset;
            Debug.Assert(animationStateAsset != null);

            if (graphView == null)
            {
                graphView = new AnimationStateView(this);
                toolbarView = new AnimationToolbarView(graphView, animationStateAsset.m_OwnerGraph, animationStateAsset.m_OwnerStateMachine);
                graphView.Add(toolbarView);
            }

            rootView.Add(graphView);
        }

        protected override void InitializeGraphView(BaseGraphView view)
        {
            //graphView.OpenPinned<ExposedParameterView>();
            toolbarView.UpdateButtonStatus();
        }
    }
}
