using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyVision))]
public class EnemyController : MonoBehaviour {

    [Header("Movement Parameters")]
    [SerializeField] float movementSpeed = 3f;
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] float investigationTurnDuration = 2f;
    [SerializeField] float turnUpdateFreq = 0.05f;

    [Header("Pathfinding Parameters")]
    [SerializeField] MovementGrid movementGrid;
    [SerializeField] float minDistToTarget = 0.7f;

    [Header("Path Parameters")]
    [SerializeField] Vector2[] globalPath;
    [Tooltip("Angle to point towards when reaching global path point in single waypoint global path")]
    [SerializeField] float targetAngle;

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

    private bool alerted {
        get {
            return status == Status.Alerted;
        }
    }

    private bool hasSingleGlobalWaypoint {
        get {
            return globalPath.Length == 1;
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
                    if (!hasSingleGlobalWaypoint) {
                        AdvanceGlobalPath();
                        SetPathToGlobalWaypoint();
                    }
                    else {
                        isMoving = false;
                    }
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
        if (followingGlobalPath && hasSingleGlobalWaypoint && !isMoving) {
            RotateTowardsAngle(targetAngle);
        }
        else if (followingGlobalPath) {
            RotateWithMovement();
        }
        else {
            RotateTowardsTarget(lastTargetLocation);
        }
    }

    // Increment index of the global path
    private void AdvanceGlobalPath() {
        globalPathIndex = (globalPathIndex + 1) % globalPath.Length;
    }

    // Increment index of the current local path
    private void AdvanceCurrentPath() {
        currentPathIndex++;
    }

    // Set new local path towards next waypoint in global path
    private void SetPathToGlobalWaypoint() {
        SetNewPath(globalPath[globalPathIndex]);
    }

    // Set new local path
    private void SetNewPath(Vector2 _target) {
        currentPath = Pathfinding.instance.FindPath(transform.position, Math2D.V2ToV3AtZero(_target));

        if (currentPath.Length == 0) {
            isMoving = false;
            return;
        }

        currentPathIndex = 0;
        UpdateDestination();
    }
    
    // Set new destination when setting new path or arriving at current destination
    private void UpdateDestination() {
        currentDestination = Math2D.V2ToV3AtHeight(currentPath[currentPathIndex], transform.position.y);
    }

    // Move the enemy towards its next destination in the local path
    private void Move() {
        if (!isMoving) {
            return;
        }

        if (alerted && Math2D.V3ToV3Dist(transform.position, lastTargetLocation) < minDistToTarget) {
            isMoving = false;
            return;
        }

        Vector3 _from = transform.position;
        Vector3 _to = currentDestination;
        float _maxDist = movementSpeed * Time.fixedDeltaTime;
        rb.MovePosition(Vector3.MoveTowards(_from, _to, _maxDist));
    }

    // Gradually rotate the enemy towards its moving direction on normal status
    private void RotateWithMovement() {
        if (!isMoving || currentDestination == rb.position) {
            return;
        }

        Quaternion _from = rb.rotation;
        Quaternion _to = Quaternion.LookRotation((currentDestination - rb.position).normalized, Vector3.up);
        float _maxAngle = rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(_from, _to, _maxAngle));
    }

    // Set rotation to look at the player's last location
    private void RotateTowardsTarget(Vector3 _targetPos) {
        rb.MoveRotation(Quaternion.LookRotation(Math2D.V3ToV3Dir(transform.position, _targetPos), Vector3.up));
    }

    private void RotateTowardsAngle(float _angle) {
        if (rb.rotation == Quaternion.Euler(0f, _angle, 0f)) {
            return;
        }

        Quaternion _from = rb.rotation;
        Quaternion _to = Quaternion.Euler(0f, _angle, 0f);
        float _maxAngle = rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(_from, _to, _maxAngle));
    }

    // Method called when target is detected by EnemyVision component to set the status field and the last player position
    public void TargetDetected(bool _detected, Vector3 _targetLocation) {
        Status _newStatus;

        if (_detected) {
            if (!alerted) {
                isMoving = true;
            }

            _newStatus = Status.Alerted;
        }
        else if (alerted) {
            _newStatus = Status.Investigating;
            isMoving = true;
        }
        else if (investigating) {
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
                SetNewPath(Math2D.V3ToV2(_targetLocation));
            }
        }

        status = _newStatus;
    }

    private void StartInvestigativeTurn() {
        StartCoroutine(TurnAround(investigationTurnDuration, turnUpdateFreq));
    }

    // Complete a 360 turn when arriving at the player's last position when investigating
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

    // Draw global path's waypoints for easier path definition
    private void OnDrawGizmos() {
        foreach (Vector2 _point in globalPath) {
            Vector3 _sphereCenter = Math2D.V2ToV3AtZero(_point);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_sphereCenter, 0.1f);
        }
    }
}
