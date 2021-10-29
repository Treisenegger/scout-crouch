using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour {

    [Header("Movement parameters")]
    [SerializeField] float movementSpeed = 1f;

    Vector3 movDir = Vector3.zero;
    Rigidbody rb;
    bool crouching = false;

    private void Start() {
        rb = GetComponent<Rigidbody>();
    }

    private void Update() {
        float _movX = Input.GetAxisRaw("Horizontal");
        float _movZ = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector3(_movX, 0f, _movZ).normalized * movementSpeed;
    }
}
