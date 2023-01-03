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
	[NodeCustomEditor(typeof(StateNode))]
	public class StateNodeView : BaseNodeView
    {
        public override void Enable()
        {
            base.Enable();
            var node = nodeTarget as StateNode;

            var inputField = new TextField("StateName");
            inputField.SetValueWithoutNotify(node.StateName);
            controlsContainer.Add(inputField);

            var button = new Button(() =>
            {
                if (node.StateAsset != null)
                    EditorWindow.GetWindow<AnimationStateEditor>().InitializeGraph(node.StateAsset);
                else
                    Debug.LogError("node state asset is null!");
            });
            button.text = "Open State Graph";
            controlsContainer.Add(button);
        }

        public override void OnCreated()
        {
            base.OnCreated();

            if (owner.graph != null)
            {
                var node = nodeTarget as StateNode;
                var path = AssetDatabase.GetAssetPath(owner.graph);
                var folderName = owner.graph.name;
                var parentFolderPath = path.Replace(".asset", "").Replace("/" + folderName, "");
                path = path.Replace(".asset", "/State_" + node.GUID + ".asset");
                if (node != null && node.StateAsset == null)
                {
                    if (!AssetDatabase.IsValidFolder(parentFolderPath))
                        AssetDatabase.CreateFolder(parentFolderPath, folderName);
                    
                    var stateAsset = ScriptableObject.CreateInstance<AnimationStateAsset>();
                    stateAsset.m_OwnerStateMachine = owner.graph as AnimationStateMachineAsset;
                    stateAsset.m_OwnerGraph = stateAsset.m_OwnerStateMachine.m_OwnerGraph;
                    AssetDatabase.CreateAsset(stateAsset, path);
                    EditorUtility.SetDirty(stateAsset);
                    AssetDatabase.SaveAssets();
                    node.StateAsset = stateAsset;
                }
                MarkDirtyRepaint();
            }
        }

        public override void OnRemoved()
        {
            if (owner.graph != null)
            {
                var node = nodeTarget as StateNode;
                if (node != null && node.StateAsset != null)
                {
                    var path = AssetDatabase.GetAssetPath(node.StateAsset);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.SaveAssets();

                    node.StateAsset = null;
                }
            }

            base.OnRemoved();
        }

        public override void HandleEvent(EventBase evt)
        {
            base.HandleEvent(evt);
            var changeEvent = evt as ChangeEvent<string>;
            if (changeEvent != null)
            {
                var node = nodeTarget as StateNode;
                node.StateName = changeEvent.newValue;
            }
        }
    }
}
