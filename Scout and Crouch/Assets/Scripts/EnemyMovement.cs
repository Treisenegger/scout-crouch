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
        ev = GetComponent<EnemyVision>();

        SetNewDestination();
    }

    private void FixedUpdate() {
        if (rb.position == currentDestination) {
            currentPathIndex = (currentPathIndex + 1) % pathPoints.Length;
            SetNewDestination();
        }
        
        Move();
        Rotate();
    }

    // Determine next destination in path
    private void SetNewDestination() {
        Vector2 _nextPoint = pathPoints[currentPathIndex];
        currentDestination = new Vector3(_nextPoint.x, transform.position.y, _nextPoint.y);
        velocityDir = (currentDestination - transform.position).normalized;
    }

    // Move towards current destination
    private void Move() {
        rb.MovePosition(Vector3.MoveTowards(rb.position, currentDestination, movementSpeed * Time.fixedDeltaTime));
    }
    
    // Set rotation in line with movement direction
    private void Rotate() {
        if (transform.forward == velocityDir) {
            return;
        }
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, Quaternion.LookRotation(velocityDir), rotationSpeed * Time.fixedDeltaTime));
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
