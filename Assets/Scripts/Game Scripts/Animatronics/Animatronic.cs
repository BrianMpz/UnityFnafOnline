using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class Animatronic : NetworkBehaviour // main animatronic logic ALWAYS runs on server
{

    [Header("Dynamic Values")]
    [SerializeField] private protected NetworkVariable<bool> isCurrentlyAggrivated;
    [SerializeField] private protected NetworkVariable<float> currentMovementWaitTime;
    public NetworkVariable<float> currentDifficulty;
    [SerializeField] private protected NetworkVariable<float> audioLureResistance;
    [SerializeField] private protected Node target;
    public List<NodeData> nodeDatas;
    private protected float hourlyDifficultyIncrementAmount;
    private protected Node currentNode;
    [Header("Static Values")]
    [SerializeField] private protected int waitTimeToStartMoving;
    [SerializeField] private float footStepPitch;
    [SerializeField] private protected string deathScream;
    [SerializeField] private protected Node startNode;
    [SerializeField] private protected Transform animatronicModel;

    [Header("Misc")]
    private protected RectTransform MapTransform { get => GetComponent<RectTransform>(); }
    private protected bool isWaitingOnClient;
    private protected Coroutine gameplayLoop;
    private Coroutine lerpingToPosition;

    public virtual void Initialise()
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugCanvasUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;
        AnimatronicManager.Instance.OnAudioLure += AudioLure_AttractAnimatronic;
        AnimatronicManager.Instance.AttentionDivert += OnAttentionDivert;

        SetNode(startNode, false, false);

        var difficultyData = new Dictionary<GameNight, (float difficulty, float waitTime, float increment)>
        {
            { GameNight.One, (1f, 10f, 3f) },
            { GameNight.Two, (3f, 9f, 3.25f) },
            { GameNight.Three, (5f, 8f, 3.5f) },
            { GameNight.Four, (7f, 7f, 3.75f) },
            { GameNight.Five, (9f, 6f, 4f) },
            { GameNight.Six, (16f, 5f, 4.25f) },
            { GameNight.Seven, (20f, 4f, 4.5f) },
        };

        if (difficultyData.ContainsKey(GameManager.Instance.gameNight))
        {
            var (difficulty, waitTime, increment) = difficultyData[GameManager.Instance.gameNight];
            currentDifficulty.Value = difficulty;
            currentMovementWaitTime.Value = waitTime;
            hourlyDifficultyIncrementAmount = increment;
        }

        gameplayLoop = StartCoroutine(GameplayLoop());
    }

    public virtual void Disable()
    {
        if (!IsServer) return;

        SetNode(startNode, false);
        StopAllCoroutines();
    }

    private protected void IncreaseAnimatronicDifficulty()
    {
        currentDifficulty.Value += hourlyDifficultyIncrementAmount;
    }

    private void Update()
    {
        if (!IsServer) return;

        audioLureResistance.Value -= 1 * Time.deltaTime;

        audioLureResistance.Value = Mathf.Clamp(audioLureResistance.Value, 0f, 100f);
    }

    public void AudioLure_AttractAnimatronic(Node targetedNode)
    {
        if (target == targetedNode)
        {
            audioLureResistance.Value += 50;
        }

        if (audioLureResistance.Value < UnityEngine.Random.Range(0, 100))
        {
            SetAggrivation(true);
            SetTarget(targetedNode);
            audioLureResistance.Value += Mathf.Lerp(10f, 80f, currentDifficulty.Value / 20f);
        }
        else
        {
            print("resisted audioLure");
        }
    }

    public void OnAttentionDivert(Node targetedNode) // called if the alarm goes off etc
    {
        SetTarget(targetedNode);
    }

    private protected void SetAggrivation(bool shouldBeAggrivated, float aggrivatedDifficultyAdditionAmount = 15, float aggrivatedWaitTimeCoefficient = 1.2f)
    {
        if (shouldBeAggrivated)
        {
            if (!isCurrentlyAggrivated.Value)
            {
                isCurrentlyAggrivated.Value = true;
                currentMovementWaitTime.Value /= aggrivatedWaitTimeCoefficient;
                currentDifficulty.Value += aggrivatedDifficultyAdditionAmount;
            }
        }
        else
        {
            if (isCurrentlyAggrivated.Value)
            {
                isCurrentlyAggrivated.Value = false;
                currentMovementWaitTime.Value *= aggrivatedWaitTimeCoefficient;
                currentDifficulty.Value -= aggrivatedDifficultyAdditionAmount;
            }
        }

    }

    private protected virtual IEnumerator GameplayLoop()
    {
        if (currentDifficulty.Value == 0 || currentMovementWaitTime.Value == 0) yield break;

        yield return new WaitForSeconds(waitTimeToStartMoving);

        while (GameManager.Instance.isPlaying && IsServer)
        {
            bool shouldBeAggrivated = PlayerRoleManager.Instance.IsAnimatronicAboutToAttack(currentNode);
            SetAggrivation(shouldBeAggrivated);

            yield return new WaitForSeconds(currentMovementWaitTime.Value);

            if (NeedsANewTarget()) TargetRandomPlayer();
            if (target == null) continue; // No valid target, restart loop

            List<Node> path = AnimatronicManager.Instance.BreadthFirstSearch(currentNode, target, this, takeOccupancyIntoAccount: false);

            if (path.Count < 2) continue; // there is no path to the target or has reached target

            if (MovementCondition()) // successful movement opportunity
            {
                yield return MovementOpportunity(path);
            }
        }
    }

    private protected virtual bool MovementCondition()
    {
        return UnityEngine.Random.Range(1, 20 + 1) <= currentDifficulty.Value;
    }

    private bool NeedsANewTarget() => target == null || UnityEngine.Random.Range(1, 10 + 1) < 2;

    private protected void TargetRandomPlayer()
    {
        if (PlayerRoleManager.Instance.IsEveryoneDead())
        {
            SetTarget(null);
            return;
        }

        var attackablePlayers = AnimatronicManager.Instance.PlayerNodes
            .Where(x => x.playerBehaviour?.isPlayerAlive.Value == true)
            .ToList();

        SetTarget(attackablePlayers.Count > 0 ? attackablePlayers[UnityEngine.Random.Range(0, attackablePlayers.Count)] : null);

    }

    private void SetTarget(Node targetNode)
    {
        target = targetNode;
    }

    private IEnumerator MovementOpportunity(List<Node> path)
    {
        Node nodeToGoTo = path[1];

        bool aboutToAttackPlayer = nodeToGoTo.TryGetComponent(out PlayerNode playerNode);

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
                AdvanceInPath(nodeToGoTo, false, true);
            }
        }
        else
        {
            AdvanceInPath(nodeToGoTo, true, true);
        }

        // the gameplay loop coroutine continues...
    }

    private IEnumerator KillPlayer(PlayerNode playerNode)
    {
        ClientRpcParams clientRpcParams = MultiplayerManager.NewClientRpcSendParams(playerNode.playerBehaviour.OwnerClientId);
        int indexOfTargetNode = AnimatronicManager.Instance.PlayerNodes.IndexOf(playerNode);
        int indexOfCurrentNode = AnimatronicManager.Instance.Nodes.IndexOf(currentNode);

        if (playerNode.playerBehaviour.canAnimatronicsStandInDoorway)
        {
            Node doorwayNode = playerNode.playerBehaviour.GetDoorwayNode(currentNode);
            AdvanceInPath(doorwayNode, true, true);
        }

        WaitToKillLocalClientRpc(indexOfTargetNode, indexOfCurrentNode, clientRpcParams);
        isWaitingOnClient = true;

        float clientWaitingTimeout = Time.time + 60f;
        yield return new WaitUntil(() => isWaitingOnClient == false || Time.time > clientWaitingTimeout || !playerNode.playerBehaviour.isPlayerAlive.Value);

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
            AdvanceInPath(targetNode, true, false);
            string killerName = gameObject.name;
            playerBehaviour.DieClientRpc(killerName, deathScream, MultiplayerManager.NewClientRpcSendParams(targetNode.playerBehaviour.OwnerClientId));
        }
        else // player is dead so treat as normal node
        {
            AdvanceInPath(targetNode, false, true);
        }
    }

    private protected virtual void Blocked(PlayerBehaviour playerBehaviour)
    {
        int indexOfCurrentNode = AnimatronicManager.Instance.Nodes.IndexOf(currentNode);
        ulong blockId = MultiplayerManager.Instance.GetPlayerDataFromPlayerRole(playerBehaviour.playerRole).clientId;

        playerBehaviour.currentPower.Value -= 1;
        playerBehaviour.PlayDoorKnockAudioClientRpc(indexOfCurrentNode, false, MultiplayerManager.NewClientRpcSendParams(blockId));

        SetNode(startNode, false, false);
    }

    private void AdvanceInPath(Node nodeToGoTo, bool lerpToPosition, bool makeNoise)
    {
        if (!nodeToGoTo.isOccupied.Value)
        {
            SetNode(nodeToGoTo, lerpToPosition); // Move to the next node in the path
        }
    }

    private protected virtual void SetNode(Node nodeToGoTo, bool lerpToPosition, bool makeNoise = true)
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

    private protected virtual void HandleMovementAudio(bool makeNoise)
    {
        if (GameManager.localPlayerBehaviour?.playerRole == PlayerRoles.Janitor) return; // dont play normal movement noise while entering janitor room

        if (makeNoise && PlayerRoleManager.Instance.IsLocalPlayerAlive() && animatronicModel.gameObject.activeSelf)
        {
            AudioSource audioSource = animatronicModel.GetComponent<AudioSource>();

            string randomWalkClipName = "walk" + UnityEngine.Random.Range(0, 5).ToString();

            audioSource.clip = GameAudioManager.Instance.GetAudioClip(randomWalkClipName);
            audioSource.pitch = footStepPitch;
            audioSource.Play();
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
            yield return null;
            elapsedTime += Time.deltaTime;

            MapTransform.anchoredPosition = Vector2.Lerp(MapTransform.anchoredPosition, nodeToSet.MapTransform.anchoredPosition, Time.deltaTime * lerpSpeed);

            if (animatronicModel == null) continue; // some animatronics may not have a physical model
            animatronicModel.position = Vector3.Lerp(animatronicModel.position, nodeToSet.physicalTransform.position, Time.deltaTime * lerpSpeed);
            float newAnimatronicModelYRotation = Mathf.LerpAngle(animatronicModel.eulerAngles.y, nodeToSet.physicalTransform.eulerAngles.y, Time.deltaTime * lerpSpeed);
            animatronicModel.rotation = Quaternion.Euler(0, newAnimatronicModelYRotation, 0);
        }
    }

    private protected virtual void TeleportToPosition(Node nodeToSet)
    {
        MapTransform.anchoredPosition = nodeToSet.MapTransform.anchoredPosition;
        MapTransform.rotation = nodeToSet.MapTransform.rotation;

        if (animatronicModel == null) return; // some animatronics may not have a physical model
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
