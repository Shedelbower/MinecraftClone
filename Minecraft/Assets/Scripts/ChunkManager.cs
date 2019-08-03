using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public PlayerController player;
    public Vector3Int loadDistance = new Vector3Int(4,1,4);
    public Vector3Int chunkSize = Vector3Int.one * 16;
    //[Range(4, 64)] public int chunkHeight = 32;

    private Texture2D blockAtlas;
    public Material chunkOpaqueMaterial;
    public Material chunkFadeMaterial;

    private Queue<WorldChunk> _chunkBuildQueue;

    [SerializeField] private Dictionary<Vector3Int, WorldChunk> _chunks;

    private Vector3Int _prevPlayerChunk;

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        Initialize();
    }

    public void Initialize()
    {
        _chunkBuildQueue = new Queue<WorldChunk>();
        _chunks = new Dictionary<Vector3Int, WorldChunk>();
        
        _prevPlayerChunk = Vector3Int.one * int.MaxValue;

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
        Vector3Int playerPos = GetPlayerPosition();

        Vector3Int playerChunkPos = GetNearestChunkPosition(playerPos);
        if (playerChunkPos == _prevPlayerChunk)
        {
            return; // The player has not left the previous chunk, so don't bother checking if they're closer to other chunks
        }

        _prevPlayerChunk = playerChunkPos;

        List<Vector3Int> chunksPositionsToLoad = GetInRangeChunkPositions(playerChunkPos, loadDistance);

        foreach (Vector3Int pos in chunksPositionsToLoad)
        {
            LoadChunk(pos);
        }

        // Unload
        foreach (Vector3Int pos in _chunks.Keys)
        {
            // TODO: Use a set for faster contains check
            if (chunksPositionsToLoad.Contains(pos) == false)
            {
                UnloadChunk(pos);
            }
        }
    }

    public void UnloadChunk(Vector3Int pos)
    {
        WorldChunk chunk = _chunks[pos];
        chunk.gameObject.SetActive(false);
    }

    public void LoadChunk(Vector3Int pos)
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

    public WorldChunk InitializeChunk(Vector3Int pos)
    {
        GameObject go = new GameObject("Chunk [" + pos.x + "," + pos.y + "," + pos.z + "]");
        go.transform.parent = this.transform;
        WorldChunk chunk = go.AddComponent<WorldChunk>();

        // TODO: Have chunk load these from resources
        chunk.blockAtlas = this.blockAtlas;
        chunk.chunkOpaqueMaterial = this.chunkOpaqueMaterial;
        chunk.chunkFadeMaterial = this.chunkFadeMaterial;

        //Debug.Log("Init Chunk [" + pos.x + "," + pos.y + "," + pos.z + "]");
        chunk.Initialize(pos, chunkSize);
        chunk.chunkManager = this;

        //chunk.BuildMesh();

        return chunk;
    }

    private List<Vector3Int> GetInRangeChunkPositions(Vector3Int centerChunkPos, Vector3Int radius)
    {
        List<Vector3Int> positionsInRange = new List<Vector3Int>();

		//      int maxManhattanDist = radius.x;
		//radius.y = radius.x;
		//radius.z = radius.x;

		float maxDistSqrd = Mathf.Pow(radius.x * chunkSize.x,2f);

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
                        //    Vector3Int offset = new Vector3Int(dx * chunkSize.x, dy * chunkSize.y, dz * chunkSize.z);
                        positionsInRange.Add(centerChunkPos + Vector3Int.RoundToInt(offset));
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

        return positionsInRange;
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

    private Vector3Int GetPlayerPosition()
    {
        return Vector3Int.CeilToInt(player.transform.position);
    }

    public void UpdateBlocks(List<Vector3Int> positions, List<Block> newBlocks)
    {
        HashSet<WorldChunk> relevantChunks = new HashSet<WorldChunk>();

        foreach(Vector3Int pos in positions)
        {
            relevantChunks.Add(GetNearestChunk(pos));
        }

        foreach(WorldChunk chunk in relevantChunks)
        {
            if (chunk != null)
            {
                chunk.UpdateBlocks(positions, newBlocks);
            }
        }
    }

}
