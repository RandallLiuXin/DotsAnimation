using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using GraphProcessor;
using Tinder.Animation.Asset;
using Status = UnityEngine.UIElements.DropdownMenuAction.Status;

namespace Tinder.Animation.Editor
{
    public class ClipNodeView : BaseNodeView
    {
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Copy Node ConfigId", (e) => CopyNodeConfigId(), CopyNodeConfigIdStatus);
        }

        public void CopyNodeConfigId()
        {
            var node = nodeTarget as AnimationClipNodeAsset;
            if (node != null)
            {
                TextEditor textEditor = new TextEditor
                {
                    text = node.GetClipConfigId.ToString()
                };
                textEditor.SelectAll();
                textEditor.Copy();
            }
        }

        Status CopyNodeConfigIdStatus(DropdownMenuAction action)
        {
            return Status.Normal;
        }
    }
}

