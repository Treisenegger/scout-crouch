using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {
    public Vector3 worldPos;
    public Vector2Int gridPos;

    public Node(Vector3 _worldPos, Vector2Int _gridPos) {
        worldPos = _worldPos;
        gridPos = _gridPos;
    }
}
