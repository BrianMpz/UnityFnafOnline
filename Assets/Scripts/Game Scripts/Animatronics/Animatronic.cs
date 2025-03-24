using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using System.IO.Pipes;

public class Animatronic : NetworkBehaviour // main animatronic logic ALWAYS runs on server
{

    [Header("Dynamic Values")]
    [SerializeField] private protected NetworkVariable<bool> isCurrentlyAggrivated;
    [SerializeField] private protected NetworkVariable<float> currentMovementWaitTime;
    [SerializeField] private protected NetworkVariable<float> audioLureResistance;
    public NetworkVariable<float> currentDifficulty;
    [SerializeField] private protected Node target;

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
        AnimatronicManager.Instance.OnAudioLure += AudioLure_AttractAnimatronic;

        SetNode(startNode, false, false);

        switch (GameManager.Instance.gameNight)
        {
            case GameNight.One:
                currentDifficulty.Value = 1f;
                currentMovementWaitTime.Value = 10f;
                break;
            case GameNight.Two:
                currentDifficulty.Value = 3f;
                currentMovementWaitTime.Value = 8f;
                break;
            case GameNight.Three:
                currentDifficulty.Value = 5f;
                currentMovementWaitTime.Value = 6f;
                break;
            case GameNight.Four:
                currentDifficulty.Value = 7f;
                currentMovementWaitTime.Value = 5f;
                break;
            case GameNight.Five:
                currentDifficulty.Value = 9f;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Six:
                currentDifficulty.Value = 16f;
                currentMovementWaitTime.Value = 4f;
                break;
            case GameNight.Seven:
                currentDifficulty.Value = 20f;
                currentMovementWaitTime.Value = 4f;
                break;
        }

        gameplayLoop = StartCoroutine(GameplayLoop());
    }

    private void Update()
    {
        if (!IsServer) return;

        audioLureResistance.Value -= 1 * Time.deltaTime;

        audioLureResistance.Value = Mathf.Clamp(audioLureResistance.Value, 0f, 100f);
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
        PlayerNode playerToExclude = AnimatronicManager.Instance.PlayerNodes.FirstOrDefault(x =>
            x.playerBehaviour != null && x.playerBehaviour.playerRole == playerRole);

        if (playerToExclude != target || isWaitingOnClient) return;

        // Get a new target (excluding the current player)
        var alivePlayers = AnimatronicManager.Instance.PlayerNodes
            .Where(x => x.playerBehaviour != null && x.playerBehaviour.isPlayerAlive.Value && x.playerBehaviour.playerRole != playerToExclude.playerBehaviour.playerRole)
            .ToList();

        if (alivePlayers.Count > 0)
        {
            PlayerNode randomPlayerWithPower = alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];
            SetTarget(randomPlayerWithPower);
        }
    }

    public void AudioLure_AttractAnimatronic(Node targetedNode)
    {
        bool alreadyAtThisNode = targetedNode == target;
        bool canResistLure = (int)audioLureResistance.Value >= UnityEngine.Random.Range(0, 100); // 0 to 99

        if (!canResistLure || alreadyAtThisNode)
        {
            SetTarget(targetedNode);

            audioLureResistance.Value += Mathf.Lerp(0f, 80f, currentDifficulty.Value / 20f);
        }
        else
        {
            print("resisted audioLure");

            TargetRandomPlayer();
        }
    }

    private protected void HandleAggrivation(bool shouldBeAggrivated, float aggrivatedDifficultyAdditionAmount = 20, float aggrivatedWaitTimeCoefficient = 1.5f)
    {
        if (isCurrentlyAggrivated.Value)
        {
            isCurrentlyAggrivated.Value = false;
            currentMovementWaitTime.Value *= aggrivatedWaitTimeCoefficient;
            currentDifficulty.Value -= aggrivatedDifficultyAdditionAmount;
        }
        if (shouldBeAggrivated)
        {
            isCurrentlyAggrivated.Value = true;
            currentMovementWaitTime.Value /= aggrivatedWaitTimeCoefficient;
            currentDifficulty.Value += aggrivatedDifficultyAdditionAmount;
        }
    }

    private protected virtual IEnumerator GameplayLoop()
    {
        if (currentDifficulty.Value == 0 || currentMovementWaitTime.Value == 0) yield break;

        yield return new WaitForSeconds(waitTimeToStartMoving);

        while (GameManager.Instance.isPlaying && IsServer)
        {
            bool shouldBeAggrivated = PlayerRoleManager.Instance.IsAnimatronicAboutToAttack(currentNode);
            HandleAggrivation(shouldBeAggrivated);

            yield return new WaitForSeconds(currentMovementWaitTime.Value);

            if (NeedsANewTarget()) TargetRandomPlayer();
            if (target == null) continue; // No valid target, restart loop

            List<Node> path = AnimatronicManager.Instance.BreadthFirstSearch(currentNode, target, this, takeOccupancyIntoAccount: false);

            if (path.Count < 2) continue; // there is no path to the target or has reached target

            if (UnityEngine.Random.Range(1, 20 + 1) <= currentDifficulty.Value) // successful movement opportunity
            {
                yield return MovementOpportunity(path);
            }
        }
    }

    private bool NeedsANewTarget() => target == null || UnityEngine.Random.Range(1, 10 + 1) < 2;

    private protected void TargetRandomPlayer()
    {
        if (PlayerRoleManager.Instance.IsEveryoneDead())
        {
            SetTarget(null);
        }
        else
        {
            var attackablePlayers = AnimatronicManager.Instance.PlayerNodes
                .Where(x => x.playerBehaviour != null && x.playerBehaviour.isPlayerAlive.Value)
                .ToList();

            PlayerNode randomAttackablePlayer = attackablePlayers.Count > 0 ? attackablePlayers[UnityEngine.Random.Range(0, attackablePlayers.Count)] : null;

            SetTarget(randomAttackablePlayer);
        }
    }

    private void SetTarget(Node targetNode)
    {
        target = targetNode;
    }

    private IEnumerator MovementOpportunity(List<Node> path)
    {
        Node nodeToGoTo = path[1];

        bool aboutToAttackPlayer = nodeToGoTo.TryGetComponent(out PlayerNode playerNode);
        bool movingIntoAttackPosition = path.Count == 3 && path[2].GetComponent<PlayerNode>() != null;

        if (aboutToAttackPlayer) // about to attack a player
        {
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
        else if (movingIntoAttackPosition) // moving to attack position
        {
            AdvanceInPath(nodeToGoTo, true);
        }
        else
        {
            AdvanceInPath(nodeToGoTo);
        }

        // the gameplay loop coroutine continues...
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
