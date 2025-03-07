using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Animatronic))]
public class AnimatronicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Animatronic animatronic = (Animatronic)target;

        // Validate and populate the NodeData
        PopulateNodeData(animatronic);
    }

    private void PopulateNodeData(Animatronic animatronic)
    {
        AnimatronicManager animatronicManager = FindAnyObjectByType<AnimatronicManager>();

        // Get the list of nodes from AnimatronicManager
        Node[] managerNodes = animatronicManager.Nodes.ToArray();

        // Track which nodes we already have data for
        List<Node> existingNodes = animatronic.nodeDatas.Select(ni => ni.node).ToList();

        // Add missing NodeData for nodes that are in AnimatronicManager but not in the existing ones
        foreach (Node node in managerNodes)
        {
            if (!existingNodes.Contains(node))
            {
                // Create a new NodeData for the missing node
                NodeData newData = new()
                {
                    name = node.gameObject.name,
                    node = node,
                    isAllowedToGoTo = true,  // Default value, can be changed
                };

                // Add it to the nodeData list
                animatronic.nodeDatas.Add(newData);
            }
        }

        // Remove any nodeData that no longer have corresponding nodes in the AnimatronicManager
        animatronic.nodeDatas = animatronic.nodeDatas
            .Where(ni => managerNodes.Contains(ni.node))  // Keep only data with valid nodes
            .Distinct()  // Remove duplicates based on the node reference
            .ToList();

        // Reorder the nodeData to match the order of AnimatronicManager.nodes
        animatronic.nodeDatas = animatronic.nodeDatas
            .OrderBy(ni => System.Array.IndexOf(managerNodes, ni.node))
            .ToList();

        // Mark the animatronic as dirty to ensure changes are saved
        EditorUtility.SetDirty(animatronic);
    }
}
