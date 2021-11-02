using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// TODO: - Implement node adjacency just for contiguous nodes
public class MovementGrid : MonoBehaviour {

    // only for testing, have to remove later
    [SerializeField] PlayerMovement player;
    [SerializeField] EnemyController enemy;
    [SerializeField] Pathfinding pathfinding;
    [SerializeField] bool drawGizmos = false;

    [Header("Grid Geometry Parameters")]
    [SerializeField] float width = 1f;
    [SerializeField] float height = 1f;
    [SerializeField] float nodeWidth = 1f;

    [Header("Line of Sight Detection Parameters")]
    [SerializeField] FloatVariable crouchHeight;
    [SerializeField] FloatVariable uprightHeight;
    [Range(0, 1)]
    [SerializeField] float lineOfSightPrecision = 0.5f;
    [SerializeField] LayerMask obstacleMask;
    
    [HideInInspector] public Node[,] nodes;
    [HideInInspector] public bool[,,,] crouchEdges;
    [HideInInspector] public bool[,,,] uprightEdges;
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

    // Determine node adjacency based on line of sight at both crouching and upright heights (might need to replace with simple adjacency)
    private void InitializeEdgeInfo() {
        crouchEdges = new bool[gridWidth, gridHeight, gridWidth, gridHeight];
        uprightEdges = new bool[gridWidth, gridHeight, gridWidth, gridHeight];

        foreach (Node _node1 in nodes) {
            foreach (Node _node2 in nodes) {
                if (_node2.gridPos.x < _node1.gridPos.x || (_node2.gridPos.x == _node1.gridPos.x && _node2.gridPos.y <= _node1.gridPos.y)) {
                    continue;
                }

                bool _crouchVisible = CheckVisibility(_node1, _node2, crouchHeight.Value, lineOfSightPrecision);

                if (_crouchVisible) {
                    crouchEdges[_node1.gridPos.x, _node1.gridPos.y, _node2.gridPos.x, _node2.gridPos.y] = true;
                    crouchEdges[_node2.gridPos.x, _node2.gridPos.y, _node1.gridPos.x, _node1.gridPos.y] = true;
                    uprightEdges[_node1.gridPos.x, _node1.gridPos.y, _node2.gridPos.x, _node2.gridPos.y] = true;
                    uprightEdges[_node2.gridPos.x, _node2.gridPos.y, _node1.gridPos.x, _node1.gridPos.y] = true;
                }
                else {
                    crouchEdges[_node1.gridPos.x, _node1.gridPos.y, _node2.gridPos.x, _node2.gridPos.y] = false;
                    crouchEdges[_node2.gridPos.x, _node2.gridPos.y, _node1.gridPos.x, _node1.gridPos.y] = false;
                    bool _uprightVisible = CheckVisibility(_node1, _node2, uprightHeight.Value, lineOfSightPrecision);

                    if (_uprightVisible) {
                        uprightEdges[_node1.gridPos.x, _node1.gridPos.y, _node2.gridPos.x, _node2.gridPos.y] = true;
                        uprightEdges[_node2.gridPos.x, _node2.gridPos.y, _node1.gridPos.x, _node1.gridPos.y] = true;
                    }
                    else {
                        uprightEdges[_node1.gridPos.x, _node1.gridPos.y, _node2.gridPos.x, _node2.gridPos.y] = false;
                        uprightEdges[_node2.gridPos.x, _node2.gridPos.y, _node1.gridPos.x, _node1.gridPos.y] = false;
                    }
                }
            }
        }
    }

    // Returns true if visibility from node 1 to node 2 (or viceversa) is not obstructed by an obstacle at specified height
    private bool CheckVisibility(Node _node1, Node _node2, float _visibilityHeight, float _losPrecision) {
        Vector3 _originPos = Math2D.V3AtHeight(_node1.worldPos, _visibilityHeight);
        Vector3 _targetDir = Math2D.V3ToV3Dir(_node1.worldPos, _node2.worldPos);
        float _nodeDist = Math2D.V3ToV3Dist(_node1.worldPos, _node2.worldPos);

        foreach (Vector3 _displacementVector in new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left }) {
            if (Physics.Raycast(_originPos + _displacementVector * _losPrecision, _targetDir, _nodeDist, obstacleMask)) {
                return false;
            }
            if (Physics.Raycast(_originPos + _displacementVector * _losPrecision + _targetDir * _nodeDist, -_targetDir, _nodeDist, obstacleMask)) {
                return false;
            }
        }

        return true;
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
        if (!drawGizmos) {
            return;
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, 1f, height));
        if (nodes != null) {
            Node _playerNode = GetNodeFromWorldPos(player.transform.position);
            Node _enemyNode = GetNodeFromWorldPos(enemy.transform.position);
            // Vector2Int[] _path = pathfinding.FindPath(player.transform.position, enemy.transform.position, 0f);
            foreach (Node _node in nodes) {
                Gizmos.color = crouchEdges[_enemyNode.gridPos.x, _enemyNode.gridPos.y, _node.gridPos.x, _node.gridPos.y] ? Color.red : Color.white;
                // Gizmos.color = uprightEdges[_enemyNode.gridPos.x, _enemyNode.gridPos.y, _node.gridPos.x, _node.gridPos.y] ? Color.red : Color.white;
                // Gizmos.color = Array.IndexOf(_path, _node.gridPos) > -1 ? Color.red : Color.white;
                // Gizmos.DrawCube(_node.worldPos, new Vector3(realNodeWidth, 1f, realNodeHeight) * 0.9f);
                Gizmos.DrawSphere(_node.worldPos, lineOfSightPrecision);
            }
        }
    }
}
