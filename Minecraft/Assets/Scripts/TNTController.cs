using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TNTController : MonoBehaviour
{

    public static readonly float FUSE_TIME = 2.0f;
    public static readonly float LAUNCH_VELOCITY = 20.0f;

    public GameObject explosionEffect;

    public Rigidbody rb;

    private float _timer = 0.0f;

    private ChunkManager _chunkManager;

    private Material[] _materials;
    

    public void Launch(Vector3 direction)
    {
        _chunkManager = GameObject.Find("Chunk Manager").GetComponent<ChunkManager>();

        MeshRenderer[] renders = GetComponentsInChildren<MeshRenderer>();
        _materials = new Material[renders.Length];
        for (int i = 0; i < _materials.Length; i++)
        {
            _materials[i] = renders[i].material;
            _materials[i].EnableKeyword("_EMISSION");
        }
        rb.velocity = direction.normalized * LAUNCH_VELOCITY;

    }

    public void Update()
    {
        _timer += Time.deltaTime;
        FlashMaterial();

        if (_timer >= FUSE_TIME)
        {
            Explode();
        }
    }

    private void FlashMaterial()
    {
        float t = Mathf.Sin(_timer * Mathf.PI * 3.0f);
        Color color = Color.Lerp(Color.black, Color.white, t);
        foreach (Material material in _materials)
        {
            material.SetColor("_EmissionColor", color);
        }
    }

    private void Explode()
    {
        Vector3Int center = Vector3Int.RoundToInt(this.transform.position);

        Block block = _chunkManager.GetBlockAtPosition(center);
        if (block != null && block.type != null && block.type.name == "Water")
        {
            // Don't cause damage when in water
        } else
        {
            int blastRadius = 4;

            List<Vector3Int> positions = new List<Vector3Int>();

            for (int x = -blastRadius; x <= blastRadius; x++)
            {
                for (int y = -blastRadius; y <= blastRadius; y++)
                {
                    for (int z = -blastRadius; z <= blastRadius; z++)
                    {
                        float dist = Mathf.Sqrt(x * x + y * y + z * z);
                        if (dist <= blastRadius)
                        {
                            Vector3Int pos = new Vector3Int(x, y, z) + center;
                            positions.Add(pos);
                        }
                    }
                }
            }

            List<Block> replacements = new List<Block>();
            BlockType airType = BlockType.GetBlockType("Air");
            for (int i = 0; i < positions.Count; i++)
            {
                Block replacement = new Block(airType);
                replacements.Add(replacement);
            }

            _chunkManager.ModifyBlocks(positions, replacements);
        }

        // Effect 
        GameObject effect = Instantiate(explosionEffect, null) as GameObject;
        effect.transform.position = center;

        Destroy(this.gameObject);
    }

}
