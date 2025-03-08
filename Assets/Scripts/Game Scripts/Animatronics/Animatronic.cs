using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class Animatronic : NetworkBehaviour // main animatronic logic ALWAYS runs on server
{

    [Header("Dynamic Values")]
    [SerializeField] private protected NetworkVariable<bool> isAggrivated;
    public NetworkVariable<float> currentDifficulty;
    [SerializeField] private protected NetworkVariable<float> currentMovementWaitTime;
    [SerializeField] private protected PlayerNode targetedPlayer;

    [Header("Starting Values")]
    [SerializeField] private int hourlyDifficultyIncrementAmount;
    [SerializeField] private protected int waitTimeToStartMoving;

    [Header("Nodes")]
    [SerializeField] private protected Node startNode;
    private protected Node currentNode;
    public List<NodeData> nodeDatas;

    [Header("Miscellaneous")]
    [SerializeField] private protected Transform animatronicModel; // position on map
    private protected RectTransform MapTransform { get => GetComponent<RectTransform>(); }
    [SerializeField] private float footStepPitch;
    [SerializeField] private protected string deathScream;
    private protected bool isWaitingOnClient;
    private protected Coroutine gameplayLoop;
    private Coroutine lerpingToPosition;

    public virtual void Initialise()
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugCanvasUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;
        GameManager.Instance.OnPlayerPowerDown += GameManager_OnPlayerPowerDown;

        SetNode(startNode, false, false);

        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty.Value = 1;
                currentMovementWaitTime.Value = 7;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 2;
                currentMovementWaitTime.Value = 6f;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 4;
                currentMovementWaitTime.Value = 6.5f;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 7;
                currentMovementWaitTime.Value = 5.5f;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 11;
                currentMovementWaitTime.Value = 5;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16;
                currentMovementWaitTime.Value = 4.5f;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20;
                currentMovementWaitTime.Value = 4;
                break;
        }

        gameplayLoop = StartCoroutine(GameplayLoop());
    }

    public virtual void Disable()
    {
        if (!IsServer) return;

        SetNode(startNode);
        StopAllCoroutines();
    }

    private protected void IncreaseAnimatronicDifficulty()
    {
        currentDifficulty.Value += hourlyDifficultyIncrementAmount;
    }

    private void GameManager_OnPlayerPowerDown(PlayerRoles playerRole)
    {
        PlayerNode playerNode = AnimatronicManager.Instance.PlayerNodes.FirstOrDefault(x =>
            x.playerBehaviour != null && x.playerBehaviour.playerRole == playerRole);

        Retarget(playerNode);
    }

    private void Retarget(PlayerNode playerNode)
    {
        if (playerNode != targetedPlayer) return;
        if (isWaitingOnClient) return;

        PlayerNode playerToTarget = AnimatronicManager.Instance.PlayerNodes.FirstOrDefault(x
            => x.playerBehaviour != null && x.playerBehaviour.playerRole != playerNode.playerBehaviour.playerRole && x.playerBehaviour.isPlayerAlive.Value);

        TargetPlayer(playerToTarget);
    }

    private protected virtual IEnumerator GameplayLoop()
    {
        yield return new WaitForSeconds(waitTimeToStartMoving); // 5 seconds default

        while (GameManager.Instance.isPlaying && IsServer)
        {
            bool shouldBeAggrivated = currentNode == PlayerRoleManager.Instance.janitorPlayerBehaviour.insideNode;
            if (isAggrivated.Value)
            {
                isAggrivated.Value = false;
                currentMovementWaitTime.Value *= 1.5f;
                currentDifficulty.Value -= 10;
            }
            if (shouldBeAggrivated)
            {
                isAggrivated.Value = true;
                currentMovementWaitTime.Value /= 1.5f;
                currentDifficulty.Value += 10;
            }

            yield return new WaitForSeconds(currentMovementWaitTime.Value); // 5 seconds default

            if (currentDifficulty.Value == 0 || currentMovementWaitTime.Value == 0) continue;

            if (targetedPlayer == null || !targetedPlayer.playerBehaviour.isPlayerAlive.Value) TargetRandomPlayer();
            else if (UnityEngine.Random.Range(1, 11) <= 2) TargetRandomPlayer();

            if (targetedPlayer == null) continue;

            List<Node> path = AnimatronicManager.Instance.BreadthFirstSearch(currentNode, targetedPlayer, this, true);

            if (path.Count == 0)
            {
                //Debug.Log("There is no path to this player!");

                continue;
            }

            if (path.Count == 1)
            {
                //Debug.Log("Target Reached");

                continue;
            }

            if (UnityEngine.Random.Range(1, 21) <= currentDifficulty.Value)
            {
                yield return MovementOpportunity(path);
            }
            else
            {
                //Debug.Log("Movement Opportunity failed");

                continue;
            }
        }
    }

    private protected void TargetRandomPlayer(bool includeDeadPeople = false)
    {
        if (PlayerRoleManager.Instance.IsEveryoneDead())
        {
            StopAllCoroutines();
            TargetPlayer(null);
            return;
        }

        List<PlayerNode> attackablePlayers = AnimatronicManager.Instance.PlayerNodes.Where(x => x.playerBehaviour != null).ToList();

        if (!includeDeadPeople) attackablePlayers = attackablePlayers.Where(x => x.playerBehaviour.isPlayerAlive.Value).ToList();

        if (attackablePlayers.Count == 0) TargetPlayer(null);
        else TargetPlayer(attackablePlayers[UnityEngine.Random.Range(0, attackablePlayers.Count)]);
    }

    private void TargetPlayer(PlayerNode playerNode)
    {
        targetedPlayer = null;
        if (playerNode == null) return;

        targetedPlayer = playerNode;
        TargetPlayerClientRpc(playerNode.playerBehaviour.playerRole);
    }

    [ClientRpc]
    private void TargetPlayerClientRpc(PlayerRoles playerRole)
    {
        if (!IsServer) targetedPlayer = AnimatronicManager.Instance.GetPlayerNodeFromPlayerRole(playerRole);
    }

    private IEnumerator MovementOpportunity(List<Node> path)
    {
        Node nodeToGoTo = path[1];

        if (nodeToGoTo.GetComponent<PlayerNode>() != null) // about to attack a player
        {
            PlayerNode playerNode = nodeToGoTo.GetComponent<PlayerNode>();

            if (PlayerRoleManager.Instance.IsPlayerVulnerableToAttack(currentNode, playerNode) && playerNode.IsAlive)
            {
                yield return KillPlayer(playerNode);
            }
            else if (playerNode.IsAlive)
            {
                Blocked(playerNode.playerBehaviour);
            }
            else // player is dead so treat as normal node
            {
                AdvanceInPath(nodeToGoTo);
            }
        }
        else if (path[2].GetComponent<PlayerNode>() != null) // moving to attack position
        {
            AdvanceInPath(nodeToGoTo, true);
        }
        else
        {
            AdvanceInPath(nodeToGoTo);
        }
    }

    private IEnumerator KillPlayer(PlayerNode playerNode)
    {
        ClientRpcParams clientRpcParams = MultiplayerManager.NewClientRpcSendParams(playerNode.playerBehaviour.OwnerClientId);
        int indexOfTargetNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
        int indexOfCurrentNode = AnimatronicManager.Instance.Nodes.IndexOf(currentNode);

        if (playerNode.playerBehaviour.animatronicsCanStandInDoorway)
        {
            Node doorwayNode = playerNode.playerBehaviour.GetDoorwayNode(currentNode);
            AdvanceInPath(doorwayNode, true);
        }

        WaitToKillLocalClientRpc(indexOfTargetNode, indexOfCurrentNode, clientRpcParams);
        isWaitingOnClient = true;

        float clientWaitingTimeout = Time.time + 60f;
        yield return new WaitUntil(() =>
        {
            return isWaitingOnClient == false || Time.time > clientWaitingTimeout || !playerNode.playerBehaviour.isPlayerAlive.Value;
        });

        // the gameplay loop coroutine continues...
    }

    [ClientRpc]
    private void WaitToKillLocalClientRpc(int indexOfTargetNode, int indexOfCurrentNode, ClientRpcParams _)
    {
        StartCoroutine(WaitToKill(indexOfTargetNode, indexOfCurrentNode));
    }

    private IEnumerator WaitToKill(int indexOfTargetNode, int indexOfCurrentNode)
    {
        PlayerNode targetNode = AnimatronicManager.Instance.PlayerNodes[indexOfTargetNode];
        Node currentNode = AnimatronicManager.Instance.Nodes[indexOfCurrentNode];

        yield return targetNode.playerBehaviour.WaitUntilKillConditionsAreMet(currentNode);

        ConfirmKillServerRpc(indexOfTargetNode);
    }

    [ServerRpc(RequireOwnership = false)]
    private protected void ConfirmKillServerRpc(int indexOfTargetNode)
    {
        isWaitingOnClient = false;

        PlayerNode targetNode = AnimatronicManager.Instance.PlayerNodes[indexOfTargetNode];
        PlayerBehaviour playerBehaviour = targetNode.playerBehaviour;

        if (playerBehaviour.isPlayerAlive.Value)
        {
            AdvanceInPath(targetNode, true);
            string killerName = gameObject.name;
            playerBehaviour.DieClientRpc(killerName, deathScream, MultiplayerManager.NewClientRpcSendParams(targetNode.playerBehaviour.OwnerClientId));
        }
        else
        {
            AdvanceInPath(targetNode);
        }
    }

    private void Blocked(PlayerBehaviour playerBehaviour)
    {
        int indexOfCurrentNode = AnimatronicManager.Instance.Nodes.IndexOf(currentNode);
        ulong blockId = MultiplayerManager.Instance.GetPlayerDataFromPlayerRole(playerBehaviour.playerRole).clientId;

        playerBehaviour.currentPower.Value -= 1;
        playerBehaviour.PlayDoorKnockAudioClientRpc(indexOfCurrentNode, MultiplayerManager.NewClientRpcSendParams(blockId));

        SetNode(startNode, false, false);
    }

    private void AdvanceInPath(Node nodeToGoTo, bool lerpToPosition = false)
    {
        if (!nodeToGoTo.isOccupied.Value)
        {
            SetNode(nodeToGoTo, lerpToPosition); // Move to the next node in the path
        }
        else
        {
            Node randomNode = GetRandomAvailableNode(nodeToGoTo);
            if (randomNode == null || randomNode.GetComponent<PlayerNode>() != null) return;

            AdvanceInPath(randomNode, lerpToPosition);
        }
    }

    private Node GetRandomAvailableNode(Node dontGoToThisNode)
    {
        if (currentNode.neighbouringNodes.Length < 2) return null;

        return currentNode.neighbouringNodes.FirstOrDefault(node => node != dontGoToThisNode && !node.isOccupied.Value);
    }

    private protected virtual void SetNode(Node nodeToGoTo, bool lerpToPosition = false, bool makeNoise = true)
    {
        List<Node> nodes = AnimatronicManager.Instance.Nodes;
        int indexOfNodeToGoTo = nodes.IndexOf(nodeToGoTo);

        SetNodeClientRpc(indexOfNodeToGoTo, lerpToPosition, makeNoise);
    }

    [ClientRpc]
    private protected virtual void SetNodeClientRpc(int nodeToSetIndex, bool lerpToPosition = false, bool makeNoise = true)
    {
        Node nodeToSet = AnimatronicManager.Instance.Nodes[nodeToSetIndex];
        Node startingNode = currentNode;

        GameManager.Instance.OnAnimatronicMoved?.Invoke(currentNode, nodeToSet);

        SwapNodeOccupancy(nodeToSet);
        HandleMovement(lerpToPosition, nodeToSet, startingNode);
        HandleMovementAudio(makeNoise);
    }

    private void HandleMovementAudio(bool makeNoise)
    {
        // dont play normal movement noise while entering janitor room
        if (GameManager.localPlayerBehaviour?.playerRole == PlayerRoles.Janitor) return;

        if (makeNoise && PlayerRoleManager.Instance.IsLocalPlayerAlive() && animatronicModel.gameObject.activeSelf)
        {
            animatronicModel.GetComponent<AudioSource>().Play();
            animatronicModel.GetComponent<AudioSource>().pitch = footStepPitch;
        }
    }

    private void HandleMovement(bool lerpToPosition, Node nodeToSet, Node startingNode)
    {
        if (lerpToPosition)
        {
            if (lerpingToPosition != null) StopCoroutine(lerpingToPosition);
            lerpingToPosition = StartCoroutine(LerpToPosition(startingNode, nodeToSet, 12));
        }
        else
        {
            TeleportToPosition(nodeToSet);
        }
    }

    private void SwapNodeOccupancy(Node nodeToSet)
    {
        if (currentNode != null)
        {
            if (IsServer) currentNode.isOccupied.Value = false;
            currentNode.occupier = null;
            currentNode = null;
        }

        if (IsServer) nodeToSet.isOccupied.Value = true;
        nodeToSet.occupier = this;
        currentNode = nodeToSet;
    }

    private protected IEnumerator LerpToPosition(Node startingNode, Node nodeToSet, float lerpSpeed, float movementTime = 1)
    {
        float elapsedTime = 0;

        while (elapsedTime < movementTime)
        {
            MapTransform.anchoredPosition = Vector2.Lerp(MapTransform.anchoredPosition, nodeToSet.MapTransform.anchoredPosition, Time.deltaTime * lerpSpeed);

            animatronicModel.position = Vector3.Lerp(animatronicModel.position, nodeToSet.physicalTransform.position, Time.deltaTime * lerpSpeed);
            float newAnimatronicModelYRotation = Mathf.LerpAngle(animatronicModel.eulerAngles.y, nodeToSet.physicalTransform.eulerAngles.y, Time.deltaTime * lerpSpeed);
            animatronicModel.rotation = Quaternion.Euler(0, newAnimatronicModelYRotation, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private protected virtual void TeleportToPosition(Node nodeToSet)
    {
        MapTransform.anchoredPosition = nodeToSet.MapTransform.anchoredPosition;
        MapTransform.rotation = nodeToSet.MapTransform.rotation;

        animatronicModel.SetPositionAndRotation(nodeToSet.physicalTransform.position, nodeToSet.physicalTransform.rotation);
    }

    public NodeData GetNodeData(Node targetNode)
    {
        return nodeDatas.FirstOrDefault(data => data.node == targetNode);
    }

}

[Serializable]
public class NodeData
{
    [HideInInspector] public string name;
    public Node node;
    public bool isAllowedToGoTo;

}
