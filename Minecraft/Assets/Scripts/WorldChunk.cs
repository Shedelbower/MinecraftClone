using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    public Vector3Int ID
    {
        get { return _minCorner; }
    }

    public bool IsInitialized
    {
        get;
    }

    public bool IsLoaded
    {
        get;
        private set;
    }

    public bool IsModified
    {
        get;
        private set;
    }


    public bool highlight = true;
    public ChunkManager chunkManager;

    public Texture2D blockAtlas;
    public Material chunkOpaqueMaterial;
    public Material chunkWaterMaterial;
    public Material chunkFoliageMaterial;

    public Vector3Int _size = Vector3Int.one * 16;

    protected Vector3Int _minCorner;
    protected Block[,,] _blocks;
    protected AtlasReader _atlasReader;
    protected int _seed;

    public void Initialize(Vector3Int minCorner, Vector3Int size, int seed)
    {
        _size = size;
        _minCorner = minCorner;
        _seed = seed;

        //UnityEngine.Random.InitState(_seed);
        //_seedOffset = new Vector2Int(Random.Range(int.MinValue, int.MaxValue), Random.Range(int.MinValue, int.MaxValue));

        this.transform.position = minCorner;
        this.gameObject.isStatic = true;

        _blocks = new Block[size.x, size.y, size.z];

        _atlasReader = new AtlasReader(blockAtlas, 8);

        for (int x = 0; x < _size.x; x++)
        {
            for (int z = 0; z < _size.z; z++)
            {
                for (int y = 0; y < _size.y; y++)
                {
                    BlockType type = GetBlockType(x + _minCorner.x, y + _minCorner.y, z + _minCorner.z, _seed);

                    if (type != null)
                    {
                        _blocks[x, y, z] = new Block(type);
                    }
                }
            }
        }

    }

    public static BlockType GetBlockType(Vector3 pos, int seed)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        int z = Mathf.RoundToInt(pos.z);
        return GetBlockType(x,y,z,seed);
    }

    public BlockType ReadBlockType(Vector3Int worldPos)
    {
        Vector3Int localPos = WorldToLocalPosition(worldPos);
        Block block = _blocks[localPos.x, localPos.y, localPos.z];
        BlockType type = block == null ? BlockType.GetBlockType("Air") : block.type;
        return type;
    }

    public static float RidgeNoise(float x, float y) {
        return Mathf.Abs(Mathf.PerlinNoise(x,y)-0.5f) * 2.0f;
    }

    public static BlockType GetBlockType(int x, int y, int z, int seed)
    {

        Random.State prevState = Random.state;
        UnityEngine.Random.InitState(seed);
        Vector2Int offset = new Vector2Int(Random.Range(-100, 100), Random.Range(-100, 100));
        Random.state = prevState;

        float ox = 1 * (x) / (float)32 + offset.x;
        float oz = 1 * (z) / (float)32 + offset.y;

        float p0 = Mathf.PerlinNoise(ox, oz);
        p0 = Mathf.Pow(p0, 1.5f);
        if (float.IsNaN(p0) || p0 < 0)
        {
            p0 = 0f;
        }

        int baseNoise = Mathf.FloorToInt(p0 * 20);
        baseNoise += 63;

        BlockType type = null;

        int waterLevel = 65;
        int ironDepth = 5;

        float ridgeMask = Mathf.PerlinNoise((x + offset.x) / 60f, (z + offset.y)/60f);

        int noise = baseNoise;

        bool isLake = baseNoise <= waterLevel;

        bool isRavine = false;
        bool isInnerRavine = false;
        if (ridgeMask < 0.3f) {
            float ridgeNoise = RidgeNoise(x / 20f, z/20f);
            isInnerRavine = ridgeNoise < 0.1f && baseNoise > waterLevel + 1;
            isRavine = ridgeNoise < 0.15f && baseNoise > waterLevel + 1;

            if (isInnerRavine) {
                noise -= 16;
            } else if (isRavine) {
                noise -= Mathf.RoundToInt(16 * (1-Mathf.InverseLerp(0.1f,0.15f,ridgeNoise)));
            }
        }

        if (y <= 0)
        {
            type = BlockType.GetBlockType("Bedrock");
        }
        else if (y >= noise - 8 && y <= noise && isLake && isRavine == false)
        {
            type = (y >= noise - 3) ? BlockType.GetBlockType("Sand") : BlockType.GetBlockType("Sandstone");
        }
        else if (y < baseNoise - 3 || (isRavine && y < baseNoise && y < noise))
        {
            if (y < noise)
            {
                type = null;

                if (y < baseNoise - 35 && Random.value < 0.001f)
                {
                    type = BlockType.GetBlockType("Diamond Ore");
                }

                if (type == null && y <= baseNoise - 6)
                {
                    float p1 = Mathf.PerlinNoise((x + offset.x) / 6f + 0.5f, (z + offset.y) / 6f + 0.5f);
                    float p2 = Mathf.PerlinNoise(y / 6f, 0);

                    float p3 = p1 + p2;
                    if (p3 > 1.3f)
                    {
                        type = BlockType.GetBlockType("Gravel");
                    }
                }

                if (type == null && y <= baseNoise - ironDepth)
                {
                    float p1 = Mathf.PerlinNoise((x + offset.x) / 4f + 100, (z + offset.y) / 4f + 100);
                    float p2 = Mathf.PerlinNoise(y / 4f, 0);

                    float p3 = p1 + p2;
                    if (p3 > 1.4f)
                    {
                        type = BlockType.GetBlockType("Iron Ore");
                    }

                }

                type = type ?? BlockType.GetBlockType("Stone");
            }


        }
        else if (isLake && y <= waterLevel && isRavine == false)
        {
            type = BlockType.GetBlockType("Water");
        }
        else if (y < noise)
        {
            type = BlockType.GetBlockType("Dirt");
        }
        else if (y == noise && y > waterLevel)
        {
            type = BlockType.GetBlockType("Grass");
        }
        else if (y == noise + 1 && y > waterLevel + 1 && isRavine == false)
        {
            float plantProbability = 1f - Mathf.InverseLerp(waterLevel, waterLevel + 30, y);
            plantProbability = Mathf.Pow(plantProbability,7f);
            //plantProbability /= 2;

            if (Random.value < plantProbability)
            {

                float p = Random.value;

                type = BlockType.GetBlockType("Tall Grass");

                if (p < 0.25f)
                {
                    var plantTypes = BlockType.GetPlantBlockTypes();
                    type = plantTypes[Random.Range(0, plantTypes.Count)];
                }


            }
        } else
        {
            // Air block
        }

        return type;
    }

    // public static float PerlinNoise3D(float x, float y, float z) {
    //     return (Mathf.PerlinNoise(x,z) + Mathf.PerlinNoise(0f,y)) - 1.0f;
    // }

    // public static float InverseLerpUnclamped(float a, float b, float value) {
    //     return (value-a)/(b-a);
    // }

    // public static BlockType GetBlockType(int x, int y, int z, int seed)
    // {

    //     Random.State prevState = Random.state;
    //     UnityEngine.Random.InitState(seed);
    //     Vector3Int offset = new Vector3Int(Random.Range(-100, 100), Random.Range(-100, 100),Random.Range(-100, 100));
    //     Random.state = prevState;

    //     // Constants
    //     int bedrockLevel = 0;
    //     int seaLevel = 60;
    //     int hillLevel = 70;

    //     float horizontalScale = 1/32f;
    //     float verticalScale = 1/16f;

    //     float noise = PerlinNoise3D((x+offset.x)*horizontalScale,(y+offset.y)*verticalScale,(z+offset.z)*horizontalScale);
    //     noise *= 5;

    //     float t = InverseLerpUnclamped((float)seaLevel,(float)hillLevel,(float)y);
    //     float v = Mathf.Lerp(-1f,1f,t) * 2f;
    //     // float t = Mathf.InverseLerp((float)seaLevel, (float)hillLevel,(float)y);

    //     noise -= v;

    //     noise *= 0.25f;
    //     if (noise > 0.98) {
    //         return BlockType.GetBlockType("Bedrock");
    //     } else if (noise > 0.2)
    //     {
    //         return BlockType.GetBlockType("Stone");
    //     } else if (noise > 0.1)
    //     {
    //         return BlockType.GetBlockType("Dirt");
    //     } else if (noise > 0)
    //     {
    //         return BlockType.GetBlockType("Grass");
    //     } else {
    //         return null;
    //     }


    // }

    public void BuildMesh()
    {
        bool[,,] isExternalBlock = GetExternalBlockMatrix();

        BuildOpaqueMesh(isExternalBlock);
        BuildWaterMesh(isExternalBlock);
        BuildFoliageMesh(isExternalBlock);

        this.IsLoaded = true;
    }

    public Mesh BuildOpaqueMesh(bool[,,] isExternalBlock)
    {

        List<MeshData> meshes = new List<MeshData>();

        for (int i = 0; i < _size.x; i++)
        {
            for (int j = 0; j < _size.y; j++)
            {
                for (int k = 0; k < _size.z; k++)
                {
                    Block block = _blocks[i, j, k];

                    if (isExternalBlock[i,j,k] && block.IsTransparent() == false)
                    {
                        bool[] visibility = GetVisibility(i,j,k);
                        
                        MeshData mesh = block.GenerateMesh(visibility, _atlasReader);
                        mesh.TransformVertices(Matrix4x4.Translate(new Vector3(i, j, k)));
                        meshes.Add(mesh);

                    }
                }
            }
        }

        if (IsModified == false && meshes.Count == 0)
        {
            // Block is entirely invisible, so don't make the mesh game object.
            return null;
        }

        string childName = "Mesh (Opaque)";
        GameObject go = transform.Find(childName)?.gameObject;
        if (go == null)
        {
            go = new GameObject(childName)
            {
                isStatic = true
            };
            go.transform.parent = this.transform;
            go.transform.localPosition = new Vector3(0, 0, 0);
        }

        MeshFilter mf = go.GetComponent<MeshFilter>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        MeshCollider mc = go.GetComponent<MeshCollider>();

        mf = mf == null ? go.AddComponent<MeshFilter>() : mf;
        mr = mr == null ? go.AddComponent<MeshRenderer>() : mr;
        mc = mc == null ? go.AddComponent<MeshCollider>() : mc;

        mf.sharedMesh = null;
        Mesh final = MeshData.Combine(meshes);
        

        mf.sharedMesh = final;
        mr.sharedMaterial = chunkOpaqueMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        mc.sharedMesh = mf.sharedMesh;

        return final;
    }

    public Mesh BuildWaterMesh(bool[,,] isExternalBlock)
    {
        // TODO: Store this from other build
        //bool[,,] isExternalBlock = GetExternalBlockMatrix();

        List<MeshData> meshes = new List<MeshData>();

        for (int i = 0; i < _size.x; i++)
        {
            for (int j = 0; j < _size.y; j++)
            {
                for (int k = 0; k < _size.z; k++)
                {
                    Block block = _blocks[i, j, k];

                    if (isExternalBlock[i, j, k] && Block.IsAirBlock(block) == false && block.type.name == "Water")
                    {
                        bool[] visibility = GetVisibility(i, j, k);

                        MeshData mesh = block.GenerateMesh(visibility, _atlasReader);
                        mesh.TransformVertices(Matrix4x4.Translate(new Vector3(i, j, k)));
                        meshes.Add(mesh);
                    }
                }
            }
        }

        if (IsModified == false && meshes.Count == 0)
        {
            // No visible water blocks in chunk, so don't make the mesh game object.
            return null;
        }

        

        string childName = "Mesh (Water)";
        GameObject go = transform.Find(childName)?.gameObject;
        if (go == null)
        {
            go = new GameObject(childName)
            {
                isStatic = true
            };
            go.transform.parent = this.transform;
            go.transform.localPosition = new Vector3(0, -0.0625f, 0);// Shift down to create gap between shore and the water's surface.
            go.tag = "Water";
            go.layer = LayerMask.NameToLayer("Water");
        }

        //GameObject go = new GameObject("Mesh (Water)");
        //go.isStatic = true;
        //go.transform.parent = this.transform;
        //go.transform.localPosition = new Vector3(0, -0.0625f, 0); // Shift down to create gap between shore and the water's surface.
        MeshFilter mf = go.GetComponent<MeshFilter>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        //MeshCollider mc = go.GetComponent<MeshCollider>();

        mf = mf == null ? go.AddComponent<MeshFilter>() : mf;
        mr = mr == null ? go.AddComponent<MeshRenderer>() : mr;
        //mc = mc == null ? go.AddComponent<MeshCollider>() : mc;

        mf.sharedMesh = null;
        Mesh final = MeshData.Combine(meshes);

        mf.sharedMesh = final;
        mr.sharedMaterial = chunkWaterMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        //mc.sharedMesh = mf.sharedMesh;
        //mc.convex = true; // TODO: Temporary fix
        //mc.isTrigger = true;

        return final;
    }

    public Mesh BuildFoliageMesh(bool[,,] isExternalBlock)
    {
        // TODO: Store this from other build
        //bool[,,] isExternalBlock = GetExternalBlockMatrix();

        List<MeshData> meshes = new List<MeshData>();

        for (int i = 0; i < _size.x; i++)
        {
            for (int j = 0; j < _size.y; j++)
            {
                for (int k = 0; k < _size.z; k++)
                {
                    Block block = _blocks[i, j, k];

                    if (isExternalBlock[i, j, k] && block.type != null && block.type.isBillboard)
                    {
                        bool[] visibility = GetVisibility(i, j, k);

                        MeshData mesh = block.GenerateMesh(visibility, _atlasReader);
                        mesh.TransformVertices(Matrix4x4.Translate(new Vector3(i, j - 0.02f, k)));
                        meshes.Add(mesh);

                    }
                }
            }
        }

        if (IsModified == false && meshes.Count == 0)
        {
            // No visible foliage blocks in chunk, so don't make the mesh game object.
            return null;
        }

        string childName = "Mesh (Foliage)";
        GameObject go = transform.Find(childName)?.gameObject;
        if (go == null)
        {
            go = new GameObject(childName)
            {
                isStatic = true
            };
            go.transform.parent = this.transform;
            go.transform.localPosition = new Vector3(0, 0, 0);
            go.tag = "Foliage";
            go.layer = LayerMask.NameToLayer("Foliage");
        }

        MeshFilter mf = go.GetComponent<MeshFilter>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        MeshCollider mc = go.GetComponent<MeshCollider>();

        mf = mf == null ? go.AddComponent<MeshFilter>() : mf;
        mr = mr == null ? go.AddComponent<MeshRenderer>() : mr;
        mc = mc == null ? go.AddComponent<MeshCollider>() : mc;

        mf.sharedMesh = null;
        Mesh final = MeshData.Combine(meshes);


        mf.sharedMesh = final;
        mr.sharedMaterial = chunkFoliageMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        mc.sharedMesh = mf.sharedMesh;

        return final;
    }

    public bool[,,] GetExternalBlockMatrix()
    {
        bool[,,] isExternalBlock = new bool[_size.x, _size.y, _size.z];

        for (int i = 0; i < _size.x; i++)
        {
            for (int j = 0; j < _size.y; j++)
            {
                for (int k = 0; k < _size.z; k++)
                {
                    if (_blocks[i,j,k] == null)
                    {
                        // Don't render air blocks
                        continue;
                    }

                    List<Vector3Int> neighbors = GetNeighbors(i, j, k);

                    if (neighbors.Count < 6)
                    {
                        isExternalBlock[i, j, k] = true;
                        continue;
                    }

                    bool hasTransparentNeighbor = false;
                    foreach (Vector3Int coord in neighbors)
                    {
                        Block block = _blocks[coord.x, coord.y, coord.z];
                        if (block == null || block.IsTransparent())
                        {
                            hasTransparentNeighbor = true;
                            break;
                        }
                    }

                    isExternalBlock[i, j, k] = hasTransparentNeighbor;
                }
            }
        }

        return isExternalBlock;
    }

    public static List<Vector3Int> GetNeighbors(Vector3Int worldPos)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        neighbors.Add(worldPos + Vector3Int.up);
        neighbors.Add(worldPos + Vector3Int.down);
        neighbors.Add(worldPos + Vector3Int.right);
        neighbors.Add(worldPos + Vector3Int.left);
        neighbors.Add(worldPos + new Vector3Int(0,0,1));
        neighbors.Add(worldPos + new Vector3Int(0,0,-1));
        return neighbors;
    }


    protected List<Vector3Int> GetNeighbors(int i, int j, int k)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        if (i != 0)
        {
            neighbors.Add(new Vector3Int(i - 1, j, k));
        }
        if (i != _size.x - 1)
        {
            neighbors.Add(new Vector3Int(i + 1, j, k));
        }

        if (j != 0)
        {
            neighbors.Add(new Vector3Int(i, j - 1, k));
        }
        if (j != _size.y - 1)
        {
            neighbors.Add(new Vector3Int(i, j + 1, k));
        }

        if (k != 0)
        {
            neighbors.Add(new Vector3Int(i, j, k - 1));
        }
        if (k != _size.z - 1)
        {
            neighbors.Add(new Vector3Int(i, j, k + 1));
        }

        return neighbors;
    }

    protected bool[] GetVisibility(int i, int j, int k)
    {
        bool[] visibility = new bool[6];

        BlockType type = _blocks[i, j, k].type;

        // Up, Down, Front, Back, Left, Right
        Vector3Int[] neighbors = {
            new Vector3Int(i,j+1,k),
            new Vector3Int(i,j-1,k),
            new Vector3Int(i+1,j,k),
            new Vector3Int(i-1,j,k),
            new Vector3Int(i,j,k+1),
            new Vector3Int(i,j,k-1)
        };

        for (int ni = 0; ni < neighbors.Length; ni++)
        {
            Vector3Int npos = neighbors[ni];
            BlockType ntype;
            if (LocalPositionIsInRange(npos))
            {
                Block block = _blocks[npos.x, npos.y, npos.z];
                ntype = block == null ? null : block.type;
            } else
            {
                Vector3Int worldPos = LocalToWorldPosition(npos);
                WorldChunk chunkNeighbor = chunkManager.GetNearestChunk(worldPos);
                if (chunkNeighbor != null)
                {
                    ntype = chunkNeighbor.ReadBlockType(worldPos);
                }
                else
                {
                    ntype = GetBlockType(worldPos, _seed);
                }

            }
            visibility[ni] = (ntype == null || ntype.isTransparent) && (type != ntype);
        }

        return visibility;
    }

    private Vector3Int WorldToLocalPosition(Vector3Int worldPos)
    {
        return worldPos - _minCorner;
    }

    private Vector3Int LocalToWorldPosition(Vector3Int localPos)
    {
        return localPos + _minCorner;
    }

    private bool LocalPositionIsInRange(Vector3Int localPos)
    {
        return localPos.x >= 0 && localPos.z >= 0 && localPos.x < _size.x && localPos.z < _size.z && localPos.y >= 0 && localPos.y < _size.y;
    }

    public HashSet<Vector3Int> ModifyBlocks(List<Vector3Int> positions, List<Block> newBlocks, out HashSet<WorldChunk> touchedChunks)
    {
        touchedChunks = new HashSet<WorldChunk>(); // Adjacent chunks that are adjacent to one of the modified blocks.
        HashSet<Vector3Int> blocksToUpdate = new HashSet<Vector3Int>();
        bool shouldRebuild = false;

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3Int worldPos = positions[i];
            Vector3Int localPos = WorldToLocalPosition(worldPos);
            if (LocalPositionIsInRange(localPos))
            {
                Block newBlock = (newBlocks[i] == null || newBlocks[i].type == null) ? null : newBlocks[i];
                Block currBlock = _blocks[localPos.x, localPos.y, localPos.z];

                BlockType currType = currBlock == null ? null : currBlock.type;
                BlockType newType = newBlock == null ? null : newBlock.type;
                bool bothAir = currType == null && newType == currType;
                if (!bothAir && (currType != newType || (newType.isFluid && currType.isFluid)))
                {
                    _blocks[localPos.x, localPos.y, localPos.z] = newBlock;
                    shouldRebuild = true;

                    var neighbors = GetNeighbors(worldPos);
                    foreach(var neighbor in neighbors)
                    {
                        if (LocalPositionIsInRange(WorldToLocalPosition(neighbor)) == false)
                        {
                            // Neighbor is in an adjacent chunk
                            WorldChunk neighborChunk = chunkManager.GetNearestChunk(neighbor);
                            touchedChunks.Add(neighborChunk);
                        }
                    }

                    neighbors.Add(worldPos);
                    blocksToUpdate.UnionWith(neighbors);
                }
            }
        }

        if (shouldRebuild)
        {
            //Debug.Log("Rebuilding...");
            this.IsModified = true;
            //Transform child = transform.Find("Mesh (Opaque)");
            //child.name = "TO DESTROY";
            //Destroy(child.gameObject);
            //BuildMesh(); //TODO: Add chunk to build queue instead
        }

        return blocksToUpdate;
    }

    public Vector3 Center()
    {
        return new Vector3(_size.x / 2f + _minCorner.x, _size.y / 2f + _minCorner.y, _size.z / 2f + _minCorner.z);
    }

    public Block GetBlockAtPosition(Vector3Int worldPos)
    {
        Vector3Int localPos = WorldToLocalPosition(worldPos);
        return _blocks[localPos.x, localPos.y, localPos.z];
    }

    public void Highlight(Color color)
    {
        Vector3 chunkCenter = Center();


        Gizmos.color = color;
        Gizmos.DrawWireCube(chunkCenter, _size);
        //Debug.DrawLine(chunkCenter + Vector3.down * 20, chunkCenter + Vector3.up * 20, color);
    }

    private void OnDrawGizmos()
    {
        if (highlight)
        {
            Highlight(Color.red);
        }
    }

    //public bool UpdateChunk()
    //{
    //    bool anyBlockModified = false;
    //    for (int i = 0; i < _size.x; i++)
    //    {
    //        for (int j = 0; j < _size.y; j++)
    //        {
    //            for (int k = 0; k < _size.z; k++)
    //            {
    //                Vector3Int localPos = new Vector3Int(i,j,k);
    //                Vector3Int worldPos = LocalToWorldPosition(localPos);
    //                anyBlockModified |= UpdateBlock(worldPos);
    //            }
    //        }
    //    }
    //    return anyBlockModified;
    //}

    public HashSet<Vector3Int> UpdateBlock(Vector3Int worldPos, out HashSet<WorldChunk> modifedChunks)
    {
        modifedChunks = new HashSet<WorldChunk>();
        HashSet<Vector3Int> nextBlocksToUpdate = new HashSet<Vector3Int>();

        Vector3Int localPos = WorldToLocalPosition(worldPos);
        Block blockToUpdate = _blocks[localPos.x, localPos.y, localPos.z];

        if (blockToUpdate == null)
        {
            //return blocksToUpdate;
            // Ignore
        }
        else if (blockToUpdate.type.isFluid)
        {
            Vector3Int bottomPos = worldPos + Vector3Int.down;
            Block bottomBlock = chunkManager.GetBlockAtPosition(bottomPos);
            if (Block.IsAirBlock(bottomBlock))
            {
                nextBlocksToUpdate.UnionWith(chunkManager.ModifyBlock(bottomPos, blockToUpdate, out HashSet<WorldChunk> modified));
                modifedChunks.UnionWith(modified);

                //nextBlocksToUpdate.UnionWith(chunkManager.ModifyBlock(worldPos, null, out modified));
                //modifedChunks.UnionWith(modified);
            }
            else if (bottomBlock.type.isFluid == false)
            {
                Vector3Int[] adjacentPositions =
                {
                    worldPos + Vector3Int.right,
                    worldPos + Vector3Int.left,
                    worldPos + new Vector3Int(0,0,1),
                    worldPos + new Vector3Int(0,0,-1),
                };

                List<Vector3Int> positionsToModify = new List<Vector3Int>();
                List<Block> newBlocks = new List<Block>();
                foreach (Vector3Int adjPos in adjacentPositions)
                {
                    Block target = chunkManager.GetBlockAtPosition(adjPos);
                    if (target == null || (target.type.isTransparent && target.type.name != "Water"))
                    {
                        positionsToModify.Add(adjPos);
                        newBlocks.Add(blockToUpdate);
                        nextBlocksToUpdate.Add(adjPos);
                    }
                }

                chunkManager.ModifyBlocks(positionsToModify, newBlocks, out HashSet<WorldChunk> modified);
                modifedChunks.UnionWith(modified);
            }
        } else if (blockToUpdate.type.affectedByGravity)
        {
            Vector3Int bottomPos = worldPos;
            bottomPos.y--;
            Block bottomBlock = chunkManager.GetBlockAtPosition(bottomPos);
            if (bottomBlock == null || bottomBlock.type.isTransparent)
            {
                nextBlocksToUpdate.UnionWith(chunkManager.ModifyBlock(worldPos, null, out HashSet<WorldChunk> modified));
                modifedChunks.UnionWith(modified);
                chunkManager.entityManager.CreateBlockEntity(worldPos,blockToUpdate.type);

                //nextBlocksToUpdate.Add(worldPos + Vector3Int.up);
                //nextBlocksToUpdate.Add(worldPos + Vector3Int.right);
                //nextBlocksToUpdate.Add(worldPos + Vector3Int.left);
                //nextBlocksToUpdate.Add(worldPos + new Vector3Int(0,0,1));
                //nextBlocksToUpdate.Add(worldPos + new Vector3Int(0, 0, -1));
            }
        }
        else if (blockToUpdate.type.mustBeOnGrassBlock)
        {
            Vector3Int bottomPos = worldPos;
            bottomPos.y--;
            Block bottomBlock = chunkManager.GetBlockAtPosition(bottomPos);
            if (bottomBlock == null || bottomBlock.type.name != "Grass")
            {
                List<Vector3Int> positions = new List<Vector3Int>()
                {
                    bottomPos,
                    worldPos
                };
                List<Block> blocks = new List<Block>()
                {
                    null,
                    null
                };
                chunkManager.ModifyBlocks(positions, blocks, out HashSet<WorldChunk> modified);
                modifedChunks.UnionWith(modified);
            }
        }

        return nextBlocksToUpdate;
    }

}
