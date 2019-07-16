using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController characterController;
    public float speed = 10.0f;

    void Start()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }   
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            characterController.SimpleMove(this.transform.forward * speed);
        } else if (Input.GetKey(KeyCode.S))
        {
            characterController.SimpleMove(-this.transform.forward * speed);
        }

        if (Input.GetKey(KeyCode.D))
        {
            this.transform.Rotate(0f,90f*Time.deltaTime,0f);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            this.transform.Rotate(0f, -90f*Time.deltaTime, 0f);
        }
    }
}
