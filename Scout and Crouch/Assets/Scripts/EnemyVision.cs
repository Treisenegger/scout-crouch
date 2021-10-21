using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Move rotation/movement to an enemy movement controller
public class EnemyVision : MonoBehaviour {

    struct ViewCastInfo {
        public bool hit;
        public Vector3 point;
        public float distance;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _distance, float _angle) {
            hit = _hit;
            point = _point;
            distance = _distance;
            angle = _angle;
        }
    }

    public float visionRange = 2f;
    [Range(0f, 360f)]
    public float visionAngle = 45f;
    public bool isAlerted = false;

    [SerializeField] LayerMask playerMask;
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float rotationSpeed = 45f;
    [SerializeField] float targetDetectionFreq = 0.2f;
    [SerializeField] int visionResolution = 10;
    [SerializeField] MeshFilter visionConeMeshFilter;
    Mesh visionConeMesh;

    private void Start() {
        visionConeMesh = new Mesh();
        visionConeMesh.name = "Vision Cone Mesh";
        visionConeMeshFilter.mesh = visionConeMesh;

        StartCoroutine(InitiatePlayerDetection());
    }

    private void LateUpdate() {
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

    private ViewCastInfo ViewCast(float _globalAngle) {
        Vector3 _dir = DirFromAngle(_globalAngle, true);
        RaycastHit _hit;

        bool _collided = Physics.Raycast(transform.position, _dir, out _hit, visionRange, obstacleMask);
        if (_collided) {
            return new ViewCastInfo(true, _hit.point, _hit.distance, _globalAngle);
        }
        else {
            return new ViewCastInfo(false, transform.position + _dir * visionRange, visionRange, _globalAngle);
        }
    }

    private void DrawVisionCone() {
        float _stepAngleSize = visionAngle / visionResolution;
        List<Vector3> _viewPoints = new List<Vector3>();

        for (int i = 0; i < visionResolution + 1; i++) {
            float _angle = transform.eulerAngles.y - (visionAngle / 2) + i * _stepAngleSize;
            ViewCastInfo newViewCast = ViewCast(_angle);
            _viewPoints.Add(newViewCast.point);
        }

        int _vertexCount = _viewPoints.Count + 1;
        Vector3[] _vertices = new Vector3[_vertexCount];
        int[] _triangles = new int[(_vertexCount - 2) * 3];
        _vertices[0] = Vector3.zero;

        for (int i = 0; i < _vertexCount - 1; i++) {
            _vertices[i + 1] = transform.InverseTransformPoint(_viewPoints[i]);

            if (i < _vertexCount - 2) {
                _triangles[3*i] = 0;
                _triangles[3*i + 1] = i + 1;
                _triangles[3*i + 2] = i + 2;
            }
        }

        visionConeMesh.Clear();
        visionConeMesh.vertices = _vertices;
        visionConeMesh.triangles = _triangles;
        visionConeMesh.RecalculateNormals();
    }

    public Vector3 DirFromAngle(float _angleInDegrees, bool _angleIsGlobal) {
        if (!_angleIsGlobal) {
            _angleInDegrees += transform.eulerAngles.y;
        }
        float _angleInRad = _angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(_angleInRad), 0f, Mathf.Cos(_angleInRad));
    }
}
