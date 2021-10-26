using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MovementGrid : MonoBehaviour {
    // // only for testing, have to remove later
    [SerializeField] PlayerMovement player;
    [SerializeField] EnemyController enemy;
    [SerializeField] Pathfinding pathfinding;

    [SerializeField] float width = 1f;
    [SerializeField] float height = 1f;
    [SerializeField] float nodeWidth = 1f;
    [SerializeField] float walkingHeightDetection = 0.1f; // might be better to implement this variable universally
    [Range(0, 1)]
    [SerializeField] float lineOfSightPrecision = 0.5f;
    [SerializeField] LayerMask obstacleMask;
    
    [HideInInspector] public Node[,] nodes;
    [HideInInspector] public bool[,,,] edges;
    [HideInInspector] public int gridWidth, gridHeight;

    float realNodeWidth, realNodeHeight;

    private void Awake() {
        gridWidth = Mathf.RoundToInt(width / nodeWidth);
        gridHeight = Mathf.RoundToInt(height / nodeWidth);
        realNodeWidth = width / gridWidth;
        realNodeHeight = height / gridHeight;
        InitializeGrid();
        InitializeEdgeInfo();
    }

    // Create nodes from grid parameters
    private void InitializeGrid() {
        nodes = new Node[gridWidth, gridHeight];
        Vector3 _bottomLeftWorldPos = transform.position - Vector3.right * width / 2 - Vector3.forward * height / 2;

        for (int i = 0; i < gridWidth; i++) {
            for (int j = 0; j < gridHeight; j++) {
                Vector3 _worldPos = _bottomLeftWorldPos + Vector3.right * (realNodeWidth * i + realNodeWidth / 2) + Vector3.forward * (realNodeHeight * j + realNodeHeight / 2);
                Vector2Int _gridPos = new Vector2Int(i, j);
                Node _newNode = new Node(_worldPos, _gridPos);
                nodes[i, j] = _newNode;
            }
        }
    }

    // Determine node adjacency based on line of sight
    private void InitializeEdgeInfo() {
        edges = new bool[gridWidth, gridHeight, gridWidth, gridHeight];
        Vector3 _precisionLOSDisplacement1 = (Vector3.forward + Vector3.right).normalized * lineOfSightPrecision;
        Vector3 _precisionLOSDisplacement2 = (-Vector3.forward + Vector3.right).normalized * lineOfSightPrecision;
        Vector3 _precisionLOSDisplacement3 = -_precisionLOSDisplacement1;
        Vector3 _precisionLOSDisplacement4 = -_precisionLOSDisplacement2;

        foreach (Node _node1 in nodes) {
            foreach (Node _node2 in nodes) {
                if (_node2.gridPos.x >= _node1.gridPos.x && _node2.gridPos.y >= _node1.gridPos.y) {
                    continue;
                }

                Vector3 _originPos = _node1.worldPos + Vector3.up * walkingHeightDetection;
                Vector3 _targetDir = (_node2.worldPos - _node1.worldPos).normalized;
                float _nodeDist = Vector3.Distance(_node1.worldPos, _node2.worldPos);
                bool _visible = true;
                
                if (_visible) {
                    _visible = !Physics.Raycast(_originPos + _precisionLOSDisplacement1, _targetDir, _nodeDist, obstacleMask);
                }
                if (_visible) {
                    _visible = !Physics.Raycast(_originPos + _precisionLOSDisplacement2, _targetDir, _nodeDist, obstacleMask);
                }
                if (_visible) {
                    _visible = !Physics.Raycast(_originPos + _precisionLOSDisplacement3, _targetDir, _nodeDist, obstacleMask);
                }
                if (_visible) {
                    _visible = !Physics.Raycast(_originPos + _precisionLOSDisplacement4, _targetDir, _nodeDist, obstacleMask);
                }

                if (_visible) {
                    edges[_node1.gridPos.x, _node1.gridPos.y, _node2.gridPos.x, _node2.gridPos.y] = true;
                    edges[_node2.gridPos.x, _node2.gridPos.y, _node1.gridPos.x, _node1.gridPos.y] = true;
                }
                else {
                    edges[_node1.gridPos.x, _node1.gridPos.y, _node2.gridPos.x, _node2.gridPos.y] = false;
                    edges[_node2.gridPos.x, _node2.gridPos.y, _node1.gridPos.x, _node1.gridPos.y] = false;
                }
            }
        }
    }

    // Returns node reference from world position
    public Node GetNodeFromWorldPos(Vector3 _worldPos) {
        float _relativeX = Mathf.InverseLerp(transform.position.x - width / 2 + realNodeWidth / 2, transform.position.x + width / 2 - realNodeWidth / 2, _worldPos.x);
        float _relativeY = Mathf.InverseLerp(transform.position.z - width / 2 + realNodeHeight / 2, transform.position.z + width / 2 - realNodeHeight / 2, _worldPos.z);
        int _gridX = Mathf.RoundToInt(Mathf.Lerp(0f, gridWidth - 1, _relativeX));
        int _gridY = Mathf.RoundToInt(Mathf.Lerp(0f, gridHeight - 1, _relativeY));
        return nodes[_gridX, _gridY];
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, 1f, height));
        if (nodes != null) {
            Node playerNode = GetNodeFromWorldPos(player.transform.position);
            // Vector2Int[] _path = pathfinding.FindPath(player.transform.position, enemy.transform.position, 0f);
            foreach (Node _node in nodes) {
                Gizmos.color = edges[playerNode.gridPos.x, playerNode.gridPos.y, _node.gridPos.x, _node.gridPos.y] ? Color.red : Color.white;
                // Gizmos.color = Array.IndexOf(_path, _node.gridPos) > -1 ? Color.red : Color.white;
                Gizmos.DrawCube(_node.worldPos, new Vector3(realNodeWidth, 1f, realNodeHeight) * 0.9f);
            }
        }
    }
}
