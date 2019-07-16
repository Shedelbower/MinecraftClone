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
                    Block.Type type;

                    int waterLevel = _height / 4;

                    if (y == 0)
                    {
                        type = Block.Type.Bedrock;
                    }
                    else if (y < noise-3)
                    {
                        type = Block.Type.Stone;
                    }
                    else if (y < noise)
                    {
                        type = Block.Type.Dirt;
                    }
                    else if (y == noise && y >= waterLevel)
                    {
                        type = Block.Type.Grass;
                    }
                    else if (y == noise && y < waterLevel)
                    {
                        type = Block.Type.Sand;
                    }
                    else if (y <= waterLevel)
                    {
                        type = Block.Type.Water;
                    }
                    else
                    {
                        type = Block.Type.Air;
                    }

                    // Height is 3rd index
                    _blocks[x, y, z] = new Block(type);
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
                    if (_blocks[i,j,k].type == Block.Type.Air)
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
                        if (_blocks[coord.x, coord.y, coord.z].IsTransparent())
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

        Block.Type type = _blocks[i, j, k].type;

        visibility[0] = (j == _height - 1) || (_blocks[i, j + 1, k].IsTransparent() && _blocks[i, j + 1, k].type != type);
        visibility[1] = (j == 0) || (_blocks[i, j - 1, k].IsTransparent() && _blocks[i, j - 1, k].type != type);
        visibility[2] = (i == _width - 1) || (_blocks[i + 1, j, k].IsTransparent() && _blocks[i + 1, j, k].type != type);
        visibility[3] = (i == 0) || (_blocks[i - 1, j, k].IsTransparent() && _blocks[i - 1, j, k].type != type);
        visibility[4] = (k == _width - 1) || (_blocks[i, j, k + 1].IsTransparent() && _blocks[i, j, k + 1].type != type);
        visibility[5] = (k == 0) || (_blocks[i, j, k - 1].IsTransparent() && _blocks[i, j, k - 1].type != type);

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

public class Block
{
    public enum Type
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Bedrock,
        Water,
        Sand
    }

    public static Vector3[] FACE_DIRECTIONS = {
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    public Type type;

    public Block(Type type)
    {
        this.type = type;
    }

    public bool IsTransparent()
    {
        return this.type == Type.Air || this.type == Type.Water;
    }

    public static Mesh GenerateCube()
    {
        Vector3[] directions = {
            Vector3.up,
            Vector3.down,
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back
        };

        CombineInstance[] combine = new CombineInstance[directions.Length];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = GenerateQuad(directions[i]);
            //Debug.Log(combine[i].mesh.vertexCount);
            combine[i].transform = Matrix4x4.identity;
        }
        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, true);

        return mesh;
    }

    public static Mesh GenerateQuad(Vector3 direction)
    {
        List<Vector3> vertices = new List<Vector3>();
        //List<Vector2> uvs = new List<Vector2>();
        int[] triangles = new int[6];

        float min = -0.5f;
        float max = 0.5f;
        vertices.Add(new Vector3(min, max, min));
        vertices.Add(new Vector3(min, max, max));
        vertices.Add(new Vector3(max, max, max));
        vertices.Add(new Vector3(max, max, min));

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, direction);

        Vector3 temp = direction;
        temp.y = 0.0f;
        if (Vector3.Dot(Vector3.forward, temp) > 0.99f)
        {
            Quaternion rot2 = Quaternion.AngleAxis(180f, Vector3.up);
            rot = rot * rot2;
        }
        else if (temp.sqrMagnitude > 0.01f)
        {
            Quaternion rot2 = Quaternion.FromToRotation(Vector3.back,temp);
            rot = rot * rot2;
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = rot * vertices[i];
        }



        //uvs.Add(new Vector2(0f, 0f));
        //uvs.Add(new Vector2(0f, 1f));
        //uvs.Add(new Vector2(1f, 1f));
        //uvs.Add(new Vector2(1f, 0f));

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        //mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();

        return mesh;
    }

    public Mesh GenerateFaces(bool[] faceIsVisible, AtlasReader atlasReader)
    {
        List<List<Vector3>> vertexLists = new List<List<Vector3>>();
        List<List<Vector3>> normalLists = new List<List<Vector3>>();
        List<List<Vector2>> uvLists = new List<List<Vector2>>();
        List<int[]> triangleLists = new List<int[]>();

        for (int i = 0; i < FACE_DIRECTIONS.Length; i++)
        {
            if (faceIsVisible[i] == false)
            {
                continue; // Don't bother making a mesh for a face that can't be seen.
            }

            GenerateBlockFace(FACE_DIRECTIONS[i], out List<Vector3> vertices, out List<Vector3> normals, out int[] triangles);

            List<Vector2> uvs;
            switch (type)
            {
                case Type.Sand:
                    Debug.Log("SAND");
                    uvs = atlasReader.GetUVs(4, 0);
                    break;
                case Type.Bedrock:
                    uvs = atlasReader.GetUVs(3, 0);
                    break;
                case Type.Stone:
                    uvs = atlasReader.GetUVs(2, 0);
                    break;
                case Type.Dirt:
                    uvs = atlasReader.GetUVs(1, 0);
                    break;
                case Type.Grass:
                    if (i == 0)
                    {
                        uvs = atlasReader.GetUVs(0, 0);
                    } else if (i == 1)
                    {
                        uvs = atlasReader.GetUVs(1, 0);
                    } else
                    {
                        uvs = atlasReader.GetUVs(0, 1);
                    }
                    break;
                case Type.Water:
                    uvs = atlasReader.GetUVs(0, 2);
                    break;
                default:
                    uvs = atlasReader.GetUVs(0, 0);
                    break;
            }

            vertexLists.Add(vertices);
            normalLists.Add(normals);
            uvLists.Add(uvs);
            triangleLists.Add(triangles);
        }

        List<Vector3> allVertices = new List<Vector3>();
        List<Vector3> allNormals = new List<Vector3>();
        List<Vector2> allUVs = new List<Vector2>();
        List<int> allTriangles = new List<int>();

        foreach (List<Vector3> vertexList in vertexLists)
        {
            allVertices.AddRange(vertexList);
        }

        foreach (List<Vector3> normalList in normalLists)
        {
            allNormals.AddRange(normalList);
        }

        foreach (List<Vector2> uvList in uvLists)
        {
            allUVs.AddRange(uvList);
        }

        for (int i = 0; i < triangleLists.Count; i++)
        {
            for (int j = 0; j < triangleLists[i].Length; j++)
            {
                triangleLists[i][j] += i * 4;
            }
            allTriangles.AddRange(triangleLists[i]);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(allVertices);
        mesh.SetNormals(allNormals);
        mesh.SetUVs(0, allUVs);
        mesh.SetTriangles(allTriangles.ToArray(),0);

        return mesh;
    }

    public static void GenerateBlockFace(in Vector3 direction, out List<Vector3> vertices, out List<Vector3> normals, out int[] triangles)
    {
        vertices = new List<Vector3>();
        normals = new List<Vector3>() { direction, direction, direction, direction };
        triangles = new int[6]; // 2 Triangles

        // Set vertices
        float min = -0.5f;
        float max = 0.5f;
        vertices.Add(new Vector3(min, max, min));
        vertices.Add(new Vector3(min, max, max));
        vertices.Add(new Vector3(max, max, max));
        vertices.Add(new Vector3(max, max, min));

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, direction);

        Vector3 temp = direction;
        temp.y = 0.0f;
        if (Vector3.Dot(Vector3.forward, temp) > 0.99f)
        {
            Quaternion rot2 = Quaternion.AngleAxis(180f, Vector3.up);
            rot = rot * rot2;
        }
        else if (temp.sqrMagnitude > 0.01f)
        {
            Quaternion rot2 = Quaternion.FromToRotation(Vector3.back, temp);
            rot = rot * rot2;
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = rot * vertices[i];
        }

        // Set triangles
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;
    }
}
