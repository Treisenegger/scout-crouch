using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtifactHandler : MonoBehaviour {
    [SerializeField] int maxBugleBounces = 3;
    [SerializeField] float maxRaycastDist = 10f;
    [SerializeField] FloatVariable uprightHeight;
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] MovementGrid movementGrid;
    
    bool selectedArtifact = true;
    Plane artifactPlane;

    private void Start() {
        artifactPlane = new Plane(Vector3.up, -uprightHeight.Value);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            selectedArtifact = !selectedArtifact;
        }

        if (selectedArtifact) {
            BugleDrawTrajectory();
        }
        else {
            OudDrawTrajectory();
        }
    }

    private void OudDrawTrajectory() {
        UpdateLineRenderer(GetTrajectoryToMouse(0, maxRaycastDist));
    }

    private void BugleDrawTrajectory() {
        UpdateLineRenderer(GetTrajectoryToMouse(maxBugleBounces, maxRaycastDist));
    }

    private List<Vector3> GetTrajectoryToMouse(int _maxBounces = 0, float _maxDist = 0f) {
        Vector3 _mousePos = GetMousePosition();
        Vector3 _dir = Math2D.V3ToV3Dir(transform.position, _mousePos);
        return GetBouncingTrajectory(Math2D.V3AtHeight(transform.position, uprightHeight.Value), _dir, _maxBounces, _maxDist);
    }

    private List<Vector3> GetBouncingTrajectory(Vector3 _startPos, Vector3 _dir, int _maxBounces = 0, float _maxDist = 0f) {
        List<Vector3> _linePoints = new List<Vector3>();
        _linePoints.Add(_startPos);
        Ray _ray = new Ray(_startPos, _dir);
        RaycastHit _hit;

        if (!Physics.Raycast(_ray, out _hit, _maxDist, obstacleMask)) {
            _linePoints.Add(_ray.GetPoint(_maxDist));
            return _linePoints;
        }

        _linePoints.Add(_hit.point);
        float _remainingDist = _maxDist - _hit.distance;
        int _remainingBounces = _maxBounces;

        while (_remainingBounces > 0) {
            _dir = _dir + 2 * Vector3.Dot(-_dir, _hit.normal) * _hit.normal;
            _ray = new Ray(_hit.point, _dir);
            if (!Physics.Raycast(_ray, out _hit, _remainingDist, obstacleMask)) {
                _linePoints.Add(_ray.GetPoint(_remainingDist));
                return _linePoints;
            }
            _linePoints.Add(_hit.point);
            _remainingDist -= _hit.distance;
            _remainingBounces--;
        }

        return _linePoints;

    }

    private Vector3 GetMousePosition() {
        float _dist;
        Ray _ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (artifactPlane.Raycast(_ray, out _dist)) {
            return _ray.GetPoint(_dist);
        }
        return Vector3.zero;
    }

    private void UpdateLineRenderer(List<Vector3> _linePoints) {
        lineRenderer.positionCount = _linePoints.Count;
        lineRenderer.SetPositions(_linePoints.ToArray());
    }
}
