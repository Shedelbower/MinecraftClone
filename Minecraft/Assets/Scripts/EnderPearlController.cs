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
            _playerController.BeginTeleport(this.transform.position + Vector3.up*0.25f);
            GameObject.Destroy(this.gameObject);
        }
    }

}
