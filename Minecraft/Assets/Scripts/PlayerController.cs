using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController characterController;
    public MonitorController monitorController;
    public ChunkManager chunkManager;
    public Transform body;
    public Transform feet;
    public AudioSource stepAudioSource;
    [Range(0.0f, 100f)] public float baseSpeed = 10f;
    [Range(0.0f, 100f)] public float jumpPower = 10f;
    [Range(0.0f, 1.0f)] public float sidewaysSpeedModifier = 0.5f;
    [Range(0.0f, 2.0f)] public float waterSpeedModifier = 0.5f;
    public GameObject splashEffect;
    public GameObject explosionEffect;
    public GameObject breakEffect;

    public GameObject tntPrefab;

    public Color waterTintColor = Color.blue;
    private Color initialSkyColor;
    public Material[] materialsToColor;

    public new Transform camera;

    private float _speed;

    private Vector3Int _prevPosition;
    private Vector3Int _currPosition;
    private BlockType _prevType;

    private bool _isJumping = false;
    private float _jumpTimer = 0.0f;
    [SerializeField] private float _jumpDuration = 2.0f;

    [SerializeField] private bool _isInWater = false;
    //[SerializeField] private bool _cameraIsInWater = false;

    // Mouse
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;

    private float rotY = 0.0f; // rotation around the up/y axis
    private float rotX = 0.0f; // rotation around the right/x axis

    void Start()
    {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 30;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //initialSkyColor = Camera.main.backgroundColor;
        initialSkyColor = RenderSettings.fogColor;

        _speed = baseSpeed;

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
        _currPosition = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);

        if (_currPosition != _prevPosition)
        {
            monitorController.OnBlockTraveled();

            chunkManager.UpdateLoadedChunks();
            _prevPosition = _currPosition;

            // Check if feet in water
            Block currBlock = chunkManager.GetBlockAtPosition(Vector3Int.RoundToInt(feet.position));
            BlockType currentType = currBlock == null ? null : currBlock.type;
            if (_prevType != currentType)
            {
                string blockTypeName = currentType == null ? "Air" : currentType.name;
                if (blockTypeName == "Water")
                {
                    OnEnterWater();
                }
                else if (_prevType != null && _prevType.name == "Water")
                {
                    OnExitWater();
                }

                _prevType = currentType;
            }

            // Check if camera in water
            currBlock = chunkManager.GetBlockAtPosition(Vector3Int.RoundToInt(camera.position));
            currentType = currBlock == null ? null : currBlock.type;
            if (currentType != null && currentType.name == "Water")
            {
                OnCameraEnterWater();
            } else
            {
                OnCameraExitWater();
            }

        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        
        if (Input.GetMouseButtonDown(1))
        {
            //PlaceBlock("Sand");
            PlaceSameBlock();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            BreakBlock();
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            //Boom();
            LaunchTNT();
        }

        if (Input.GetKeyDown(KeyCode.Space) && _isInWater == false && characterController.isGrounded)
        {
            _isJumping = true;
            _jumpTimer = 0.0f;
        }

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            movement += this.transform.forward * _speed;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            movement += -this.transform.forward * _speed;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            movement += -this.transform.right * _speed * sidewaysSpeedModifier;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            movement += this.transform.right * _speed * sidewaysSpeedModifier;
        }

        if (movement.sqrMagnitude > 0.001f)
        {
            PlayStepAudio();
        }

        movement = camera.rotation * movement;

        float mag = movement.magnitude;

        movement.y = 0.0f;
        movement = movement.normalized * mag;

        if (_isJumping)
        {
            _jumpTimer += Time.deltaTime;
            float t = Mathf.Lerp(jumpPower, 0.0f, _jumpTimer / _jumpDuration);
            movement += Vector3.up * t;

            if (_jumpTimer >= _jumpDuration || _isInWater)
            {
                _isJumping = false;
            }
        }

        float gravity = -9.0f;
        if (_isInWater)
        {
            gravity *= waterSpeedModifier;
        }
        movement += Vector3.up * gravity;

        if (Input.GetKey(KeyCode.Space) && _isInWater)
        {
            movement += Vector3.up * _speed * waterSpeedModifier;
            movement -= Vector3.up * gravity; // Remove effects of gravity
        }

        characterController.Move(movement * Time.deltaTime);


        DoMouseRotation();


        
    }

    private void PlayStepAudio()
    {
        if (stepAudioSource.isPlaying == false)
        {
            Vector3Int positionBeneathFeet = _currPosition + Vector3Int.down;
            Block blockBeneathFeet = chunkManager.GetBlockAtPosition(positionBeneathFeet);
            if (blockBeneathFeet != null && blockBeneathFeet.type != null)
            {
                AudioClip[] stepClips = blockBeneathFeet.type.stepClips;
                if (stepClips != null && stepClips.Length > 0)
                {
                    AudioClip clip = stepClips[Random.Range(0, stepClips.Length - 1)];
                    stepAudioSource.clip = clip;
                    stepAudioSource.Play();
                }
                
            }
        }
    }

    private void SetMaterialColors(Color color)
    {
        foreach(Material material in materialsToColor)
        {
            material.color = color;
        }
    }

    private void OnEnterWater()
    {
        _isInWater = true;
        _speed = baseSpeed * waterSpeedModifier; // Entering water
        GameObject effect = Instantiate(splashEffect, null) as GameObject;
        effect.transform.position = feet.position;
        Destroy(effect, 1.5f);
    }

    private void OnExitWater()
    {
        _isInWater = false;
        _speed = baseSpeed;
        SetMaterialColors(Color.white);
        Camera.main.backgroundColor = initialSkyColor;
    }

    private void OnCameraEnterWater()
    {
        SetMaterialColors(waterTintColor);
        Camera.main.backgroundColor = waterTintColor;

        //RenderSettings.fog = true;
        RenderSettings.fogColor = waterTintColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.15f;
    }

    private void OnCameraExitWater()
    {
        SetMaterialColors(Color.white);
        Camera.main.backgroundColor = initialSkyColor;

        //RenderSettings.fog = false;
        RenderSettings.fogColor = initialSkyColor;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 60;
        RenderSettings.fogEndDistance = 65;
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

        body.rotation = camera.rotation;
        Vector3 angles = body.eulerAngles;
        angles.x = 0.0f;
        angles.z = 0.0f;
        body.eulerAngles = angles;
    }

    private void LaunchTNT()
    {
        GameObject prefab = Instantiate(tntPrefab, null);
        prefab.transform.position = camera.position + camera.forward;
        TNTController tnt = prefab.GetComponent<TNTController>();
        tnt.Launch(camera.forward);
    }

    //private void Boom()
    //{
    //    Vector3Int center;
    //    if (RaycastToBlock(30.0f, true, out center) == false)
    //    {
    //        return;
    //    }

    //    int blastRadius = 4;

    //    List<Vector3Int> positions = new List<Vector3Int>();

    //    for (int x = -blastRadius; x <= blastRadius; x++)
    //    {
    //        for (int y = -blastRadius; y <= blastRadius; y++)
    //        {
    //            for (int z = -blastRadius; z <= blastRadius; z++)
    //            {
    //                int manhattanDist = Mathf.Abs(x) + Mathf.Abs(y) + Mathf.Abs(z);
    //                if (manhattanDist <= blastRadius)
    //                {
    //                    Vector3Int pos = new Vector3Int(x, y, z) + center;
    //                    positions.Add(pos);
    //                }
    //            }
    //        }
    //    }

    //    List<Block> replacements = new List<Block>();
    //    BlockType airType = BlockType.GetBlockType("Air");
    //    for (int i = 0; i < positions.Count; i++)
    //    {
    //        Block replacement = new Block(airType);
    //        replacements.Add(replacement);
    //    }

    //    monitorController.OnBlockDestroyed(replacements.Count);

    //    chunkManager.ModifyBlocks(positions, replacements);

    //    GameObject effect = Instantiate(explosionEffect, null) as GameObject;
    //    effect.transform.position = center;
    //}

    private void PlaceBlock(string blockTypeName)
    {
        Block block = new Block(blockTypeName);
        PlaceBlock(block);
    }

    private void PlaceBlock(BlockType blockType)
    {
        Block block = new Block(blockType);
        PlaceBlock(block);
    }

    private void PlaceBlock(Block block)
    {
        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, true, out blockPos))
        {
            if (blockPos == _currPosition)
            {
                return; // Don't place block in feet space
            }
            List<Vector3Int> positions = new List<Vector3Int>();
            List<Block> blocks = new List<Block>();

            positions.Add(blockPos);
            blocks.Add(block);

            chunkManager.ModifyBlocks(positions, blocks);

            PlayBlockSound(block.type.name, blockPos);

            monitorController.OnBlockPlaced();
        }
    }

    private Block GetTargetBlock(float maxDistance)
    {
        Vector3Int blockPos;
        if (RaycastToBlock(maxDistance, false, out blockPos))
        {
            Block targetBlock = chunkManager.GetBlockAtPosition(blockPos);
            return targetBlock;
        }

        return null;
    }

    private void PlaceSameBlock()
    {
        Block targetBlock = GetTargetBlock(10.0f);
        if (targetBlock == null)
        {
            return;
        }
        BlockType targetType = targetBlock.type;

        PlaceBlock(targetType);
    }

    private void BreakBlock()
    {
        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, false, out blockPos))
        {
            List<Vector3Int> positions = new List<Vector3Int>();
            List<Block> blocks = new List<Block>();

            positions.Add(blockPos);
            Block block = new Block("Air");
            blocks.Add(block);

            Block breakingBlock = chunkManager.GetBlockAtPosition(blockPos);
            if (breakingBlock != null && breakingBlock.type != null)
            {
                PlayBlockSound(breakingBlock.type.name, blockPos);


                GameObject effect = Instantiate(breakEffect, null);
                effect.transform.position = blockPos;

                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                ParticleSystem.MainModule main = ps.main;

                main.startColor = breakingBlock.type.breakParticleColors;

                monitorController.OnBlockDestroyed();

            }
            

            chunkManager.ModifyBlocks(positions, blocks);

            
        }
    }

    private void PlayBlockSound(string blockTypeName, Vector3Int position)
    {
        AudioClip clip = BlockType.GetBlockType(blockTypeName).digClip;
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }

    private bool RaycastToBlock(in float maxDistance, in bool getEmptyBlock, out Vector3Int hitBlockPosition)
    {
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        Vector3 direction = camera.TransformDirection(Vector3.forward);
        if (Physics.Raycast(camera.position, direction, out hit, Mathf.Infinity))
        {
            if (hit.distance <= maxDistance)
            {
                Vector3 offset = getEmptyBlock ? direction * -0.01f : direction * 0.01f;
                hitBlockPosition = Vector3Int.RoundToInt(hit.point + offset);
                return true;
            }
        }

        hitBlockPosition = Vector3Int.zero;
        return false;
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
