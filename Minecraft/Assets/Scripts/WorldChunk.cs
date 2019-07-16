using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    public Texture2D blockAtlas;
    public Material chunkMaterial;



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
                int noise = Mathf.FloorToInt(Mathf.PerlinNoise(1*(x+minX) /(float)_height, 1*(z+minZ) / (float)_height) * _height*0.6f);

                //noise += Mathf.FloorToInt(Mathf.PerlinNoise(3.2f + 1f * (x + minX) / (float)_width, -4.85f + 1f * (z + minZ) / (float)_width) * _height * 0.5f);
                noise = Mathf.FloorToInt(noise + 2);

                for (int y = 0; y < _height; y++)
                {
                    //Block.Type type;
                    string type;

                    int waterLevel = _height / 4;

                    if (y == 0)
                    {
                        //type = "Bedrock";
                        type = "Grass";

                    }
                    else if (y < noise-3)
                    {
                        type = "Stone";
                    }
                    else if (y < noise)
                    {
                        type = "Dirt";
                    }
                    else if (y == noise && y >= waterLevel)
                    {
                        type = "Grass";
                    }
                    else if (y == noise && y < waterLevel)
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

                    // Height is 3rd index
                    if (type != "")
                    {
                        _blocks[x, y, z] = new Block(type);
                    }
                }
            }
        }

        Debug.Log("Initialized Chunk");
    }

    public Mesh BuildMesh()
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
                    if (isExternalBlock[i,j,k])
                    {
                        Block block = _blocks[i, j, k];

                        //GameObject go = new GameObject("Block (" + i + "," + j + "," + k + ")");
                        //go.transform.parent = this.transform;
                        //go.transform.localPosition = new Vector3(i, j, k);
                        //MeshFilter mf = go.AddComponent<MeshFilter>();
                        //MeshRenderer mr = go.AddComponent<MeshRenderer>();

                        bool[] visibility = GetVisibility(i,j,k);
                        //{
                        //    true,true,true,true,true,true
                        //};

                        //mf.sharedMesh = block.GenerateFaces(visibility, _atlasReader);

                        //mr.sharedMaterial = chunkMaterial;

                       
                        Mesh mesh = block.GenerateFaces(visibility, _atlasReader);
                        meshes.Add(mesh);

                        translations.Add(Matrix4x4.Translate(new Vector3(i, j, k)));
                    }
                }
            }
        }

        Debug.Log("Done Building");

        Mesh final = new Mesh();
        CombineInstance[] combine = new CombineInstance[meshes.Count];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = translations[i];
        }

        final.CombineMeshes(combine, true);

        GameObject go = new GameObject("Mesh");
        go.isStatic = true;
        go.transform.parent = this.transform;
        go.transform.localPosition = new Vector3(0,0,0);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        MeshCollider mc = go.AddComponent<MeshCollider>();

        mf.sharedMesh = final;
        mr.sharedMaterial = chunkMaterial;
        mc.sharedMesh = mf.sharedMesh;

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
                        //Debug.Log(coord.ToString());
                        Block block = _blocks[coord.x, coord.y, coord.z];
                        if (block == null || block.IsTransparent())
                        {
                            hasTransparentNeighbor = true;
                            break;
                        }
                        //hasTransparentNeighbor |= _blocks[coord.x, coord.y, coord.z].IsTransparent();
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
        visibility[0] = (j == _height - 1) || _blocks[i, j + 1, k] == null || (_blocks[i, j + 1, k].IsTransparent() && _blocks[i, j + 1, k].type != type);
        visibility[1] = (j == 0) || _blocks[i, j - 1, k] == null || (_blocks[i, j - 1, k].IsTransparent() && _blocks[i, j - 1, k].type != type);
        visibility[2] = (i == _width - 1) || _blocks[i + 1, j, k] == null || (_blocks[i + 1, j, k].IsTransparent() && _blocks[i + 1, j, k].type != type);
        visibility[3] = (i == 0) || _blocks[i - 1, j, k] == null || (_blocks[i - 1, j, k].IsTransparent() && _blocks[i - 1, j, k].type != type);
        visibility[4] = (k == _width - 1) || _blocks[i, j, k + 1] == null || (_blocks[i, j, k + 1].IsTransparent() && _blocks[i, j, k + 1].type != type);
        visibility[5] = (k == 0) || _blocks[i, j, k - 1] == null || (_blocks[i, j, k - 1].IsTransparent() && _blocks[i, j, k - 1].type != type);

        return visibility;
    }

    //public void Start()
    //{
    //    int x = Mathf.FloorToInt(this.transform.position.x);
    //    int z = Mathf.FloorToInt(this.transform.position.z);
    //    Initialize(x, z);

    //    this.BuildMesh();
    //}

    public void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    int x = Mathf.FloorToInt(this.transform.position.x);
        //    int z = Mathf.FloorToInt(this.transform.position.z);
        //    Initialize(x,z);
        //}

        //if(Input.GetKeyDown(KeyCode.Return))
        //{
        //    this.BuildMesh();
        //}
    }

}
