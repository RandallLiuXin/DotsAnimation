using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using GraphProcessor;
using Tinder.Animation.Asset;
using System.Reflection;
using System.Linq;

namespace Tinder.Animation.Editor
{
    [NodeCustomEditor(typeof(TransitionNode))]
    public class TransitionNodeView : BaseNodeView
    {
        public override void Enable()
        {
            base.Enable();
            // Remove useless elements
            this.Q("title").RemoveFromHierarchy();
            this.Q("divider").RemoveFromHierarchy();

            var node = nodeTarget as TransitionNode;
            var button = new Button(() =>
            {
                if (node.Data != null)
                {
                    var editor = EditorWindow.GetWindow<TransitionNodeDataEditorWindow>("Condition Data", true);
                    editor.InitTransitionNodeData(node.Data);
                }
                else
                    Debug.LogError("transition config is null");
            });
            button.text = "Config";
            button.style.height = 15;

            controlsContainer.Add(button);
        }

        VisualElement input => this.Q("input");
        VisualElement output => this.Q("output");

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(new Rect(newPos.position, new Vector2(200, 200)));
            UpdateSize();
        }

        void UpdateSize()
        {
            if (input != null)
                input.style.height = 16;
            if (output != null)
                output.style.height = 16;
        }
    }

    public class TransitionNodeDataEditorWindow : EditorWindow
    {
        [Serializable]
        public class TransitionDataScriptableObject : ScriptableObject
        {
            [SerializeField]
            public TransitionNodeData m_Data;
        }

        protected TransitionDataScriptableObject m_ScriptableObject;
        private SerializedObject m_SerializedObject;

        public void InitTransitionNodeData(TransitionNodeData data)
        {
            m_ScriptableObject = ScriptableObject.CreateInstance<TransitionDataScriptableObject>();
            m_ScriptableObject.m_Data = data;

            m_SerializedObject = new SerializedObject(m_ScriptableObject);
        }

        private void OnGUI()
        {
            if (m_SerializedObject == null)
                return;

            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.PropertyField(m_SerializedObject.FindProperty("m_Data"), new GUIContent("Condition Data"), true);
                m_SerializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndVertical();
        }
    }
}
