using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimatronicManager : NetworkSingleton<AnimatronicManager>
{
    public Foxy foxy;
    public Freddy freddy;
    private List<Animatronic> Animatronics { get => GetComponentsInChildren<Animatronic>().ToList(); }
    public List<Node> Nodes
    {
        get => GetComponentsInChildren<Node>().ToList();
    }
    public List<PlayerNode> PlayerNodes
    {
        get => GetComponentsInChildren<PlayerNode>().ToList();
    }
    public Action<Node> AttentionDivert;

    public event Action<Node> OnAudioLure;

    public Dictionary<GameNight, Dictionary<string, AnimatronicData>> animatronicData = new()
    {
        [GameNight.One] = new()
        {
            ["Bonnie"] = new AnimatronicData(10, 1, 5, 1),
            ["Chica"] = new AnimatronicData(30, 0, 6, 2),
            ["Foxy"] = new AnimatronicData(60, 5, 5, 1),
            ["Freddy"] = new AnimatronicData(180, 5, 10, 2),
        },
        [GameNight.Two] = new()
        {
            ["Bonnie"] = new AnimatronicData(3f, 9f, 3.25f),
            ["Chica"] = new AnimatronicData(3.5f, 10f, 3.1f),
        }
    };
    public bool CanFindNightData(GameNight night, string animatronicName, out AnimatronicData data)
    {
        data = default;
        return animatronicData.TryGetValue(night, out var nightData) && nightData.TryGetValue(animatronicName, out data);
    }


    private void Start()
    {
        if (!IsServer) return;

        GameManager.Instance.OnGameStarted += InitialiseAnimatronics;
        GameManager.Instance.OnGameWin += DisableAnimatronics;
        GameManager.Instance.OnGameOver += DisableAnimatronics;
    }

    private void InitialiseAnimatronics()
    {
        Animatronics.ForEach((animatronic) => { if (animatronic.enabled) animatronic.Initialise(); });
    }

    private void DisableAnimatronics()
    {
        Animatronics.ForEach((animatronic) => { if (animatronic.enabled) animatronic.Disable(); });
    }

    public List<Node> BreadthFirstSearch(Node start, Node target, Animatronic animatronic, bool takeOccupancyIntoAccount)
    {
        Queue<Node> queue = new();
        Dictionary<Node, Node> cameFrom = new() { { start, null } };
        HashSet<Node> visited = new() { start };
        queue.Enqueue(start);

        // Perform BFS to calculate distances from target
        Dictionary<Node, int> targetDistances = CalculateNodesFromTarget(target, animatronic);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();

            if (current == target)
            {
                return ReconstructPath(cameFrom, current);
            }

            List<Node> neighbors;
            if (takeOccupancyIntoAccount)
            {
                neighbors = current.neighbouringNodes
                    .Where(neighbor => !visited.Contains(neighbor)
                        && animatronic.GetNodeData(neighbor).isAllowedToGoTo
                        && !neighbor.isOccupied.Value)
                    .ToList();
            }
            else
            {
                neighbors = current.neighbouringNodes
                    .Where(neighbor => !visited.Contains(neighbor)
                        && animatronic.GetNodeData(neighbor).isAllowedToGoTo)
                    .ToList();
            }

            // Sort neighbors based on distance to target
            neighbors = neighbors.OrderBy(neighbor => targetDistances.ContainsKey(neighbor) ? targetDistances[neighbor] : int.MaxValue).ToList();

            // pick a more efficient route if the difficulty is higher
            if (UnityEngine.Random.Range(1, 20 + 1) >= animatronic.currentDifficulty.Value)
            {
                neighbors.Reverse();
            }

            foreach (Node neighbor in neighbors)
            {
                queue.Enqueue(neighbor);
                visited.Add(neighbor);
                cameFrom[neighbor] = current;
            }
        }

        return new List<Node>(); // no possible path
    }

    public Dictionary<Node, int> CalculateNodesFromTarget(Node target, Animatronic animatronic)
    {
        Queue<Node> queue = new();
        Dictionary<Node, int> distances = new() { { target, 0 } };
        HashSet<Node> visited = new() { target };
        queue.Enqueue(target);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();
            int currentDistance = distances[current];

            foreach (Node neighbor in current.neighbouringNodes)
            {
                if (animatronic.GetNodeData(neighbor) == null) throw new System.Exception($"node data hasnt been added for {neighbor} on {animatronic.gameObject.name}");
                if (!visited.Contains(neighbor) && animatronic.GetNodeData(neighbor).isAllowedToGoTo)
                {
                    visited.Add(neighbor);
                    distances[neighbor] = currentDistance + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distances;
    }

    public List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        List<Node> path = new();
        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    public PlayerNode GetPlayerNodeFromPlayerRole(PlayerRoles playerRole)
    {
        return PlayerNodes.FirstOrDefault(playerNode => playerNode.playerBehaviour != null && playerNode.playerBehaviour.playerRole == playerRole);
    }

    public PlayerNode GetPlayerNodeFromName(NodeName nodeName)
    {
        return PlayerNodes.FirstOrDefault(playerNode => playerNode.playerBehaviour != null && playerNode.nodeName == nodeName);
    }

    public Node GetNodeFromName(NodeName nodeName)
    {
        return Nodes.FirstOrDefault(node => node.nodeName == nodeName);
    }

    public void PlayAudioLure(NodeName nodeName)
    {
        Node node = GetNodeFromName(nodeName);

        OnAudioLure?.Invoke(node);
    }

    public float GetAverageAnimatronicDifficulty()
    {
        if (Animatronics == null || Animatronics.Count == 0)
            return 1f; // Default to minimum difficulty

        return Animatronics.Average(a => a.currentDifficulty.Value);
    }

}

[Serializable]
public struct AnimatronicData
{
    public int waitTimeToStartMoving;
    public float startingDifficulty;
    public float timeBetweenMovementOpportunities;
    public float hourlyDifficultyIncrementAmount;

    public AnimatronicData(int waitTimeToStartMoving, float startingDifficulty, float timeBetweenMovementOpportunities, float hourlyDifficultyIncrementAmount)
    {
        this.waitTimeToStartMoving = waitTimeToStartMoving;
        this.startingDifficulty = startingDifficulty;
        this.timeBetweenMovementOpportunities = timeBetweenMovementOpportunities;
        this.hourlyDifficultyIncrementAmount = hourlyDifficultyIncrementAmount;
    }
}

public enum NodeName
{
    Stage1,
    Stage2,
    Stage3,
    Hall1,
    Hall2,
    Hall3,
    Hall4,
    Hall5,
    Bathroom1,
    Bathroom2,
    Bathroom3,
    LeftHallway1,
    LeftHallway2,
    Janitor_Inside,
    LeftHallway3,
    SecurityOffice_DoorwayL,
    RightHallway1,
    RightHallway2,
    RightHallway3,
    SecurityOffice_DoorwayR,
    Cove,
    BackstageHall1,
    BackstageHall2,
    PartsAndService,
    Kitchen,
    Janitor,
    SecurityOffice,
    Backstage
}
