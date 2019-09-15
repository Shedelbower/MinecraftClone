using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnderPearlController : MonoBehaviour
{
    private static readonly float MAX_THROW_DURATION = 10.0f;

    public AudioClip throwClip;

    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private AudioSource _audioSource;
    private PlayerController _playerController;
    private float _timer;
    public void Initialize(Vector3 initialVelocity, PlayerController playerController) {
        _rigidbody.velocity = initialVelocity;
        _playerController = playerController;
        _audioSource.PlayOneShot(throwClip);
    }

    void Update() {
        transform.LookAt(Camera.main.transform.position, -Vector3.up);

        _timer += Time.deltaTime;
        if (_timer >= MAX_THROW_DURATION) {
            GameObject.Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag != "Player") {
            // Offset the player so they are ideally not half inside the block they teleport to.
            Vector3 positionOffset = _rigidbody.velocity.normalized * -1.0f;
            _playerController.BeginTeleport(this.transform.position + positionOffset);
            GameObject.Destroy(this.gameObject);
        }
    }

}
