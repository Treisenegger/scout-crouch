using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementGrid : MonoBehaviour {
    // only for testing, have to remove later
    // [SerializeField] PlayerMovement player;

    [SerializeField] float width = 1f;
    [SerializeField] float height = 1f;
    [SerializeField] float nodeWidth = 1f;
    [SerializeField] float walkingHeightDetection = 0.1f; // might be better to implement this variable universally
    [SerializeField] LayerMask obstacleMask;

    int gridWidth, gridHeight;
    float realNodeWidth, realNodeHeight;
    Node[,] nodes;
    bool[,,,] edges;

    private void Start() {
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

        foreach (Node node1 in nodes) {
            foreach (Node node2 in nodes) {
                if (node2.gridPos.x >= node1.gridPos.x && node2.gridPos.y >= node1.gridPos.y) {
                    continue;
                }

                bool _visible = !Physics.Raycast(node1.worldPos + Vector3.up * walkingHeightDetection, (node2.worldPos - node1.worldPos).normalized, Vector3.Distance(node1.worldPos, node2.worldPos), obstacleMask);
                if (_visible) {
                    edges[node1.gridPos.x, node1.gridPos.y, node2.gridPos.x, node2.gridPos.y] = true;
                    edges[node2.gridPos.x, node2.gridPos.y, node1.gridPos.x, node1.gridPos.y] = true;
                }
                else {
                    edges[node1.gridPos.x, node1.gridPos.y, node2.gridPos.x, node2.gridPos.y] = false;
                    edges[node2.gridPos.x, node2.gridPos.y, node1.gridPos.x, node1.gridPos.y] = false;
                }
            }
        }
    }

    // Returns node reference from world position
    private Node GetNodeFromWorldPos(Vector3 _worldPos) {
        float _relativeX = Mathf.InverseLerp(transform.position.x - width / 2 + realNodeWidth / 2, transform.position.x + width / 2 - realNodeWidth / 2, _worldPos.x);
        float _relativeY = Mathf.InverseLerp(transform.position.z - width / 2 + realNodeHeight / 2, transform.position.z + width / 2 - realNodeHeight / 2, _worldPos.z);
        int _gridX = Mathf.RoundToInt(Mathf.Lerp(0f, gridWidth - 1, _relativeX));
        int _gridY = Mathf.RoundToInt(Mathf.Lerp(0f, gridHeight - 1, _relativeY));
        return nodes[_gridX, _gridY];
    }

    // private void OnDrawGizmos() {
    //     Gizmos.color = Color.white;
    //     Gizmos.DrawWireCube(transform.position, new Vector3(width, 1f, height));
    //     if (nodes != null) {
    //         Node playerNode = GetNodeFromWorldPos(player.transform.position);
    //         foreach (Node node in nodes) {
    //             Gizmos.color = edges[playerNode.gridPos.x, playerNode.gridPos.y, node.gridPos.x, node.gridPos.y] ? Color.red : Color.white;
    //             Gizmos.DrawCube(node.worldPos, new Vector3(realNodeWidth, 1f, realNodeHeight) * 0.9f);
    //         }
    //     }
    // }
}
