using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimatronicManager : NetworkSingleton<AnimatronicManager>
{
    public Foxy foxy;
    public Freddy freddy;
    private List<Animatronic> Animatronics
    {
        get => GetComponentsInChildren<Animatronic>().ToList();
    }
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
            ["Bonnie"] = new AnimatronicData(waitTimeToStartMoving: 20, startingDifficulty: 1, timeBetweenMovement: 5, hourlyDifficultyIncrement: 1),
            ["Chica"] = new AnimatronicData(30, 0, 6, 2),
            ["Foxy"] = new AnimatronicData(60, 4, 9, 1),
            ["Freddy"] = new AnimatronicData(30, 0, 10, 0.3f),
            ["Zap"] = new AnimatronicData(60, 5, 10, 2),
            ["Golden Freddy"] = new AnimatronicData(180, 5, null, 2),
            ["Keypad System"] = new AnimatronicData(null, 5, null, 2),
            ["Maintenance System"] = new AnimatronicData(null, 5, null, null),
        },
        [GameNight.Two] = new()
        {
            ["Bonnie"] = new AnimatronicData(17, 2, 5, 2),
            ["Chica"] = new AnimatronicData(24, 2, 6, 2),
            ["Foxy"] = new AnimatronicData(8, 7, 8, 2),
            ["Freddy"] = new AnimatronicData(30, 1, 9, 1f),
            ["Zap"] = new AnimatronicData(51, 7, 8, 2),
            ["Golden Freddy"] = new AnimatronicData(130, 7, null, 2),
            ["Keypad System"] = new AnimatronicData(null, 7, null, 2),
            ["Maintenance System"] = new AnimatronicData(null, 7, null, null),
        },
        [GameNight.Three] = new()
        {
            ["Bonnie"] = new AnimatronicData(14, 5, 5, 2),
            ["Chica"] = new AnimatronicData(18, 5, 6, 2),
            ["Foxy"] = new AnimatronicData(7, 9, 7, 2),
            ["Freddy"] = new AnimatronicData(30, 2, 8, 2f),
            ["Zap"] = new AnimatronicData(42, 9, 7, 2),
            ["Golden Freddy"] = new AnimatronicData(70, 9, null, 2),
            ["Keypad System"] = new AnimatronicData(null, 9, null, 2),
            ["Maintenance System"] = new AnimatronicData(null, 9, null, null),
        },
        [GameNight.Four] = new()
        {
            ["Bonnie"] = new AnimatronicData(11, 6, 5, 2),
            ["Chica"] = new AnimatronicData(12, 7, 6, 2),
            ["Foxy"] = new AnimatronicData(6, 11, 6, 2),
            ["Freddy"] = new AnimatronicData(30, 3, 7, 2f),
            ["Zap"] = new AnimatronicData(33, 11, 6, 2),
            ["Golden Freddy"] = new AnimatronicData(30, 11, null, 2),
            ["Keypad System"] = new AnimatronicData(null, 11, null, 2),
            ["Maintenance System"] = new AnimatronicData(null, 11, null, null),
        },
        [GameNight.Five] = new()
        {
            ["Bonnie"] = new AnimatronicData(8, 8, 5, 3),
            ["Chica"] = new AnimatronicData(6, 10, 6, 2),
            ["Foxy"] = new AnimatronicData(5, 13, 5, 2),
            ["Freddy"] = new AnimatronicData(30, 5, 6, 2f),
            ["Zap"] = new AnimatronicData(24, 13, 5, 2),
            ["Golden Freddy"] = new AnimatronicData(20, 13, null, 2),
            ["Keypad System"] = new AnimatronicData(null, 13, null, 2),
            ["Maintenance System"] = new AnimatronicData(null, 13, null, null),
        },
        [GameNight.Six] = new()
        {
            ["Bonnie"] = new AnimatronicData(3, 10, 5, 3),
            ["Chica"] = new AnimatronicData(3, 12, 6, 2),
            ["Foxy"] = new AnimatronicData(3, 15, 4, 3),
            ["Freddy"] = new AnimatronicData(30, 10, 5, 2f),
            ["Zap"] = new AnimatronicData(3, 15, 3, 1),
            ["Golden Freddy"] = new AnimatronicData(10, 15, null, 2),
            ["Keypad System"] = new AnimatronicData(null, 15, null, 2),
            ["Maintenance System"] = new AnimatronicData(null, 17, null, null),
        },
        [GameNight.Seven] = new()
        {
            ["Bonnie"] = new AnimatronicData(0, 20, 4, 0),
            ["Chica"] = new AnimatronicData(0, 20, 4, 0),
            ["Foxy"] = new AnimatronicData(0, 20, 3, 0),
            ["Freddy"] = new AnimatronicData(0, 20, 4, 0),
            ["Zap"] = new AnimatronicData(0, 20, 3, 0),
            ["Golden Freddy"] = new AnimatronicData(5, 20, null, 2),
            ["Keypad System"] = new AnimatronicData(null, 20, null, 2),
            ["Maintenance System"] = new AnimatronicData(null, 20, null, null),
        },
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
}

[Serializable]
public struct AnimatronicData
{
    public int? waitTimeToStartMoving;
    public float? startingDifficulty;
    public float? timeBetweenMovementOpportunities;
    public float? hourlyDifficultyIncrementAmount;

    public AnimatronicData(int? waitTimeToStartMoving, float? startingDifficulty, float? timeBetweenMovement, float? hourlyDifficultyIncrement)
    {
        this.waitTimeToStartMoving = waitTimeToStartMoving;
        this.startingDifficulty = startingDifficulty;
        this.timeBetweenMovementOpportunities = timeBetweenMovement;
        this.hourlyDifficultyIncrementAmount = hourlyDifficultyIncrement;
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
