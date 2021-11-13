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
        artifactPlane = new Plane(Vector3.up, Math2D.V3AtHeight(Vector3.zero, uprightHeight.Value));
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
        Vector3 _mousePos = GetMousePosition();
        List<Vector3> _linePoints = new List<Vector3>();
        _linePoints.Add(Math2D.V3AtHeight(transform.position, uprightHeight.Value));

        Ray _ray = new Ray(Math2D.V3AtHeight(transform.position, uprightHeight.Value), Math2D.V3ToV3Dir(transform.position, _mousePos));
        RaycastHit _hit;
        if (Physics.Raycast(_ray, out _hit, maxRaycastDist, obstacleMask)) {
            _linePoints.Add(_hit.point);
        }
        else {
            _linePoints.Add(_ray.GetPoint(maxRaycastDist));
        }

        UpdateLineRenderer(_linePoints);
    }

    private void BugleDrawTrajectory() {
        Vector3 _mousePos = GetMousePosition();
        List<Vector3> _linePoints = new List<Vector3>();
        _linePoints.Add(Math2D.V3AtHeight(transform.position, uprightHeight.Value));

        Vector3 _dir = Math2D.V3ToV3Dir(transform.position, _mousePos);
        Ray _ray = new Ray(Math2D.V3AtHeight(transform.position, uprightHeight.Value), _dir);
        RaycastHit _hit;

        if (!Physics.Raycast(_ray, out _hit, maxRaycastDist, obstacleMask)) {
            _linePoints.Add(_ray.GetPoint(maxRaycastDist));
            UpdateLineRenderer(_linePoints);
            return;
        }

        _linePoints.Add(_hit.point);
        float _remainingDist = maxRaycastDist - _hit.distance;
        int _remainingBounces = maxBugleBounces;

        while (_remainingBounces > 0) {
            _dir = _dir + 2 * Vector3.Dot(-_dir, _hit.normal) * _hit.normal;
            _ray = new Ray(_hit.point, _dir);
            if (!Physics.Raycast(_ray, out _hit, _remainingDist, obstacleMask)) {
                _linePoints.Add(_ray.GetPoint(_remainingDist));
                break;
            }
            _linePoints.Add(_hit.point);
            _remainingDist -= _hit.distance;
            _remainingBounces--;
        }

        UpdateLineRenderer(_linePoints);
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
