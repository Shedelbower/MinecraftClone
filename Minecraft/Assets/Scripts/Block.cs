using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Block
{
    /*------------------------ STATIC VARIABLES ------------------------*/

    public static readonly Vector3[] FACE_DIRECTIONS = {
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };


    /*------------------------ MEMBER VARIABLES ------------------------*/

    public BlockType type;

    /*------------------------ CONSTRUCTORS ------------------------*/

    public Block(int index) : this(BlockType.GetBlockType(index)) { }

    public Block(string name) : this(BlockType.GetBlockType(name)) {}

    public Block(BlockType type)
    {
        this.type = type;
    }

    /*------------------------ PUBLIC METHODS ------------------------*/

    public bool IsTransparent()
    {
        return this.type == null || this.type.isTransparent;
    }

    public MeshData GenerateMesh(bool[] faceIsVisible, AtlasReader atlasReader)
    {
        if (this.type.isBillboard)
        {
            return GenerateBillboardFaces(atlasReader);
        } else
        {
            return GenerateCubeFaces(faceIsVisible, atlasReader);
        }
    }

    public MeshData GenerateBillboardFaces(AtlasReader atlasReader)
    {
        Vector3[] baseVertices =
        {
            new Vector3(1.0f, -1.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f, -1.0f, 0.0f)
        };

        Color[] baseColors = {
            Color.black,
            Color.red,
            Color.red,
            Color.black,
        };

        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();

        Quaternion[] rotations =
        {
            Quaternion.AngleAxis(45f, Vector3.up),
            Quaternion.AngleAxis(-45f, Vector3.up),
            Quaternion.AngleAxis(135f, Vector3.up),
            Quaternion.AngleAxis(-135f, Vector3.up)
        };


        for (int i = 0; i < rotations.Length; i++)
        {
            Quaternion rotation = rotations[i];
            foreach(Vector3 vertex in baseVertices)
            {
                vertices.Add(rotation * vertex * 0.5f);
            }

            colors.AddRange(baseColors);
        }

        List<Vector3> normals = new List<Vector3>();
        Vector3 normal = new Vector3(0f,0f,1f);
        for (int i = 0; i < vertices.Count; i++)
        {
            normals.Add(normal);
        }

        Vector2Int atlasIndex = type.atlasPositions[0];
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < rotations.Length; i++)
        {
            uvs.AddRange(atlasReader.GetUVs(atlasIndex.x, atlasIndex.y));
        }

        int[] triangles = {
            0, 1, 2, 0, 2, 3,
            0+4, 1+4, 2+4, 0+4, 2+4, 3+4,
            0+8, 1+8, 2+8, 0+8, 2+8, 3+8,
            0+12, 1+12, 2+12, 0+12, 2+12, 3+12
        };

        MeshData data = new MeshData(vertices,normals,uvs,triangles,colors.ToArray());

        return data;
    }

    public MeshData GenerateCubeFaces(bool[] faceIsVisible, AtlasReader atlasReader)
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

            Vector2Int[] atlasPositions = type.atlasPositions;
            int index = atlasPositions.Length == 1 ? 0 : i;

            List<Vector2> uvs = atlasReader.GetUVs(atlasPositions[index].x, atlasPositions[index].y);


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

        List<Color> allColors = new List<Color>();
        if (this.type.isFluid)
        {
            foreach (var vertex in allVertices)
            {
                //Color color = vertex.y > 0.0f ? Color.blue : Color.black;
                allColors.Add(Color.blue);
            }
        }

        MeshData data = new MeshData(allVertices, allNormals, allUVs, allTriangles.ToArray(), allColors.ToArray());

        return data;
    }
    

    /*------------------------ STATIC METHODS ------------------------*/

    public static bool IsAirBlock(Block block)
    {
        return block == null || block.type == null || block.type.name == "Air";
    }

    public static Mesh GenerateCube()
    {
        Vector3[] directions = {
            Vector3.up,
            Vector3.down,
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        CombineInstance[] combine = new CombineInstance[directions.Length];

        for (int i = 0; i < combine.Length; i++)
        {
            combine[i].mesh = GenerateQuad(directions[i]);
            combine[i].transform = Matrix4x4.identity;
        }
        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, true);

        return mesh;
    }

    public static Mesh GenerateQuad(Vector3 direction)
    {
        List<Vector3> vertices = new List<Vector3>();
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
            Quaternion rot2 = Quaternion.FromToRotation(Vector3.back, temp);
            rot = rot * rot2;
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = rot * vertices[i];
        }

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

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
