using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(MovementGrid))]
public class Pathfinding : MonoBehaviour {
    MovementGrid movementGrid;

    private void Start() {
        movementGrid = GetComponent<MovementGrid>();
    }

    // TODO: implement max distance search
    public Vector2[] FindPath(Vector3 _startPos, Vector3 _endPos, float _maxDist) {
        Heap<Node> _openSet = new Heap<Node>(movementGrid.gridWidth * movementGrid.gridHeight);
        HashSet<Node> _closedSet = new HashSet<Node>();
        Node _startNode = movementGrid.GetNodeFromWorldPos(_startPos);
        Node _endNode = movementGrid.GetNodeFromWorldPos(_endPos);
        _openSet.Add(_startNode);

        _startNode.parent = null;
        _startNode.gCost = 0f;
        _startNode.hCost = Vector3.Distance(_startNode.worldPos, _endNode.worldPos);

        while (_openSet.Count > 0) {
            Node _currentNode = _openSet.Pop();

            if (_currentNode == _endNode) {
                return ReconstructPath(_endNode);
            }

            _closedSet.Add(_currentNode);

            foreach (Node _neighbour in movementGrid.nodes) {
                if (!movementGrid.edges[_currentNode.gridPos.x, _currentNode.gridPos.y, _neighbour.gridPos.x, _neighbour.gridPos.y]) {
                    continue;
                }
                if (_closedSet.Contains(_neighbour)) {
                    continue;
                }
                bool _inOpenSet = _openSet.Contains(_neighbour);
                float _newGCost = _currentNode.gCost + Vector3.Distance(_currentNode.worldPos, _neighbour.worldPos);
                if (!_inOpenSet || _newGCost < _neighbour.gCost) {
                    _neighbour.parent = _currentNode;
                    _neighbour.gCost = _newGCost;
                    _neighbour.hCost = Vector3.Distance(_neighbour.worldPos, _endNode.worldPos);

                    if (!_inOpenSet) {
                        _openSet.Add(_neighbour);
                    }
                    else {
                        _openSet.UpdatePriority(_neighbour);
                    }
                }
            }
        }

        return new Vector2[0];
    }

    private Vector2[] ReconstructPath(Node _endNode) {
        List<Vector2> _pathList = new List<Vector2>();
        Node _currentNode = _endNode;

        while (_currentNode != null) {
            _pathList.Add(new Vector2(_currentNode.worldPos.x, _currentNode.worldPos.z));
            _currentNode = _currentNode.parent;
        }

        Vector2[] _pathArray = new Vector2[_pathList.Count];
        _pathArray = _pathList.ToArray();
        Array.Reverse(_pathArray);

        return _pathArray;
    }
}
