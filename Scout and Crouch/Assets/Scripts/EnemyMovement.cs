using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour {
    [Header("Movement Parameters")]
    [SerializeField] float movementSpeed = 6f;
    [SerializeField] float rotationSpeed = 10f;

    [Header("Path Parameters")]
    [SerializeField] Transform[] pathPoints;
    [SerializeField] int currentPathIndex = 0;

    Rigidbody rb;
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

    private void SetNewDestination() {
        // Set new destination
        currentDestination = pathPoints[currentPathIndex].position;
        currentDestination.y = transform.position.y; // Make sure path points are at same height as enemy

        velocityDir = (currentDestination - transform.position).normalized;
    }

    private void Move() {
        Vector3 _movementVector = velocityDir * movementSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + _movementVector);
    }
    
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
}
