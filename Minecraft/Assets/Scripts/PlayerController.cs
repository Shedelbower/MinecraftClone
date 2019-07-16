using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController characterController;
    public ChunkManager chunkManager;
    [Range(0.0f, 100.0f)] public float baseSpeed = 10.0f;
    [Range(0.0f, 2.0f)] public float waterSpeedModifier = 0.5f;

    private float _speed;

    private Vector2Int _prevPosition;
    private List<Collider> _waterColliders;

    void Start()
    {
        _speed = baseSpeed;
        _waterColliders = new List<Collider>();

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }   
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            characterController.SimpleMove(this.transform.forward * _speed);
        } else if (Input.GetKey(KeyCode.S))
        {
            characterController.SimpleMove(-this.transform.forward * _speed);
        }

        if (Input.GetKey(KeyCode.D))
        {
            this.transform.Rotate(0f,90f*Time.deltaTime,0f);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            this.transform.Rotate(0f, -90f*Time.deltaTime, 0f);
        }

        Vector2Int currPosition = new Vector2Int((int)transform.position.x, (int)transform.position.z);

        if (currPosition != _prevPosition)
        {
            chunkManager.UpdateLoadedChunks();
            _prevPosition = currPosition;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            _waterColliders.Add(other);
            _speed = baseSpeed * waterSpeedModifier;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            _waterColliders.Remove(other);
            if (_waterColliders.Count == 0)
            {
                _speed = baseSpeed; // Left all water, return to normal speed
            }
       
        }
    }
}
