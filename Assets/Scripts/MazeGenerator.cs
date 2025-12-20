using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    public int width = 40;
    public int height = 40;
    public float cellSize = 4f;

    [Range(0f, 1f)]
    public float loopChance = 0.2f;

    [Header("Visuals")]
    public float wallHeight = 10.0f;

    [Header("Lighting")]
    public GameObject torchPrefab;
    [Range(0f, 1f)]
    public float torchChance = 0.15f;
    public float torchHeight = 2.5f;
    public float torchWallOffset = 0.35f;

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float speedBoostChance = 0.01f;
    [Range(0f, 1f)]
    public float flashbangChance = 0.01f;
    public int minimapCount = 3;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject playerPrefab;
    public GameObject minotaurPrefab;
    public GameObject endTriggerPrefab;
    public GameObject speedBoostPrefab;
    public GameObject flashbangPrefab;
    public GameObject minimapPrefab;

    private System.Random rng = new System.Random();

    private struct Cell
    {
        public bool visited;
        public bool topWall;
        public bool bottomWall;
        public bool leftWall;
        public bool rightWall;
    }

    private Cell[,] grid;

    void Start()
    {
        GenerateAndBuild();
    }

    void GenerateAndBuild()
    {
        foreach (Transform child in transform) { Destroy(child.gameObject); }

        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new Cell { visited = false, topWall = true, bottomWall = true, leftWall = true, rightWall = true };
            }

        Stack<Vector2Int> cellStack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(0, 0);
        grid[current.x, current.y].visited = true;
        cellStack.Push(current);

        while (cellStack.Count > 0)
        {
            current = cellStack.Peek();
            List<Vector2Int> neighbors = new List<Vector2Int>();

            if (current.y + 1 < height && !grid[current.x, current.y + 1].visited) neighbors.Add(new Vector2Int(current.x, current.y + 1));
            if (current.y - 1 >= 0 && !grid[current.x, current.y - 1].visited) neighbors.Add(new Vector2Int(current.x, current.y - 1));
            if (current.x + 1 < width && !grid[current.x + 1, current.y].visited) neighbors.Add(new Vector2Int(current.x + 1, current.y));
            if (current.x - 1 >= 0 && !grid[current.x - 1, current.y].visited) neighbors.Add(new Vector2Int(current.x - 1, current.y));

            if (neighbors.Count > 0)
            {
                Vector2Int next = neighbors[rng.Next(neighbors.Count)];
                RemoveWall(current, next);
                grid[next.x, next.y].visited = true;
                cellStack.Push(next);
            }
            else
            {
                cellStack.Pop();
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (rng.NextDouble() < loopChance)
                {
                    List<Vector2Int> neighbors = new List<Vector2Int>();
                    if (x + 1 < width && grid[x, y].rightWall) neighbors.Add(new Vector2Int(x + 1, y));
                    if (y + 1 < height && grid[x, y].topWall) neighbors.Add(new Vector2Int(x, y + 1));

                    if (neighbors.Count > 0)
                    {
                        Vector2Int target = neighbors[rng.Next(neighbors.Count)];
                        RemoveWall(new Vector2Int(x, y), target);
                    }
                }
            }
        }

    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            Vector3 cellPos = new Vector3(x * cellSize, 0, y * cellSize);

            if (floorPrefab) Instantiate(floorPrefab, cellPos, Quaternion.identity, transform);

            if (grid[x, y].topWall) SpawnWall(cellPos + new Vector3(0, 0, cellSize/2), Quaternion.identity);
            if (grid[x, y].rightWall) SpawnWall(cellPos + new Vector3(cellSize/2, 0, 0), Quaternion.Euler(0, 90, 0));

            if (y == 0 && grid[x, y].bottomWall) SpawnWall(cellPos + new Vector3(0, 0, -cellSize/2), Quaternion.identity);

            if (x == 0 && grid[x, y].leftWall) SpawnWall(cellPos + new Vector3(-cellSize/2, 0, 0), Quaternion.Euler(0, 90, 0));
        }
    }

        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface)
        {
            surface.BuildNavMesh();
        }

        SpawnUnitsAndItems();
    }

    [Header("AI Settings")]
    public Unity.Barracuda.NNModel trainedModel;

    void SpawnUnitsAndItems()
    {
        GameObject playerObj = null;
        if (playerPrefab)
        {
            playerObj = Instantiate(playerPrefab, new Vector3(0, 0.2f, 0), Quaternion.identity);
            playerObj.tag = "Player";
            playerObj.name = "Player_Spawned";

            // AUTO-ASSIGN AI BRAIN
            if (trainedModel != null)
            {
                var bp = playerObj.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
                if (bp)
                {
                    bp.Model = trainedModel;
                    bp.BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;
                }
            }
        }

        if (minotaurPrefab)
        {
            SpawnMinotaur(playerObj);
        }

        if (endTriggerPrefab) Instantiate(endTriggerPrefab, new Vector3((width - 1) * cellSize, 0.2f, (height - 1) * cellSize), Quaternion.identity);

        SpawnItems();
    }

    void SpawnMinotaur(GameObject playerObj)
    {
        int cx = width / 2;
        int cy = height / 2;
        Vector3 bestSpawnPos = Vector3.zero;
        bool foundCell = false;

        for (int r = 0; r < 5; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx > 0 && nx < width - 1 && ny > 0 && ny < height - 1)
                    {
                        bestSpawnPos = new Vector3(nx * cellSize, 0.2f, ny * cellSize);
                        foundCell = true;
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(bestSpawnPos, out hit, 2.0f, NavMesh.AllAreas))
                        {
                            bestSpawnPos = hit.position;
                            goto FoundSpawn;
                        }
                    }
                }
            }
        }

        FoundSpawn:
        if (foundCell)
        {
            GameObject minotaur = Instantiate(minotaurPrefab, bestSpawnPos, Quaternion.identity);
            MinotaurAI ai = minotaur.GetComponent<MinotaurAI>();
            if (ai != null && playerObj != null) ai.target = playerObj.transform;
        }
    }

    void SpawnItems()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < 3 && y < 3) continue;
                if (x > width - 3 && y > height - 3) continue;
                emptyCells.Add(new Vector2Int(x, y));
            }
        }

        for (int i = 0; i < emptyCells.Count; i++)
        {
            Vector2Int temp = emptyCells[i];
            int randomIndex = rng.Next(i, emptyCells.Count);
            emptyCells[i] = emptyCells[randomIndex];
            emptyCells[randomIndex] = temp;
        }

        if (minimapPrefab)
        {
            for (int i = 0; i < minimapCount && emptyCells.Count > 0; i++)
            {
                Vector2Int pos = emptyCells[0];
                Instantiate(minimapPrefab, new Vector3(pos.x * cellSize, 0.5f, pos.y * cellSize), Quaternion.identity);
                emptyCells.RemoveAt(0);
            }
        }

        foreach (Vector2Int pos in emptyCells)
        {
            double roll = rng.NextDouble();
            if (roll < speedBoostChance && speedBoostPrefab)
            {
                Instantiate(speedBoostPrefab, new Vector3(pos.x * cellSize, 0.5f, pos.y * cellSize), Quaternion.identity);
            }
            else if (rng.NextDouble() < flashbangChance && flashbangPrefab)
            {
                Instantiate(flashbangPrefab, new Vector3(pos.x * cellSize, 0.5f, pos.y * cellSize), Quaternion.identity);
            }
        }
    }

    void RemoveWall(Vector2Int current, Vector2Int next)
    {
        if (next.x > current.x) { grid[current.x, current.y].rightWall = false; grid[next.x, next.y].leftWall = false; }
        else if (next.x < current.x) { grid[current.x, current.y].leftWall = false; grid[next.x, next.y].rightWall = false; }
        else if (next.y > current.y) { grid[current.x, current.y].topWall = false; grid[next.x, next.y].bottomWall = false; }
        else if (next.y < current.y) { grid[current.x, current.y].bottomWall = false; grid[next.x, next.y].topWall = false; }
    }

    void SpawnWall(Vector3 pos, Quaternion rot)
    {
        if (wallPrefab)
        {
            GameObject w = Instantiate(wallPrefab, pos, rot, transform);
            w.transform.localScale = new Vector3(cellSize, wallHeight, 0.5f);

            if (torchPrefab != null && rng.NextDouble() < torchChance)
            {
                Vector3 torchPos = pos + (Vector3.up * torchHeight) + (rot * Vector3.forward * -torchWallOffset);
                Instantiate(torchPrefab, torchPos, rot, transform);
            }
        }
    }
}