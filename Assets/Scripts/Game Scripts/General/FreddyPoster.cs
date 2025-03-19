using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class FreddyPoster : NetworkBehaviour
{
    [SerializeField] private NodeName nameOfTargetPlayerNode;
    private bool isKillingPlayer;

    public IEnumerator KillPlayer()
    {
        if (isKillingPlayer) yield break; // prevent multiple screams
        isKillingPlayer = true;

        yield return new WaitForSeconds(1f);

        ConfirmKillServerRpc(nameOfTargetPlayerNode);
    }

    [ServerRpc(RequireOwnership = false)]
    private protected void ConfirmKillServerRpc(NodeName nodeName)
    {
        PlayerNode targetNode = AnimatronicManager.Instance.GetPlayerNodeFromName(nodeName);
        PlayerBehaviour playerBehaviour = targetNode.playerBehaviour;

        if (playerBehaviour.isPlayerAlive.Value)
        {
            MoveToNode(nodeName);
            string killerName = gameObject.name;
            playerBehaviour.DieClientRpc(killerName, "jumpscare scream", MultiplayerManager.NewClientRpcSendParams(targetNode.playerBehaviour.OwnerClientId));
        }
        else
        {
            MoveToNode(nodeName);
        }
    }

    private void MoveToNode(NodeName nodeName)
    {
        MoveToNodeClientRpc(nodeName);
    }

    [ClientRpc]
    private void MoveToNodeClientRpc(NodeName nodeName)
    {
        Transform targetPosition = AnimatronicManager.Instance.GetPlayerNodeFromName(nodeName).physicalTransform;

        StartCoroutine(LerpToPosition(targetPosition, 12));
    }

    private protected IEnumerator LerpToPosition(Transform targetTransform, float lerpSpeed)
    {
        Vector3 targetPosition = new(targetTransform.position.x, targetTransform.position.y + 2.3f, targetTransform.position.z);

        float elapsedTime = 0;

        while (elapsedTime < 2f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);

            float newAnimatronicModelYRotation = Mathf.LerpAngle(transform.eulerAngles.y, targetTransform.eulerAngles.y, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Euler(0, newAnimatronicModelYRotation, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
