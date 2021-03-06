using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: - Implement crouching
// - Implement cooldown between crouching/uncrouching
// - Fix bug when obstacle is long

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour {

    [Header("Movement Parameters")]
    [SerializeField] float movementSpeed = 4f;
    [SerializeField] float crouchedMovementSpeed = 2f;

    [Header("Crouching Parameters")]
    [SerializeField] FloatVariable crouchHeight;
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float crouchMaxDistToObstacle = 0.5f;
    [SerializeField] float crouchDistToObstacle = 0.4f;
    [SerializeField] float crouchDistToEdge = 0.2f;
    [SerializeField] float edgeTraversalMargin = 0.1f;
    [SerializeField] GameObject edgeIndicator;
    [SerializeField] MovementGrid movementGrid;

    Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
    int crouchDirIndex = -1;
    Vector3 pointOverEdge = Vector3.zero;
    int crouchDirIndexOverEdge = -1;
    bool crouched = false;
    bool inAnimation = false;
    Rigidbody rb;
    Animator anim;

    private void Start() {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    private void Update() {
        if (inAnimation) {
            ResetEdgeIndicator();
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift) && !crouched) {
            Crouch();
        }
        else if (!Input.GetKey(KeyCode.LeftShift) && crouched) {
            ResetEdgeIndicator();
            Uncrouch();
        }
        else if (Input.GetKeyDown(KeyCode.Space) && crouched) {
            TraverseEdge();
        }
    }

    private void FixedUpdate() {
        if (inAnimation) {
            return;
        }

        if (crouched) {
            MoveCrouched();
            DetectEdge();
            DrawOverEdge();
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
                bool _foundRight = Physics.Raycast(Math2D.V3AtHeight(rb.position + _offset * crouchDistToEdge, crouchHeight.Value), _dir, crouchMaxDistToObstacle, obstacleMask);
                bool _foundLeft = Physics.Raycast(Math2D.V3AtHeight(rb.position - _offset * crouchDistToEdge, crouchHeight.Value), _dir, crouchMaxDistToObstacle, obstacleMask);

                if (_foundRight && _foundLeft) {
                    Physics.Raycast(Math2D.V3AtHeight(rb.position, crouchHeight.Value), _dir, out _hit, crouchMaxDistToObstacle, obstacleMask);
                    _minAngle = _angle;
                    _minDirIndex = i;
                }
            }
        }

        if (_minAngle <= 180f) {
            crouched = true;
            crouchDirIndex = _minDirIndex;
            rb.MovePosition(Math2D.V3AtHeight(_hit.point - directions[_minDirIndex] * crouchDistToObstacle, rb.position.y));
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

    private void TraverseEdge() {
        if (crouchDirIndexOverEdge < 0 || crouchDirIndex < 0) {
            return;
        }

        StartCoroutine(CrossEdge(Math2D.V3AtHeight(pointOverEdge, transform.position.y), directions[crouchDirIndex], directions[crouchDirIndexOverEdge]));
        crouchDirIndex = crouchDirIndexOverEdge;
    }

    private void DetectEdge() {
        Vector3 _crouchDir = Vector3.zero;
        Vector3 _perpendicularDir = Vector3.zero;
        Node _currentNode = movementGrid.GetNodeFromWorldPos(transform.position);

        if (crouchDirIndex >= 0) {
            _crouchDir = directions[crouchDirIndex];
            _perpendicularDir = directions[(crouchDirIndex + 1) % directions.Length];
        }
        else {
            crouchDirIndexOverEdge = -1;
            pointOverEdge = Vector3.zero;
            return;
        }

        Node _rightNode = movementGrid.GetNodeFromWorldPos(_currentNode.worldPos + _perpendicularDir);
        Node _leftNode = movementGrid.GetNodeFromWorldPos(_currentNode.worldPos - _perpendicularDir);
        Node _fwdRightNode = movementGrid.GetNodeFromWorldPos(_currentNode.worldPos + _perpendicularDir + _crouchDir);
        Node _fwdLeftNode = movementGrid.GetNodeFromWorldPos(_currentNode.worldPos - _perpendicularDir + _crouchDir);

        float _dstRight = Mathf.Abs(Vector3.Dot(transform.position - _rightNode.worldPos, _perpendicularDir));
        float _dstLeft = Mathf.Abs(Vector3.Dot(transform.position - _leftNode.worldPos, _perpendicularDir));

        if (_dstRight <= crouchDistToEdge + edgeTraversalMargin + movementGrid.nodeWidth / 2) {
            if (_rightNode != _currentNode && Physics.Raycast(Math2D.V3AtHeight(_currentNode.worldPos, crouchHeight.Value), _perpendicularDir, movementGrid.nodeWidth, obstacleMask)) {
                pointOverEdge = _currentNode.worldPos + (movementGrid.nodeWidth / 2 - crouchDistToObstacle) * _perpendicularDir;
                crouchDirIndexOverEdge = (crouchDirIndex + 1) % directions.Length;
            }
            else if (_fwdRightNode != _currentNode && !Physics.Raycast(Math2D.V3AtHeight(_rightNode.worldPos, crouchHeight.Value), _crouchDir, movementGrid.nodeWidth, obstacleMask) &&
                        Physics.Raycast(Math2D.V3AtHeight(_fwdRightNode.worldPos, crouchHeight.Value), -_perpendicularDir, movementGrid.nodeWidth, obstacleMask)) {
                pointOverEdge = _fwdRightNode.worldPos + (movementGrid.nodeWidth / 2 - crouchDistToEdge) * (-_perpendicularDir);
                crouchDirIndexOverEdge = (crouchDirIndex + 3) % directions.Length;
            }
            else {
                pointOverEdge = Vector3.zero;
                crouchDirIndexOverEdge = -1;
            }
        }
        else if (_dstLeft <= crouchDistToEdge + edgeTraversalMargin + movementGrid.nodeWidth / 2) {
            if (_leftNode != _currentNode && Physics.Raycast(Math2D.V3AtHeight(_currentNode.worldPos, crouchHeight.Value), -_perpendicularDir, movementGrid.nodeWidth, obstacleMask)) {
                pointOverEdge = _currentNode.worldPos + (movementGrid.nodeWidth / 2 - crouchDistToObstacle) * (-_perpendicularDir);
                crouchDirIndexOverEdge = (crouchDirIndex + 3) % directions.Length;
            }
            else if (_fwdLeftNode != _currentNode && !Physics.Raycast(Math2D.V3AtHeight(_leftNode.worldPos, crouchHeight.Value), _crouchDir, movementGrid.nodeWidth, obstacleMask) &&
                        Physics.Raycast(Math2D.V3AtHeight(_fwdLeftNode.worldPos, crouchHeight.Value), _perpendicularDir, movementGrid.nodeWidth, obstacleMask)) {
                pointOverEdge = _fwdLeftNode.worldPos + (movementGrid.nodeWidth / 2 - crouchDistToEdge) * _perpendicularDir;
                crouchDirIndexOverEdge = (crouchDirIndex + 1) % directions.Length;
            }
            else {
                pointOverEdge = Vector3.zero;
                crouchDirIndexOverEdge = -1;
            }
        }
        else {
            pointOverEdge = Vector3.zero;
            crouchDirIndexOverEdge = -1;
        }
    }

    private IEnumerator CrossEdge(Vector3 _endPoint, Vector3 _crouchDir, Vector3 _newCrouchDir) {
        inAnimation = true;
        _endPoint = Math2D.V3AtHeight(_endPoint, rb.position.y);

        Vector3 _crouchDirTranslation = Vector3.Dot(_crouchDir, _endPoint - rb.position) * _crouchDir;
        Vector3 _perpDirTranslation = _endPoint - rb.position - _crouchDirTranslation;
        Vector3 _firstDestination = rb.position + _perpDirTranslation;
        Quaternion _newRotation = Quaternion.LookRotation(-_newCrouchDir, Vector3.up);

        float _movementSpeed = 2f;
        float _rotationSpeed = 180f;
        float _frequency = 30f;

        while (rb.position != _firstDestination) {
            yield return new WaitForSeconds(1 / _frequency);
            rb.MovePosition(Vector3.MoveTowards(rb.position, _firstDestination, _movementSpeed / _frequency));
        }
        while (rb.rotation != _newRotation) {
            yield return new WaitForSeconds(1 / _frequency);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, _newRotation, _rotationSpeed / _frequency));
        }
        while (rb.position != _endPoint) {
            yield return new WaitForSeconds(1 / _frequency);
            rb.MovePosition(Vector3.MoveTowards(rb.position, _endPoint, _movementSpeed / _frequency));
        }

        inAnimation = false;
    }

    private void DrawOverEdge() {
        if (crouchDirIndexOverEdge < 0) {
            ResetEdgeIndicator();
        }
        else {
            edgeIndicator.SetActive(true);
            edgeIndicator.transform.position = Math2D.V3AtHeight(pointOverEdge + directions[crouchDirIndexOverEdge] * crouchDistToObstacle, 0.01f);
        }
    }

    private void ResetEdgeIndicator() {
        edgeIndicator.SetActive(false);
    }
}
