using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Move rotation/movement to an enemy movement controller
public class EnemyVision : MonoBehaviour {
    public float visionRange = 2f;
    [Range(0f, 360f)]
    public float visionAngle = 45f;
    public bool isAlerted = false;

    [SerializeField] LayerMask playerMask;
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float rotationSpeed = 45f;
    [SerializeField] float targetDetectionFreq = 0.2f;
    [SerializeField] int visionResolution = 10;

    private void Start() {
        StartCoroutine(InitiatePlayerDetection());
    }

    private void Update() {
        DrawVisionCone();
    }

    private void FixedUpdate() {
        transform.Rotate(new Vector3(0f, rotationSpeed * Time.fixedDeltaTime, 0f));
    }

    private IEnumerator InitiatePlayerDetection() {
        while (true) {
            DetectTargets();
            yield return new WaitForSecondsRealtime(targetDetectionFreq);
        }
    }

    private void DetectTargets() {
        // Determine if a player is in radius
        Collider[] _detectedPlayers = Physics.OverlapSphere(transform.position, visionRange, playerMask);
        if (_detectedPlayers.Length == 0) {
            isAlerted = false;
            return;
        }

        // Determine if player is in cone
        GameObject _player = _detectedPlayers[0].gameObject;
        Vector3 _directionToPlayer = (_player.transform.position - transform.position).normalized;
        float _angleToPlayer = Mathf.Abs(Vector3.Angle(transform.forward, _directionToPlayer));
        if (_angleToPlayer > visionAngle / 2) {
            isAlerted = false;
            return;
        }

        // Determine if player is occluded
        float _distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        bool _occluded = Physics.Raycast(transform.position, _directionToPlayer, _distanceToPlayer, obstacleMask);
        if (_occluded) {
            isAlerted = false;
        }
        else {
            isAlerted = true;
        }
    }

    private void DrawVisionCone() {
        float _stepAngleSize = visionAngle / visionResolution;

        for (int i = 0; i < visionResolution + 1; i++) {
            float _angle = transform.eulerAngles.y - (visionAngle / 2) + i * _stepAngleSize;
            Color _color = isAlerted ? Color.red : Color.white;
            Debug.DrawLine(transform.position, transform.position + DirFromAngle(_angle, true) * visionRange, _color);
        }
    }

    public Vector3 DirFromAngle(float _angleInDegrees, bool _angleIsGlobal) {
        if (!_angleIsGlobal) {
            _angleInDegrees += transform.eulerAngles.y;
        }
        float _angleInRad = _angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(_angleInRad), 0f, Mathf.Cos(_angleInRad));
    }
}
