using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController characterController;
    public ChunkManager chunkManager;
    public Transform feet;
    [Range(0.0f, 100.0f)] public float baseSpeed = 10.0f;
    [Range(0.0f, 2.0f)] public float waterSpeedModifier = 0.5f;
    public GameObject splashEffect;
    public GameObject explosionEffect;

    public Transform camera;

    private float _speed;

    private Vector3Int _prevPosition;
    private BlockType _prevType;

    // Mouse
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;

    private float rotY = 0.0f; // rotation around the up/y axis
    private float rotX = 0.0f; // rotation around the right/x axis

    void Start()
    {
        Cursor.visible = false;

        _speed = baseSpeed;
        //_waterColliders = new List<Collider>();

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Mouse Movement
        Vector3 rot = camera.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Boom();
        }

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            movement = this.transform.forward * _speed;
        } else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            movement = -this.transform.forward * _speed;
        }

        movement = camera.rotation * movement;

        characterController.SimpleMove(movement);


        DoMouseRotation();

        Vector3Int currPosition = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);

        if (currPosition != _prevPosition)
        {
            chunkManager.UpdateLoadedChunks();
            _prevPosition = currPosition;

            // Check if in water
            BlockType currentType = WorldChunk.GetBlockType(feet.position);
            if (_prevType != currentType)
            {
                string blockTypeName = currentType == null ? "Air" : currentType.name;
                //Debug.Log(blockTypeName);

                if (currentType != null && currentType.name == "Water")
                {
                    _speed = baseSpeed * waterSpeedModifier; // Entering water
                    GameObject effect = Instantiate(splashEffect, null) as GameObject;
                    effect.transform.position = feet.position;
                    Destroy(effect, 1.5f);
                }
                else if (_prevType != null && _prevType.name == "Water")
                {
                    _speed = baseSpeed; // Exiting water
                }

                _prevType = currentType;
            }

        }
    }

    private void DoMouseRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
        camera.rotation = localRotation;
    }

    private void Boom()
    {
        //Vector3Int center = Vector3Int.CeilToInt(this.feet.position);
        Vector3Int center = Vector3Int.RoundToInt(this.camera.position + this.camera.rotation * this.transform.forward * 3);

        int blastRadius = 4;

        List<Vector3Int> positions = new List<Vector3Int>();

        for (int x = -blastRadius; x <= blastRadius; x++)
        {
            for (int y = -blastRadius; y <= blastRadius; y++)
            {
                for (int z = -blastRadius; z <= blastRadius; z++)
                {
                    int manhattanDist = Mathf.Abs(x) + Mathf.Abs(y) + Mathf.Abs(z);
                    if (manhattanDist <= blastRadius)
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

        chunkManager.UpdateBlocks(positions, replacements);

        GameObject effect = Instantiate(explosionEffect, null) as GameObject;
        effect.transform.position = center;
    }


    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Water"))
    //    {
    //        _waterColliders.Add(other);
    //        _speed = baseSpeed * waterSpeedModifier;
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("Water"))
    //    {
    //        _waterColliders.Remove(other);
    //        if (_waterColliders.Count == 0)
    //        {
    //            _speed = baseSpeed; // Left all water, return to normal speed
    //        }
       
    //    }
    //}
}
