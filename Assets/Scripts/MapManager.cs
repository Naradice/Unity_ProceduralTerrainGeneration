using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager: MonoBehaviour
{
    public int width = 100;
    public int length = 100;
    public int height = 100;
    public float noiseScale = 10;
    public float persistance = 0.5f;
    public float lacunarity = 2.0f;
    public int seed = 0;
    public AnimationCurve heightCurve;
    public Vector2 offset = new Vector2(0, 0);
    public bool autoUpdate = true;
    public int heightmapResolution = 129; // Height Map Resolution
    public int detailResolution = 1024; // Detail Map Resolution
    public int alphaMapResolution = 1024; // SplatMap Resolution
    public int baseMapResolution = 1024;

    // Texture Settings
    public TerrainType[] regions;

    // Variables for endless chunk loading
    public float maxViewDist = 100;
    public Transform viewer;
    public static Vector2 viewerPosition;
    int chunkSize = 100;
    int chunksVisibleInViewDist;
    int currentChunkCoordX = int.MinValue;
    int currentChunkCoordY = int.MaxValue;
    Dictionary<Vector2, MapUnit> mapUnitDictionary = new Dictionary<Vector2, MapUnit>();
    TerrainGenerator terrainGenerator;
    List<TreeData> treeDatas;
    Queue<MapUnit> pathQueue = new Queue<MapUnit>();


    void Start()
    {
        chunkSize = width - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
        terrainGenerator = GetComponent<TerrainGenerator>();
        terrainGenerator.Init(heightmapResolution, detailResolution, alphaMapResolution, baseMapResolution, width, height, length, noiseScale, persistance, lacunarity, seed, offset, heightCurve, regions);

        float previousHeight = 0f;
        treeDatas = new List<TreeData>();
        for(int i = 0; i < regions.Length; i++){
            regions[i].treeData.AntitudeLowerBound = previousHeight;
            regions[i].treeData.AntitudeUpperBound = regions[i].height;
            previousHeight = regions[i].height;
            treeDatas.Add(regions[i].treeData);
        }
    }

    void Update(){
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        int nextChunkCoordX = Mathf.FloorToInt(viewerPosition.x / chunkSize);
        int nextChunkCoordY = Mathf.FloorToInt(viewerPosition.y / chunkSize);

        if (currentChunkCoordX != nextChunkCoordX || currentChunkCoordY != nextChunkCoordY){
            currentChunkCoordX = nextChunkCoordX;
            currentChunkCoordY = nextChunkCoordY;
            UpdateVisibleChunks();
        }

        if(pathQueue.Count > 0){
            MapUnit mapUnit = pathQueue.Dequeue();
            mapUnit.CreatePathTexture(2.0f, regions[0].pathTexture);
        }
    }

    public MapUnit this[Vector2 coord]{
        get{
            if(mapUnitDictionary.ContainsKey(coord)){
                return mapUnitDictionary[coord];
            }else{
                return null;
            }
        }
        set{
            if(mapUnitDictionary.ContainsKey(coord)){
                mapUnitDictionary[coord] = value;
            }else{
                mapUnitDictionary.Add(coord, value);
            }
        }
    }

    public MapUnit this[int x, int y]{
        get{
            Vector2 coordinate = new Vector2(x, y);
            return this[coordinate];
        }
        set{
            Vector2 coordinate = new Vector2(x, y);
            this[coordinate] = value;
        }
    }

    public int Count{
        get{
            return mapUnitDictionary.Count;
        }
    }

    public bool Exists(Vector2 coord){
        return mapUnitDictionary.ContainsKey(coord);
    }

    public bool Exists(int x, int y){
        return mapUnitDictionary.ContainsKey(new Vector2(x, y));
    }

    public void Remove(int x, int y){
        mapUnitDictionary.Remove(new Vector2(x, y));
    }


    public void Clear(){
        mapUnitDictionary.Clear();
    }

    public Vector2[] GetCoords(){
        Vector2[] keys = new Vector2[mapUnitDictionary.Keys.Count];
        mapUnitDictionary.Keys.CopyTo(keys, 0);
        return keys;
    }

    public bool Activate(Vector2 coord){

        bool activated = false;
        if(mapUnitDictionary.ContainsKey(coord)){
            mapUnitDictionary[coord].Activate();
            activated = true;
        }
        return activated;
    }

    public bool Deactivate(Vector2 coord){

        bool deactivated = false;
        if(mapUnitDictionary.ContainsKey(coord)){
            mapUnitDictionary[coord].Deactivate();
            deactivated = true;
        }
        return deactivated;
    }

    public bool IsActive(Vector2 coord){
        bool isActive = false;
        if(mapUnitDictionary.ContainsKey(coord)){
            isActive = mapUnitDictionary[coord].IsActive();
        }
        return isActive;
    }

    private void UpdateVisibleChunks(){

        foreach(Vector2 coord in GetCoords()){
            if(this.IsActive(coord)){
                if(coord.x < currentChunkCoordX - chunksVisibleInViewDist || coord.x > currentChunkCoordX + chunksVisibleInViewDist || coord.y < currentChunkCoordY - chunksVisibleInViewDist || coord.y > currentChunkCoordY + chunksVisibleInViewDist){
                    this.Deactivate(coord);
                }
            }
        }

        for(int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++){
            for(int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++){
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if(this.Exists(viewedChunkCoord)){
                    this.Activate(viewedChunkCoord);
                }
                else{
                    int x = (int)viewedChunkCoord.x;
                    int y = (int)viewedChunkCoord.y;
                    terrainGenerator.CreateTerrain(x, y, OnTerrainDataCreated);
                }
            }
        }
    }

    public void OnTerrainDataCreated(TerrainData terrainData, Vector2 coord){
        Vector3 position = new Vector3(width * coord.x, 0,  length * coord.y);
        string name = $"Generated Terrain ({coord.x}, {coord.y})";
        MapUnit mapUnit = new MapUnit(terrainData, position, name);
        this[coord] = mapUnit;
        StartCoroutine(mapUnit.CreatePathPoints(OnPathPointCreated));
        mapUnit.RequestTreeInitialization(treeDatas);
    }

    public void OnPathPointCreated(MapUnit mapUnit){
        StartCoroutine(mapUnit.FindPath(OnPathFound));
    }

    public void OnPathFound(MapUnit mapUnit){
        pathQueue.Enqueue(mapUnit);
    }

    public void DrawMap(float[,] noiseMap = null){
        if(noiseMap == null)
            noiseMap = Noise.GenerateNoiseMap(heightmapResolution, heightmapResolution, noiseScale, persistance, lacunarity, seed, offset, heightCurve);
        NoiseDisplay noiseDisplay = FindObjectOfType<NoiseDisplay>();
        noiseDisplay.DrawNoiseMap(Noise.HeightMapToRenderMap(noiseMap));
        noiseDisplay.UpdateMapSize(width/10, length/10);
    }


    // void OnDrawGizmos()
    // {
    //     int numOfChunks = Mathf.RoundToInt(maxViewDist / width);
    //     int nextChunkCoordX = Mathf.FloorToInt(viewerPosition.x / width);
    //     int nextChunkCoordY = Mathf.FloorToInt(viewerPosition.y / width);
    //     List<float[,] > heightsArray = new List<float[,]>();
    //     for(int yOffset = -numOfChunks; yOffset <= numOfChunks; yOffset++){
    //         for(int xOffset = -numOfChunks; xOffset <= numOfChunks; xOffset++){
    //             int x = nextChunkCoordX + xOffset;
    //             int y = nextChunkCoordY + yOffset;
    //             Vector2 newOffset = new Vector2(offset.x + y*((heightmapResolution-1)/noiseScale), offset.y + x * ((heightmapResolution - 1)/noiseScale));
    //             float[,] heights = Noise.GenerateNoiseMap(heightmapResolution, heightmapResolution, noiseScale, persistance, lacunarity, seed, newOffset, heightCurve);
    //             heightsArray.Add(heights);
    //         }
    //     }
    //     Grid.DrawGizmos(0.5f, (float)width / 2, (float)length / 2, (float)width * numOfChunks * 2.5f, (float)length * numOfChunks * 2.5f, height, heightsArray);
    // }
}
