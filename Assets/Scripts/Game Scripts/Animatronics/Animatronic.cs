using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class Animatronic : NetworkBehaviour // main animatronic logic ALWAYS runs on server
{

    [Header("Dynamic Values")]
    public NetworkVariable<float> currentDifficulty;
    public NetworkVariable<float> currentMovementWaitTime;
    private protected int loopsAggrivated;
    [SerializeField] private protected NetworkVariable<bool> isCurrentlyAggrivated;
    [SerializeField] private protected float resistanceToAudioLure;
    [SerializeField] private protected Node currentTarget;
    [SerializeField] private protected Node currentNode;

    [Header("Static Values")]
    public List<NodeData> nodeDatas;
    private protected float hourlyDifficultyIncrementAmount;
    private protected int waitTimeToStartMoving;
    [SerializeField] private protected string deathScream;
    [SerializeField] private protected Node startNode;
    [SerializeField] private protected Transform animatronicModel;
    [SerializeField] private float aggrivationAddition = 20f;
    [SerializeField] private float aggrivationDivisor = 1.1f;

    [Header("Misc")]
    private protected RectTransform MapTransform { get => GetComponent<RectTransform>(); }
    private protected bool isWaitingOnClient;
    private protected bool hasStartedMoving;
    private protected Coroutine gameplayLoop;
    private Coroutine lerpingToPosition;

    public virtual void Initialise()
    {
        if (!IsServer) return;

        GameManager.Instance.currentHour.OnValueChanged += (currentValue, newValue) => { IncreaseAnimatronicDifficulty(); };
        DebugUI.Instance.OnBuff += IncreaseAnimatronicDifficulty;
        AnimatronicManager.Instance.OnAudioLure += AudioLure_AttractAnimatronic;
        AnimatronicManager.Instance.AttentionDivert += OnAttentionDivert;

        SetNode(startNode, false, false);
        GetAnimatronicData();

        gameplayLoop = StartCoroutine(GameplayLoop());
    }

    private protected virtual void GetAnimatronicData()
    {
        string animatronicName = gameObject.name;
        GameNight currentNight = GameManager.Instance.gameNight;

        if (AnimatronicManager.Instance.CanFindNightData(currentNight, animatronicName, out AnimatronicData animatronicData))
        {
            waitTimeToStartMoving = (int)animatronicData.waitTimeToStartMoving;
            currentDifficulty.Value = (float)animatronicData.startingDifficulty;
            currentMovementWaitTime.Value = (float)animatronicData.timeBetweenMovementOpportunities;
            hourlyDifficultyIncrementAmount = (float)animatronicData.hourlyDifficultyIncrementAmount;
        }
        else
        {
            Debug.LogWarning($"Difficulty data not found for {animatronicName} on night {currentNight}");
        }
    }

    public virtual void Disable()
    {
        if (!IsServer) return;

        SetNode(startNode, false);
        StopAllCoroutines();
    }

    private protected void IncreaseAnimatronicDifficulty()
    {
        if (!hasStartedMoving) return;

        currentDifficulty.Value += hourlyDifficultyIncrementAmount;
    }

    private void Update()
    {
        if (!IsServer) return;

        resistanceToAudioLure -= 1 * Time.deltaTime;

        resistanceToAudioLure = Mathf.Clamp(resistanceToAudioLure, 0f, 100f);
    }

    public void AudioLure_AttractAnimatronic(Node targetedNode)
    {
        // Early resistance if already targeting the node
        if (currentTarget == targetedNode)
        {
            resistanceToAudioLure = Mathf.Min(resistanceToAudioLure + 30, 100f);
            Debug.Log($"[AudioLure:{name}] Already targeting this node. Resistance increased by 10 to {resistanceToAudioLure}");
        }

        bool targetIsAlive = targetedNode.GetComponent<PlayerNode>()?.IsAlive ?? false;
        float roll = UnityEngine.Random.Range(0f, 100f);
        float threshold = resistanceToAudioLure;

        Debug.Log($"[AudioLure:{name}] Trying to attract to {targetedNode.name}. Resistance: {threshold:F2}, Roll: {roll:F2}, TargetAlive: {targetIsAlive}");

        if (threshold < roll || targetIsAlive)
        {
            SetAggrivation(true);
            SetTarget(targetedNode);

            float resistanceGain = Mathf.Lerp(10f, 80f, GetUnaggrivatedDifficulty() / 20f);

            resistanceToAudioLure = Mathf.Min(resistanceToAudioLure + resistanceGain, 100f);

            Debug.Log($"[AudioLure:{name}] Lure SUCCESSFUL. New Target: {targetedNode.name}, Resistance increased by {resistanceGain:F2} to {resistanceToAudioLure:F2}");
        }
        else
        {
            Debug.Log($"[AudioLure:{name}] Lure RESISTED. No change in behavior.");
        }
    }


    public void OnAttentionDivert(Node targetedNode) // called if the alarm goes off etc
    {
        SetTarget(targetedNode);
        currentMovementWaitTime.Value /= 1.1f; // permanantly makes animatronic faster
    }

    // if isnt aggrivated and should be then aggrivate and vise versa
    private protected void SetAggrivation(bool shouldBeAggrivated)
    {
        if (shouldBeAggrivated)
        {
            if (!isCurrentlyAggrivated.Value)
            {
                isCurrentlyAggrivated.Value = true;
                currentMovementWaitTime.Value /= aggrivationDivisor;
                currentDifficulty.Value += aggrivationAddition;
                loopsAggrivated = 0;
            }
        }
        else
        {
            if (isCurrentlyAggrivated.Value)
            {
                isCurrentlyAggrivated.Value = false;
                currentMovementWaitTime.Value *= aggrivationDivisor;
                currentDifficulty.Value -= aggrivationAddition;
                loopsAggrivated = 0;
            }
        }
    }

    private protected float GetUnaggrivatedDifficulty()
    {
        return currentDifficulty.Value - (isCurrentlyAggrivated.Value ? 20 : 0);
    }

    private protected virtual IEnumerator GameplayLoop()
    {
        SetNode(startNode, false, false);
        // Don't start the loop if difficulty or wait time is 0
        if (currentDifficulty.Value == 0 || currentMovementWaitTime.Value == 0) yield break;

        hasStartedMoving = false;
        yield return new WaitForSeconds(waitTimeToStartMoving);
        hasStartedMoving = true;

        // Main loop: runs as long as the game is active and this is the server
        while (GameManager.Instance.isPlaying && IsServer)
        {
            if (currentNode == PlayerRoleManager.Instance.janitorBehaviour.insideNode)
            {
                RecognitionResult recognitionResult = new();
                yield return PlayerRoleManager.Instance.janitorBehaviour.HandleRecognitionLogic(GetUnaggrivatedDifficulty(), recognitionResult);

                if (recognitionResult.Value == true)
                {
                    StartCoroutine(KillPlayer(AnimatronicManager.Instance.GetPlayerNodeFromPlayerRole(PlayerRoles.Janitor)));
                    yield return new WaitForSeconds(currentMovementWaitTime.Value);
                }
                else Blocked(PlayerRoleManager.Instance.janitorBehaviour);
            }
            else
            {
                yield return new WaitForSeconds(currentMovementWaitTime.Value);

                if (NeedsANewTarget()) TargetRandomPlayer(); // Acquire a target if needed

                if (currentTarget == null) continue;// If there's still no target, skip this loop iteration

                // Get path to the target without considering node occupancy
                List<Node> path = AnimatronicManager.Instance.BreadthFirstSearch(currentNode, currentTarget, this, takeOccupancyIntoAccount: false);

                // Skip if path is empty or we are already at the target
                if (path.Count <= 1) continue;

                // Try to move if allowed by conditions
                if (MovementCondition())
                {
                    yield return MovementOpportunity(path);
                }
            }
        }
    }


    private protected virtual bool MovementCondition()
    {
        return UnityEngine.Random.Range(1, 20 + 1) <= currentDifficulty.Value;
    }

    private protected virtual IEnumerator SecondaryMovementCondition()
    {
        yield break;
    }

    private bool NeedsANewTarget()
    {
        if (currentTarget == null) return true;

        bool isTargetingPlayer = currentTarget.GetComponent<PlayerNode>() != null;

        float difficulty = currentDifficulty.Value / 20f;

        // Base chance to switch targets
        float baseChance = isTargetingPlayer
            ? Mathf.Lerp(20f, 10f, difficulty)
            : Mathf.Lerp(10f, 20f, difficulty);

        // If aggravated, dampen the chance (e.g. by 50%)
        if (isCurrentlyAggrivated.Value)
            baseChance *= 0.5f;

        return UnityEngine.Random.Range(0f, 100f) < baseChance;
    }

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
        SetAggrivation(false); // reset aggrivation
        currentTarget = targetNode;
    }

    private IEnumerator MovementOpportunity(List<Node> path)
    {
        yield return SecondaryMovementCondition(); // if needed
        Node nodeToGoTo = path[1];

        bool aboutToAttackPlayer = nodeToGoTo.TryGetComponent(out PlayerNode playerNode);

        if (aboutToAttackPlayer) // about to attack a player
        {
            if (IsPlayerVulnerableToAttack(playerNode))
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

    private protected virtual bool IsPlayerVulnerableToAttack(PlayerNode playerNode)
    {
        return PlayerRoleManager.Instance.IsPlayerVulnerableToAttack(currentNode, playerNode) && playerNode.IsAlive;
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

        playerBehaviour.currentPower.Value -= 1;
        playerBehaviour.PlayDoorKnockAudioClientRpc(indexOfCurrentNode, false);

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
            audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
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

        nodeToSet.occupier = this;
        currentNode = nodeToSet;
        if (IsServer) nodeToSet.isOccupied.Value = true;
    }

    private protected IEnumerator LerpToPosition(Node startingNode, Node nodeToSet, float lerpSpeed, float movementTime = 1)
    {
        float elapsedTime = 0;

        while (elapsedTime < movementTime)
        {
            yield return null;
            elapsedTime += Time.deltaTime;

            // UI Map position lerp
            MapTransform.anchoredPosition = Vector2.Lerp(
                MapTransform.anchoredPosition,
                nodeToSet.MapTransform.anchoredPosition,
                Time.deltaTime * lerpSpeed
            );

            if (animatronicModel == null) continue;

            animatronicModel.SetPositionAndRotation
            (
                Vector3.Lerp(animatronicModel.position, nodeToSet.physicalTransform.position, Time.deltaTime * lerpSpeed),
                Quaternion.Lerp(animatronicModel.rotation, nodeToSet.physicalTransform.rotation, Time.deltaTime * lerpSpeed)
            );
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

public class RecognitionResult
{
    public bool Value;
}
