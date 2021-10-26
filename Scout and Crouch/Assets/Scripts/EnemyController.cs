using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyVision))]
public class EnemyController : MonoBehaviour {

    [SerializeField] float movementSpeed = 3f;
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] float investigationTurnDuration = 2f;
    [SerializeField] float turnUpdateFreq = 0.05f;
    [SerializeField] Vector2[] globalPath;
    [SerializeField] MovementGrid movementGrid;

    Rigidbody rb;
    EnemyVision ev;
    Vector2[] currentPath;
    int currentPathIndex = 0;
    int globalPathIndex = 0;
    Vector3 currentDestination;
    bool isMoving = true;
    Status status = Status.Normal;
    Vector3 lastTargetLocation;

    private bool followingGlobalPath {
        get {
            return status == Status.Normal;
        }
    }

    private Vector2 pos2D {
        get {
            return new Vector2(transform.position.x, transform.position.z);
        }
        set {
            rb.MovePosition(new Vector3(value.x, transform.position.y, value.y));
        }
    }

    private Vector2 currentPathEndPosition {
        get {
            return currentPath[currentPath.Length - 1];
        }
    }

    private bool investigating {
        get {
            return status == Status.Investigating;
        }
    }

    private enum Status {
        Normal,
        Alerted,
        Investigating
    }

    private void Start() {
        rb = GetComponent<Rigidbody>();
        ev = GetComponent<EnemyVision>();

        SetPathToGlobalWaypoint();
    }

    private void FixedUpdate() {
        if (rb.position == currentDestination) {
            if (currentPathIndex == currentPath.Length - 1) {
                if (followingGlobalPath) {
                    AdvanceGlobalPath();
                    SetPathToGlobalWaypoint();
                }
                else if (investigating) {
                    StartInvestigativeTurn();
                    status = Status.Normal;
                    SetPathToGlobalWaypoint();
                }
            }
            else {
                AdvanceCurrentPath();
                UpdateDestination();
            }
        }

        Move();
        if (followingGlobalPath) {
            RotateWithMovement();
        }
        else {
            RotateTowardsTarget(lastTargetLocation);
        }
    }

    private void AdvanceGlobalPath() {
        globalPathIndex = (globalPathIndex + 1) % globalPath.Length;
    }

    private void AdvanceCurrentPath() {
        currentPathIndex++;
    }

    private void SetPathToGlobalWaypoint() {
        SetNewPath(globalPath[globalPathIndex]);
    }

    private void SetNewPath(Vector2 _target) {
        currentPath = Pathfinding.instance.FindPath(transform.position, new Vector3(_target.x, 0f, _target.y), 0f);
        currentPathIndex = 0;
        UpdateDestination();
    }
    
    private void UpdateDestination() {
        currentDestination = new Vector3(currentPath[currentPathIndex].x, transform.position.y, currentPath[currentPathIndex].y);
    }

    private void Move() {
        if (!isMoving) {
            return;
        }

        Vector3 _from = transform.position;
        Vector3 _to = currentDestination;
        float _maxDist = movementSpeed * Time.fixedDeltaTime;
        rb.MovePosition(Vector3.MoveTowards(_from, _to, _maxDist));
    }

    private void RotateWithMovement() {
        if (!isMoving || currentDestination == rb.position) {
            return;
        }

        Quaternion _from = rb.rotation;
        Quaternion _to = Quaternion.LookRotation((currentDestination - rb.position).normalized, Vector3.up);
        float _maxAngle = rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(_from, _to, _maxAngle));
    }

    private void RotateTowardsTarget(Vector3 _targetPos) {
        rb.MoveRotation(Quaternion.LookRotation((_targetPos - transform.position).normalized, Vector3.up));
    }

    public void TargetDetected(bool _detected, Vector3 _targetLocation) {
        Status _newStatus;

        if (_detected) {
            _newStatus = Status.Alerted;
        }
        else if (status == Status.Alerted || status == Status.Investigating) {
            _newStatus = Status.Investigating;
        }
        else {
            _newStatus = Status.Normal;
        }

        if (_detected) {
            lastTargetLocation = _targetLocation;
            Node _prevTargetNode = movementGrid.GetNodeFromWorldPos(currentPathEndPosition);
            Node _newTargetNode = movementGrid.GetNodeFromWorldPos(_targetLocation);

            if (_newStatus != status || _prevTargetNode != _newTargetNode) {
                SetNewPath(new Vector2(_targetLocation.x, _targetLocation.z));
            }
        }

        status = _newStatus;
    }

    private void StartInvestigativeTurn() {
        StartCoroutine(TurnAround(investigationTurnDuration, turnUpdateFreq));
    }

    private IEnumerator TurnAround(float _duration, float _updateFreq) {
        float _turnSpeed = 360 / _duration;
        float _angleTurned = 0f;
        isMoving = false;
        while (_angleTurned < 360f) {
            yield return new WaitForSeconds(_updateFreq);
            float _turnAngle = _updateFreq * _turnSpeed;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, _turnAngle, 0f));
            _angleTurned += _turnAngle;
        }
        isMoving = true;
    }

    private void OnDrawGizmos() {
        foreach (Vector2 _point in globalPath) {
            Vector3 _sphereCenter = new Vector3(_point.x, 0f, _point.y);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_sphereCenter, 0.1f);
        }
    }
}
