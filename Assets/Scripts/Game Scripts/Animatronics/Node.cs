using System;
using Unity.Netcode;
using UnityEngine;

public class Node : NetworkBehaviour
{
    public NodeName nodeName;
    public Node[] neighbouringNodes;
    public RectTransform MapTransform { get => GetComponent<RectTransform>(); }
    public Transform physicalTransform;
    public NetworkVariable<bool> isOccupied = new(writePerm: NetworkVariableWritePermission.Server);
    public Animatronic occupier;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (neighbouringNodes != null && neighbouringNodes.Length > 0)
        {
            for (int i = 0; i < neighbouringNodes.Length; i++)
            {
                Gizmos.DrawLine(gameObject.transform.position, neighbouringNodes[i].gameObject.transform.position);
            }
        }
    }
}
