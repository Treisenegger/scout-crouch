using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node> {
    
    public Vector3 worldPos;
    public Vector2Int gridPos;
    public float gCost;
    public float hCost;
    public Node parent;

    public int HeapIndex { get; set; }

    public Node(Vector3 _worldPos, Vector2Int _gridPos) {
        worldPos = _worldPos;
        gridPos = _gridPos;
    }

    // Calculate fcost of node based on gcost and hcost
    public float FCost() {
        return gCost + hCost;
    }

    // Returns 1 when priority is higher than other node, -1 when it is higher and 0 when it is the same
    public int CompareTo(Node _otherNode) {
        int _compare = FCost().CompareTo(_otherNode.FCost());
        if (_compare == 0) {
            _compare = hCost.CompareTo(_otherNode.hCost);
        }
        return -_compare;
    }

    public void UpdateParameters(Node _parent, float _gCost, float _hCost) {
        parent = _parent;
        gCost = _gCost;
        hCost = _hCost;
    }
}
