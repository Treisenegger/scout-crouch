using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyVision))]
public class EnemyMovement : MonoBehaviour {
    [Header("Movement Parameters")]
    [SerializeField] float movementSpeed = 6f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Path Parameters")]
    [SerializeField] Vector2[] pathPoints;
    [SerializeField] int currentPathIndex = 0;

    Rigidbody rb;
    EnemyVision ev;
    Vector3 velocityDir;
    Vector3 currentDestination;

    private void Start() {
        rb = GetComponent<Rigidbody>();

        SetNewDestination();
    }

    private void FixedUpdate() {
        float _distance = Vector3.Distance(transform.position, currentDestination);
        bool _inRangeOfDestination = _distance < movementSpeed * Time.fixedDeltaTime;

        if (_inRangeOfDestination) {
            rb.MovePosition(currentDestination);
            currentPathIndex = (currentPathIndex + 1) % pathPoints.Length;
            SetNewDestination();
        }
        else {
            Move();
        }

        Rotate();
    }

    // Determine next destination in path
    private void SetNewDestination() {
        // Set new destination
        Vector2 _nextPoint = pathPoints[currentPathIndex];
        currentDestination = new Vector3(_nextPoint.x, transform.position.y, _nextPoint.y);

        velocityDir = (currentDestination - transform.position).normalized;
    }

    // Move towards current destination
    private void Move() {
        Vector3 _movementVector = velocityDir * movementSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + _movementVector);
    }
    
    // Set rotation in line with movement direction
    private void Rotate() {
        if (transform.forward == velocityDir) {
            return;
        }

        float _angle = Vector3.SignedAngle(transform.forward, velocityDir, Vector3.up);
        bool _inRangeOfRotation = Mathf.Abs(_angle) < rotationSpeed * Time.fixedDeltaTime;

        if (_inRangeOfRotation) {
            transform.forward = velocityDir;
        }
        else {
            rb.MoveRotation(transform.rotation * Quaternion.Euler(0f, Mathf.Sign(_angle) * rotationSpeed * Time.fixedDeltaTime, 0f));
        }
    }

    // Visualize enemy path
    private void OnDrawGizmos() {
        foreach (Vector2 _point in pathPoints) {
            Vector3 _sphereCenter = new Vector3(_point.x, 0f, _point.y);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_sphereCenter, 0.1f);
        }
    }
}
