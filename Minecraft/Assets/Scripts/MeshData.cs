using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshData
{

    private List<Vector3> _vertices;
    private List<Vector3> _normals;
    private List<Vector2> _uvs;
    private int[] _triangles;

    public MeshData(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, int[] triangles) {
        _vertices = vertices;
        _normals = normals;
        _uvs = uvs;
        _triangles = triangles;
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

        List<int> triangles = new List<int>();
        int tcount = 0;

        foreach(MeshData mesh in data) {
            vertices.AddRange(mesh._vertices);
            normals.AddRange(mesh._normals);
            uvs.AddRange(mesh._uvs);

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
        combinedMesh.SetTriangles(triangles.ToArray(),0);
        return combinedMesh;

    }
}
