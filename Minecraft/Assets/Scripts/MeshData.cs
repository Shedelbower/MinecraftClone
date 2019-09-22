using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshData
{

    private List<Vector3> _vertices;
    private List<Vector3> _normals;
    private List<Vector2> _uvs;
    private Color[] _colors;
    private int[] _triangles;

    public MeshData(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, int[] triangles) {
        _vertices = vertices;
        _normals = normals;
        _uvs = uvs;
        _triangles = triangles;
        _colors = new Color[0];
    }

    public MeshData(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, int[] triangles, Color[] colors)
    {
        _vertices = vertices;
        _normals = normals;
        _uvs = uvs;
        _triangles = triangles;
        _colors = colors;
    }

    public void TransformVertices(Matrix4x4 mat) {
        for (int i = 0; i < _vertices.Count; i++) {
            _vertices[i] = mat.MultiplyPoint(_vertices[i]);
        }
    }

    public static Mesh Combine(List<MeshData> data) {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        List<int> triangles = new List<int>();
        int tcount = 0;

        foreach(MeshData mesh in data) {
            vertices.AddRange(mesh._vertices);
            normals.AddRange(mesh._normals);
            uvs.AddRange(mesh._uvs);
            colors.AddRange(mesh._colors);

            for (int i = 0; i < mesh._triangles.Length; i++) {
                mesh._triangles[i] += tcount;
            }
            
            triangles.AddRange(mesh._triangles);

            tcount += mesh._vertices.Count;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.SetVertices(vertices);
        combinedMesh.SetNormals(normals);
        combinedMesh.SetUVs(0,uvs);
        combinedMesh.SetColors(colors);
        combinedMesh.SetTriangles(triangles.ToArray(),0);
        return combinedMesh;

    }
}
