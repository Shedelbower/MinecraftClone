using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    public Texture2D blockAtlas;
    public Material chunkOpaqueMaterial;
    public Material chunkFadeMaterial;



    [Range(1,128)] public int _width = 16;
    [Range(1,64)] public int _height = 32;

    protected Block[,,] _blocks;

    protected int _minX, _minZ;

    protected AtlasReader _atlasReader;

    public void Initialize(int minX, int minZ, int width, int height)
    {
        _minX = minX;
        _minZ = minZ;

        this.transform.position = new Vector3(_minX, 0, _minZ);
        this.gameObject.isStatic = true;

        _width = width;
        _height = height;

        _blocks = new Block[_width, _height, _width];

        _atlasReader = new AtlasReader(blockAtlas, 32);

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _width; z++)
            {
                for (int y = 0; y < _height; y++)
                {
                    BlockType type = GetBlockType(x + _minX, y, z + _minZ, _height);

                    if (type != null)
                    {
                        _blocks[x, y, z] = new Block(type);
                    }
                }
            }
        }

        Debug.Log("Initialized Chunk");
    }

    public static BlockType GetBlockType(Vector3 pos, int height)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        int z = Mathf.RoundToInt(pos.z);
        return GetBlockType(x,y,z, height);
    }

    public static BlockType GetBlockType(int x, int y, int z, int height)
    {
        int noise = Mathf.FloorToInt(Mathf.PerlinNoise(1 * (x) / (float)height, 1 * (z) / (float)height) * height * 0.6f);
        noise = Mathf.FloorToInt(noise + 2);

        //Block.Type type;
        string type;

        int waterLevel = height / 4;

        if (y == 0)
        {
            type = "Bedrock";
        }
        else if (y < noise - 3)
        {
            type = "Stone";
        }
        else if (y < noise)
        {
            type = "Dirt";
        }
        else if (y == noise && y > waterLevel)
        {
            type = "Grass";
        }
        else if (y == noise && y <= waterLevel)
        {
            type = "Sand";
        }
        else if (y <= waterLevel)
        {
            type = "Water";
        }
        else
        {
            type = "";
        }

        BlockType blockType = null;
        foreach (BlockType btype in Block.BLOCK_TYPES)
        {
            if (btype.displayName == type)
            {
                blockType = btype;
                break;
            }
        }

        return blockType;
    }

    public void BuildMesh()
    {
        BuildOpaqueMesh();
        BuildWaterMesh();
    }

    public Mesh BuildOpaqueMesh()
    {
        bool[,,] isExternalBlock = GetExternalBlockMatrix();

        List<Mesh> meshes = new List<Mesh>();
        List<Matrix4x4> translations = new List<Matrix4x4>();

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                for (int k = 0; k < _width; k++)
                {
                    Block block = _blocks[i, j, k];

                    if (isExternalBlock[i,j,k] && block.IsTransparent() == false)
                    {
                        bool[] visibility = GetVisibility(i,j,k);
                        
                        Mesh mesh = block.GenerateFaces(visibility, _atlasReader);
                        meshes.Add(mesh);

                        translations.Add(Matrix4x4.Translate(new Vector3(i, j, k)));
                    }
                }
            }
        }

        Mesh final = new Mesh();
        CombineInstance[] combine = new CombineInstance[meshes.Count];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = translations[i];
        }

        final.CombineMeshes(combine, true);

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

        mf.sharedMesh = final;
        mr.sharedMaterial = chunkOpaqueMaterial;
        mc.sharedMesh = mf.sharedMesh;

        return final;
    }

    public Mesh BuildWaterMesh()
    {
        // TODO: Store this from other build
        bool[,,] isExternalBlock = GetExternalBlockMatrix();

        List<Mesh> meshes = new List<Mesh>();
        List<Matrix4x4> translations = new List<Matrix4x4>();

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                for (int k = 0; k < _width; k++)
                {
                    Block block = _blocks[i, j, k];

                    if (isExternalBlock[i, j, k] && block.type != null && block.type.displayName == "Water")
                    {
                        bool[] visibility = GetVisibility(i, j, k);

                        Mesh mesh = block.GenerateFaces(visibility, _atlasReader);
                        meshes.Add(mesh);

                        translations.Add(Matrix4x4.Translate(new Vector3(i, j, k)));
                    }
                }
            }
        }

        Mesh final = new Mesh();
        CombineInstance[] combine = new CombineInstance[meshes.Count];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = translations[i];
        }

        final.CombineMeshes(combine, true);

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
        }

        //GameObject go = new GameObject("Mesh (Water)");
        //go.isStatic = true;
        //go.transform.parent = this.transform;
        //go.transform.localPosition = new Vector3(0, -0.0625f, 0); // Shift down to create gap between shore and the water's surface.
        MeshFilter mf = go.GetComponent<MeshFilter>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        MeshCollider mc = go.GetComponent<MeshCollider>();

        mf = mf == null ? go.AddComponent<MeshFilter>() : mf;
        mr = mr == null ? go.AddComponent<MeshRenderer>() : mr;
        mc = mc == null ? go.AddComponent<MeshCollider>() : mc;

        mf.sharedMesh = final;
        mr.sharedMaterial = chunkFadeMaterial;
        mc.sharedMesh = mf.sharedMesh;
        mc.convex = true; // TODO: Temporary fix
        mc.isTrigger = true;

        return final;
    }

    public bool[,,] GetExternalBlockMatrix()
    {
        bool[,,] isExternalBlock = new bool[_width, _height, _width];

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                for (int k = 0; k < _width; k++)
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

    protected List<Vector3Int> GetNeighbors(int i, int j, int k)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        if (i != 0)
        {
            neighbors.Add(new Vector3Int(i - 1, j, k));
        }
        if (i != _width-1)
        {
            neighbors.Add(new Vector3Int(i + 1, j, k));
        }

        if (j != 0)
        {
            neighbors.Add(new Vector3Int(i, j - 1, k));
        }
        if (j != _height-1)
        {
            neighbors.Add(new Vector3Int(i, j + 1, k));
        }

        if (k != 0)
        {
            neighbors.Add(new Vector3Int(i, j, k - 1));
        }
        if (k != _width - 1)
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
            if (LocalPositionInRange(npos))
            {
                Block block = _blocks[npos.x, npos.y, npos.z];
                ntype = block == null ? null : block.type;
            } else
            {
                ntype = GetBlockType(npos.x + _minX, npos.y, npos.z + _minZ, _height);
            }
            visibility[ni] = (ntype == null || ntype.isTransparent) && (type != ntype);
        }

        return visibility;
    }

    private Vector3Int WorldToLocalPosition(Vector3Int worldPos)
    {
        int x = worldPos.x - _minX;
        int y = worldPos.y;
        int z = worldPos.z - _minZ;

        Vector3Int localPos = new Vector3Int(x,y,z);
        return localPos;
    }

    private bool LocalPositionInRange(Vector3Int localPos)
    {
        return localPos.x >= 0 && localPos.z >= 0 && localPos.x < _width && localPos.z < _width && localPos.y >= 0 && localPos.y < _height;
    }

    public void UpdateBlocks(List<Vector3Int> positions, List<Block> newBlocks)
    {
        bool shouldRebuild = false;

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3Int worldPos = positions[i];
            Vector3Int localPos = WorldToLocalPosition(worldPos);
            if (LocalPositionInRange(localPos))
            {
                Block newBlock = newBlocks[i].type == null ? null : newBlocks[i];
                Block currBlock = _blocks[localPos.x, localPos.y, localPos.z];

                BlockType currType = currBlock == null ? null : currBlock.type;
                BlockType newType = newBlock == null ? null : newBlock.type;
                if (currType != newType)
                {
                    _blocks[localPos.x, localPos.y, localPos.z] = newBlock;
                    shouldRebuild = true;
                }
            }
            
        }

        if (shouldRebuild)
        {
            Debug.Log("Rebuilding...");
            //Transform child = transform.Find("Mesh (Opaque)");
            //child.name = "TO DESTROY";
            //Destroy(child.gameObject);
            BuildMesh(); //TODO: Add chunk to build queue instead
        }
    }

    public void Highlight(Color color)
    {
        Vector3 chunkCenter = new Vector3(_minX + _width/2, _height/2, _minZ + _width/2);
        Gizmos.color = color;
        Gizmos.DrawCube(chunkCenter, new Vector3(_width, _height, _width));
        Debug.DrawLine(chunkCenter + Vector3.down * 20, chunkCenter + Vector3.up * 20, color);
    }

}
