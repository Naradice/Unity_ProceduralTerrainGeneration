using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public enum DrawMode {Gizmos, LineRenderer};
    public DrawMode drawMode = DrawMode.LineRenderer;
    public float GirdUnitSize = 0.5f;

    public static void DrawGizmos(float gridUnitSize, float offsetX, float offsetY, float gridWorldSizeX, float gridWorldSizeY, float maxHeight, List<float[,]> heights)
    {
        Gizmos.DrawWireCube(new Vector3(offsetX, 0, offsetY), new Vector3(gridWorldSizeX, maxHeight, gridWorldSizeY));
        // if(heights != null){
        //     for(int i = 0; i < heights.Count; i++){
        //         //NodeMap grid = new NodeMap(heights[i]);
        //         float[,] grid = heights[i];
        //         for(int x = 0; x < grid.GetLength(0); x++){
        //             for(int y = 0; y < grid.GetLength(1); y++){
        //                 float height = grid[x, y];
        //                 Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(0, 1, grid[x, y]));
        //                 Gizmos.DrawCube(new Vector3(-gridWorldSizeX / 2 + x * gridUnitSize + gridUnitSize / 2, height * grid[x, y] / 2, -gridWorldSizeY / 2 + y * gridUnitSize + gridUnitSize / 2), Vector3.one * gridUnitSize);
        //             }
        //         }

        //     }
        // }
    }

    private class Node {

        public float height;
    }

    private class NodeMap {
        private Node[,] nodes;

        public NodeMap(float[,] heights){
            //initialize nodes
            
        }

        public int GetLength(int dimension){
            return nodes.GetLength(dimension);
        }

        public float this[int x, int y]{
            get{
                return nodes[x, y].height;
            }
        }
    }
}
