using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Linq;
using System;
using GraphProcessor;

namespace Tinder.Animation.Asset
{
    public class AnimationExposedParameterView : PinnedElementView
    {
        protected bool m_IsGraphAsset;
        protected AnimationGraphAsset m_GraphAsset;
        protected AnimationGraphAsset graphAsset
        {
            get 
            {
                if (m_GraphAsset == null)
                {
                    var graph = graphView.graph;
                    Debug.Assert(graph != null);
                    m_IsGraphAsset = graph is AnimationGraphAsset;
                    if (m_IsGraphAsset)
                        m_GraphAsset = graph as AnimationGraphAsset;
                    else if (graph is AnimationStateMachineAsset)
                        m_GraphAsset = (graph as AnimationStateMachineAsset).m_OwnerGraph;
                    else if (graph is AnimationStateAsset)
                        m_GraphAsset = (graph as AnimationStateAsset).m_OwnerGraph;
                    else
                        throw new NotImplementedException();
                }
                return m_GraphAsset; 
            }
        }
        protected BaseGraphView graphView;

        readonly string exposedParameterViewStyle = "GraphProcessorStyles/ExposedParameterView";

        List<Rect> blackboardLayouts = new List<Rect>();

        public AnimationExposedParameterView()
        {
            var style = Resources.Load<StyleSheet>(exposedParameterViewStyle);
            if (style != null)
                styleSheets.Add(style);
        }

        protected virtual void OnAddClicked()
        {
            var parameterType = new GenericMenu();

            foreach (var paramType in GetExposedParameterTypes())
                parameterType.AddItem(new GUIContent(GetNiceNameFromType(paramType)), false, () =>
                {
                    string uniqueName = "New " + GetNiceNameFromType(paramType);

                    uniqueName = GetUniqueExposedPropertyName(uniqueName);
                    graphAsset.AddExposedParameter(uniqueName, paramType);
                });

            parameterType.ShowAsContext();
        }

        protected string GetNiceNameFromType(Type type)
        {
            string name = type.Name;

            // Remove parameter in the name of the type if it exists
            name = name.Replace("Parameter", "");

            return ObjectNames.NicifyVariableName(name);
        }

        protected string GetUniqueExposedPropertyName(string name)
        {
            // Generate unique name
            string uniqueName = name;
            int i = 0;
            while (graphAsset.exposedParameters.Any(e => e.name == name))
                name = uniqueName + " " + i++;
            return name;
        }

        protected virtual IEnumerable<Type> GetExposedParameterTypes()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<ExposedParameter>())
            {
                if (type.IsGenericType)
                    continue;

                yield return type;
            }
        }

        protected virtual void UpdateParameterList()
        {
            content.Clear();

            if (m_IsGraphAsset)
            {
                foreach (var param in graphAsset.exposedParameters)
                {
                    var row = new BlackboardRow(new ExposedParameterFieldView(graphView, param), new ExposedParameterPropertyView(graphView, param));
                    row.expanded = param.settings.expanded;
                    row.RegisterCallback<GeometryChangedEvent>(e => {
                        param.settings.expanded = row.expanded;
                    });

                    content.Add(row);
                }
            }
            else
            {
                foreach (var param in graphAsset.exposedParameters)
                {
                    var row = new BlackboardRow(new ExposedParameterFieldView(graphView, param), new VisualElement());
                    row.expanded = false;
                    content.Add(row);
                }
            }
        }

        protected override void Initialize(BaseGraphView graphView)
        {
            this.graphView = graphView;
            base.title = "Parameters";
            scrollable = true;

            graphView.onExposedParameterListChanged += UpdateParameterList;
            graphView.initialized += UpdateParameterList;
            Undo.undoRedoPerformed += UpdateParameterList;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
            RegisterCallback<DetachFromPanelEvent>(OnViewClosed);

            UpdateParameterList();

            // Add exposed parameter button
            if (m_IsGraphAsset)
            {
                header.Add(new Button(OnAddClicked)
                {
                    text = "+"
                });
            }
        }

        void OnViewClosed(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= UpdateParameterList;
        }

        void OnMouseDownEvent(MouseDownEvent evt)
        {
            blackboardLayouts = content.Children().Select(c => c.layout).ToList();
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (!m_IsGraphAsset)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            int newIndex = GetInsertIndexFromMousePosition(evt.mousePosition);
            var graphSelectionDragData = DragAndDrop.GetGenericData("DragSelection");

            if (graphSelectionDragData == null)
                return;

            foreach (var obj in graphSelectionDragData as List<ISelectable>)
            {
                if (obj is ExposedParameterFieldView view)
                {
                    var blackBoardRow = view.parent.parent.parent.parent.parent.parent;
                    int oldIndex = content.Children().ToList().FindIndex(c => c == blackBoardRow);
                    // Try to find the blackboard row
                    content.Remove(blackBoardRow);

                    if (newIndex > oldIndex)
                        newIndex--;

                    content.Insert(newIndex, blackBoardRow);
                }
            }
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            if (!m_IsGraphAsset)
                return;

            bool updateList = false;
            int newIndex = GetInsertIndexFromMousePosition(evt.mousePosition);
            foreach (var obj in DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>)
            {
                if (obj is ExposedParameterFieldView view)
                {
                    if (!updateList)
                        graphView.RegisterCompleteObjectUndo("Moved parameters");

                    int oldIndex = graphAsset.exposedParameters.FindIndex(e => e == view.parameter);
                    var parameter = graphAsset.exposedParameters[oldIndex];
                    graphAsset.exposedParameters.RemoveAt(oldIndex);

                    // Patch new index after the remove operation:
                    if (newIndex > oldIndex)
                        newIndex--;

                    graphAsset.exposedParameters.Insert(newIndex, parameter);

                    updateList = true;
                }
            }

            if (updateList)
            {
                graphAsset.NotifyExposedParameterListChanged();
                evt.StopImmediatePropagation();
                UpdateParameterList();
            }
        }

        int GetInsertIndexFromMousePosition(Vector2 pos)
        {
            pos = content.WorldToLocal(pos);
            // We only need to look for y axis;
            float mousePos = pos.y;

            if (mousePos < 0)
                return 0;

            int index = 0;
            foreach (var layout in blackboardLayouts)
            {
                if (mousePos > layout.yMin && mousePos < layout.yMax)
                    return index + 1;
                index++;
            }

            return content.childCount;
        }
    }
}
