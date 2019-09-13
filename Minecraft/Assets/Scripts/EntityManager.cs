using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public ChunkManager chunkManager;
    public GameObject blockEntityPrefab;
    private Dictionary<System.Guid,Entity> _entities;

    private void Start() {
        _entities = new Dictionary<System.Guid, Entity>();
    }

    public void RegisterEntity(Entity entity) {
        _entities.Add(entity.Id, entity);
    }

    public void RemoveEntity(System.Guid id) {
        _entities.Remove(id);
    }

    public void CreateBlockEntity(Vector3Int position, BlockType blockType) {
        GameObject go = Instantiate(blockEntityPrefab, this.transform);
        go.transform.position = position;
        go.transform.rotation = Quaternion.identity;
        BlockEntity entity = go.GetComponent<BlockEntity>();
        entity.Initialize(blockType, chunkManager.chunkOpaqueMaterial, chunkManager);
    }

}
