using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtlasReader
{
    protected Texture2D _atlas;
    protected int _pixelsPerPatch;
    protected int _size;
    protected int _divisions;


    public AtlasReader(Texture2D atlas, int divisions)
    {
        _atlas = atlas;
        _size = atlas.width; // Atlas is assumed to be square.
        _divisions = divisions;

        _pixelsPerPatch = _size / _divisions;
    }

    public List<Vector2> GetUVs(int i, int j)
    {
        // Index origin (0,0) is at the upper left corner of the atlas
        float d = 1.0f / _divisions;
        float pad = 0.0001f;

        Vector2 uv00 = new Vector2(i * d + pad, 1f - (j * d + pad));
        Vector2 uv01 = new Vector2(i * d + pad, 1f - (j * d + d - pad));
        Vector2 uv11 = new Vector2(i * d + d - pad, 1f - (j * d + d - pad));
        Vector2 uv10 = new Vector2(i * d + d - pad, 1f - (j * d + pad));

        List<Vector2> uvs = new List<Vector2>();
        uvs.Add(uv11);
        uvs.Add(uv10);
        uvs.Add(uv00);
        uvs.Add(uv01);

        return uvs;
    }
}
