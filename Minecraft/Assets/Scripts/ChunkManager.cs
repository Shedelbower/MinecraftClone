using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public PlayerController player;
    [Range(1,100)] public int loadDistance = 4;
    [Range(4, 32)] public int chunkWidth = 16;
    [Range(4, 64)] public int chunkHeight = 32;

    public Texture2D blockAtlas;
    public Material chunkMaterial;

    [SerializeField] private Dictionary<Vector2Int, WorldChunk> _chunks;

    private Vector2Int _prevPlayerChunk;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _chunks = new Dictionary<Vector2Int, WorldChunk>();

        _prevPlayerChunk = Vector2Int.one * int.MaxValue;

        UpdateLoadedChunks();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateLoadedChunks();
        }
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

        //Debug.Log("Player:" + playerPos);
        //Debug.Log("Neighbor Count: " + chunksPositionsToLoad.Count);

        foreach (Vector2Int pos in chunksPositionsToLoad)
        {
            //Debug.Log(pos);
            LoadChunk(pos);
        }

        // Unload
        //List<WorldChunk> chunksToUnload = new List<WorldChunk>();
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
            _chunks.Add(pos, CreateChunk(pos));
        } else
        {
            Debug.Log("Using Cached Chunk");
        }

        WorldChunk chunk = _chunks[pos];

        chunk.gameObject.SetActive(true);
    }

    public WorldChunk CreateChunk(Vector2Int pos)
    {
        GameObject go = new GameObject("Chunk [" + pos.x + "," + pos.y + "]");
        go.transform.parent = this.transform;
        WorldChunk chunk = go.AddComponent<WorldChunk>();

        // TODO: Have chunk load these from resources
        chunk.blockAtlas = this.blockAtlas;
        chunk.chunkMaterial = this.chunkMaterial;

        chunk.Initialize(pos.x, pos.y, chunkWidth, chunkHeight);

        chunk.BuildMesh();

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
        int xi = pos.x / chunkWidth;
        int zi = pos.y / chunkWidth;

        int x = xi * chunkWidth;
        int z = zi * chunkWidth;

        return new Vector2Int(x, z);
    }

    private Vector2Int GetPlayerPosition()
    {
        int x = (int)player.transform.position.x;
        int z = (int)player.transform.position.z;
        return new Vector2Int(x, z);
    }

}
