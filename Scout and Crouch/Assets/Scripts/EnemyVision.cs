using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class EnemyVision : MonoBehaviour {

    [Header("Vision Geometry Parameters")]
    public float visionRange = 4f;
    [Range(0f, 360f)]
    [SerializeField] float visionAngle = 45f;

    [Header("Collision Parameters")]
    [SerializeField] LayerMask playerMask;
    [SerializeField] LayerMask obstacleMask;

    [Header("Vision Cone Detection Parameters")]
    [SerializeField] float targetDetectionFreq = 30f;
    [SerializeField] FloatVariable crouchHeight;
    [SerializeField] FloatVariable uprightHeight;

    [Header("Vision Cone Rendering Parameters")]
    [SerializeField] float visionResolution = 1f;
    [SerializeField] int edgeIdIterations = 6;
    [SerializeField] float maxRaycastDst = 0.5f;
    [SerializeField] Color defaultColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] Color alertedColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] MeshFilter crouchMeshFilter;
    [SerializeField] Transform crouchConeBase;
    [SerializeField] MeshFilter uprightMeshFilter;
    [SerializeField] Transform uprightConeBase;

    Mesh crouchConeMesh;
    Mesh uprightConeMesh;
    bool isAlerted = false;
    EnemyController ec;
    MeshRenderer crouchMeshRenderer;
    MeshRenderer uprightMeshRenderer;

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

    struct PointPair {
        public Vector3 pointA;
        public Vector3 pointB;

        public PointPair(Vector3 _pointA, Vector3 _pointB) {
            pointA = _pointA;
            pointB = _pointB;
        }
    }

    struct MeshInfo {
        public Vector3[] vertices;
        public int[] triangles;

        public MeshInfo(Vector3[] _vertices, int[] _triangles) {
            vertices = _vertices;
            triangles = _triangles;
        }
    }

    private void Start() {
        ec = GetComponent<EnemyController>();
        SetupConeMesh(ref crouchConeMesh, ref crouchMeshRenderer, crouchMeshFilter, "Crouch Vision Cone Mesh");
        SetupConeMesh(ref uprightConeMesh, ref uprightMeshRenderer, uprightMeshFilter, "Upright Vision Cone Mesh");
        StartCoroutine(InitiatePeriodicAction(DetectTargets, targetDetectionFreq));
    }

    private void LateUpdate() {
        DrawVisionCone(crouchConeMesh, crouchHeight.Value, crouchConeBase);
        DrawVisionCone(uprightConeMesh, uprightHeight.Value, uprightConeBase);
    }

    private void SetupConeMesh(ref Mesh _coneMesh, ref MeshRenderer _coneMeshRenderer, MeshFilter _coneMeshFilter, string _name) {
        _coneMesh = new Mesh();
        _coneMesh.name = _name;
        _coneMeshFilter.mesh = _coneMesh;
        _coneMeshRenderer = _coneMeshFilter.GetComponent<MeshRenderer>();
    }

    private IEnumerator InitiatePeriodicAction(Action _action, float _freq) {
        while (true) {
            _action();
            yield return new WaitForSeconds(1 / _freq);
        }
    }

    private void DetectTargets() {
        // Determine if a player is in radius
        Collider[] _detectedPlayers = Physics.OverlapSphere(transform.position, visionRange, playerMask);
        if (_detectedPlayers.Length == 0) {
            SetAlerted(false);
            return;
        }

        // Determine if player is in cone
        GameObject _player = _detectedPlayers[0].gameObject;
        Vector3 _directionToPlayer = Math2D.V3ToV3Dir(transform.position, _player.transform.position);
        float _angleToPlayer = Mathf.Abs(Vector3.Angle(transform.forward, _directionToPlayer));
        if (_angleToPlayer > visionAngle / 2) {
            SetAlerted(false);
            return;
        }

        // Determine if player is occluded
        float _distanceToPlayer = Math2D.V3ToV3Dist(transform.position, _player.transform.position);
        bool _occluded = RaycastAtHeight(crouchHeight.Value, _directionToPlayer, _distanceToPlayer, obstacleMask) &&
                (RaycastAtHeight(uprightHeight.Value, _directionToPlayer, _distanceToPlayer, obstacleMask) ||
                !RaycastAtHeight(uprightHeight.Value, _directionToPlayer, _distanceToPlayer, playerMask));

        if (_occluded) {
            SetAlerted(false);
            return;
        }

        SetAlerted(true, _player.transform.position);
    }

    private void SetAlerted(bool _isAlerted) {
        SetAlerted(_isAlerted, Vector3.zero);
    }

    private void SetAlerted(bool _isAlerted, Vector3 _position) {
        Color _coneColor = _isAlerted ? alertedColor : defaultColor;

        isAlerted = _isAlerted;
        crouchMeshRenderer.material.SetColor("_Color", _coneColor);
        uprightMeshRenderer.material.SetColor("_Color", _coneColor);
        ec.TargetDetected(_isAlerted, _position);
    }

    private bool RaycastAtHeight(float _height, Vector3 _dir, float _dist, LayerMask _layerMask) {
        return Physics.Raycast(Math2D.V3AtHeight(transform.position, _height), _dir, _dist, _layerMask);
    }

    private void DrawVisionCone(Mesh _coneMesh, float _height, Transform _coneBase) {
        int _stepCount = Mathf.RoundToInt(visionAngle * visionResolution);
        float _stepAngleSize = visionAngle / _stepCount;
        ViewCastInfo _oldViewCast = new ViewCastInfo();
        List<Vector3> _viewPoints = new List<Vector3>();

        for (int i = 0; i < _stepCount + 1; i++) {
            float _angle = transform.eulerAngles.y - (visionAngle / 2) + i * _stepAngleSize;
            ViewCastInfo _newViewCast = ViewCast(_angle, _height);

            if (i > 0) {
                bool _maxDstExceeded = Mathf.Abs(_oldViewCast.distance - _newViewCast.distance) > maxRaycastDst;
                if (_newViewCast.hit != _oldViewCast.hit || (_oldViewCast.hit && _newViewCast.hit && _maxDstExceeded)) {
                    PointPair _edgePoints = FindEdge(_oldViewCast, _newViewCast, _height);
                    _viewPoints.Add(_edgePoints.pointA);
                    _viewPoints.Add(_edgePoints.pointB);
                }
            }

            _viewPoints.Add(_newViewCast.point);
            _oldViewCast = _newViewCast;
        }

        MeshInfo _meshInfo = GenerateMeshInfo(_viewPoints, _coneBase);
        UpdateConeMesh(_coneMesh, _meshInfo);
    }

    private PointPair FindEdge(ViewCastInfo _vcInfoA, ViewCastInfo _vcInfoB, float _height) {
        ViewCastInfo _vcMin = _vcInfoA;
        ViewCastInfo _vcMax = _vcInfoB;

        for (int i = 0; i < edgeIdIterations; i++) {
            ViewCastInfo _vcMid = ViewCast((_vcMin.angle + _vcMax.angle) / 2, _height);
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

    private ViewCastInfo ViewCast(float _globalAngle, float _height) {
        Vector3 _dir = DirFromAngle(_globalAngle, true);
        RaycastHit _hit;

        bool _collided = Physics.Raycast(Math2D.V3AtHeight(transform.position, _height), _dir, out _hit, visionRange, obstacleMask);
        if (_collided) {
            return new ViewCastInfo(true, Math2D.V3AtHeight(_hit.point, transform.position.y), _hit.distance, _globalAngle);
        }
        else {
            return new ViewCastInfo(false, transform.position + _dir * visionRange, visionRange, _globalAngle);
        }
    }

    private MeshInfo GenerateMeshInfo(List<Vector3> _viewPoints, Transform _coneBase) {
        int _vertexCount = _viewPoints.Count + 1;
        Vector3[] _vertices = new Vector3[_vertexCount];
        int[] _triangles = new int[(_vertexCount - 2) * 3];
        _vertices[0] = _coneBase.localPosition;

        for (int i = 0; i < _vertexCount - 1; i++) {
            _vertices[i + 1] = transform.InverseTransformPoint(_viewPoints[i]) + _coneBase.localPosition;

            if (i < _vertexCount - 2) {
                _triangles[3*i] = 0;
                _triangles[3*i + 1] = i + 1;
                _triangles[3*i + 2] = i + 2;
            }
        }

        return new MeshInfo(_vertices, _triangles);
    }

    private void UpdateConeMesh(Mesh _coneMesh, MeshInfo _meshInfo) {
        _coneMesh.Clear();
        _coneMesh.vertices = _meshInfo.vertices;
        _coneMesh.triangles = _meshInfo.triangles;
        _coneMesh.RecalculateNormals();
    }

    public Vector3 DirFromAngle(float _angleInDegrees, bool _angleIsGlobal) {
        if (!_angleIsGlobal) {
            _angleInDegrees += transform.eulerAngles.y;
        }

        float _angleInRad = _angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(_angleInRad), 0f, Mathf.Cos(_angleInRad));
    }
}
