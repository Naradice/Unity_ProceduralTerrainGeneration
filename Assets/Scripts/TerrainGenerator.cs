using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using System.Threading;

public class TerrainGenerator: MonoBehaviour
{
    int heightmapResolution;
    int detailResolution;
    int alphaMapResolution;
    int baseMapResolution;
    float width;
    float height;
    float length;
    float noiseScale;
    float persistance;
    float lacunarity;
    int seed;
    Vector2 offset;
    AnimationCurve heightCurve;
    TerrainType[] regions;
    TerrainLayer[] terrainLayer;
    Queue<MapThreadInfo> mapDataThreadInfoQueue = new Queue<MapThreadInfo>();


    public void Init(int heightmapResolution, int detailResolution, int alphaMapResolution, int baseMapResolution, float width, float height, float length, float noiseScale, float persistance, float lacunarity, int seed, Vector2 offset, AnimationCurve heightCurve, TerrainType[] regions)
    {
        this.heightmapResolution = heightmapResolution;
        this.detailResolution = detailResolution;
        this.alphaMapResolution = alphaMapResolution;
        this.baseMapResolution = baseMapResolution;
        this.width = width;
        this.height = height;
        this.length = length;
        this.noiseScale = noiseScale;
        this.persistance = persistance;
        this.lacunarity = lacunarity;
        this.seed = seed;
        this.offset = offset;
        this.heightCurve = heightCurve;
        this.regions = regions;

        terrainLayer = new TerrainLayer[regions.Length];

        for(int i = 0; i < regions.Length; i++)
        {
            TerrainType region = regions[i];
            terrainLayer[i] = new TerrainLayer();
            terrainLayer[i].diffuseTexture = region.texture;
            //terrainLayer[i].tileOffset = region.tileOffset;
            if (region.normalMapTexture != null)
                terrainLayer[i].normalMapTexture = region.normalMapTexture;
            terrainLayer[i].tileSize = region.tileSize;
            terrainLayer[i].diffuseTexture.Apply(true);
        }
    }

    void Update(){
        if (mapDataThreadInfoQueue.Count > 0){
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++){
                MapThreadInfo threadInfo = mapDataThreadInfoQueue.Dequeue();
                TerrainData terrainData = new TerrainData() {
                    heightmapResolution = heightmapResolution,
                    size = new Vector3(width, height, length),
                    alphamapResolution = alphaMapResolution,
                    baseMapResolution = baseMapResolution,
                    terrainLayers = terrainLayer
                };
                terrainData.SetDetailResolution(detailResolution, 16);
                terrainData.SetHeights(0, 0, threadInfo.heights);
                terrainData.SetAlphamaps(0, 0, threadInfo.alphamap);
                threadInfo.callback(terrainData, threadInfo.coords);
            }
        }
    }

    public void CreateTerrain(int x, int y, System.Action<TerrainData, Vector2> parentCallback){
        ThreadStart threadStart = delegate{
            Vector2 newOffset = new Vector2(offset.x + y*((heightmapResolution-1)/noiseScale), offset.y + x * ((heightmapResolution - 1)/noiseScale));
            float[,] heights = Noise.GenerateNoiseMap(heightmapResolution, heightmapResolution, noiseScale, persistance, lacunarity, seed, newOffset, heightCurve);
            float[,,] alphaMap = CreateAlphaMap(heights);
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo(parentCallback, heights, alphaMap, x, y));
        };
        threadStart.Invoke();
    }

    private void CreatePlaneWidthHeigtMap(int x, int y, float[,] heights){
        // Create a plane object using CreatePrimitive method
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position = new Vector3(50 + width * x, 0, 50 + length * y);
        plane.transform.rotation = Quaternion.Euler(0, 0, 0);
        plane.transform.localScale = new Vector3(width, 1, length);
        plane.name = $"Generated Plane ({x}, {y})";

        // Get a reference to the Renderer component of the plane
        Renderer planeRenderer = plane.GetComponent<Renderer>();
        Material planeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Texture2D texture = new Texture2D(heightmapResolution, heightmapResolution);
        Color[] colorMap = new Color[heightmapResolution * heightmapResolution];

        float[,] noiseMap = Noise.HeightMapToRenderMap(heights);
        for (int y_ = 0; y_ < heightmapResolution; y_++){
            for(int x_ = 0; x_ < heightmapResolution; x_++){
                colorMap[y_ * heightmapResolution + x_] = Color.Lerp(Color.black, Color.white, noiseMap[x_, y_]);
            }
        }
        texture.SetPixels(colorMap);
        texture.Apply();
        
        planeMaterial.mainTexture = texture;
        planeRenderer.sharedMaterial = planeMaterial;
        planeRenderer.transform.localScale = new Vector3(heightmapResolution, 1, heightmapResolution);
        plane.transform.localScale = new Vector3(width/10, 1, length/10);
    }

    private float[,,] CreateAlphaMap(float[,] heights){
         // Apply the textures to the terrain based on height
        float[,,] newAlphamaps = new float[alphaMapResolution, alphaMapResolution, regions.Length];

        float conversionFuctor = (float)heightmapResolution/(float)alphaMapResolution;
        for (int x = 0; x < alphaMapResolution; x++)
        {
            for (int z = 0; z < alphaMapResolution; z++)
            {
                int heightMapX = Mathf.FloorToInt(conversionFuctor * x);
                int heightMapY = Mathf.FloorToInt(conversionFuctor * z);
                float normalizedHeight = heights[heightMapX, heightMapY];

                // Set the blend strength for each texture based on the height threshold
                for (int i = 0; i < regions.Length; i++)
                {
                    float blendThreshold = regions[i].height;
                    if (normalizedHeight <= blendThreshold){
                        if(i==0)
                            newAlphamaps[x, z, i] = 1f;
                        else{
                            float prevBlendThreshold = regions[i-1].height;
                            float blendStrength = Mathf.InverseLerp(prevBlendThreshold, blendThreshold, normalizedHeight);
                            newAlphamaps[x, z, i] = blendStrength;
                            newAlphamaps[x, z, i-1] = 1 - blendStrength;
                        }
                        break;
                    }
                }
            }
        }

        return newAlphamaps;
    }

    struct MapThreadInfo {
		public readonly Action<TerrainData, Vector2> callback;
		public readonly float[,] heights;
        public readonly float[,,] alphamap;
        public readonly Vector2 coords;

		public MapThreadInfo (Action<TerrainData, Vector2> callback, float[,] heights, float[,,] alphamap, int x, int y)
		{
			this.callback = callback;
			this.heights = heights;
            this.alphamap = alphamap;
            this.coords = new Vector2(x, y);
		}
		
	}
}

[System.Serializable]
public class TerrainType {
    public string name;
    public float height;
    public Texture2D texture;
    public Texture2D normalMapTexture;
    public Texture2D pathTexture;
    public Vector2 tileSize;

    public TreeData treeData;
}