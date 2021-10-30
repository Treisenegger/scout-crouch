using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: - Implement crouching
// - Implement cooldown between crouching/uncrouching
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour {

    [Header("Movement parameters")]
    [SerializeField] float movementSpeed = 1f;
    [SerializeField] FloatVariable crouchHeight;
    [SerializeField] LayerMask obstacleMask;

    // Vector3 movDir = Vector3.zero;
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
        Vector3 _movDir = new Vector3(_movX, 0f, _movZ).normalized;
        rb.velocity = _movDir * movementSpeed;
        if (_movDir != Vector3.zero) {
            rb.MoveRotation(Quaternion.LookRotation(_movDir, Vector3.up));
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            // crouched = !crouched;
            // if (crouched) {
            //     anim.SetTrigger("Crouching");
            // }
            // else {
            //     anim.SetTrigger("Standing Up");
            // }
            Crouch();
        }
    }

    private void Crouch() {
        RaycastHit _hit;
        RaycastHit _newHit;

        bool _foundObstacle = Physics.Raycast(Math2D.V3AtHeight(transform.position, crouchHeight.Value), Vector3.forward, out _hit, 1f, obstacleMask);
        float _minAngle = _foundObstacle ? Vector3.Angle(transform.forward, Vector3.forward) : 181f;

        foreach (Vector3 _dir in new Vector3[] { Vector3.right, Vector3.back, Vector3.left }) {
            _foundObstacle = Physics.Raycast(Math2D.V3AtHeight(transform.position, crouchHeight.Value), _dir, out _newHit, 1f, obstacleMask);
            float _angle = Vector3.Angle(transform.forward, _dir);
            if (_foundObstacle && _angle < _minAngle) {
                _hit = _newHit;
                _minAngle = _angle;
            }
        }

        if (_minAngle <= 180f) {
            Debug.DrawLine(Math2D.V3AtHeight(transform.position, crouchHeight.Value), _hit.point, Color.black, 1f);
        }
    }
}
