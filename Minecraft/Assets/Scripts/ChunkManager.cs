using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{

    private static readonly float CHUNK_UPDATE_INTERVAL = 0.25f;
    public EntityManager entityManager;
    public bool useRandomSeed = false;
    public int seed;

    public PlayerController player;
    public Vector3Int loadDistance = new Vector3Int(4,1,4);
    public Vector3Int chunkSize = Vector3Int.one * 16;

    private Texture2D blockAtlas;
    public Material chunkOpaqueMaterial;
    public Material chunkWaterMaterial;
    public Material chunkFoliageMaterial;

    public int LoadedChunkCount
    {
        get { return _chunks.Count; }
    }

    private Dictionary<Vector3Int, WorldChunk> _chunks; // All chunks that have been initialized
    private HashSet<Vector3Int> _generatedChunkIDs;     // Chunks that have had their terrain data generated, though are not necessarily visible.
    private HashSet<Vector3Int> _loadedChunkIDs;        // Chunks that are currently loaded and visible

    private Queue<Vector3Int> _chunkLoadQueue;
    private Queue<Vector3Int> _chunkUnloadQueue;

    private HashSet<Vector3Int> _chunksToUpdate;
    private HashSet<Vector3Int> _blocksToUpdate;

    private Vector3Int _prevPlayerChunk;

    private float _chunkUpdateTimer;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (useRandomSeed)
        {
            seed = UnityEngine.Random.Range(int.MinValue,int.MaxValue);
        }

        _chunks = new Dictionary<Vector3Int, WorldChunk>();
        _generatedChunkIDs = new HashSet<Vector3Int>();
        _loadedChunkIDs = new HashSet<Vector3Int>();

        _chunkLoadQueue = new Queue<Vector3Int>();
        _chunkUnloadQueue = new Queue<Vector3Int>();

        //_chunksToUpdate = new HashSet<Vector3Int>();
        _blocksToUpdate = new HashSet<Vector3Int>();


        _prevPlayerChunk = Vector3Int.one * int.MaxValue;

        blockAtlas = (UnityEngine.Texture2D)chunkOpaqueMaterial.GetTexture("_MainTex");

        UpdateLoadedChunks();

        LoadChunksImmediately();
    }

    /// <summary>
    /// Load all the chunks in the chunk load queue immediately in this frame.
    /// (Useful when first loading the scene.)
    /// </summary>
    private void LoadChunksImmediately()
    {
        while (_chunkLoadQueue.Count > 0)
        {
            LoadNextChunk();
        }
    }

    /// <summary>
    /// Load the next chunk in the chunk load queue.
    /// </summary>
    private void LoadNextChunk()
    {
        Vector3Int chunkID = _chunkLoadQueue.Dequeue();
        if (_chunks.ContainsKey(chunkID) == false)
        {
            WorldChunk newChunk = InitializeChunk(chunkID);
            _chunks.Add(chunkID, newChunk);
        }
        WorldChunk chunk = _chunks[chunkID];
        chunk.gameObject.SetActive(true);
        if (chunk.IsLoaded == false)
        {
            chunk.BuildMesh();
        }
    }

    /// <summary>
    /// Unload all the chunks in the chunk unload queue immediately in this frame.
    /// </summary>
    private void UnloadChunksImmediately()
    {
        while (_chunkUnloadQueue.Count > 0)
        {
            UnloadNextChunk();
        }
    }

    /// <summary>
    /// Unload the next chunk in the chunk load queue.
    /// </summary>
    private void UnloadNextChunk()
    {
        Vector3Int chunkID = _chunkUnloadQueue.Dequeue();
        if (_chunks.ContainsKey(chunkID) == false)
        {
            Debug.LogWarning("Missing chunk key!");
            return;
        }
        WorldChunk chunk = _chunks[chunkID];
        chunk.gameObject.SetActive(false);

        if (chunk.IsModified == false)
        {
            _chunks.Remove(chunkID);
            Destroy(chunk.gameObject);
        }
    }

    public void UnloadChunk(Vector3Int chunkID)
    {
        // Check that the chunk has been initialized and loaded (in the _chunk dictionary).
        if (_chunks.ContainsKey(chunkID) == false)
        {
            Debug.LogWarning("Attempting to unload chunk that is not currently loaded.");
            return;
        }

        // Add chunk to chunk unload queue.
        _chunkUnloadQueue.Enqueue(chunkID);
    }

    public void LoadChunk(Vector3Int chunkID)
    {
        if (_chunks.ContainsKey(chunkID))
        {
            Debug.LogWarning("Attempting to load an already loaded chunk.");
            return;
        }

        _chunkLoadQueue.Enqueue(chunkID);
    }


    private void Update()
    {

        float startTime = Time.realtimeSinceStartup;
        float elapsedTime = 0.0f;
        while (_chunkLoadQueue.Count > 0 && elapsedTime < 0.02f)
        {
            LoadNextChunk();
            elapsedTime = Time.realtimeSinceStartup - startTime;
        }

        if (_chunkUnloadQueue.Count > 0)
        {
            UnloadChunksImmediately();
        }

        //if (_chunksToUpdate.Count > 0)
        //{
        //    _chunkUpdateTimer += Time.deltaTime;
        //    if (_chunkUpdateTimer > CHUNK_UPDATE_INTERVAL)
        //    {
        //        UpdateChunks();
        //        _chunkUpdateTimer = 0.0f;
        //    }
        //}

        if (_blocksToUpdate.Count > 0)
        {
            _chunkUpdateTimer += Time.deltaTime;
            if (_chunkUpdateTimer > CHUNK_UPDATE_INTERVAL)
            {
                UpdateBlocks();
                _chunkUpdateTimer = 0.0f;
            }
        }
    }

    private void UpdateBlocks()
    {
        HashSet<Vector3Int> nextBlocksToUpdate = new HashSet<Vector3Int>();

        foreach(Vector3Int blockPos in _blocksToUpdate)
        {
            Vector3Int chunkId = GetNearestChunkPosition(blockPos);

            if (_chunks.ContainsKey(chunkId))
            {
                WorldChunk chunk = _chunks[chunkId];

                nextBlocksToUpdate.UnionWith(chunk.UpdateBlock(blockPos, out HashSet<WorldChunk> touchedChunks));

                foreach (WorldChunk touchedChunk in touchedChunks)
                {
                    touchedChunk.BuildMesh();
                }
            }
        }

        _blocksToUpdate = nextBlocksToUpdate;
    }

    public void UpdateLoadedChunks()
    {
        Vector3Int playerPos = GetPlayerPosition();

        Vector3Int playerChunkPos = GetNearestChunkPosition(playerPos);
        if (playerChunkPos == _prevPlayerChunk)
        {
            return; // The player has not left the previous chunk, so don't bother checking if they're closer to other chunks
        }

        _prevPlayerChunk = playerChunkPos;

        List<Vector3Int> chunksPositionsToLoad = GetInRangeChunkPositions(playerChunkPos, loadDistance);

        //foreach (Vector3Int pos in chunksPositionsToLoad)
        //{
        //    if (_chunks.ContainsKey(pos) == false)
        //    {
        //        LoadChunk(pos);
        //    }
        //}

        _chunkLoadQueue = new Queue<Vector3Int>(chunksPositionsToLoad);

        // Unload
        Vector3Int[] keys = new Vector3Int[_chunks.Count];
        _chunks.Keys.CopyTo(keys,0);
        foreach (Vector3Int pos in keys)
        {
            // TODO: Use a set for faster contains check
            if (chunksPositionsToLoad.Contains(pos) == false)
            {
                UnloadChunk(pos);
            }
        }
    }

    public WorldChunk InitializeChunk(Vector3Int pos)
    {
        GameObject go = new GameObject("Chunk [" + pos.x + "," + pos.y + "," + pos.z + "]");
        go.transform.parent = this.transform;
        WorldChunk chunk = go.AddComponent<WorldChunk>();

        // TODO: Have chunk load these from resources
        chunk.blockAtlas = this.blockAtlas;
        chunk.chunkOpaqueMaterial = this.chunkOpaqueMaterial;
        chunk.chunkWaterMaterial = this.chunkWaterMaterial;
        chunk.chunkFoliageMaterial = this.chunkFoliageMaterial;

        //Debug.Log("Init Chunk [" + pos.x + "," + pos.y + "," + pos.z + "]");
        chunk.Initialize(pos, chunkSize, seed);
        chunk.chunkManager = this;

        return chunk;
    }

    private List<Vector3Int> GetInRangeChunkPositions(Vector3Int centerChunkPos, Vector3Int radius)
    {
        SortedList<float, Vector3Int> positionsInRange = new SortedList<float,Vector3Int>();
 

        //      int maxManhattanDist = radius.x;
        //radius.y = radius.x;
        //radius.z = radius.x;

        float maxDistSqrd = Mathf.Pow(radius.x * chunkSize.x, 2f);

        int i = 0;

        for (int dx = -radius.x; dx <= radius.x; dx += 1)
        {
            for (int dy = -radius.y; dy <= radius.y; dy += 1)
            {
                for (int dz = -radius.z; dz <= radius.z; dz += 1)
                {

                    Vector3 offset = new Vector3(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                    float distSqrd = offset.sqrMagnitude;

                    if (distSqrd <= maxDistSqrd)
                    {
                        i++;
                        positionsInRange.Add(distSqrd + (i)*0.01f, centerChunkPos + Vector3Int.RoundToInt(offset));
                    }

                    //int manhattanDist = Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz);


                    //if (manhattanDist <= maxManhattanDist)
                    //{
                    //    Vector3Int offset = new Vector3Int(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                    //    positionsInRange.Add(centerChunkPos + offset);
                    //}

                    //Vector3Int offset = new Vector3Int(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                    //positionsInRange.Add(centerChunkPos + offset);
                }
            }
        }
        

        return positionsInRange.Values.ToList();
    }

    private Vector3Int GetNearestChunkPosition(Vector3Int pos)
    {
        Vector3Int offset = new Vector3Int(int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2);
        pos += offset;

        int xi = pos.x / chunkSize.x;
        int yi = pos.y / chunkSize.y;
        int zi = pos.z / chunkSize.z;

        int x = xi * chunkSize.x;
        int y = yi * chunkSize.y;
        int z = zi * chunkSize.z;

        return new Vector3Int(x, y, z) - offset;
    }

    public WorldChunk GetNearestChunk(Vector3Int pos)
    {
        Vector3Int nearestPos = GetNearestChunkPosition(pos);
        if (_chunks.ContainsKey(nearestPos) == false)
        {
            return null;
        }

        return _chunks[nearestPos];
    }

    public Block GetBlockAtPosition(Vector3Int pos)
    {
        WorldChunk chunk = GetNearestChunk(pos);
        if (chunk == null)
        {
            return null;
        }
        return chunk.GetBlockAtPosition(pos);
    }

    private Vector3Int GetPlayerPosition()
    {
        return Vector3Int.CeilToInt(player.transform.position);
    }

    public HashSet<Vector3Int> ModifyBlock(Vector3Int position, Block newBlock, out HashSet<WorldChunk> modifiedChunks)
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        List<Block> newBlocks = new List<Block>();
        positions.Add(position);
        newBlocks.Add(newBlock);
        return ModifyBlocks(positions, newBlocks, out modifiedChunks);
    }

    public HashSet<Vector3Int> ModifyBlocks(List<Vector3Int> positions, List<Block> newBlocks, out HashSet<WorldChunk> modifiedChunks)
    {
        modifiedChunks = new HashSet<WorldChunk>();
        HashSet<WorldChunk> relevantChunks = new HashSet<WorldChunk>();
        HashSet<Vector3Int> blocksToUpdate = new HashSet<Vector3Int>();

        foreach (Vector3Int pos in positions)
        {
            relevantChunks.Add(GetNearestChunk(pos));
        }

        foreach (WorldChunk chunk in relevantChunks)
        {
            if (chunk != null)
            {
                HashSet<Vector3Int> touchedBlocks = chunk.ModifyBlocks(positions, newBlocks, out HashSet<WorldChunk> touchedChunks); // TODO: Should I add these to modifed chunks
                bool chunkWasModified = touchedBlocks.Count > 0;

                if (chunkWasModified)
                {
                    blocksToUpdate.UnionWith(touchedBlocks);
                    modifiedChunks.Add(chunk);
                    modifiedChunks.UnionWith(touchedChunks);
                }
            }
        }

        return blocksToUpdate;
    }

    public bool ModifyAndUpdateBlock(Vector3Int position, Block newBlock)
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        List<Block> newBlocks = new List<Block>();
        positions.Add(position);
        newBlocks.Add(newBlock);
        return ModifyAndUpdateBlocks(positions, newBlocks);
    }

    public bool ModifyAndUpdateBlocks(List<Vector3Int> positions, List<Block> newBlocks)
    {
        HashSet<WorldChunk> relevantChunks = new HashSet<WorldChunk>();
        HashSet<WorldChunk> chunksToRebuild = new HashSet<WorldChunk>();
        HashSet<Vector3Int> blocksToUpdate = new HashSet<Vector3Int>();

        foreach (Vector3Int pos in positions)
        {
            relevantChunks.Add(GetNearestChunk(pos));
        }

        foreach (WorldChunk chunk in relevantChunks)
        {
            if (chunk != null)
            {
                HashSet<Vector3Int> touchedBlocks = chunk.ModifyBlocks(positions, newBlocks, out HashSet<WorldChunk> touchedChunks);
                bool chunkWasModified = touchedBlocks.Count > 0 || touchedChunks.Count > 0;

                if (chunkWasModified)
                {
                    blocksToUpdate.UnionWith(touchedBlocks);
                    chunksToRebuild.Add(chunk);
                    chunksToRebuild.UnionWith(touchedChunks);
                }
            }
        }

        bool shouldRebuildChunks = chunksToRebuild.Count > 0;

        if (shouldRebuildChunks)
        {
            // Rebuild the modified chunks.
            foreach (WorldChunk chunk in chunksToRebuild)
            {
                chunk.BuildMesh();
            }
        }

        // Add more blocks to the update list
        _blocksToUpdate.UnionWith(blocksToUpdate);

        return shouldRebuildChunks;
    }
}
