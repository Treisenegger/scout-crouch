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

    Rigidbody rb;
    Animator anim;
    bool crouched = false;
    int crouchDirIndex = -1;
    Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

    private void Start() {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && !crouched) {
            Crouch();
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift)) {
            Uncrouch();
        }
    }

    private void FixedUpdate() {
        if (crouched) {
            MoveCrouched();
        }
        else {
            Move();
        }
    }

    private void Crouch() {
        RaycastHit _hit = new RaycastHit();
        float _minAngle = 181f;
        int _minDirIndex = -1;

        for (int i = 0; i < directions.Length; i++) {
            Vector3 _dir = directions[i];
            Vector3 _offset = directions[(i + 1) % directions.Length];
            float _angle = Vector3.Angle(transform.forward, _dir);
            if (_angle < _minAngle) {
                bool _foundRight = Physics.Raycast(Math2D.V3AtHeight(transform.position + _offset * crouchDistToEdge, crouchHeight.Value), _dir, crouchMaxDistToObstacle, obstacleMask);
                bool _foundLeft = Physics.Raycast(Math2D.V3AtHeight(transform.position - _offset * crouchDistToEdge, crouchHeight.Value), _dir, crouchMaxDistToObstacle, obstacleMask);
                if (_foundRight && _foundLeft) {
                    Physics.Raycast(Math2D.V3AtHeight(transform.position, crouchHeight.Value), _dir, out _hit, crouchMaxDistToObstacle, obstacleMask);
                    _minAngle = _angle;
                    _minDirIndex = i;
                }
            }
        }

        if (_minAngle <= 180f) {
            crouched = true;
            crouchDirIndex = _minDirIndex;
            rb.MovePosition(Math2D.V3AtHeight(_hit.point - directions[_minDirIndex] * crouchDistToObstacle, transform.position.y));
            rb.MoveRotation(Quaternion.LookRotation(-directions[_minDirIndex], Vector3.up));
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

        rb.MovePosition(transform.position + _movDir * movementSpeed * Time.fixedDeltaTime);
        rb.velocity = _movDir * movementSpeed;
        if (_movDir != Vector3.zero) {
            rb.MoveRotation(Quaternion.LookRotation(_movDir, Vector3.up));
        }
    }

    private void MoveCrouched() {
        Vector3 _crouchDir = Vector3.zero;
        Vector3 _movDir = Vector3.zero;
        Vector3 _offset = Vector3.zero;

        if (crouchDirIndex >= 0) {
            _crouchDir = directions[crouchDirIndex];
            _offset = directions[(crouchDirIndex + 1) % directions.Length];
        }
        else {
            return;
        }

        if (_crouchDir == Vector3.forward || _crouchDir == Vector3.back) {
            float _movX = Input.GetAxisRaw("Horizontal");
            _movDir = Vector3.right * _movX;
        }
        else if (_crouchDir == Vector3.right || _crouchDir == Vector3.left) {
            float _movZ = Input.GetAxisRaw("Vertical");
            _movDir = Vector3.forward * _movZ;
        }

        Vector3 _newPos = transform.position + _movDir * crouchedMovementSpeed * Time.fixedDeltaTime;
        bool _foundRight = Physics.Raycast(Math2D.V3AtHeight(_newPos + _offset * crouchDistToEdge, crouchHeight.Value), _crouchDir, crouchMaxDistToObstacle, obstacleMask);
        bool _foundLeft = Physics.Raycast(Math2D.V3AtHeight(_newPos - _offset * crouchDistToEdge, crouchHeight.Value), _crouchDir, crouchMaxDistToObstacle, obstacleMask);

        if (_foundLeft && _foundRight) {
            rb.MovePosition(_newPos);
        }
    }
}
