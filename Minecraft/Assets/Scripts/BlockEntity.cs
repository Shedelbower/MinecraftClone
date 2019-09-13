using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockEntity : Entity
{
    [SerializeField] private GameObject[] _faces;

    private BlockType _blockType;
    private ChunkManager _chunkManager;

    public void Initialize(BlockType blockType, Material material, ChunkManager chunkManager) {
        base.Initialize();
        _chunkManager = chunkManager;
        _blockType = blockType;
        AtlasReader reader = new AtlasReader((Texture2D) material.mainTexture,8);

        for (int i = 0; i < _faces.Length; i++) {
            MeshRenderer mr = _faces[i].GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            Mesh mesh = _faces[i].GetComponent<MeshFilter>().mesh;
            Vector2Int atlasPos = i >= blockType.atlasPositions.Length ? blockType.atlasPositions[0] : blockType.atlasPositions[i];
            List<Vector2> uvs = reader.GetUVs(atlasPos.x, atlasPos.y);
            var temp = uvs[0];
            uvs[0] = uvs[1];
            uvs[1] = temp;
            mesh.SetUVs(0, uvs);
        }
    }

    private void Start() {
        Initialize();
    }

    private void FixedUpdate() {
        Vector3Int pos = Vector3Int.RoundToInt(this.transform.position + Vector3.down*0.5f);
        Block block = _chunkManager.GetBlockAtPosition(pos);
        if (block != null && block.type != null) {
            if (block.type.isTransparent == false) {
                // Landed on a block
                Destroy(this.gameObject);
                Block newBlock = new Block(_blockType);
                _chunkManager.ModifyBlock(pos + Vector3Int.up, newBlock);
            }
        }
    }


}
