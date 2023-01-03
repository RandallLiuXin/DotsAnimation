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
    [NodeCustomEditor(typeof(StateMachineNode))]
    public class StateMachineView : BaseNodeView
    {
        public override void Enable()
        {
            var node = nodeTarget as StateMachineNode;

            // Create your fields using node's variables and add them to the controlsContainer
            var button = new Button(() =>
            {
                if (node.StateMachineAsset != null)
                    EditorWindow.GetWindow<AnimationStateMachineEditor>().InitializeGraph(node.StateMachineAsset);
                else
                    Debug.LogError("state machine asset is null!");
            });
            button.text = "Open StateMachine Graph";
            controlsContainer.Add(button);
        }

        public override void OnCreated()
        {
            base.OnCreated();

            if (owner.graph != null)
            {
                var node = nodeTarget as StateMachineNode;
                var path = AssetDatabase.GetAssetPath(owner.graph);
                var folderName = owner.graph.name;
                var parentFolderPath = path.Replace(".asset", "").Replace("/" + folderName, "");
                path = path.Replace(".asset", "/StateMachine_" + node.GUID + ".asset");
                if (node != null && node.StateMachineAsset == null)
                {
                    AssetDatabase.CreateFolder(parentFolderPath, folderName);

                    var stateMachineAsset = ScriptableObject.CreateInstance<AnimationStateMachineAsset>();
                    stateMachineAsset.m_OwnerGraph = owner.graph as AnimationGraphAsset;
                    AssetDatabase.CreateAsset(stateMachineAsset, path);
                    EditorUtility.SetDirty(stateMachineAsset);
                    AssetDatabase.SaveAssets();
                    node.StateMachineAsset = stateMachineAsset;
                }
                MarkDirtyRepaint();
            }
        }

        public override void OnRemoved()
        {
            if (owner.graph != null)
            {
                var node = nodeTarget as StateMachineNode;
                if (node != null && node.StateMachineAsset != null)
                {
                    var path = AssetDatabase.GetAssetPath(node.StateMachineAsset);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.SaveAssets();

                    node.StateMachineAsset = null;
                }
            }

            base.OnRemoved();
        }
    }
}
