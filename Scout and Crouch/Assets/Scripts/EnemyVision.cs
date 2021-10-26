using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class EnemyVision : MonoBehaviour {

    [Header("Vision Geometry Parameters")]
    [SerializeField] float visionRange = 4f;
    [Range(0f, 360f)]
    [SerializeField] float visionAngle = 45f;

    [Header("Collision Parameters")]
    [SerializeField] LayerMask playerMask;
    [SerializeField] LayerMask obstacleMask;

    [Header("Vision Cone Detection Parameters")]
    [SerializeField] float targetDetectionFreq = 0.2f;

    [Header("Vision Cone Rendering Parameters")]
    [SerializeField] float visionResolution = 1f;
    [SerializeField] int edgeIdIterations = 6;
    [SerializeField] float maxRaycastDst = 0.5f;
    [SerializeField] Color defaultColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] Color alertedColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] MeshFilter visionConeMeshFilter;
    [SerializeField] Transform visionConeBase;
    [SerializeField] Material meshMaterial;

    Mesh visionConeMesh;
    bool isAlerted = false;
    EnemyController ec;

    private void Start() {
        ec = GetComponent<EnemyController>();

        visionConeMesh = new Mesh();
        visionConeMesh.name = "Vision Cone Mesh";
        visionConeMeshFilter.mesh = visionConeMesh;

        StartCoroutine(InitiatePlayerDetection());
    }

    private void LateUpdate() {
        DrawVisionCone();
    }

    public Vector3 DirFromAngle(float _angleInDegrees, bool _angleIsGlobal) {
        if (!_angleIsGlobal) {
            _angleInDegrees += transform.eulerAngles.y;
        }
        float _angleInRad = _angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(_angleInRad), 0f, Mathf.Cos(_angleInRad));
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
            ec.TargetDetected(false, Vector3.zero);
            SetAlerted(false);
            return;
        }

        // Determine if player is in cone
        GameObject _player = _detectedPlayers[0].gameObject;
        Vector3 _directionToPlayer = (_player.transform.position - transform.position).normalized;
        float _angleToPlayer = Mathf.Abs(Vector3.Angle(transform.forward, _directionToPlayer));
        if (_angleToPlayer > visionAngle / 2) {
            ec.TargetDetected(false, Vector3.zero);
            SetAlerted(false);
            return;
        }

        // Determine if player is occluded
        float _distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        bool _occluded = Physics.Raycast(transform.position, _directionToPlayer, _distanceToPlayer, obstacleMask);
        if (_occluded) {
            ec.TargetDetected(false, Vector3.zero);
            SetAlerted(false);
        }
        else {
            ec.TargetDetected(true, _player.transform.position);
            SetAlerted(true);
        }
    }

    private void SetAlerted(bool _isAlerted) {
        isAlerted = _isAlerted;
        meshMaterial.SetColor("_Color", isAlerted ? alertedColor : defaultColor);
    }

    private void DrawVisionCone() {
        int _stepCount = Mathf.RoundToInt(visionAngle * visionResolution);
        float _stepAngleSize = visionAngle / _stepCount;
        ViewCastInfo _oldViewCast = new ViewCastInfo();
        List<Vector3> _viewPoints = new List<Vector3>();

        for (int i = 0; i < _stepCount + 1; i++) {
            float _angle = transform.eulerAngles.y - (visionAngle / 2) + i * _stepAngleSize;
            ViewCastInfo _newViewCast = ViewCast(_angle);

            if (i > 0) {
                bool _maxDstExceeded = Mathf.Abs(_oldViewCast.distance - _newViewCast.distance) > maxRaycastDst;
                if (_newViewCast.hit != _oldViewCast.hit || (_oldViewCast.hit && _newViewCast.hit && _maxDstExceeded)) {
                    PointPair _edgePoints = FindEdge(_oldViewCast, _newViewCast);
                    _viewPoints.Add(_edgePoints.pointA);
                    _viewPoints.Add(_edgePoints.pointB);
                }
            }

            _viewPoints.Add(_newViewCast.point);
            _oldViewCast = _newViewCast;
        }

        int _vertexCount = _viewPoints.Count + 1;
        Vector3[] _vertices = new Vector3[_vertexCount];
        int[] _triangles = new int[(_vertexCount - 2) * 3];
        // Debug.Log(visionConeBase.position);
        _vertices[0] = visionConeBase.localPosition;

        for (int i = 0; i < _vertexCount - 1; i++) {
            _vertices[i + 1] = transform.InverseTransformPoint(_viewPoints[i]) + visionConeBase.localPosition;
            // Debug.DrawLine(transform.position, _viewPoints[i], Color.black);

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

    private PointPair FindEdge(ViewCastInfo _vcInfoA, ViewCastInfo _vcInfoB) {
        ViewCastInfo _vcMin = _vcInfoA;
        ViewCastInfo _vcMax = _vcInfoB;

        for (int i = 0; i < edgeIdIterations; i++) {
            ViewCastInfo _vcMid = ViewCast((_vcMin.angle + _vcMax.angle) / 2);
                bool _maxDstExceeded = Mathf.Abs(_vcMin.distance - _vcMid.distance) > maxRaycastDst;
            if (_vcMid.hit == _vcMin.hit && !_maxDstExceeded) {
                _vcMin = _vcMid;
            }
            else {
                _vcMax = _vcMid;
            }
        }

        return new PointPair(_vcMin.point, _vcMax.point);
    }

    struct PointPair {
        public Vector3 pointA;
        public Vector3 pointB;

        public PointPair(Vector3 _pointA, Vector3 _pointB) {
            pointA = _pointA;
            pointB = _pointB;
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
}
