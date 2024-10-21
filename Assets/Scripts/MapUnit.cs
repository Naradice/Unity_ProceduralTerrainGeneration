using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

public class MapUnit
{
    Terrain terrain;
    Node[,] nodes;
    enum Progress {NotStarted, InProgress, Finished};
    Progress nodeProgress = Progress.NotStarted;

    enum Edge {Top, Bottom, Left, Right};
    Dictionary<Edge, Vector2> pathNodes = null;
    List<Node[]> pathNodesList = null;
    float gridUnitSize;

    public MapUnit(TerrainData terrainData, Vector3 position, string name, float gridUnitSize = 0.5f){
        terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
        terrain.transform.position = position;
        terrain.name = name;
        this.gridUnitSize = gridUnitSize;
        CreateNodes(gridUnitSize);
    }

    public void Activate(){
        terrain.gameObject.SetActive(true);
    }

    public void Deactivate(){
        terrain.gameObject.SetActive(false);
    }

    public bool IsActive(){
        return terrain.gameObject.activeSelf;
    }

    public float[,] GetRowHeights(){
        return terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
    }


    public void RequestTreeInitialization(List<TreeData> treeDatas){
        float[,] heights = GetRowHeights();
        TerrainData terrainData = terrain.terrainData;

        TreeManager treeManager = new TreeManager();
        treeManager.SpawnTrees(terrainData, treeDatas.ToArray(), heights);
    }

    public Vector2[] GetEdgePathToConnect(){
        if(pathNodes == null){
            Debug.LogError("Path nodes have not been created yet");
            return null;
        }
        Vector2[] edgePathNodes = new Vector2[4]{
            pathNodes[Edge.Top],
            pathNodes[Edge.Bottom],
            pathNodes[Edge.Left],
            pathNodes[Edge.Right]
        };
        return edgePathNodes;
    }

    public IEnumerator CreatePathPoints(Action<MapUnit> callback, MapUnit leftMapUnit=null, MapUnit rightMapUnit=null, MapUnit topMapUnit=null, MapUnit bottomMapUnit=null){
        while(nodeProgress != Progress.Finished){
            yield return null;
        }
        Debug.Log("Creating path points");
        Node leftPathNode = null;
        Node rightPathNode = null;
        Node topPathNode = null;
        Node bottomPathNode = null;

        if(leftMapUnit != null){
            Vector2[] leftPath = leftMapUnit.GetEdgePathToConnect();
            Vector2 leftRightCoord = leftPath[3];
            leftPathNode = nodes[(int)leftRightCoord.x, (int)leftRightCoord.y];

        }else{
            leftPathNode = SearchEdgePathRandomly(Edge.Left);
        }
        if(rightMapUnit != null){
            Vector2[] rightPath = rightMapUnit.GetEdgePathToConnect();
            Vector2 rightLeftCoord = rightPath[2];
            rightPathNode = nodes[(int)rightLeftCoord.x, (int)rightLeftCoord.y];
        }else{
            rightPathNode = SearchEdgePathRandomly(Edge.Right);
        }
        if(topMapUnit != null){
            Vector2[] topPath = topMapUnit.GetEdgePathToConnect();
            Vector2 topBottomCoord = topPath[1];
            topPathNode = nodes[(int)topBottomCoord.x, (int)topBottomCoord.y];
        }else{
            topPathNode = SearchEdgePathRandomly(Edge.Top);
        }
        if(bottomMapUnit != null){
            Vector2[] bottomPath = bottomMapUnit.GetEdgePathToConnect();
            Vector2 bottomTopCoord = bottomPath[0];
            bottomPathNode = nodes[(int)bottomTopCoord.x, (int)bottomTopCoord.y];
        }else{
            bottomPathNode = SearchEdgePathRandomly(Edge.Bottom);
        }

        pathNodes = new Dictionary<Edge, Vector2>();

        pathNodes.Add(Edge.Top, new Vector2(topPathNode.gridX, topPathNode.gridY));
        pathNodes.Add(Edge.Bottom, new Vector2(bottomPathNode.gridX, bottomPathNode.gridY));
        pathNodes.Add(Edge.Left, new Vector2(leftPathNode.gridX, leftPathNode.gridY));
        pathNodes.Add(Edge.Right, new Vector2(rightPathNode.gridX, rightPathNode.gridY));
        callback(this);
        yield break;
    }

    public IEnumerator FindPath(Action<MapUnit> callback) {
        if(pathNodesList == null)
            pathNodesList = new List<Node[]>();

        bool pathSuccess = false;
        // get random key from pathNodes
        System.Random random = new System.Random();
        List<Edge> keys = new List<Edge>(pathNodes.Keys);

        Node startNode = null;
        Node targetNode = null;

        while (keys.Count > 0)
        {
            int randomIndex = random.Next(0, keys.Count);
            Edge randomKey = keys[randomIndex];
            Vector2 value = pathNodes[randomKey];

            keys.RemoveAt(randomIndex);

            if(startNode == null){
                startNode = nodes[(int)value.x, (int)value.y];
            }else if(targetNode == null){
                targetNode = nodes[(int)value.x, (int)value.y];
            }
            if(startNode != null && targetNode != null){
                if(startNode.walkable && targetNode.walkable) {
                    Heap<Node> openSet = new Heap<Node>(nodes.GetLength(0) * nodes.GetLength(1));
                    HashSet<Node> closeSet = new HashSet<Node>();

                    openSet.Add(startNode);

                    while(openSet.Count > 0) {
                        //UnityEngine.Debug.Log("openSet.Count: " + openSet.Count);
                        Node currentNode = openSet.RemoveFirst();
                        closeSet.Add(currentNode);

                        bool isEnded = currentNode == targetNode;
                        if(isEnded) {
                            pathSuccess = true;
                            break;
                        }

                        foreach(Node neighbours in GetNeighbours(currentNode)) {
                            if(!neighbours.walkable || closeSet.Contains(neighbours)) {
                                continue;
                            }

                            float newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbours);// + neighbours.movementPenalty;
                            if(newMovementCostToNeighbour < neighbours.gCost || !openSet.Contains(neighbours)) {
                                neighbours.gCost = newMovementCostToNeighbour;
                                neighbours.hCost = GetDistance(neighbours, targetNode);
                                neighbours.parent = currentNode;

                                if(!openSet.Contains(neighbours)) {
                                    openSet.Add(neighbours);
                                }else{
                                    openSet.UpdateItem(neighbours);
                                }
                            }
                        }
                    }

                    if(pathSuccess){
                        pathNodesList.Add(RetracePath(startNode, targetNode));
                        Debug.Log("Found path");
                        startNode = null;
                        targetNode = null;
                        pathSuccess = false;
                    }
                    yield return null;
                }
            }
        }

        callback(this);
    }

    public void CreatePathTexture(float pathWidth, Texture2D texture){
        if(pathNodes == null){
            Debug.LogError("Path nodes have not been created yet");
            return;
        }

        TerrainLayer[] layers = new TerrainLayer[terrain.terrainData.terrainLayers.Length + 1];
        int index = 0;
        for(index = 0; index < terrain.terrainData.terrainLayers.Length; index++){
            layers[index] = terrain.terrainData.terrainLayers[index];
        }
        layers[index] = new TerrainLayer();
        layers[index].diffuseTexture = texture;
        //terrainLayer[i].tileOffset = region.tileOffset;
        layers[index].tileSize = new Vector2(10, 10);
        layers[index].diffuseTexture.Apply(true);
        terrain.terrainData.terrainLayers = layers;

        float width = terrain.terrainData.size.x;
        float alphaMapResolution = terrain.terrainData.alphamapResolution;
        float[,,] alphaMap = terrain.terrainData.GetAlphamaps(0, 0, (int)alphaMapResolution, (int)alphaMapResolution);

        float alphaMapUnitSize = width/alphaMapResolution;
        int numOfAlphamapUnits = (int)(gridUnitSize/alphaMapUnitSize);

        foreach(Node[] path in pathNodesList){
            for(int i = 0; i < path.Length - 1; i++){
                //caliculate resolution map position from node position
                int baseX = path[i].gridX * numOfAlphamapUnits;
                int baseY = path[i].gridY * numOfAlphamapUnits;

                int endX = baseX + (int)(pathWidth/alphaMapUnitSize);
                if(endX > alphaMapResolution){
                    endX = (int)alphaMapResolution;
                }
                int endY = baseY + (int)(pathWidth/alphaMapUnitSize);
                if(endY > alphaMapResolution){
                    endY = (int)alphaMapResolution;
                }
                //make alphamap to 1 for the texture
                for(int x = baseX; x < endX; x++){
                    for(int y = baseY; y < endY; y++){
                        alphaMap[x, y, index] = 1;
                        for (int l = 0; l < index; l++)
                        {
                            alphaMap[x, y, l] = 0;
                        }
                    }
                }
            }
        }
        terrain.terrainData.SetAlphamaps(0, 0, alphaMap);
        terrain.Flush();

    }

    Node[] RetracePath(Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        return path.ToArray();
    }

    private float GetDistance(Node nodeA, Node nodeB){
        float DiagonalCost = 1.4f;//14 = 10 * sqrt(2)
        float dstX = (float)Mathf.Abs(nodeA.gridX - nodeB.gridX);
        float dstY = (float)Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if(dstX > dstY) {
            return DiagonalCost * dstY +  (dstX - dstY);
        }

        return DiagonalCost * dstX +  (dstY - dstX);
    }

    private Node[] GetNeighbours(Node node){
        List<Node> neighbours = new List<Node>();

        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++){
                if(x == 0 && y == 0) {
                    continue;
                }

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if(checkX >= 0 && checkX < nodes.GetLength(0) && checkY >= 0 && checkY < nodes.GetLength(1)) {
                    neighbours.Add(nodes[checkX, checkY]);
                }
            }
        }

        return neighbours.ToArray();
    }

    private Node SearchEdgePathRandomly(Edge edge){
        int numOfNodes = nodes.GetLength(0);
        Func<Vector2> randomCoord;
        switch(edge){
            case Edge.Top:
                randomCoord = () => new Vector2(UnityEngine.Random.Range(0, numOfNodes), numOfNodes - 1);
                break;
            case Edge.Bottom:
                randomCoord = () => new Vector2(UnityEngine.Random.Range(0, numOfNodes), 0);
                break;
            case Edge.Left:
                randomCoord = () => new Vector2(0, UnityEngine.Random.Range(0, numOfNodes));
                break;
            case Edge.Right:
                randomCoord = () => new Vector2(numOfNodes - 1, UnityEngine.Random.Range(0, numOfNodes));
                break;
            default:
                Debug.Log("Invalid edge specified");
                randomCoord = () => new Vector2(UnityEngine.Random.Range(0, numOfNodes), UnityEngine.Random.Range(0, numOfNodes));
                break;
        }

        int patients = 10;
        while(patients > 0){
            Vector2 coord = randomCoord();
            Node node = nodes[(int)coord.x, (int)coord.y];
            if(node.walkable){
                return node;
            }
            patients--;
        }
        Debug.Log("No walkable nodes found");
        return null;
    }

    /// <summary>
    /// Reduce the number of nodes from alhpaMapResolution to gridUnitSize
    /// </summary>
    /// <param name="gridUnitSize"></param>
    /// <returns></returns>
    private async void CreateNodes(float gridUnitSize){
        // Assume heightMapResolution < alphamapResolution
        float alphaMapResolution = (float)terrain.terrainData.alphamapResolution;
        float alphamapResolutionUnitSize = (float)terrain.terrainData.heightmapResolution / alphaMapResolution;
        float [,] heights = GetRowHeights();
        float width = terrain.terrainData.size.x;
        // comment out length for now as we assume square terrain
        //float length = terrain.terrainData.size.z;

        await Task.Run(() => {
            nodeProgress = Progress.InProgress;

            float alphaMapUnitSize = width/alphaMapResolution;
            float numOfAlphamapUnits = gridUnitSize/alphaMapUnitSize;

            int numOfNodes = (int)(alphaMapResolution/numOfAlphamapUnits);

            nodes = new Node[numOfNodes, numOfNodes];

            for(int i = 0; i < numOfNodes; i++){
                for(int j = 0; j < numOfNodes; j++){
                    float x = i * numOfAlphamapUnits * alphamapResolutionUnitSize;
                    float y = j * numOfAlphamapUnits * alphamapResolutionUnitSize;
                    float height = heights[(int)x, (int)y];
                    nodes[i, j] = new Node(i, j, true, new Vector3(x, height, y));
                }
            }
            nodeProgress = Progress.Finished;
        });
    }
}
