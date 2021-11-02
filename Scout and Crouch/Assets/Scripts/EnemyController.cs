using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: - Implement wait time on global waypoints and target rotation

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyVision))]
public class EnemyController : MonoBehaviour {

    [SerializeField] CapsuleCollider parentCollider;
    [SerializeField] CapsuleCollider childCollider;

    [Header("Movement Parameters")]
    [SerializeField] float movementSpeed = 3f;
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] float investigationTurnDuration = 2f;
    [SerializeField] float turnUpdateFreq = 0.05f;

    [Header("Pathfinding Parameters")]
    [SerializeField] MovementGrid movementGrid;
    [SerializeField] float minDistToTarget = 0.7f;
    [SerializeField] float maxAlertedPathLength = 7f;
    [SerializeField] float maxDistToEnd = 0.1f;

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

    private Vector2 currentPathEndPosition {
        get {
            return currentPath.Length > 0 ? currentPath[currentPath.Length - 1] : Vector2.zero;
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

        Physics.IgnoreCollision(parentCollider, childCollider);

        SetPathToGlobalWaypoint();
    }

    private void FixedUpdate() {
        bool _atDestination = rb.position == currentDestination;
        bool _atEndOfPath = currentPathIndex == currentPath.Length - 1;
        bool _hasOneWayPoint = globalPath.Length == 1;

        if (isMoving && _atDestination) {
            if (_atEndOfPath) {
                if (followingGlobalPath) {
                    if (_hasOneWayPoint) {
                        isMoving = false;
                    }
                    else {
                        AdvanceGlobalPath();
                        SetPathToGlobalWaypoint();
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
        else if (isMoving && _atEndOfPath && investigating && Math2D.V3ToV3Dist(transform.position, currentDestination) < maxDistToEnd) {
            StartInvestigativeTurn();
            status = Status.Normal;
            SetPathToGlobalWaypoint();
        }

        if (isMoving) {
            Move();
        }

        if (followingGlobalPath) {
            if (!isMoving && _atDestination && _hasOneWayPoint) {
                RotateTowardsAngle(targetAngle);
            }
            else if (isMoving && !_atDestination) {
                RotateWithMovement();
            }
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
    private void SetNewPath(Vector2 _target, bool _constrained = false) {
        if (_constrained) {
            currentPath = Pathfinding.instance.FindPath(transform.position, Math2D.V2ToV3AtZero(_target), maxAlertedPathLength, ev.visionRange, false, false);
        }
        else {
            currentPath = Pathfinding.instance.FindPath(transform.position, Math2D.V2ToV3AtZero(_target));
        }

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
        if (rb.position == currentDestination) {
            return;
        }

        Quaternion _from = rb.rotation;
        Quaternion _to = Quaternion.LookRotation((currentDestination - rb.position).normalized, Vector3.up);

        if (_to == _from) {
            return;
        }

        float _maxAngle = rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(_from, _to, _maxAngle));
    }

    private void RotateTowardsAngle(float _angle) {
        Quaternion _from = rb.rotation;
        Quaternion _to = Quaternion.Euler(0f, _angle, 0f);

        if (_to == _from) {
            return;
        }

        float _maxAngle = rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(_from, _to, _maxAngle));
    }

    // Set rotation to look at the player's last location
    private void RotateTowardsTarget(Vector3 _targetPos) {
        rb.MoveRotation(Quaternion.LookRotation(Math2D.V3ToV3Dir(transform.position, _targetPos), Vector3.up));
    }

    // Method called when target is detected by EnemyVision component to set the status field and the last player position
    public void TargetDetected(bool _detected, Vector3 _targetLocation) {
        if (_detected) {
            Node _prevTargetNode = movementGrid.GetNodeFromWorldPos(Math2D.V2ToV3AtZero(currentPathEndPosition));
            Node _newTargetNode = movementGrid.GetNodeFromWorldPos(_targetLocation);

            if (!alerted || (isMoving && _prevTargetNode != _newTargetNode)) {
                isMoving = true;
                SetNewPath(Math2D.V3ToV2(_targetLocation), true);
            }

            lastTargetLocation = _targetLocation;
            status = Status.Alerted;
        }
        else if (alerted) {
            isMoving = true;
            SetNewPath(Math2D.V3ToV2(lastTargetLocation));
            status = Status.Investigating;
        }
        else if (investigating) {
            status = Status.Investigating;
        }
        else {
            status = Status.Normal;
        }
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

        Gizmos.DrawWireSphere(Math2D.V3AtZero(transform.position), maxAlertedPathLength);
    }
}
