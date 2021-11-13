using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyVision))]
public class EnemyController : MonoBehaviour {
    [Header("Collider Parameters")]
    [SerializeField] CapsuleCollider parentCollider;
    [SerializeField] CapsuleCollider childCollider;

    [Header("Movement Parameters")]
    [SerializeField] float movementSpeed = 3f;
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] float investigationTurnDuration = 2f;
    [SerializeField] float turnUpdateFreq = 30f;

    [Header("Pathfinding Parameters")]
    [SerializeField] float minDistToTarget = 0.7f;
    [SerializeField] float maxAlertedPathLength = 7f;
    [SerializeField] float maxDistToEnd = 0.1f;
    [SerializeField] MovementGrid movementGrid;

    [Header("Path Parameters")]
    [SerializeField] float waitTime = 1f;
    [SerializeField] Waypoint[] globalWaypoints;

    Rigidbody rb;
    EnemyVision ev;
    bool isMoving = true;
    int currentPathIndex = 0;
    int globalWaypointsIndex = 0;
    Vector3 currentDestination;
    Vector3 lastTargetLocation;
    Vector2[] currentPath;
    Status status = Status.Normal;
    Dictionary<Status, StatusActions> statusActionsDictionary;
    IEnumerator waitCoroutine;

    private bool FollowingGlobalPath {
        get {
            return status == Status.Normal;
        }
    }

    private bool Investigating {
        get {
            return status == Status.Investigating;
        }
    }

    private bool Alerted {
        get {
            return status == Status.Alerted;
        }
    }

    private Vector2 CurrentPathEndPosition {
        get {
            return currentPath.Length > 0 ? currentPath[currentPath.Length - 1] : Vector2.zero;
        }
    }

    private enum Status {
        Normal,
        Alerted,
        Investigating
    }

    private struct StatusActions {
        public Action pathUpdate;
        public Action move;
        public Action rotate;

        public StatusActions(Action _pathUpdate, Action _move, Action _rotate) {
            pathUpdate = _pathUpdate;
            move = _move;
            rotate = _rotate;
        }
    }

    [Serializable]
    private struct Waypoint {
        public Vector2 position;
        [Range(0, 360)]
        public float rotation;

        public Waypoint(Vector2 _position, float _rotation) {
            position = _position;
            rotation = _rotation;
        }
    }

    private void Start() {
        rb = GetComponent<Rigidbody>();
        ev = GetComponent<EnemyVision>();

        statusActionsDictionary = new Dictionary<Status, StatusActions>();
        statusActionsDictionary[Status.Normal] = new StatusActions(NormalUpdatePath, Move, NormalRotate);
        statusActionsDictionary[Status.Investigating] = new StatusActions(InvestigatingUpdatePath, Move, InvestigatingRotate);
        statusActionsDictionary[Status.Alerted] = new StatusActions(AlertedUpdatePath, Move, AlertedRotate);

        Physics.IgnoreCollision(parentCollider, childCollider);

        SetPathToGlobalWaypoint();
    }

    private void FixedUpdate() {
        StatusActions _statusActions = statusActionsDictionary[status];
        _statusActions.pathUpdate();
        _statusActions.move();
        _statusActions.rotate();
    }

    private void NormalUpdatePath() {
        bool _atDestination = rb.position == currentDestination;
        bool _atEndOfPath = currentPathIndex == currentPath.Length - 1;
        bool _hasOneWayPoint = globalWaypoints.Length == 1;

        if (!isMoving || !_atDestination) {
            return;
        }

        if (_atEndOfPath) {
            isMoving = false;
            if (_hasOneWayPoint) {
                return;
            }
            StartWaitCoroutine();
            return;
        }

        AdvanceCurrentPath();
        UpdateDestination();
    }

    private void InvestigatingUpdatePath() {
        bool _atDestination = rb.position == currentDestination;
        bool _atEndOfPath = currentPathIndex == currentPath.Length - 1;
        bool _hasOneWayPoint = globalWaypoints.Length == 1;

        if (!isMoving) {
            return;
        }

        if (_atEndOfPath) {
            if (_atDestination || Math2D.V3ToV3Dist(transform.position, currentDestination) < maxDistToEnd) {
                StartInvestigativeTurn();
                SetPathToGlobalWaypoint();
            }
            return;
        }

        if (_atDestination) {
            AdvanceCurrentPath();
            UpdateDestination();
        }
    }

    private void AlertedUpdatePath() {
        bool _atDestination = rb.position == currentDestination;
        bool _atEndOfPath = currentPathIndex == currentPath.Length - 1;
        bool _hasOneWayPoint = globalWaypoints.Length == 1;

        if (Math2D.V3ToV3Dist(transform.position, lastTargetLocation) < minDistToTarget) {
            isMoving = false;
            return;
        }

        if (!isMoving || !_atDestination || _atEndOfPath) {
            return;
        }

        AdvanceCurrentPath();
        UpdateDestination();
    }

    private void NormalRotate() {
        bool _atDestination = rb.position == currentDestination;

        if (!isMoving && _atDestination) {
            RotateTowardsAngle(globalWaypoints[globalWaypointsIndex].rotation);
        }
        else if (isMoving && !_atDestination) {
            RotateWithMovement();
        }
    }

    private void InvestigatingRotate() {
        if (!isMoving) {
            return;
        }

        SnapToTarget(lastTargetLocation);
    }

    private void AlertedRotate() {
        SnapToTarget(lastTargetLocation);
    }

    private void StartWaitCoroutine() {
        waitCoroutine = WaitOnWaypoint();
        StartCoroutine(waitCoroutine);
    }

    private IEnumerator WaitOnWaypoint() {
        yield return new WaitForSeconds(waitTime);
        isMoving = true;
        AdvanceGlobalPath();
        SetPathToGlobalWaypoint();
    }

    // Increment index of the global path
    private void AdvanceGlobalPath() {
        globalWaypointsIndex = (globalWaypointsIndex + 1) % globalWaypoints.Length;
    }

    // Increment index of the current local path
    private void AdvanceCurrentPath() {
        currentPathIndex++;
    }

    // Set new local path towards next waypoint in global path
    private void SetPathToGlobalWaypoint() {
        SetNewPath(globalWaypoints[globalWaypointsIndex].position);
    }

    // Set new local path
    private void SetNewPath(Vector2 _target, bool _constrained = false) {
        if (_constrained) {
            currentPath = Pathfinding.instance.FindPath(transform.position, Math2D.V2ToV3AtZero(_target), maxAlertedPathLength, ev.visionRange);
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
        if (!isMoving) {
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
        Quaternion _to = Quaternion.LookRotation((currentDestination - rb.position).normalized, Vector3.up);
        RotateSlowly(_to);
    }

    private void RotateTowardsAngle(float _angle) {
        Quaternion _to = Quaternion.Euler(0f, _angle, 0f);
        RotateSlowly(_to);
    }

    private void RotateSlowly(Quaternion _to) {
        Quaternion _from = rb.rotation;
        if (_to == _from) {
            return;
        }
        float _maxAngle = rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(_from, _to, _maxAngle));
    }

    // Set rotation to look at the player's last location
    private void SnapToTarget(Vector3 _targetPos) {
        rb.MoveRotation(Quaternion.LookRotation(Math2D.V3ToV3Dir(transform.position, _targetPos), Vector3.up));
    }

    // Method called when target is detected by EnemyVision component to set the status field and the last player position
    public void TargetDetected(bool _detected, Vector3 _targetLocation) {
        if (_detected) {
            if (waitCoroutine != null) {
                StopCoroutine(waitCoroutine);
            }

            Node _prevTargetNode = movementGrid.GetNodeFromWorldPos(Math2D.V2ToV3AtZero(CurrentPathEndPosition));
            Node _newTargetNode = movementGrid.GetNodeFromWorldPos(_targetLocation);

            if (!Alerted || (isMoving && _prevTargetNode != _newTargetNode)) {
                isMoving = true;
                SetNewPath(Math2D.V3ToV2(_targetLocation), true);
            }

            lastTargetLocation = _targetLocation;
            status = Status.Alerted;
        }
        else if (Alerted) {
            isMoving = true;
            SetNewPath(Math2D.V3ToV2(lastTargetLocation));
            status = Status.Investigating;
        }
        else if (Investigating) {
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
        while (_angleTurned < 360f && !isMoving) {
            yield return new WaitForSeconds(1 / _updateFreq);
            float _turnAngle = _turnSpeed / _updateFreq;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, _turnAngle, 0f));
            _angleTurned += _turnAngle;
        }
        status = Status.Normal;
        isMoving = true;
    }

    // Draw global path's waypoints for easier path definition
    private void OnDrawGizmos() {
        foreach (Waypoint _waypoint in globalWaypoints) {
            Vector3 _sphereCenter = Math2D.V2ToV3AtZero(_waypoint.position);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(_sphereCenter, 0.1f);
            Gizmos.DrawRay(_sphereCenter, Quaternion.Euler(0f, _waypoint.rotation, 0f) * Vector3.forward * 0.5f);
        }

        // Gizmos.DrawWireSphere(Math2D.V3AtZero(transform.position), maxAlertedPathLength);
    }
}
