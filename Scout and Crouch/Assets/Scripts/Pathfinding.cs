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
    public Vector2[] FindPath(Vector3 _startPos, Vector3 _endPos, float _maxPathLength = -1f, float _maxDistToTarget = -1f, bool _preserveVisibility = false, bool _onlyContiguous = false) {
        Heap<Node> _openSet = new Heap<Node>(movementGrid.gridWidth * movementGrid.gridHeight);
        HashSet<Node> _closedSet = new HashSet<Node>();
        Node _startNode = movementGrid.GetNodeFromWorldPos(_startPos);
        Node _endNode = movementGrid.GetNodeFromWorldPos(_endPos);
        _openSet.Add(_startNode);
        _startNode.UpdateParameters(null, 0f, Vector3.Distance(_startNode.worldPos, _endNode.worldPos));

        while (_openSet.Count > 0) {
            Node _currentNode = _openSet.Pop();

            if (_maxPathLength > 0 && _currentNode.gCost > _maxPathLength) {
                continue;
            }

            if (_maxDistToTarget > 0 && _currentNode.hCost > _maxDistToTarget) {
                continue;
            }

            if (_preserveVisibility && !movementGrid.uprightEdges[_currentNode.gridPos.x, _currentNode.gridPos.y, _endNode.gridPos.x, _endNode.gridPos.y]) {
                continue;
            }

            if (_currentNode == _endNode) {
                return ReconstructPath(_startNode, _endNode, _endPos);
            }

            _closedSet.Add(_currentNode);

            foreach (Node _neighbour in GetNeighbours(_currentNode, _onlyContiguous)) {
                if (_closedSet.Contains(_neighbour)) {
                    continue;
                }
                bool _inOpenSet = _openSet.Contains(_neighbour);
                float _newGCost = _currentNode.gCost + Math2D.V3ToV3Dist(_currentNode.worldPos, _neighbour.worldPos);
                if (!_inOpenSet || _newGCost < _neighbour.gCost) {
                    _neighbour.UpdateParameters(_currentNode, _newGCost, Math2D.V3ToV3Dist(_neighbour.worldPos, _endNode.worldPos));

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

    private List<Node> GetNeighbours(Node _node, bool _onlyContiguous) {
        return _onlyContiguous ? GetContiguousNeighbours(_node) : GetReachableNeighbours(_node);
    }

    private List<Node> GetContiguousNeighbours(Node _node) {
        List<Node> _neighbours = new List<Node>();

        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i == 0 && j == 0) {
                    continue;
                }

                Vector2Int _otherGridPos = _node.gridPos + i * Vector2Int.right + j * Vector2Int.up;

                if (_otherGridPos.x < 0 || _otherGridPos.x >= movementGrid.gridWidth || _otherGridPos.y < 0 || _otherGridPos.y >= movementGrid.gridHeight) {
                    continue;
                }

                Node _otherNode = movementGrid.nodes[_otherGridPos.x, _otherGridPos.y];

                if (movementGrid.crouchEdges[_node.gridPos.x, _node.gridPos.y, _otherNode.gridPos.x, _otherNode.gridPos.y]) {
                    _neighbours.Add(_otherNode);
                }
            }
        }

        return _neighbours;
    }

    private List<Node> GetReachableNeighbours(Node _node) {
        List<Node> _neighbours = new List<Node>();

        foreach (Node _otherNode in movementGrid.nodes) {
            if (movementGrid.crouchEdges[_node.gridPos.x, _node.gridPos.y, _otherNode.gridPos.x, _otherNode.gridPos.y]) {
                _neighbours.Add(_otherNode);
            }
        }

        return _neighbours;
    }
}
