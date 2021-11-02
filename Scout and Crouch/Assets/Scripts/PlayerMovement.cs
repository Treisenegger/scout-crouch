using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: - Implement crouching
// - Implement cooldown between crouching/uncrouching
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour {

    [Header("Movement parameters")]
    [SerializeField] float movementSpeed = 4f;
    [SerializeField] float crouchedMovementSpeed = 2f;
    [SerializeField] FloatVariable crouchHeight;
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float crouchMaxDistToObstacle = 0.5f;
    [SerializeField] float crouchDistToObstacle = 0.4f;
    [SerializeField] float crouchDistToEdge = 0.2f;

    // Vector3 movDir = Vector3.zero;
    Rigidbody rb;
    Animator anim;
    bool crouched = false;
    Vector3 crouchDir = Vector3.zero;

    private void Start() {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    private void Update() {
        if (crouched) {
            MoveCrouched();
        }
        else {
            Move();
        }

        if (Input.GetKey(KeyCode.LeftShift) && !crouched) {
            Crouch();
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift)) {
            Uncrouch();
        }
    }

    private void Crouch() {
        RaycastHit _hit = new RaycastHit();
        float _minAngle = 181f;
        Vector3 _minDir = Vector3.zero;
        Vector3[] _directions = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

        for (int i = 0; i < _directions.Length; i++) {
            Vector3 _dir = _directions[i];
            Vector3 _offset = _directions[(i + 1) % _directions.Length];
            float _angle = Vector3.Angle(transform.forward, _dir);
            if (_angle < _minAngle) {
                bool _foundRight = Physics.Raycast(Math2D.V3AtHeight(transform.position + _offset * crouchDistToEdge, crouchHeight.Value), _dir, crouchMaxDistToObstacle, obstacleMask);
                bool _foundLeft = Physics.Raycast(Math2D.V3AtHeight(transform.position - _offset * crouchDistToEdge, crouchHeight.Value), _dir, crouchMaxDistToObstacle, obstacleMask);
                if (_foundRight && _foundLeft) {
                    Physics.Raycast(Math2D.V3AtHeight(transform.position, crouchHeight.Value), _dir, out _hit, crouchMaxDistToObstacle, obstacleMask);
                    _minAngle = _angle;
                    _minDir = _dir;
                }
            }
        }

        if (_minAngle <= 180f) {
            crouched = true;
            crouchDir = _minDir;
            rb.MovePosition(Math2D.V3AtHeight(_hit.point - _minDir * crouchDistToObstacle, transform.position.y));
            rb.MoveRotation(Quaternion.LookRotation(-_minDir, Vector3.up));
            anim.SetTrigger("Crouching");
        }
    }

    private void Uncrouch() {
        if (crouched) {
            crouched = false;
            anim.SetTrigger("Standing Up");
        }
    }

    private void Move() {
        float _movX = Input.GetAxisRaw("Horizontal");
        float _movZ = Input.GetAxisRaw("Vertical");
        Vector3 _movDir = new Vector3(_movX, 0f, _movZ).normalized;

        rb.velocity = _movDir * movementSpeed;
        if (_movDir != Vector3.zero) {
            rb.MoveRotation(Quaternion.LookRotation(_movDir, Vector3.up));
        }
    }

    private void MoveCrouched() {
        if (crouchDir == Vector3.forward || crouchDir == Vector3.back) {
            float _movX = Input.GetAxisRaw("Horizontal");
            Vector3 _movDir = Vector3.right * _movX;
            rb.velocity = _movDir * crouchedMovementSpeed;
        }
        else if (crouchDir == Vector3.right || crouchDir == Vector3.left) {
            float _movZ = Input.GetAxisRaw("Vertical");
            Vector3 _movDir = Vector3.forward * _movZ;
            rb.velocity = _movDir * crouchedMovementSpeed;
        }
    }
}
