using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

// TODO: - Implement pathfinding with several restrictions: with max path length, that preserves max distance from target
//          and that preserves line of sight towards target 
[RequireComponent(typeof(MovementGrid))]
public class Pathfinding : MonoBehaviour {

    public static Pathfinding instance;
    MovementGrid movementGrid;

    // Implement simple singleton
    private void Awake() {
        instance = this;
        movementGrid = GetComponent<MovementGrid>();
    }

    // Find shortest path between two nodes based on the adjacency matrix
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
                return ReconstructPath(_startNode, _endNode, _endPos);
            }

            _closedSet.Add(_currentNode);

            foreach (Node _neighbour in movementGrid.nodes) {
                if (!movementGrid.crouchEdges[_currentNode.gridPos.x, _currentNode.gridPos.y, _neighbour.gridPos.x, _neighbour.gridPos.y]) {
                    continue;
                }
                if (_closedSet.Contains(_neighbour)) {
                    continue;
                }
                bool _inOpenSet = _openSet.Contains(_neighbour);
                float _newGCost = _currentNode.gCost + Math2D.V3ToV3Dist(_currentNode.worldPos, _neighbour.worldPos);
                if (!_inOpenSet || _newGCost < _neighbour.gCost) {
                    _neighbour.parent = _currentNode;
                    _neighbour.gCost = _newGCost;
                    _neighbour.hCost = Math2D.V3ToV3Dist(_neighbour.worldPos, _endNode.worldPos);

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

    // Create a 2D position array based on the found path
    private Vector2[] ReconstructPath(Node _startNode, Node _endNode, Vector3 _endPos) {
        List<Vector2> _pathList = new List<Vector2>();
        _pathList.Add(Math2D.V3ToV2(_endPos));
        Node _currentNode = _endNode;

        while (_currentNode != null) {
            if (_currentNode != _startNode && _currentNode != _endNode) {
                _pathList.Add(Math2D.V3ToV2(_currentNode.worldPos));
            }
            _currentNode = _currentNode.parent;
        }

        Vector2[] _pathArray = _pathList.ToArray();
        Array.Reverse(_pathArray);

        return _pathArray;
    }
}
