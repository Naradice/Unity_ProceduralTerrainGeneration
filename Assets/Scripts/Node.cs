using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IHeapItem<Node>
{
    public bool walkable;
    public Vector3 gridPosition;
    public Node parent;

    public int gridX;
    public int gridY;

    // cost from current node to this node
    public float gCost;
    // cost from this node to target node
    public float hCost;
    int heapIndex;

    public Node(int x, int y, bool walkable, Vector3 gridPosition)
    {
        this.gridX = x;
        this.gridY = y;
        this.walkable = walkable;
        this.gridPosition = gridPosition;
    }

    public float FCost {
        get{
            return gCost + hCost;
        }
    }

    public int HeapIndex {
        get {
            return heapIndex;
        }
        set {
            heapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare){
        int compare = FCost.CompareTo(nodeToCompare.FCost);
        if(compare == 0){
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        // For path finding, we want the lowest FCost to be at the top of the heap
        return -compare;
    }
}
