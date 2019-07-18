using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public PlayerController player;
    [Range(1,100)] public int loadDistance = 4;
    [Range(4, 32)] public int chunkWidth = 16;
    [Range(4, 64)] public int chunkHeight = 32;

    private Texture2D blockAtlas;
    public Material chunkOpaqueMaterial;
    public Material chunkFadeMaterial;

    private Queue<WorldChunk> _chunkBuildQueue;

    [SerializeField] private Dictionary<Vector2Int, WorldChunk> _chunks;

    private Vector2Int _prevPlayerChunk;

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        Initialize();
    }

    public void Initialize()
    {
        _chunkBuildQueue = new Queue<WorldChunk>();
        _chunks = new Dictionary<Vector2Int, WorldChunk>();
        
        _prevPlayerChunk = Vector2Int.one * int.MaxValue;

        blockAtlas = (UnityEngine.Texture2D)chunkOpaqueMaterial.GetTexture("_MainTex");

        UpdateLoadedChunks();

        ForceBuildAllChunks();
    }

    private void ForceBuildAllChunks()
    {
        while (_chunkBuildQueue.Count > 0)
        {
            BuildNextChunk();
        }
    }

    private void Update()
    {
        if (_chunkBuildQueue.Count > 0)
        {
            BuildNextChunk();
        }
    }

    private void BuildNextChunk()
    {
        WorldChunk chunk = _chunkBuildQueue.Dequeue();
        chunk.BuildMesh();
    }

    public void UpdateLoadedChunks()
    {
        Vector2Int playerPos = GetPlayerPosition();

        Vector2Int playerChunkPos = GetNearestChunkPosition(playerPos);
        if (playerChunkPos == _prevPlayerChunk)
        {
            return; // The player has not left the previous chunk, so don't bother checking if they're closer to other chunks
        }

        _prevPlayerChunk = playerChunkPos;

        List<Vector2Int> chunksPositionsToLoad = GetInRangeChunkPositions(playerChunkPos, loadDistance);

        foreach (Vector2Int pos in chunksPositionsToLoad)
        {
            LoadChunk(pos);
        }

        // Unload
        foreach (Vector2Int pos in _chunks.Keys)
        {
            // TODO: Use a set for faster contains check
            if (chunksPositionsToLoad.Contains(pos) == false)
            {
                UnloadChunk(pos);
            }
        }
    }

    public void UnloadChunk(Vector2Int pos)
    {
        WorldChunk chunk = _chunks[pos];
        chunk.gameObject.SetActive(false);
    }

    public void LoadChunk(Vector2Int pos)
    {
        if (_chunks.ContainsKey(pos) == false)
        {
            WorldChunk newChunk = InitializeChunk(pos);
            _chunks.Add(pos, newChunk);
            _chunkBuildQueue.Enqueue(newChunk);
        }

        WorldChunk chunk = _chunks[pos];

        chunk.gameObject.SetActive(true);
    }

    public WorldChunk InitializeChunk(Vector2Int pos)
    {
        GameObject go = new GameObject("Chunk [" + pos.x + "," + pos.y + "]");
        go.transform.parent = this.transform;
        WorldChunk chunk = go.AddComponent<WorldChunk>();

        // TODO: Have chunk load these from resources
        chunk.blockAtlas = this.blockAtlas;
        chunk.chunkOpaqueMaterial = this.chunkOpaqueMaterial;
        chunk.chunkFadeMaterial = this.chunkFadeMaterial;

        chunk.Initialize(pos.x, pos.y, chunkWidth, chunkHeight);

        //chunk.BuildMesh();

        return chunk;
    }

    private List<Vector2Int> GetInRangeChunkPositions(Vector2Int centerChunkPos, int radius)
    {
        List<Vector2Int> positionsInRange = new List<Vector2Int>();

        //Vector2Int centerChunkPos = GetNearestChunkPosition(center);

        float sqrMaxDist = (radius * chunkWidth) * (radius * chunkWidth);

        for (int dx = -radius; dx <= radius; dx += 1)
        {
            for (int dz = -radius; dz <= radius; dz += 1)
            {
                int x = centerChunkPos.x + dx * chunkWidth;
                int z = centerChunkPos.y + dz * chunkWidth;

                Vector2Int chunkPos = new Vector2Int(x,z);

                float sqrDist = (centerChunkPos - chunkPos).sqrMagnitude;

                if (sqrDist <= sqrMaxDist)
                {
                    positionsInRange.Add(chunkPos);
                }
            }
        }

            return positionsInRange;
    }

    private Vector2Int GetNearestChunkPosition(Vector2Int pos)
    {
        pos -= Vector2Int.one * (chunkWidth / 2);
        //pos -= Vector2Int.one * (chunkWidth);

        int xi = pos.x / chunkWidth;
        int zi = pos.y / chunkWidth;

        int x = xi * chunkWidth;
        int z = zi * chunkWidth;

        return new Vector2Int(x, z);
    }

    private WorldChunk GetNearestChunk(Vector2Int pos)
    {
        Vector2Int nearestPos = GetNearestChunkPosition(pos);
        if (_chunks.ContainsKey(nearestPos) == false)
        {
            return null;
        }

        return _chunks[nearestPos];
    }

    private Vector2Int GetPlayerPosition()
    {
        int x = (int)player.transform.position.x;
        int z = (int)player.transform.position.z;
        return new Vector2Int(x, z);
    }

    public void UpdateBlocks(List<Vector3Int> positions, List<Block> newBlocks)
    {
        HashSet<WorldChunk> relevantChunks = new HashSet<WorldChunk>();

        foreach(Vector3Int pos in positions)
        {
            Vector2Int pos2D = new Vector2Int(pos.x, pos.z);
            relevantChunks.Add(GetNearestChunk(pos2D));
        }

        foreach(WorldChunk chunk in relevantChunks)
        {
            chunk.UpdateBlocks(positions, newBlocks);
        }
    }

}
