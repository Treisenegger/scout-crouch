using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: - Implement crouching
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour {

    [Header("Movement parameters")]
    [SerializeField] float movementSpeed = 1f;

    Vector3 movDir = Vector3.zero;
    Rigidbody rb;
    Animator anim;
    bool crouched = false;

    private void Start() {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    private void Update() {
        float _movX = Input.GetAxisRaw("Horizontal");
        float _movZ = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector3(_movX, 0f, _movZ).normalized * movementSpeed;

        if (Input.GetKeyDown(KeyCode.Space)) {
            crouched = !crouched;
            if (crouched) {
                anim.SetTrigger("Crouching");
            }
            else {
                anim.SetTrigger("Standing Up");
            }
        }
    }
}
