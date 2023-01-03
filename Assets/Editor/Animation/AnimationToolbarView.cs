using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GraphProcessor;
using Tinder.Animation.Asset;
using Status = UnityEngine.UIElements.DropdownMenuAction.Status;

namespace Tinder.Animation.Editor
{
    public class AnimationToolbarView : ToolbarView
    {
        public AnimationGraphAsset m_GraphAsset;
        public AnimationStateMachineAsset m_StateMachineAsset;

        protected ToolbarButtonData m_ShowProcessor;
        protected ToolbarButtonData m_ShowParameters;

        public AnimationToolbarView(BaseGraphView graphView, AnimationGraphAsset graphAsset = null, AnimationStateMachineAsset stateMachineAsset = null) : base(graphView) 
        {
            m_GraphAsset = graphAsset;
            m_StateMachineAsset = stateMachineAsset;
        }

        protected override void AddButtons()
        {
            // add the default buttons (center, show processor and show in project)
            AddButton("Center", graphView.ResetPositionAndZoom);

            bool exposedParamsVisible = graphView.GetPinnedElementStatus<AnimationExposedParameterView>() != Status.Hidden;
            m_ShowParameters = AddToggle("Parameters", exposedParamsVisible, (v) => graphView.ToggleView<AnimationExposedParameterView>());
            bool processorVisible = graphView.GetPinnedElementStatus<ProcessorView>() != Status.Hidden;
            m_ShowProcessor = AddToggle("Processor", processorVisible, (v) => graphView.ToggleView<ProcessorView>());

            AddButton("Show In Project", () => EditorGUIUtility.PingObject(graphView.graph), false);

            bool conditionalProcessorVisible = graphView.GetPinnedElementStatus<ConditionalProcessorView>() != Status.Hidden;
            AddToggle("Conditional Processor", conditionalProcessorVisible, (v) => graphView.ToggleView<ConditionalProcessorView>());

            if (m_GraphAsset != null)
                AddButton("Open Graph", () => EditorWindow.GetWindow<AnimationGraphEditor>().InitializeGraph(m_GraphAsset), false);
            if (m_StateMachineAsset != null)
                AddButton("Open FSM", () => EditorWindow.GetWindow<AnimationStateMachineEditor>().InitializeGraph(m_StateMachineAsset), false);
        }

        public override void UpdateButtonStatus()
        {
            if (m_ShowParameters != null)
                m_ShowParameters.value = graphView.GetPinnedElementStatus<AnimationExposedParameterView>() != Status.Hidden;
            if (m_ShowProcessor != null)
                m_ShowProcessor.value = graphView.GetPinnedElementStatus<ProcessorView>() != Status.Hidden;
        }
    }
}
